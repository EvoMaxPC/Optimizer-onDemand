# ADR-0020: Contrato IPC de Solo Lectura por NDJSON (`Evo.Engine.Host`)

## Estado

Aceptado

## Fecha

2026-11-22

## Contexto

En la Etapa 4a (`F2`), el proyecto introduce el host de dominio `.NET` (`Evo.Engine.Host`) en modo estrictamente de solo lectura, sin conectarlo todavía a la interfaz Tauri + React. Se requiere un contrato mínimo, verificable y sin dependencias externas de JSON-RPC para el intercambio de mensajes por entrada/salida estándar (`stdio`).

## Decisión

1. **Protocolo JSON-RPC 2.0 sobre NDJSON (`stdin`/`stdout`):**
   - Cada mensaje es una única línea JSON terminada en `\n`, codificada en UTF-8 sin BOM (`UTF8Encoding(false)`).
   - Tamaño máximo de línea: **1 MiB** (`1048576` bytes).
   - `stdout` se reserva exclusivamente para respuestas del protocolo; cualquier registro de diagnóstico se envía a `stderr`.
   - Las notificaciones (peticiones sin campo `id`) se procesan sin emitir respuesta.
   - El fin de flujo (`EOF`) en `stdin` provoca salida limpia con código `0`.
2. **Superficie cerrada de tres métodos (`schemas/ipc-host.schema.json`):**
   - `engine.hello`: recibe `{ clientName: string (1..64), protocolVersion: integer }` y devuelve `{ protocolVersion: 1, engineName: "Evo.Engine.Host", readOnly: true, methods: ["engine.hello", "engine.version", "system.osInfo"] }`.
   - `engine.version`: sin `params`; devuelve `{ engineVersion, runtime }`.
   - `system.osInfo`: sin `params`; devuelve `{ platform (windows|linux|macos|other), osDescription, osVersion, architecture, source: "runtime" }`.
3. **Códigos de error:** `-32700` (parse error, `id: null`), `-32600` (invalid request, incluye batch no soportado), `-32601` (method not found), `-32602` (invalid params), `-32001` (`protocol_mismatch` cuando `protocolVersion !== 1`).
4. **Validación documental:** `R13-IPC-CONTRACT` en `tools/check-docs.mjs` verifica el esquema en cada ejecución de CI.

## Alternativas descartadas

1. **Incorporar una librería externa de JSON-RPC (p. ej. `StreamJsonRpc`):**
   - *Motivo de descarte:* Añade dependencias transitivas innecesarias para un contrato cerrado de 3 métodos sobre `System.Text.Json` de la BCL.

## Consecuencias

- **Positivas:** Cero dependencias NuGet en el host; protocolo determinista, auditable y fácil de probar en aislamiento.
- **Negativas / Coste asumido:** El framing y el despacho JSON-RPC deben mantenerse manualmente si en etapas futuras se amplían métodos.

## Límites y riesgos conocidos

- **Protocolo propio escrito a mano:** Al no usar una librería JSON-RPC estándar, el parser y el validador deben cubrir explícitamente casos límite (batch, notificaciones, BOM, límite de 1 MiB).
- **Sin autenticación en `stdio`:** El canal `stdio` asume que el proceso padre y el host se ejecutan bajo el mismo usuario sin privilegios.
- **Separación del canal elevado:** El canal elevado descrito en [ADR-0003](0003-elevated-ipc-channel.md) es un canal distinto y no se implementa en esta etapa.

## Preguntas abiertas

- Definir en la Etapa 4b cómo el contenedor Tauri lanzará y supervisará el subproceso `Evo.Engine.Host`.
