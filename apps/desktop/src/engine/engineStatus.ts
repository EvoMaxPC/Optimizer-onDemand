/**
 * Única fuente de estado del motor en Etapa 3 (F1b).
 * En Etapa 4 (F2), este valor se sustituye por el handshake real con el motor .NET.
 */
export const engineStatus = 'disconnected' as const;

export type EngineConnectionStatus = typeof engineStatus;
