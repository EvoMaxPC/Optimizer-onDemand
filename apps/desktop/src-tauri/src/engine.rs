use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::io::{BufRead, BufReader, Write};
use std::path::PathBuf;
use std::process::{Child, ChildStdin, ChildStdout, Command, Stdio};
use std::sync::atomic::{AtomicU64, Ordering};
use std::sync::{Mutex, OnceLock};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct EngineHelloResult {
    pub protocol_version: u32,
    pub engine_name: String,
    pub read_only: bool,
    pub methods: Vec<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct EngineVersionResult {
    pub engine_version: String,
    pub runtime: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SystemOsInfoResult {
    pub os_description: String,
    pub os_architecture: String,
    pub process_architecture: String,
    pub machine_name: String,
}

struct PersistentHost {
    child: Child,
    stdin: ChildStdin,
    stdout: BufReader<ChildStdout>,
}

impl PersistentHost {
    fn spawn() -> Result<Self, String> {
        let mut cmd = if let Some(csproj) = find_host_csproj() {
            let mut c = Command::new("dotnet");
            c.arg("run")
                .arg("--project")
                .arg(csproj)
                .arg("--no-launch-profile");
            c
        } else {
            let exe_dir = std::env::current_exe()
                .map_err(|e| format!("Failed to resolve current_exe: {e}"))?
                .parent()
                .map(PathBuf::from)
                .ok_or_else(|| "Failed to resolve executable directory".to_string())?;
            let host_name = if cfg!(windows) {
                "Evo.Engine.Host.exe"
            } else {
                "Evo.Engine.Host"
            };
            Command::new(exe_dir.join(host_name))
        };

        let mut child = cmd
            .stdin(Stdio::piped())
            .stdout(Stdio::piped())
            .stderr(Stdio::piped())
            .spawn()
            .map_err(|e| format!("Failed to spawn Evo.Engine.Host: {e}"))?;

        let stdin = child
            .stdin
            .take()
            .ok_or_else(|| "Host stdin was not captured".to_string())?;
        let stdout = child
            .stdout
            .take()
            .ok_or_else(|| "Host stdout was not captured".to_string())?;

        if let Some(stderr) = child.stderr.take() {
            std::thread::spawn(move || {
                let reader = BufReader::new(stderr);
                for _ in reader.lines() {
                    // Drain stderr continuously to prevent pipe buffer deadlock
                }
            });
        }

        Ok(Self {
            child,
            stdin,
            stdout: BufReader::new(stdout),
        })
    }

    fn is_alive(&mut self) -> bool {
        matches!(self.child.try_wait(), Ok(None))
    }

    fn call(&mut self, id: u64, method: &str, params: Option<Value>) -> Result<Value, String> {
        let mut req = json!({
            "jsonrpc": "2.0",
            "id": id,
            "method": method
        });
        if let Some(p) = params {
            req["params"] = p;
        }

        let line = serde_json::to_string(&req)
            .map_err(|e| format!("Failed to serialize JSON-RPC request: {e}"))?;

        writeln!(self.stdin, "{line}").map_err(|e| format!("Failed to write to host stdin: {e}"))?;
        self.stdin
            .flush()
            .map_err(|e| format!("Failed to flush host stdin: {e}"))?;

        let mut response_line = String::new();
        let bytes = self
            .stdout
            .read_line(&mut response_line)
            .map_err(|e| format!("Failed to read host stdout: {e}"))?;

        if bytes == 0 {
            return Err("Evo.Engine.Host closed stdout without a response".to_string());
        }

        let resp: Value = serde_json::from_str(response_line.trim())
            .map_err(|e| format!("Invalid JSON-RPC response from host: {e}"))?;

        if let Some(err) = resp.get("error") {
            return Err(format!("Engine returned JSON-RPC error: {err}"));
        }

        resp.get("result")
            .cloned()
            .ok_or_else(|| "Missing 'result' in JSON-RPC response".to_string())
    }
}

impl Drop for PersistentHost {
    fn drop(&mut self) {
        let _ = self.child.kill();
        let _ = self.child.wait();
    }
}

static HOST_INSTANCE: OnceLock<Mutex<Option<PersistentHost>>> = OnceLock::new();
static NEXT_REQUEST_ID: AtomicU64 = AtomicU64::new(1);

fn find_host_csproj() -> Option<PathBuf> {
    let mut dir = std::env::current_dir().ok()?;
    for _ in 0..8 {
        let candidate = dir.join("engine/src/Evo.Engine.Host/Evo.Engine.Host.csproj");
        if candidate.is_file() {
            return Some(candidate);
        }
        if !dir.pop() {
            break;
        }
    }
    None
}

fn call_persistent_host(method: &str, params: Option<Value>) -> Result<Value, String> {
    let mutex = HOST_INSTANCE.get_or_init(|| Mutex::new(None));
    let mut guard = mutex
        .lock()
        .map_err(|_| "Engine host mutex poisoned".to_string())?;

    let needs_spawn = match guard.as_mut() {
        Some(host) => !host.is_alive(),
        None => true,
    };

    if needs_spawn {
        *guard = Some(PersistentHost::spawn()?);
    }

    let id = NEXT_REQUEST_ID.fetch_add(1, Ordering::Relaxed);
    let host = guard.as_mut().ok_or_else(|| "Host unavailable".to_string())?;
    match host.call(id, method, params.clone()) {
        Ok(val) => Ok(val),
        Err(first_err) => {
            // Attempt one automatic restart if the host crashed mid-call
            if !host.is_alive() {
                *guard = Some(PersistentHost::spawn()?);
                let retry_id = NEXT_REQUEST_ID.fetch_add(1, Ordering::Relaxed);
                if let Some(restarted) = guard.as_mut() {
                    return restarted.call(retry_id, method, params);
                }
            }
            Err(first_err)
        }
    }
}

pub fn shutdown_host() {
    if let Some(mutex) = HOST_INSTANCE.get() {
        if let Ok(mut guard) = mutex.lock() {
            *guard = None;
        }
    }
}

#[tauri::command]
pub fn engine_hello(client_name: String, protocol_version: u32) -> Result<EngineHelloResult, String> {
    let result = call_persistent_host(
        "engine.hello",
        Some(json!({
            "clientName": client_name,
            "protocolVersion": protocol_version
        })),
    )?;
    serde_json::from_value(result).map_err(|e| format!("Invalid engine.hello result: {e}"))
}

#[tauri::command]
pub fn engine_version() -> Result<EngineVersionResult, String> {
    let result = call_persistent_host("engine.version", None)?;
    serde_json::from_value(result).map_err(|e| format!("Invalid engine.version result: {e}"))
}

#[tauri::command]
pub fn system_os_info() -> Result<SystemOsInfoResult, String> {
    let result = call_persistent_host("system.osInfo", None)?;
    serde_json::from_value(result).map_err(|e| format!("Invalid system.osInfo result: {e}"))
}

#[tauri::command]
pub fn catalog_list() -> Result<Value, String> {
    call_persistent_host("catalog.list", None)
}

#[tauri::command]
pub fn action_apply(action_id: String) -> Result<Value, String> {
    call_persistent_host("action.apply", Some(json!({ "actionId": action_id })))
}

#[tauri::command]
pub fn action_rollback(action_id: String) -> Result<Value, String> {
    call_persistent_host("action.rollback", Some(json!({ "actionId": action_id })))
}

#[tauri::command]
pub fn snapshot_list() -> Result<Value, String> {
    call_persistent_host("snapshot.list", None)
}

#[tauri::command]
pub fn diagnostics_collect() -> Result<Value, String> {
    call_persistent_host("diagnostics.collect", None)
}

#[tauri::command]
pub fn plan_generate(profile: String) -> Result<Value, String> {
    call_persistent_host("plan.generate", Some(json!({ "profile": profile })))
}

#[tauri::command]
pub fn cleanup_scan() -> Result<Value, String> {
    call_persistent_host("cleanup.scan", None)
}

#[tauri::command]
pub fn cleanup_quarantine(paths: Vec<String>) -> Result<Value, String> {
    call_persistent_host("cleanup.quarantine", Some(json!({ "paths": paths })))
}

#[tauri::command]
pub fn cleanup_restore(batch_id: String) -> Result<Value, String> {
    call_persistent_host("cleanup.restore", Some(json!({ "batchId": batch_id })))
}

#[tauri::command]
pub fn lab_measure(samples: Option<u32>) -> Result<Value, String> {
    call_persistent_host("lab.measure", Some(json!({ "samples": samples.unwrap_or(7) })))
}

#[tauri::command]
pub fn clean_sources() -> Result<Value, String> {
    call_persistent_host("clean.sources", None)
}

#[tauri::command]
pub fn clean_analyze() -> Result<Value, String> {
    call_persistent_host("clean.analyze", None)
}

#[tauri::command]
pub fn ledger_list() -> Result<Value, String> {
    call_persistent_host("ledger.list", None)
}

#[tauri::command]
pub fn clean_duplicates() -> Result<Value, String> {
    call_persistent_host("clean.duplicates", None)
}

#[tauri::command]
pub fn storage_analytics() -> Result<Value, String> {
    call_persistent_host("storage.analytics", None)
}

#[tauri::command]
pub fn plan_execute(profile: String) -> Result<Value, String> {
    call_persistent_host("plan.execute", Some(json!({ "profile": profile })))
}

#[tauri::command]
pub fn clean_prioritize() -> Result<Value, String> {
    call_persistent_host("clean.prioritize", None)
}

#[tauri::command]
pub fn discovery_start() -> Result<Value, String> {
    call_persistent_host("discovery.start", None)
}

#[tauri::command]
pub fn discovery_status() -> Result<Value, String> {
    call_persistent_host("discovery.status", None)
}

#[tauri::command]
pub fn discovery_cancel() -> Result<Value, String> {
    call_persistent_host("discovery.cancel", None)
}

#[tauri::command]
pub fn discovery_results() -> Result<Value, String> {
    call_persistent_host("discovery.results", None)
}
