import { describe, expect, it } from 'vitest';
import es from '../i18n/locales/es.json';
import en from '../i18n/locales/en.json';

interface LeafEntry {
  keyPath: string;
  value: unknown;
}

function collectLeafEntries(node: Record<string, unknown>, prefix = ''): LeafEntry[] {
  const entries: LeafEntry[] = [];
  for (const key of Object.keys(node).sort()) {
    const fullPath = prefix ? `${prefix}.${key}` : key;
    const value = node[key];
    if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
      entries.push(...collectLeafEntries(value as Record<string, unknown>, fullPath));
    } else {
      entries.push({ keyPath: fullPath, value });
    }
  }
  return entries;
}

const FORBIDDEN_PATTERNS = [
  /\bMB\b/,
  /\bGB\b/,
  /%/,
  /demostraci[oó]n/i,
  /\bdemo\b/i,
  /\bOptimizar\b/i,
  /\bLimpiar\b/i,
  /\bAplicar\b/i,
  /\bAnalizar\b/i,
  /\bEscanear\b/i,
  /\bReparar\b/i,
  /\bOptimize\b/i,
  /\bClean\b/i,
  /\bApply\b/i,
  /\bScan\b/i,
  /\bRepair\b/i
];

describe('i18n locales (es.json y en.json)', () => {
  const esLeaves = collectLeafEntries(es as Record<string, unknown>);
  const enLeaves = collectLeafEntries(en as Record<string, unknown>);

  it('mantiene paridad exacta de claves recursivas entre es.json y en.json', () => {
    const esKeys = esLeaves.map((e) => e.keyPath);
    const enKeys = enLeaves.map((e) => e.keyPath);
    expect(esKeys).toEqual(enKeys);
  });

  it('no contiene ningún valor vacío en es.json ni en.json', () => {
    for (const entry of [...esLeaves, ...enLeaves]) {
      expect(typeof entry.value).toBe('string');
      expect((entry.value as string).trim().length).toBeGreaterThan(0);
    }
  });

  it('no contiene unidades (MB, GB, %), la palabra demostración ni llamadas a la acción prohibidas', () => {
    for (const entry of [...esLeaves, ...enLeaves]) {
      const text = String(entry.value);
      for (const pattern of FORBIDDEN_PATTERNS) {
        expect(text, `Clave ${entry.keyPath} contiene patrón prohibido ${pattern}`).not.toMatch(
          pattern
        );
      }
    }
  });
});
