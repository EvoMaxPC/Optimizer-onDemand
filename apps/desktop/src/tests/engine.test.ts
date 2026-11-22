import { describe, expect, it, afterEach } from 'vitest';
import { probeEngine } from '../engine/engineStatus';

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
          return { protocolVersion: 1, engineName: 'Evo.Engine.Host', readOnly: true, methods: ['engine.hello', 'engine.version', 'system.osInfo'] };
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
});
