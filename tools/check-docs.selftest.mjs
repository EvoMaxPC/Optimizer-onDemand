#!/usr/bin/env node
/**
 * Autotest de mutaciones para tools/check-docs.mjs (Tarea 3c/3d + Tarea 5).
 * Usa exclusivamente módulos integrados de Node.js.
 */

import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const REPO_ROOT = path.resolve(__dirname, '..');

const EXCLUDE_DIRS = new Set(['.git', 'node_modules', 'dist', 'target']);
const BINARY_EXTS = new Set(['.png', '.jpg', '.jpeg', '.gif', '.ico', '.icns', '.woff', '.woff2', '.exe', '.dll', '.pdb', '.zip', '.tgz']);

function mutateText(filePath, fn) {
  const raw = fs.readFileSync(filePath, 'utf8');
  const eol = raw.includes('\r\n') ? '\r\n' : '\n';
  const normalized = raw.replace(/\r\n/g, '\n');
  const next = fn(normalized);
  if (typeof next === 'string') {
    fs.writeFileSync(filePath, eol === '\r\n' ? next.replace(/\n/g, '\r\n') : next, 'utf8');
  }
}

function convertDirToCrlf(dir) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      convertDirToCrlf(p);
    } else if (entry.isFile() && !BINARY_EXTS.has(path.extname(entry.name).toLowerCase())) {
      const raw = fs.readFileSync(p, 'utf8');
      fs.writeFileSync(p, raw.replace(/\r?\n/g, '\r\n'), 'utf8');
    }
  }
}

function copyRepo(src, dest) {
  fs.mkdirSync(dest, { recursive: true });
  for (const entry of fs.readdirSync(src, { withFileTypes: true })) {
    if (entry.isDirectory() && EXCLUDE_DIRS.has(entry.name)) continue;
    const s = path.join(src, entry.name);
    const d = path.join(dest, entry.name);
    if (entry.isDirectory()) {
      copyRepo(s, d);
    } else if (entry.isFile()) {
      fs.copyFileSync(s, d);
    }
  }
}

