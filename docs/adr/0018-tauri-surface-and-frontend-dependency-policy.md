# ADR-0018: Superficie Mínima de Tauri v2 y Política de Dependencias del Frontend

## Estado

Aceptado

## Fecha

2026-11-21

## Contexto

Al iniciar la Etapa 2 (`F1a`, shell de escritorio Tauri v2 + React sin motor), el proyecto introduce por primera vez código ejecutable y dependencias externas de terceros (`npm` y `crates.io`). En aplicaciones de escritorio construidas con tecnologías web y contenedores nativos como Tauri, los dos vectores de riesgo más frecuentes en etapas tempranas son:

1. **Sobreexposición de la superficie nativa (IPC y plugins):** habilitar plugins de sistema (`shell`, `fs`, `process`, `http`, `dialog`), registrar comandos nativos prematuros o relajar la Content Security Policy (CSP) antes de que exista el motor de dominio validado abre vías para que un fallo en la capa visual escale a ejecución en el sistema operativo.
2. **Inflación de la cadena de suministro (`supply chain`) y conflictos de licencia:** incorporar kits visuales pesados, librerías de estado global, clientes HTTP, telemetría o paquetes con licencias incompatibles con `GPL-3.0-or-later` incrementa la superficie de auditoría y contradice el principio de sobriedad técnica de [PRINCIPLES.md](../PRINCIPLES.md) y [ADR-0002](0002-privilege-separation.md).

Se requiere una regla arquitectónica verificable mediante tests automatizados que acote estrictamente qué expone Tauri v2 y qué dependencias puede instalar `apps/desktop`.

## Decisión

Se establece una política de superficie mínima de Tauri v2 y una lista blanca estricta de dependencias para `apps/desktop` en la Etapa 2 (`F1a`):

1. **Superficie nativa nula en Etapa 2 (`F1a`):**
   - `apps/desktop/src-tauri/src/main.rs` se limita exclusivamente a inicializar la ventana principal (`tauri::Builder::default().run(tauri::generate_context!())`).
   - Queda prohibido registrar `#[tauri::command]`, `invoke_handler`, `.plugin(...)` o invocar subprocesos (`std::process::Command` / `tokio::process::Command`) en `main.rs` durante las Etapas 2 (`F1a`) y 3 (`F1b`). El canal de comunicación con el motor `.NET` de lectura no se abre hasta la Etapa 4 (`F2`).
   - `apps/desktop/src-tauri/capabilities/default.json` concede únicamente el permiso `"core:default"` sobre la ventana `["main"]`.
2. **Content Security Policy (CSP) cerrada y navegación local:**
   - `apps/desktop/src-tauri/tauri.conf.json` define una CSP obligatoria sin orígenes remotos ni `unsafe-eval`:
     `default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'`.
   - El enrutamiento del frontend utiliza `createHashRouter` para garantizar navegación determinista dentro del protocolo estático local del WebView sin depender de reescritura de rutas de servidor.
   - Queda prohibido en `apps/desktop/src/**` el uso de `fetch(`, `XMLHttpRequest`, `WebSocket`, `eval(`, `new Function`, `localStorage`, `sessionStorage`, `indexedDB` y estilos inline (`style={{...}}`).
3. **Lista cerrada de dependencias en Etapa 2 (`F1a`):**
   - `dependencies` en `apps/desktop/package.json`: únicamente `react`, `react-dom` y `react-router-dom`.
   - `devDependencies` en `apps/desktop/package.json`: únicamente `typescript`, `vite`, `@vitejs/plugin-react`, `@types/react`, `@types/react-dom`, `vitest`, `jsdom`, `@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event` y `@tauri-apps/cli`.
   - Queda prohibido instalar `@tauri-apps/api` o cualquier `@tauri-apps/plugin-*` mientras no exista el puente IPC de la Etapa 4 (`F2`).
   - En `apps/desktop/src-tauri/Cargo.toml`: únicamente `tauri` (v2) en `[dependencies]` y `tauri-build` (v2) en `[build-dependencies]`.
   - Ningún manifiesto (`package.json`, `Cargo.toml`) incluye el campo `license` hasta que el propietario incorpore el archivo `LICENSE` definitivo (`D2`).
