export type EngineConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'error';

export const engineStatus = 'disconnected' as const;

export interface EngineHelloResult {
  protocolVersion: number;
  engineName: string;
  readOnly: boolean;
  methods: string[];
}

export interface EngineVersionResult {
  engineVersion: string;
  runtime: string;
}

export interface SystemOsInfoResult {
  osDescription: string;
  osArchitecture: string;
  processArchitecture: string;
  machineName: string;
}

export interface EngineSnapshot {
  status: EngineConnectionStatus;
  hello?: EngineHelloResult;
  version?: EngineVersionResult;
  osInfo?: SystemOsInfoResult;
  error?: string;
}

type TauriInvoke = <T>(cmd: string, args?: Record<string, unknown>) => Promise<T>;

function getTauriInvoke(): TauriInvoke | null {
  if (typeof window === 'undefined') return null;
  const w = window as unknown as { __TAURI_INTERNALS__?: { invoke?: TauriInvoke } };
  return typeof w.__TAURI_INTERNALS__?.invoke === 'function' ? w.__TAURI_INTERNALS__.invoke : null;
}

export async function probeEngine(): Promise<EngineSnapshot> {
  const invoke = getTauriInvoke();
  if (!invoke) {
    return { status: 'disconnected' };
  }

  try {
    const [hello, version, osInfo] = await Promise.all([
      invoke<EngineHelloResult>('engine_hello', { clientName: 'evo-desktop', protocolVersion: 1 }),
      invoke<EngineVersionResult>('engine_version'),
      invoke<SystemOsInfoResult>('system_os_info'),
    ]);
    return {
      status: 'connected',
      hello,
      version,
      osInfo,
    };
  } catch (err) {
    return {
      status: 'error',
      error: err instanceof Error ? err.message : String(err),
    };
  }
}

export interface CatalogAction {
  id: string;
  moduleId: string;
  category: number;
  title: string;
  description: string;
  riskLevel: string;
  reversibility: string;
  primitive: string;
  target: string;
}

export async function listCatalogActions(moduleId: string): Promise<CatalogAction[]> {
  const invoke = getTauriInvoke();
  if (!invoke) {
    return [];
  }
  const res = await invoke<{ actions: CatalogAction[] }>('catalog_list');
  return (res.actions ?? []).filter((a) => a.moduleId === moduleId);
}

export async function applyAction(actionId: string): Promise<{ actionId: string; status: string }> {
  const invoke = getTauriInvoke();
  if (!invoke) {
    throw new Error('Tauri runtime not available');
  }
  return invoke<{ actionId: string; status: string }>('action_apply', { actionId });
}

export async function rollbackAction(actionId: string): Promise<{ actionId: string; status: string }> {
  const invoke = getTauriInvoke();
  if (!invoke) {
    throw new Error('Tauri runtime not available');
  }
  return invoke<{ actionId: string; status: string }>('action_rollback', { actionId });
}

export interface SnapshotEntry {
  actionId: string;
  target: string;
  primitive: string;
  capturedAtUtc: string;
}

export async function listSnapshots(): Promise<SnapshotEntry[]> {
  const invoke = getTauriInvoke();
  if (!invoke) {
    return [];
  }
  const res = await invoke<{ snapshots: SnapshotEntry[] }>('snapshot_list');
  return res.snapshots ?? [];
}

export interface DriveSummary {
  name: string;
  format: string;
  totalBytes: number;
  freeBytes: number;
}

export interface SystemDiagnostics {
  osDescription: string;
  osVersion: string;
  architecture: string;
  processorCount: number;
  totalAvailableMemoryBytes: number;
  drives: DriveSummary[];
  appliedActionIds: string[];
  collectedAtUtc: string;
}

export async function collectDiagnostics(): Promise<SystemDiagnostics | null> {
  const invoke = getTauriInvoke();
  if (!invoke) return null;
  return invoke<SystemDiagnostics>('diagnostics_collect');
}

export interface PlannedOperation {
  actionId: string;
  moduleId: string;
  title: string;
  riskLevel: string;
  reason: string;
  alreadyApplied: boolean;
}

export interface OperationPlan {
  profile: string;
  totalRecommended: number;
  operations: PlannedOperation[];
  generatedAtUtc: string;
}

export async function generatePlan(profile: 'gaming' | 'balanced' | 'minimal'): Promise<OperationPlan | null> {
  const invoke = getTauriInvoke();
  if (!invoke) return null;
  return invoke<OperationPlan>('plan_generate', { profile });
}

export interface CleanupCandidate {
  path: string;
  sizeBytes: number;
  lastWriteUtc: string;
}

export interface QuarantineBatch {
  batchId: string;
  createdAtUtc: string;
  entries: Array<{ originalPath: string; quarantinedPath: string; sizeBytes: number }>;
}

export async function scanCleanup(): Promise<CleanupCandidate[]> {
  const invoke = getTauriInvoke();
  if (!invoke) return [];
  const res = await invoke<{ candidates: CleanupCandidate[] }>('cleanup_scan');
  return res.candidates ?? [];
}

export async function quarantineFiles(paths: string[]): Promise<QuarantineBatch> {
  const invoke = getTauriInvoke();
  if (!invoke) throw new Error('Tauri runtime not available');
  return invoke<QuarantineBatch>('cleanup_quarantine', { paths });
}

export async function restoreQuarantine(batchId: string): Promise<{ batchId: string; restored: number; total: number }> {
  const invoke = getTauriInvoke();
  if (!invoke) throw new Error('Tauri runtime not available');
  return invoke<{ batchId: string; restored: number; total: number }>('cleanup_restore', { batchId });
}

export interface LabMeasurement {
  samples: number;
  medianMs: number;
  minMs: number;
  maxMs: number;
  stdDevMs: number;
  workingSetBytes: number;
  measuredAtUtc: string;
}

export async function measureLab(samples = 7): Promise<LabMeasurement | null> {
  const invoke = getTauriInvoke();
  if (!invoke) return null;
  return invoke<LabMeasurement>('lab_measure', { samples });
}