function runCheckDocs(cwd) {
  try {
    const out = execFileSync(process.execPath, [path.join(cwd, 'tools/check-docs.mjs')], {
      cwd,
      encoding: 'utf8',
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    return { code: 0, output: out, failedRules: [], firstMsg: 'OK' };
  } catch (err) {
    const output = `${err.stdout || ''}\n${err.stderr || ''}`.trim();
    const failLines = output.split('\n').filter((l) => l.includes('[FAIL]'));
    const failedRules = [...new Set(failLines.map((l) => {
      const m = l.match(/\[FAIL\]\s+([A-Z0-9-]+):/);
      return m ? m[1] : 'UNKNOWN';
    }))];
    const firstMsg = failLines[0] ? failLines[0].trim() : output.split('\n')[0];
    return {
      code: typeof err.status === 'number' ? err.status : 1,
      output,
      failedRules,
      firstMsg,
    };
  }
}

const LOCAL_UNIX = '/' + 'home' + '/testuser/secret';
const BAD_URL = 'https://' + 'example.invalid/bad';

const tests = [
  {
    name: 'Baseline (sin mutar)',
    expectedRule: 'NONE',
    expectPass: true,
    mutate: () => {},
  },
  {
    name: 'R1: crear LICENSE',
    expectedRule: 'R1-NO-LICENSE',
    expectPass: false,
    mutate: (dir) => fs.writeFileSync(path.join(dir, 'LICENSE'), 'test\n'),
  },
  {
    name: 'R2: archivo suelto en raíz',
    expectedRule: 'R2-STRUCTURE',
    expectPass: false,
    mutate: (dir) => fs.writeFileSync(path.join(dir, 'unexpected.txt'), 'x\n'),
  },
  {
    name: 'R2: archivo nuevo en apps/desktop/src (control positivo)',
    expectedRule: 'R2-STRUCTURE',
    expectPass: true,
    mutate: (dir) => fs.writeFileSync(path.join(dir, 'apps/desktop/src/extra.ts'), 'export const x = 1;\n'),
  },
  {
    name: 'R2: borrar archivo F0 obligatorio (SECURITY.md)',
    expectedRule: 'R2-STRUCTURE',
    expectPass: false,
    mutate: (dir) => fs.unlinkSync(path.join(dir, 'SECURITY.md')),
  },
  {
    name: 'R3: añadir "license" a package.json raíz',
    expectedRule: 'R3-PACKAGE-JSON',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'package.json');
      const j = JSON.parse(fs.readFileSync(p, 'utf8'));
      j.license = 'GPL-3.0-or-later';
      fs.writeFileSync(p, JSON.stringify(j, null, 2));
    },
  },
  {
    name: 'R3: añadir "dependencies" a package.json raíz',
    expectedRule: 'R3-PACKAGE-JSON',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'package.json');
      const j = JSON.parse(fs.readFileSync(p, 'utf8'));
      j.dependencies = { foo: '1.0.0' };
      fs.writeFileSync(p, JSON.stringify(j, null, 2));
    },
  },
  {
    name: 'R4 (i): borrar línea de índice de ADR-0018',
    expectedRule: 'R4-ADR-INDEX',
    expectPass: false,
    mutate: (dir) => {
      mutateText(path.join(dir, 'docs/adr/README.md'), (c) =>
        c.replace(/.*0018-tauri-surface-and-frontend-dependency-policy\.md.*\n/, '')
      );
    },
  },
  {
    name: 'R4 (ii): enlace a archivo inexistente en docs/adr/README.md',
    expectedRule: 'R4-ADR-INDEX',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'docs/adr/README.md');
      const c = fs.readFileSync(p, 'utf8').replaceAll('0018-tauri-surface-and-frontend-dependency-policy.md', '0018-missing.md');
      fs.writeFileSync(p, c);
    },
  },
  {
    name: 'R4 (iii): ADR nuevo sin entrada de índice',
    expectedRule: 'R4-ADR-INDEX',
    expectPass: false,
    mutate: (dir) => {
      fs.writeFileSync(path.join(dir, 'docs/adr/0019-extra.md'), '# ADR-0019: Extra\n');
    },
  },
  {
    name: 'R5: vaciar "Límites y riesgos conocidos"',
    expectedRule: 'R5-ADR-TEMPLATE',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'docs/adr/0018-tauri-surface-and-frontend-dependency-policy.md');
      const c = fs.readFileSync(p, 'utf8').replace(
        /## Límites y riesgos conocidos[\s\S]*?## Preguntas abiertas/,
        '## Límites y riesgos conocidos\n\n## Preguntas abiertas'
      );
      fs.writeFileSync(p, c);
    },
  },
  {
    name: 'R5: quitar sección "## Alternativas descartadas"',
    expectedRule: 'R5-ADR-TEMPLATE',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'docs/adr/0018-tauri-surface-and-frontend-dependency-policy.md');
      const c = fs.readFileSync(p, 'utf8').replace('## Alternativas descartadas', '## Otra');
      fs.writeFileSync(p, c);
    },
  },
  {
    name: 'R6: JSON inválido en schemas/action.schema.json',
    expectedRule: 'R6-SCHEMA',
    expectPass: false,
    mutate: (dir) => fs.writeFileSync(path.join(dir, 'schemas/action.schema.json'), '{ invalid json'),
  },
  {
    name: 'R10: quitar additionalProperties:false en primera rama',
    expectedRule: 'R10-SCHEMA-STRUCTURE',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'schemas/action.schema.json');
      const j = JSON.parse(fs.readFileSync(p, 'utf8'));
      const keys = Object.keys(j.$defs);
      delete j.$defs[keys[0]].additionalProperties;
      fs.writeFileSync(p, JSON.stringify(j, null, 2));
    },
  },
  {
    name: 'R10: quitar additionalProperties:false en rama intermedia',
    expectedRule: 'R10-SCHEMA-STRUCTURE',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'schemas/action.schema.json');
      const j = JSON.parse(fs.readFileSync(p, 'utf8'));
      const keys = Object.keys(j.$defs);
      delete j.$defs[keys[Math.floor(keys.length / 2)]].additionalProperties;
      fs.writeFileSync(p, JSON.stringify(j, null, 2));
    },
  },
  {
    name: 'R10: quitar additionalProperties:false en última rama',
    expectedRule: 'R10-SCHEMA-STRUCTURE',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'schemas/action.schema.json');
      const j = JSON.parse(fs.readFileSync(p, 'utf8'));
      const keys = Object.keys(j.$defs);
      delete j.$defs[keys[keys.length - 1]].additionalProperties;
      fs.writeFileSync(p, JSON.stringify(j, null, 2));
    },
  },
  {
    name: 'R10: añadir campo libre string ("command") a una rama',
    expectedRule: 'R10-SCHEMA-STRUCTURE',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'schemas/action.schema.json');
      const j = JSON.parse(fs.readFileSync(p, 'utf8'));
      const keys = Object.keys(j.$defs);
      j.$defs[keys[0]].properties.command = { type: 'string' };
      fs.writeFileSync(p, JSON.stringify(j, null, 2));
    },
  },
  {
    name: 'R10: eliminar una de las 13 primitivas',
    expectedRule: 'R10-SCHEMA-STRUCTURE',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'schemas/action.schema.json');
      const j = JSON.parse(fs.readFileSync(p, 'utf8'));
      j.properties.changes.items.oneOf.pop();
      fs.writeFileSync(p, JSON.stringify(j, null, 2));
    },
  },
  {
    name: 'R10: cambiar discriminador a valor desconocido',
    expectedRule: 'R10-SCHEMA-STRUCTURE',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'schemas/action.schema.json');
      const j = JSON.parse(fs.readFileSync(p, 'utf8'));
      const changeKey = Object.keys(j.$defs).find((k) => k.startsWith('change'));
      j.$defs[changeKey].properties.primitive.const = 'unknown.primitive';
      fs.writeFileSync(p, JSON.stringify(j, null, 2));
    },
  },
  {
    name: 'R7: enlace relativo roto en README.md',
    expectedRule: 'R7-MD-LINKS',
    expectPass: false,
    mutate: (dir) => fs.appendFileSync(path.join(dir, 'README.md'), '\n[roto](./no-existe.md)\n'),
  },
  {
    name: 'R8: ruta local absoluta en README.md',
    expectedRule: 'R8-NO-LOCAL-PATHS',
    expectPass: false,
    mutate: (dir) => fs.appendFileSync(path.join(dir, 'README.md'), `\n${LOCAL_UNIX}\n`),
  },
  {
    name: 'R8: ruta local absoluta en tools/check-docs.mjs',
    expectedRule: 'R8-NO-LOCAL-PATHS',
    expectPass: false,
    mutate: (dir) => fs.appendFileSync(path.join(dir, 'tools/check-docs.mjs'), `\n// ${LOCAL_UNIX}\n`),
  },
  {
    name: 'R8: ruta local absoluta en .github/workflows/ci.yml',
    expectedRule: 'R8-NO-LOCAL-PATHS',
    expectPass: false,
    mutate: (dir) => fs.appendFileSync(path.join(dir, '.github/workflows/ci.yml'), `\n# ${LOCAL_UNIX}\n`),
  },
  {
    name: 'R8: marcador EJEMPLO-RUTA-LOCAL (control positivo)',
    expectedRule: 'R8-NO-LOCAL-PATHS',
    expectPass: true,
    mutate: (dir) => fs.appendFileSync(path.join(dir, 'README.md'), `\n<!-- EJEMPLO-RUTA-LOCAL: ${LOCAL_UNIX} -->\n`),
  },
  {
    name: 'R9: URL externa no permitida en README.md',
    expectedRule: 'R9-EXTERNAL-URLS',
    expectPass: false,
    mutate: (dir) => fs.appendFileSync(path.join(dir, 'README.md'), `\n${BAD_URL}\n`),
  },
  {
    name: 'R9: URL externa no permitida en tools/check-docs.mjs',
    expectedRule: 'R9-EXTERNAL-URLS',
    expectPass: false,
    mutate: (dir) => fs.appendFileSync(path.join(dir, 'tools/check-docs.mjs'), `\n// ${BAD_URL}\n`),
  },
  {
    name: 'R9: URL en pnpm-lock.yaml (control positivo)',
    expectedRule: 'R9-EXTERNAL-URLS',
    expectPass: true,
    mutate: (dir) => fs.appendFileSync(path.join(dir, 'pnpm-lock.yaml'), `\n# ${BAD_URL}\n`),
  },
  {
    name: 'R11: quitar campo requerido "id" del ejemplo',
    expectedRule: 'R11-EXAMPLE-STRUCTURE',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'docs/catalog/CATALOG_SPEC.md');
      fs.writeFileSync(p, fs.readFileSync(p, 'utf8').replace('"id":', '"removedField":'));
    },
  },
  {
    name: 'R11: enum inválido en ejemplo',
    expectedRule: 'R11-EXAMPLE-STRUCTURE',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'docs/catalog/CATALOG_SPEC.md');
      fs.writeFileSync(p, fs.readFileSync(p, 'utf8').replace('"status": "draft"', '"status": "invalid_status"'));
    },
  },
  {
    name: 'R11: tipo de change desconocido en ejemplo',
    expectedRule: 'R11-EXAMPLE-STRUCTURE',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'docs/catalog/CATALOG_SPEC.md');
      fs.writeFileSync(p, fs.readFileSync(p, 'utf8').replace(/"primitive":\s*"[^"]+"/, '"primitive": "unknown.op"'));
    },
  },
  {
    name: 'R11: propiedad extra en ejemplo',
    expectedRule: 'R11-EXAMPLE-STRUCTURE',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'docs/catalog/CATALOG_SPEC.md');
      fs.writeFileSync(p, fs.readFileSync(p, 'utf8').replace('"status": "draft",', '"status": "draft",\n  "extraProp": 123,'));
    },
  },
  {
    name: 'R11: eliminar bloque de ejemplo ```json de CATALOG_SPEC.md',
    expectedRule: 'R11-EXAMPLE-STRUCTURE',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'docs/catalog/CATALOG_SPEC.md');
      fs.writeFileSync(p, fs.readFileSync(p, 'utf8').replace(/```json[\s\S]*?```/, ''));
    },
  },
  {
    name: 'Control CRLF: convertir README.md a CRLF',
    expectedRule: 'NONE',
    expectPass: true,
    mutate: (dir) => {
      const p = path.join(dir, 'README.md');
      fs.writeFileSync(p, fs.readFileSync(p, 'utf8').replace(/\r?\n/g, '\r\n'));
    },
  },
  {
    name: 'R12: manifiesto ausente',
    expectedRule: 'R12-MODULES-MANIFEST',
    expectPass: false,
    mutate: (dir) => fs.unlinkSync(path.join(dir, 'apps/desktop/src/modules/modules.manifest.json')),
  },
  {
    name: 'R12: id duplicado en manifiesto',
    expectedRule: 'R12-MODULES-MANIFEST',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'apps/desktop/src/modules/modules.manifest.json');
      const m = JSON.parse(fs.readFileSync(p, 'utf8'));
      m.modules[1].id = 'gaming';
      fs.writeFileSync(p, JSON.stringify(m, null, 2));
    },
  },
  {
    name: 'R12: módulo faltante en manifiesto',
    expectedRule: 'R12-MODULES-MANIFEST',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'apps/desktop/src/modules/modules.manifest.json');
      const m = JSON.parse(fs.readFileSync(p, 'utf8'));
      m.modules.pop();
      fs.writeFileSync(p, JSON.stringify(m, null, 2));
    },
  },
  {
    name: 'R12: categoría repetida en manifiesto',
    expectedRule: 'R12-MODULES-MANIFEST',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'apps/desktop/src/modules/modules.manifest.json');
      const m = JSON.parse(fs.readFileSync(p, 'utf8'));
      m.modules[1].categories.push(5);
      fs.writeFileSync(p, JSON.stringify(m, null, 2));
    },
  },
  {
    name: 'R12: categoría 18 presente en manifiesto',
    expectedRule: 'R12-MODULES-MANIFEST',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'apps/desktop/src/modules/modules.manifest.json');
      const m = JSON.parse(fs.readFileSync(p, 'utf8'));
      m.modules[1].categories.push(18);
      fs.writeFileSync(p, JSON.stringify(m, null, 2));
    },
  },
  {
    name: 'R12: categoría 5 ausente en manifiesto',
    expectedRule: 'R12-MODULES-MANIFEST',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'apps/desktop/src/modules/modules.manifest.json');
      const m = JSON.parse(fs.readFileSync(p, 'utf8'));
      m.modules[0].categories = [7, 8];
      fs.writeFileSync(p, JSON.stringify(m, null, 2));
    },
  },
  {
    name: 'R12: campo extra en manifiesto',
    expectedRule: 'R12-MODULES-MANIFEST',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'apps/desktop/src/modules/modules.manifest.json');
      const m = JSON.parse(fs.readFileSync(p, 'utf8'));
      m.extra = true;
      fs.writeFileSync(p, JSON.stringify(m, null, 2));
    },
  },
  {
    name: 'R12: capability desconocida en manifiesto',
    expectedRule: 'R12-MODULES-MANIFEST',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'apps/desktop/src/modules/modules.manifest.json');
      const m = JSON.parse(fs.readFileSync(p, 'utf8'));
      m.modules[0].capabilities[0] = 'unknown';
      fs.writeFileSync(p, JSON.stringify(m, null, 2));
    },
  },
  {
    name: 'R12: orden alterado en manifiesto',
    expectedRule: 'R12-MODULES-MANIFEST',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'apps/desktop/src/modules/modules.manifest.json');
      const m = JSON.parse(fs.readFileSync(p, 'utf8'));
      [m.modules[0], m.modules[1]] = [m.modules[1], m.modules[0]];
      fs.writeFileSync(p, JSON.stringify(m, null, 2));
    },
  },
  {
    name: 'R13: esquema IPC ausente',
    expectedRule: 'R13-IPC-CONTRACT',
    expectPass: false,
    mutate: (dir) => fs.unlinkSync(path.join(dir, 'schemas/ipc-host.schema.json')),
  },
  {
    name: 'R13: JSON roto en esquema IPC',
    expectedRule: 'R13-IPC-CONTRACT',
    expectPass: false,
    mutate: (dir) => fs.writeFileSync(path.join(dir, 'schemas/ipc-host.schema.json'), '{ broken'),
  },
  {
    name: 'R13: método extra en esquema IPC',
    expectedRule: 'R13-IPC-CONTRACT',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'schemas/ipc-host.schema.json');
      const s = JSON.parse(fs.readFileSync(p, 'utf8'));
      s.properties.methods.items.oneOf.push({ type: 'object', additionalProperties: false, properties: { name: { const: 'extra.method' } } });
      fs.writeFileSync(p, JSON.stringify(s, null, 2));
    },
  },
  {
    name: 'R13: método faltante en esquema IPC',
    expectedRule: 'R13-IPC-CONTRACT',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'schemas/ipc-host.schema.json');
      const s = JSON.parse(fs.readFileSync(p, 'utf8'));
      s.properties.methods.items.oneOf.pop();
      fs.writeFileSync(p, JSON.stringify(s, null, 2));
    },
  },
  {
    name: 'R13: orden alterado en esquema IPC',
    expectedRule: 'R13-IPC-CONTRACT',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'schemas/ipc-host.schema.json');
      const s = JSON.parse(fs.readFileSync(p, 'utf8'));
      [s.properties.methods.items.oneOf[0], s.properties.methods.items.oneOf[1]] = [
        s.properties.methods.items.oneOf[1],
        s.properties.methods.items.oneOf[0],
      ];
      fs.writeFileSync(p, JSON.stringify(s, null, 2));
    },
  },
  {
    name: 'R13: additionalProperties true en esquema IPC',
    expectedRule: 'R13-IPC-CONTRACT',
    expectPass: false,
    mutate: (dir) => {
      const p = path.join(dir, 'schemas/ipc-host.schema.json');
      const s = JSON.parse(fs.readFileSync(p, 'utf8'));
      s.additionalProperties = true;
      fs.writeFileSync(p, JSON.stringify(s, null, 2));
    },
  },
];

