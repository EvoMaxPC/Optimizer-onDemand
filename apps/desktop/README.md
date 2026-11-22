# Aplicación de Escritorio (`apps/desktop`)

## Estado actual: Etapa 2 (`F1a`) — Shell Tauri v2 + React sin motor

Este paquete implementa el shell visual de la Etapa 2 (`F1a`) conforme a [CHECKPOINTS.md](../../docs/process/CHECKPOINTS.md) y [ADR-0018](../../docs/adr/0018-tauri-surface-and-frontend-dependency-policy.md).

- **Qué incluye:**
  - Enrutador local con `createHashRouter` ([`src/app/routes.tsx`](src/app/routes.tsx)) con 3 rutas reales (`/`, `/history`, `/settings`) y redirección comodín `*` a `/`.
  - Layout con barra lateral ([`src/components/layout/Sidebar.tsx`](src/components/layout/Sidebar.tsx)) y barra de estado accesible `role="status"` ([`src/components/layout/StatusBar.tsx`](src/components/layout/StatusBar.tsx)) que muestra permanentemente `"Motor no conectado"` y `"Esta versión no puede modificar Windows"`.
  - Vista de resumen ([`src/pages/OverviewPage.tsx`](src/pages/OverviewPage.tsx)) con el lema innegociable de [PRINCIPLES.md](../../docs/PRINCIPLES.md) y los 5 pilares arquitectónicos.
  - Vista de historial ([`src/pages/HistoryPage.tsx`](src/pages/HistoryPage.tsx)) con estado vacío honesto indicando que SQLite y la cadena de evidencias llegan en la Etapa 5 (`F3`).
  - Vista de ajustes ([`src/pages/SettingsPage.tsx`](src/pages/SettingsPage.tsx)) con selector de idioma `es` / `en` funcional en memoria ([`src/i18n/index.ts`](src/i18n/index.ts)) y estado técnico de solo lectura.
  - Contenedor Tauri v2 mínimo ([`src-tauri/src/main.rs`](src-tauri/src/main.rs), [`src-tauri/tauri.conf.json`](src-tauri/tauri.conf.json), [`src-tauri/capabilities/default.json`](src-tauri/capabilities/default.json)) sin comandos, sin plugins y con CSP estricta sin orígenes remotos.
- **Qué NO incluye en esta etapa:**
  - No existe motor `.NET` conectado (previsto para Etapa 4 / `F2`).
  - No muestra las tarjetas de los 6 módulos especialistas (previstas como manifiesto estático honesto en Etapa 3 / `F1b`).
  - No lee ni modifica el sistema operativo Windows, ni contiene métricas simuladas ni botones de acción.

## Comandos principales (desde la raíz del monorepo)

```bash
pnpm --filter @evo/desktop typecheck
pnpm --filter @evo/desktop test
pnpm --filter @evo/desktop build
```

## Pruebas automatizadas (`src/tests/`)

- [`src/tests/app.test.tsx`](src/tests/app.test.tsx): Verifica renderizado del lema, los 5 pilares, la barra de estado (`role="status"`), la navegación entre las 3 vistas, ausencia de botones/unidades simuladas (`MB`, `GB`, `%`), cambio de idioma `es` → `en` en memoria y redirección de rutas inexistentes.
- [`src/tests/i18n.test.ts`](src/tests/i18n.test.ts): Verifica paridad recursiva de claves entre [`src/i18n/locales/es.json`](src/i18n/locales/es.json) y [`src/i18n/locales/en.json`](src/i18n/locales/en.json), ausencia de cadenas vacías y ausencia de términos o llamadas a la acción prohibidas.
- [`src/tests/architecture.test.ts`](src/tests/architecture.test.ts): Audita las dependencias de [`package.json`](package.json), la ausencia de APIs prohibidas y estilos inline en `src/**`, la superficie nula de [`src-tauri/src/main.rs`](src-tauri/src/main.rs), los permisos mínimos de [`src-tauri/capabilities/default.json`](src-tauri/capabilities/default.json) y la CSP de [`src-tauri/tauri.conf.json`](src-tauri/tauri.conf.json).
