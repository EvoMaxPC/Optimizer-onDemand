use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use std::io::{BufRead, BufReader, Write};
use std::path::PathBuf;
use std::process::{Command, Stdio};

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

fn spawn_and_call(method: &str, params: Option<Value>) -> Result<Value, String> {
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

    let mut req = json!({
        "jsonrpc": "2.0",
        "id": 1,
        "method": method
    });
    if let Some(p) = params {
        req["params"] = p;
    }

    let line = serde_json::to_string(&req)
        .map_err(|e| format!("Failed to serialize JSON-RPC request: {e}"))?;

    if let Some(mut stdin) = child.stdin.take() {
        writeln!(stdin, "{line}").map_err(|e| format!("Failed to write to host stdin: {e}"))?;
        stdin
            .flush()
            .map_err(|e| format!("Failed to flush host stdin: {e}"))?;
    }

    let stdout = child
        .stdout
        .take()
        .ok_or_else(|| "Host stdout was not captured".to_string())?;
    let mut reader = BufReader::new(stdout);
    let mut response_line = String::new();
    let bytes = reader
        .read_line(&mut response_line)
        .map_err(|e| format!("Failed to read host stdout: {e}"))?;

    let _ = child.wait();

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

#[tauri::command]
pub fn engine_hello(client_name: String, protocol_version: u32) -> Result<EngineHelloResult, String> {
    let result = spawn_and_call(
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
    let result = spawn_and_call("engine.version", None)?;
    serde_json::from_value(result).map_err(|e| format!("Invalid engine.version result: {e}"))
}

#[tauri::command]
pub fn system_os_info() -> Result<SystemOsInfoResult, String> {
    let result = spawn_and_call("system.osInfo", None)?;
    serde_json::from_value(result).map_err(|e| format!("Invalid system.osInfo result: {e}"))
}

#[tauri::command]
pub fn catalog_list() -> Result<Value, String> {
    spawn_and_call("catalog.list", None)
}

#[tauri::command]
pub fn action_apply(action_id: String) -> Result<Value, String> {
    spawn_and_call("action.apply", Some(json!({ "actionId": action_id })))
}

#[tauri::command]
pub fn action_rollback(action_id: String) -> Result<Value, String> {
    spawn_and_call("action.rollback", Some(json!({ "actionId": action_id })))
}

#[tauri::command]
pub fn snapshot_list() -> Result<Value, String> {
    spawn_and_call("snapshot.list", None)
}

#[tauri::command]
pub fn diagnostics_collect() -> Result<Value, String> {
    spawn_and_call("diagnostics.collect", None)
}

#[tauri::command]
pub fn plan_generate(profile: String) -> Result<Value, String> {
    spawn_and_call("plan.generate", Some(json!({ "profile": profile })))
}

#[tauri::command]
pub fn cleanup_scan() -> Result<Value, String> {
    spawn_and_call("cleanup.scan", None)
}

#[tauri::command]
pub fn cleanup_quarantine(paths: Vec<String>) -> Result<Value, String> {
    spawn_and_call("cleanup.quarantine", Some(json!({ "paths": paths })))
}

#[tauri::command]
pub fn cleanup_restore(batch_id: String) -> Result<Value, String> {
    spawn_and_call("cleanup.restore", Some(json!({ "batchId": batch_id })))
}

#[tauri::command]
pub fn lab_measure(samples: Option<u32>) -> Result<Value, String> {
    spawn_and_call("lab.measure", Some(json!({ "samples": samples.unwrap_or(7) })))
}

#[tauri::command]
pub fn clean_sources() -> Result<Value, String> {
    spawn_and_call("clean.sources", None)
}

#[tauri::command]
pub fn clean_analyze() -> Result<Value, String> {
    spawn_and_call("clean.analyze", None)
}

#[tauri::command]
pub fn ledger_list() -> Result<Value, String> {
    spawn_and_call("ledger.list", None)
}

#[tauri::command]
pub fn clean_duplicates() -> Result<Value, String> {
    spawn_and_call("clean.duplicates", None)
}

#[tauri::command]
pub fn storage_analytics() -> Result<Value, String> {
    spawn_and_call("storage.analytics", None)
}

#[tauri::command]
pub fn plan_execute(profile: String) -> Result<Value, String> {
    spawn_and_call("plan.execute", Some(json!({ "profile": profile })))
}
