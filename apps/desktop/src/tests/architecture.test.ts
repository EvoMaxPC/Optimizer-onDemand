import { describe, expect, it } from 'vitest';
import pkgRaw from '../../package.json?raw';
import mainRs from '../../src-tauri/src/main.rs?raw';
import capRaw from '../../src-tauri/capabilities/default.json?raw';
import confRaw from '../../src-tauri/tauri.conf.json?raw';

const srcFilesMap = import.meta.glob('../**/*.{ts,tsx,css,json}', {
  query: '?raw',
  import: 'default',
  eager: true
}) as Record<string, string>;

const ALLOWED_DEPENDENCIES = ['react', 'react-dom', 'react-router-dom'].sort();

const ALLOWED_DEV_DEPENDENCIES = [
  '@tauri-apps/cli',
  '@testing-library/jest-dom',
  '@testing-library/react',
  '@testing-library/user-event',
  '@types/react',
  '@types/react-dom',
  '@vitejs/plugin-react',
  'jsdom',
  'typescript',
  'vite',
  'vitest'
].sort();

const FORBIDDEN_FRONTEND_TOKENS = [
  '@tauri-apps' + '/',
  'node:' + 'child_process',
  'child_' + 'process',
  'fetch' + '(',
  'XMLHttp' + 'Request',
  'Web' + 'Socket',
  'eval' + '(',
  'new ' + 'Function',
  'local' + 'Storage',
  'session' + 'Storage',
  'indexed' + 'DB',
  'style=' + '{{'
];

describe('Restricciones arquitectónicas y de seguridad de Etapa 2 (F1a)', () => {
  it('apps/desktop/package.json contiene exactamente las dependencias de B2 y no tiene campo license', () => {
    const pkg = JSON.parse(pkgRaw) as Record<string, unknown>;

    expect('license' in pkg).toBe(false);

    const deps = Object.keys((pkg.dependencies ?? {}) as Record<string, string>).sort();
    const devDeps = Object.keys((pkg.devDependencies ?? {}) as Record<string, string>).sort();

    expect(deps).toEqual(ALLOWED_DEPENDENCIES);
    expect(devDeps).toEqual(ALLOWED_DEV_DEPENDENCIES);

    const allPackages = [...deps, ...devDeps];
    expect(allPackages).not.toContain('@tauri-apps' + '/api');
    for (const pkgName of allPackages) {
      expect(pkgName.startsWith('@tauri-apps' + '/plugin-')).toBe(false);
    }
  });

  it('ningún archivo en apps/desktop/src/** usa APIs prohibidas ni estilos inline', () => {
    const entries = Object.entries(srcFilesMap);
    expect(entries.length).toBeGreaterThan(0);

    for (const [relPath, content] of entries) {
      for (const token of FORBIDDEN_FRONTEND_TOKENS) {
        expect(
          content.includes(token),
          `Archivo ${relPath} contiene token prohibido "${token}"`
        ).toBe(false);
      }
    }
  });

  it('apps/desktop/src-tauri/src/main.rs no registra comandos, plugins ni subprocesos', () => {
    const forbiddenRustTokens = [
      '#[tauri::command]',
      'invoke_handler',
      '.plugin(',
      'std::process::Command',
      'tokio::process::Command'
    ];

    for (const token of forbiddenRustTokens) {
      expect(mainRs.includes(token), `main.rs contiene "${token}"`).toBe(false);
    }
  });

  it('apps/desktop/src-tauri/capabilities/default.json solo concede core:default sobre ["main"]', () => {
    const cap = JSON.parse(capRaw) as {
      windows?: string[];
      permissions?: string[];
    };

    expect(cap.windows).toEqual(['main']);
    expect(cap.permissions).toEqual(['core:default']);
  });

  it('apps/desktop/src-tauri/tauri.conf.json define CSP estricta no nula sin orígenes remotos ni unsafe-eval', () => {
    const conf = JSON.parse(confRaw) as {
      app?: {
        security?: {
          csp?: string | null;
        };
      };
    };

    const csp = conf.app?.security?.csp;
    expect(typeof csp).toBe('string');
    expect((csp ?? '').trim().length).toBeGreaterThan(0);

    const cspText = csp ?? '';
    expect(cspText.toLowerCase()).not.toContain('http' + ':');
    expect(cspText.toLowerCase()).not.toContain('https' + ':');
    expect(cspText.toLowerCase()).not.toContain('unsafe-eval');
    expect(cspText).toContain("default-src 'self'");
    expect(cspText).toContain("object-src 'none'");
    expect(cspText).toContain("frame-ancestors 'none'");
  });
});
