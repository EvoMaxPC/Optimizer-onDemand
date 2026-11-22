# Política e Inventario de Dependencias (`docs/process/DEPENDENCIES.md`)

> **Nota de procedencia:** Escrito a mano; contrastado con `pnpm-lock.yaml` el 2026-11-22. La internacionalización (`i18n`) es código propio (`apps/desktop/src/i18n/index.ts`, locales `es`/`en`), sin `i18next`.

Este documento registra las dependencias autorizadas del monorepo en la Etapa 2 (`F1a`), el método de resolución de versiones y la auditoría de compatibilidad de licencias frente a la licencia prevista del proyecto (`GPL-3.0-or-later`, decisión `D2`). La política técnica está gobernada por [ADR-0018](../adr/0018-tauri-surface-and-frontend-dependency-policy.md).

---

## 1. Reglas generales de gestión de dependencias

1. **Cero dependencias en la raíz del monorepo:** El archivo [`../../package.json`](../../package.json) raíz solo define metadatos de workspace y scripts de orquestación (`check`, `check:docs`, `dev:desktop`, `build:desktop`, `test:desktop`, `typecheck:desktop`). Verificado por `R3-PACKAGE-JSON` en [`../../tools/check-docs.mjs`](../../tools/check-docs.mjs).
2. **Omisión temporal del campo `license` (`D2`):** Ni [`../../package.json`](../../package.json), ni [`../../apps/desktop/package.json`](../../apps/desktop/package.json), ni [`../../apps/desktop/src-tauri/Cargo.toml`](../../apps/desktop/src-tauri/Cargo.toml) incluyen el campo `license` hasta que el propietario incorpore el archivo `LICENSE` oficial.
3. **Resolución exclusiva mediante gestor de paquetes:** Las versiones de [`../../apps/desktop/package.json`](../../apps/desktop/package.json) y [`../../pnpm-lock.yaml`](../../pnpm-lock.yaml) han sido resueltas y generadas por `pnpm` (`v12.21.0` vía Corepack) contra el registro oficial de npm.
4. **Verificación en tests automatizados:** El test [`../../apps/desktop/src/tests/architecture.test.ts`](../../apps/desktop/src/tests/architecture.test.ts) falla si se añade o elimina cualquier paquete fuera de la lista autorizada.

---

## 2. Dependencias directas de `apps/desktop/package.json` (Etapa 2 / `F1a`)

### 2.1 Dependencias de producción (`dependencies`)

| Paquete | Rango resuelto | Licencia | Propósito en Etapa 2 (`F1a`) |
|---|---|---|---|
| `react` | `^19.3.1` | `MIT` | Renderizado declarativo de componentes del shell e internacionalización en memoria (`Context`). |
| `react-dom` | `^19.3.1` | `MIT` | Montaje del árbol React en el DOM del WebView (`createRoot`). |
| `react-router-dom` | `^7.18.6` | `MIT` | Enrutamiento local determinista mediante `createHashRouter` (`/`, `/history`, `/settings`, `*`). |

### 2.2 Dependencias de desarrollo (`devDependencies`)

| Paquete | Rango resuelto | Licencia | Propósito en Etapa 2 (`F1a`) |
|---|---|---|---|
| `typescript` | `^7.0.2` | `Apache-2.0` | Verificación estática de tipos (`tsc --noEmit` y `tsc -b`). |
| `vite` | `^8.3.8` | `MIT` | Servidor de desarrollo y empaquetador estático del frontend. |
| `@vitejs/plugin-react` | `^6.3.0` | `MIT` | Integración de transformación JSX/TSX en Vite. |
| `@types/react` | `^19.3.0` | `MIT` | Definiciones de tipos TypeScript para `react`. |
| `@types/react-dom` | `^19.3.0` | `MIT` | Definiciones de tipos TypeScript para `react-dom`. |
| `vitest` | `^5.1.1` | `MIT` | Ejecutor de pruebas unitarias, de integración y de arquitectura. |
| `jsdom` | `^31.0.1` | `MIT` | Entorno DOM simulado para pruebas de interfaz en Node.js. |
| `@testing-library/react` | `^16.3.3` | `MIT` | Renderizado y consultas accesibles por rol en pruebas de componentes. |
| `@testing-library/jest-dom` | `^8.0.0` | `MIT` | Aserciones semánticas del DOM (`toBeInTheDocument`, etc.) para Vitest. |
| `@testing-library/user-event` | `^14.6.13` | `MIT` | Interacción realista de usuario (clics de navegación y cambio de idioma) en tests. |
| `@tauri-apps/cli` | `^2.12.6` | `Apache-2.0 OR MIT` | CLI de construcción de Tauri v2 y generación de iconos desde `evo-icon.svg`. |