const schema = JSON.parse(fs.readFileSync(path.join(REPO_ROOT, 'schemas/action.schema.json'), 'utf8'));
for (const reqField of schema.required) {
  tests.push({
    name: `R11: quitar requerido "${reqField}"`,
    expectedRule: 'R11-EXAMPLE-STRUCTURE',
    expectPass: false,
    mutate: (dir) => {
      mutateText(path.join(dir, 'docs/catalog/CATALOG_SPEC.md'), (c) => {
        const m = c.match(/```json\n([\s\S]*?)\n```/);
        const ex = JSON.parse(m[1]);
        delete ex[reqField];
        return c.replace(m[1], JSON.stringify(ex, null, 2));
      });
    },
  });
}

tests.unshift({
  name: 'Base: copia con todos los archivos de texto en CRLF debe pasar',
  expectedRule: null,
  expectPass: true,
  mutate: (dir) => {
    convertDirToCrlf(dir);
  },
});

let failures = 0;
for (const t of tests) {
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'evo-selftest-'));
  try {
    copyRepo(REPO_ROOT, tmpDir);
    t.mutate(tmpDir);
    const res = runCheckDocs(tmpDir);
    const passed = t.expectPass ? res.code === 0 : res.code !== 0 && res.failedRules.includes(t.expectedRule);
    if (!passed) failures++;
    console.log(
      `[${passed ? 'OK' : 'FALLO'}] ${t.name} | esperada=${t.expectPass ? 'PASS' : t.expectedRule} | fallaron=${res.failedRules.join(',') || 'NONE'} | exit=${res.code} | primer mensaje: ${res.firstMsg}`
    );
  } finally {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  }
}

if (failures > 0) {
  console.error(`\nSelftest FAILED: ${failures} case(s) did not match expectations.`);
  process.exit(1);
}
console.log(`\nSelftest PASSED: ${tests.length}/${tests.length} cases matched expectations.`);