4. **Verificación automatizada obligatoria:**
   - El test `apps/desktop/src/tests/architecture.test.ts` audita en cada ejecución de CI el cumplimiento exacto de esta política sobre `package.json`, `src/**`, `main.rs`, `capabilities/default.json` y `tauri.conf.json`.
   - Cualquier dependencia futura requiere actualizar [DEPENDENCIES.md](../process/DEPENDENCIES.md) y verificar compatibilidad de su licencia con `GPL-3.0-or-later`.

## Alternativas descartadas

1. **Instalar `@tauri-apps/api` y plugins oficiales (`plugin-shell`, `plugin-fs`, `plugin-store`) desde Etapa 2 "por conveniencia futura":**
   - *Motivo de descarte:* Viola el principio de mínimo privilegio ([ADR-0002](0002-privilege-separation.md)) y expone capacidades de sistema operativo al WebView antes de que exista siquiera el motor sin privilegios. Además, toda persistencia de producto debe residir en SQLite gobernado por el motor en Etapa 5 (`F3`, [ADR-0005](0005-storage-and-evidence-chain.md)), nunca en archivos sueltos ni en `localStorage` del navegador.
2. **Adoptar frameworks de componentes UI, librerías de iconos, Tailwind o `i18next` en Etapa 2:**
   - *Motivo de descarte:* Incrementan innecesariamente el árbol de dependencias transitivas para un shell sobrio con variables CSS propias y código propio de `i18n` (`apps/desktop/src/i18n/index.ts`, locales `es`/`en`) en memoria.
3. **Permitir estilos inline (`style={{...}}`) o relajar `style-src` en la CSP:**
   - *Motivo de descarte:* Debilita la defensa en profundidad de la CSP frente a inyección de marcado o estilos y dificulta la consistencia visual del tema oscuro.

## Consecuencias

- **Positivas:**
  - La superficie de ataque IPC y de red del shell en Etapa 2 (`F1a`) es nula y verificable automáticamente mediante `architecture.test.ts`.
  - Árbol de dependencias mínimo (3 dependencias de producción directas, todas bajo licencia `MIT` compatible con `GPL-3.0-or-later`).
  - Separación estricta entre el estado de sesión en memoria de Etapa 2 y la persistencia real en SQLite de Etapa 5 (`F3`).
- **Negativas / Coste asumido:**
  - En la Etapa 4 (`F2`), cuando se conecte el motor `.NET` de solo lectura, será necesario actualizar explícitamente este ADR (o emitir uno complementario) y ajustar `architecture.test.ts` para habilitar el único comando IPC tipado permitido.

## Límites y riesgos conocidos

- **Ausencia de compilación de Rust en entornos sin toolchain `cargo`:** Cuando las pruebas se ejecutan en un entorno ligero que solo dispone de Node.js y `pnpm`, `architecture.test.ts` valida estáticamente `main.rs`, `Cargo.toml`, `capabilities/default.json` y `tauri.conf.json`, pero la verificación del compilador de Rust (`cargo check`) depende del job `tauri` en `windows-latest` de GitHub Actions.
- **Dependencia de WebView2 en tiempo de ejecución en Windows:** Aunque el shell de Etapa 2 no invoca APIs de Windows directamente, Tauri v2 requiere el runtime de Microsoft Edge WebView2 presente en el sistema anfitrión (documentado en [RUNTIME_REQUIREMENTS.md](../architecture/RUNTIME_REQUIREMENTS.md)).

## Preguntas abiertas

- Definir en la Etapa 4 (`F2`) la firma exacta del único comando Tauri puente hacia el subproceso `.NET` sin privilegios (`stdio` JSON-RPC) y su capacidad asociada en `capabilities/`.