---

## 3. Dependencias de `apps/desktop/src-tauri/Cargo.toml` (Etapa 2 / `F1a`)

| Crate | Sección | Versión | Licencia | Propósito en Etapa 2 (`F1a`) |
|---|---|---|---|---|
| `tauri` | `[dependencies]` | `2` (`features = []`) | `Apache-2.0 OR MIT` | Contenedor de ventana nativa mínima sin comandos ni plugins registrados. |
| `tauri-build` | `[build-dependencies]` | `2` (`features = []`) | `Apache-2.0 OR MIT` | Script de construcción (`build.rs`) del contexto estático de Tauri v2. |

> **Nota sobre `Cargo.lock`:** En el entorno donde se ejecutó la Etapa 2 no está instalado el toolchain de Rust (`cargo` / `rustc`), por lo que no se ha generado `apps/desktop/src-tauri/Cargo.lock` para evitar inventar un lockfile manual. Su generación y verificación queda delegada al job `tauri` en `windows-latest` de GitHub Actions y registrada en [DECISIONS_PENDING.md](DECISIONS_PENDING.md).

---

## 4. Paquetes explícitamente prohibidos en Etapa 2 (`F1a`)

De acuerdo con [ADR-0018](../adr/0018-tauri-surface-and-frontend-dependency-policy.md), quedan prohibidos en esta etapa:

- `@tauri-apps/api` y cualquier plugin `@tauri-apps/plugin-*` (`shell`, `fs`, `process`, `http`, `dialog`, `store`, etc.).
- Crates `serde`, `serde_json`, `tokio`, `windows-sys` (no existen comandos IPC ni llamadas nativas hasta la Etapa 4 / `F2`).
- Librerías de iconos, kits de componentes UI, CSS-in-JS, Tailwind, librerías de gráficas, gestores de estado externos, `i18next`, `axios`, `zod` o `ajv`.

---

## 5. Auditoría de compatibilidad de licencias transitivas con `GPL-3.0-or-later`

Ejecutado `pnpm --filter @evo/desktop licenses list` sobre el árbol completo instalado (`pnpm-lock.yaml`):

| Licencia SPDX | Paquetes en el árbol (producción + desarrollo) | Compatibilidad con `GPL-3.0-or-later` |
|---|---|---|
| `MIT` | 85 paquetes (incluye el 100% de las dependencias de producción: `react`, `react-dom`, `react-router-dom`, `react-router`, `scheduler`, `cookie`, `set-cookie-parser`) | Compatible (permisiva laxa). |
| `Apache-2.0` | 7 paquetes (solo desarrollo: `typescript`, `@typescript/typescript-linux-x64`, `aria-query`, `detect-libc`, `expect-type`, `xml-name-validator`) | Compatible con `GPL-3.0-or-later` (la versión 3 de la GPL es expresamente compatible con Apache 2.0). |
| `Apache-2.0 OR MIT` | 2 paquetes (`@tauri-apps/cli`, `@tauri-apps/cli-linux-x64-gnu`) | Compatible bajo ambos términos de la licencia dual. |
| `BSD-2-Clause` | 2 paquetes (solo desarrollo: `entities`, `webidl-conversions`) | Compatible (permisiva BSD). |
| `BSD-3-Clause` | 2 paquetes (solo desarrollo: `source-map-js`, `tough-cookie`) | Compatible (permisiva BSD modificada). |
| `ISC` | 2 paquetes (solo desarrollo: `picocolors`, `saxes`) | Compatible (equivalente funcional a MIT/BSD simplificada). |
| `BlueOak-1.0.0` | 1 paquete (solo desarrollo: `lru-cache`) | Compatible (licencia permisiva moderna reconocida como compatible con GPLv3). |
| `MPL-2.0` | 2 paquetes (solo desarrollo: `lightningcss`, `lightningcss-linux-x64-gnu`) | Compatible con `GPL-3.0-or-later` (Sección 3.3 de MPL 2.0 permite combinación con GPLv3). |

**Conclusión de la auditoría:** No existe ninguna dependencia directa ni transitiva con licencia propietaria, `GPL-2.0-only`, `SSPL` ni cláusulas incompatibles con `GPL-3.0-or-later`.
