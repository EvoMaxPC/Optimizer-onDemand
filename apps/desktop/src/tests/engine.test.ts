import { describe, expect, it, afterEach } from 'vitest';
import {
  collectDiagnostics,
  generatePlan,
  measureLab,
  probeEngine,
  quarantineFiles,
  restoreQuarantine,
  scanCleanup,
} from '../engine/engineStatus';

describe('engineStatus / probeEngine', () => {
  afterEach(() => {
    delete (window as unknown as { __TAURI_INTERNALS__?: unknown }).__TAURI_INTERNALS__;
  });

  it('returns disconnected when Tauri runtime is not present', async () => {
    const snapshot = await probeEngine();
    expect(snapshot).toEqual({ status: 'disconnected' });
  });

  it('returns connected with hello, version and osInfo when Tauri invoke succeeds', async () => {
    (window as unknown as { __TAURI_INTERNALS__: { invoke: (cmd: string) => Promise<unknown> } }).__TAURI_INTERNALS__ = {
      invoke: async (cmd: string) => {
        if (cmd === 'engine_hello') {
          return { protocolVersion: 1, engineName: 'Evo.Engine.Host', readOnly: false, methods: ['engine.hello', 'engine.version', 'system.osInfo'] };
        }
        if (cmd === 'engine_version') {
          return { engineVersion: '0.1.0', runtime: '.NET 10.0.15' };
        }
        if (cmd === 'system_os_info') {
          return { osDescription: 'Windows 11', osArchitecture: 'X64', processArchitecture: 'X64', machineName: 'DEV' };
        }
        throw new Error(`Unexpected command: ${cmd}`);
      },
    };

    const snapshot = await probeEngine();
    expect(snapshot.status).toBe('connected');
    expect(snapshot.hello?.engineName).toBe('Evo.Engine.Host');
    expect(snapshot.version?.engineVersion).toBe('0.1.0');
    expect(snapshot.osInfo?.machineName).toBe('DEV');
  });

  it('supports diagnostics, plan, cleanup quarantine/restore and lab measure via Tauri', async () => {
    (window as unknown as { __TAURI_INTERNALS__: { invoke: (cmd: string) => Promise<unknown> } }).__TAURI_INTERNALS__ = {
      invoke: async (cmd: string) => {
        if (cmd === 'diagnostics_collect') {
          return { osDescription: 'Windows 11', osVersion: '10.0', architecture: 'X64', processorCount: 8, totalAvailableMemoryBytes: 16e9, drives: [], appliedActionIds: [], collectedAtUtc: '2026-11-23T00:00:00Z' };
        }
        if (cmd === 'plan_generate') {
          return { profile: 'gaming', totalRecommended: 1, operations: [{ actionId: 'gaming.gameMode.enable', moduleId: 'gaming', title: 'Game Mode', riskLevel: 'low', reason: 'ok', alreadyApplied: false }], generatedAtUtc: '2026-11-23T00:00:00Z' };
        }
        if (cmd === 'cleanup_scan') {
          return { candidates: [{ path: 'C:\\Temp\\a.tmp', sizeBytes: 100, lastWriteUtc: '2026-11-23T00:00:00Z' }] };
        }
        if (cmd === 'cleanup_quarantine') {
          return { batchId: 'b1', createdAtUtc: '2026-11-23T00:00:00Z', entries: [{ originalPath: 'C:\\Temp\\a.tmp', quarantinedPath: 'C:\\Q\\a.tmp', sizeBytes: 100 }] };
        }
        if (cmd === 'cleanup_restore') {
          return { batchId: 'b1', restored: 1, total: 1 };
        }
        if (cmd === 'lab_measure') {
          return { samples: 7, medianMs: 0.12, minMs: 0.1, maxMs: 0.2, stdDevMs: 0.03, workingSetBytes: 1024, measuredAtUtc: '2026-11-23T00:00:00Z' };
        }
        throw new Error(`Unexpected command: ${cmd}`);
      },
    };

    const diag = await collectDiagnostics();
    expect(diag?.processorCount).toBe(8);

    const plan = await generatePlan('gaming');
    expect(plan?.totalRecommended).toBe(1);

    const items = await scanCleanup();
    expect(items).toHaveLength(1);

    const batch = await quarantineFiles([items[0].path]);
    expect(batch.batchId).toBe('b1');

    const restored = await restoreQuarantine('b1');
    expect(restored.restored).toBe(1);

    const lab = await measureLab(7);
    expect(lab?.samples).toBe(7);
  });
});
