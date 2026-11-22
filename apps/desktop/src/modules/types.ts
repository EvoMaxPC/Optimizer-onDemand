export type ModuleId = 'gaming' | 'memory' | 'system' | 'network' | 'storage' | 'security';
export type ModuleCapability = 'probes' | 'dossiers' | 'actions';

export interface ModuleEntry {
  id: ModuleId;
  categories: number[];
  capabilities: ModuleCapability[];
}

export interface ModulesManifest {
  schemaVersion: 1;
  modules: ModuleEntry[];
}
