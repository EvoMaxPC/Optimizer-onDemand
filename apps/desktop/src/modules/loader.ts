import rawManifest from './modules.manifest.json';
import type { ModuleCapability, ModuleEntry, ModuleId, ModulesManifest } from './types';

const VALID_IDS: readonly ModuleId[] = ['gaming', 'memory', 'system', 'network', 'storage', 'security'];
const VALID_CAPS: readonly ModuleCapability[] = ['probes', 'dossiers', 'actions'];

export function validateModulesManifest(input: unknown): ModulesManifest {
  if (!input || typeof input !== 'object' || Array.isArray(input)) {
    throw new Error('Invalid modules manifest: root must be an object.');
  }
  const obj = input as Record<string, unknown>;
  if (obj.schemaVersion !== 1) {
    throw new Error('Invalid modules manifest: schemaVersion must be 1.');
  }
  if (!Array.isArray(obj.modules) || obj.modules.length !== VALID_IDS.length) {
    throw new Error('Invalid modules manifest: modules must contain 6 entries.');
  }
  const seenIds = new Set<string>();
  const seenCats = new Set<number>();

  const modules: ModuleEntry[] = obj.modules.map((item, idx) => {
    if (!item || typeof item !== 'object' || Array.isArray(item)) {
      throw new Error(`Invalid modules manifest: modules[${idx}] must be an object.`);
    }
    const entry = item as Record<string, unknown>;
    if (typeof entry.id !== 'string' || entry.id !== VALID_IDS[idx] || seenIds.has(entry.id)) {
      throw new Error(`Invalid modules manifest: unexpected id at index ${idx}.`);
    }
    seenIds.add(entry.id);

    if (!Array.isArray(entry.categories) || entry.categories.length === 0) {
      throw new Error(`Invalid modules manifest: categories missing for ${entry.id}.`);
    }
    for (const c of entry.categories) {
      if (typeof c !== 'number' || !Number.isInteger(c) || c < 1 || c > 17 || seenCats.has(c)) {
        throw new Error(`Invalid modules manifest: invalid or duplicate category ${String(c)} in ${entry.id}.`);
      }
      seenCats.add(c);
    }

    const caps = entry.capabilities;
    if (
      !Array.isArray(caps) ||
      caps.length !== VALID_CAPS.length ||
      !VALID_CAPS.every((cap) => caps.includes(cap))
    ) {
      throw new Error(`Invalid modules manifest: invalid capabilities in ${entry.id}.`);
    }

    return {
      id: entry.id as ModuleId,
      categories: entry.categories as number[],
      capabilities: entry.capabilities as ModuleCapability[],
    };
  });

  if (seenCats.size !== 17) {
    throw new Error('Invalid modules manifest: categories 1..17 must all be covered.');
  }

  return { schemaVersion: 1, modules };
}

export function loadModulesManifest(override?: unknown): ModulesManifest {
  return validateModulesManifest(override ?? rawManifest);
}
