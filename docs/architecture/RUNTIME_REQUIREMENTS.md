# Requisitos de Tiempo de Ejecución y Entorno (`docs/architecture/RUNTIME_REQUIREMENTS.md`)

Este documento especifica los requisitos del sistema operativo anfitrión, componentes de tiempo de ejecución y herramientas de construcción para el monorepo de **EVO** en la Etapa 2 (`F1a`) y su evolución prevista hasta la Etapa 10 (`F7`).

---

## 1. Sistemas operativos soportados (`D1`)

Por decisión del propietario ([DECISIONS_PENDING.md](../process/DECISIONS_PENDING.md), `D-02`), EVO soporta exclusivamente ediciones de escritorio x64 de:

- **Windows 10 22H2** (número exacto de compilación mínima `19045` y fecha de fin de soporte: `PENDIENTE: fuente` — requiere cita documental oficial de Microsoft conforme a [ADR-0015](../adr/0015-source-and-reference-policy.md) antes de fijarse como filtro activo del motor en la Etapa 6 / `F4a`).
- **Windows 11** (versiones `22H2` / `23H2` / `24H2`, números de compilación `22621`, `22631`, `26100` y fechas de ciclo de vida: `PENDIENTE: fuente` — requiere cita documental oficial de Microsoft conforme a [ADR-0015](../adr/0015-source-and-reference-policy.md)).

### Exclusiones explícitas

- No se soportan versiones de Windows 10 anteriores a `22H2`, ediciones Windows Server, Windows LTSC sin verificación específica ni sistemas operativos distintos de Windows en producción.
- En la **Etapa 2 (`F1a`)**, el shell no lee ni modifica el sistema operativo anfitrión; la detección real de `SystemProfile` llega en la Etapa 4 (`F2`).

---

## 2. Requisitos en tiempo de ejecución por etapa

| Componente | Etapa en que se vuelve activo | Estado en Etapa 2 (`F1a`) | Estrategia de distribución prevista |
|---|---|---|---|
| **Microsoft Edge WebView2 Runtime** | Etapa 2 (`F1a`, ventana Tauri v2) | Requerido para abrir el contenedor nativo Tauri v2 en Windows. | Preinstalado en Windows 11 y en instalaciones actualizadas de Windows 10 22H2; estrategia de bootstrapper/instalador offline definida en la Etapa 10 (`F7`, [DECISIONS_PENDING.md](../process/DECISIONS_PENDING.md)). |
| **Motor sin privilegios (`Evo.Engine.Host`, .NET 8)** | Etapa 4 (`F2`, lectura e inventario) | No implementado en Etapa 2. | Decisión entre publicación `self-contained` frente a `framework-dependent` abierta hasta la Etapa 10 (`F7`, [DECISIONS_PENDING.md](../process/DECISIONS_PENDING.md)). |
| **Motor de persistencia SQLite (modo WAL)** | Etapa 5 (`F3`, historial y evidencias) | No implementado en Etapa 2 (el idioma de sesión vive en memoria). | Biblioteca nativa embebida junto con `Evo.Engine.Host` en `%LOCALAPPDATA%\Evo\`. |
| **Ejecutor elevado bajo demanda (`Evo.Engine.Elevated`)** | Etapa 8 (`F5`, canal Named Pipe + `ApprovalToken`) | No implementado en Etapa 2. | Binario firmado ubicado en directorio protegido con ACL de administrador (`%ProgramFiles%\Evo\`). |

---

## 3. Requisitos del entorno de desarrollo y CI

| Herramienta | Versión requerida | Uso en el monorepo |
|---|---|---|
| **Node.js** | `^22.22.2 \|\| ^24.15.0 \|\| >=26.0.0` (CI fijado en Node 22) | Ejecución de `tools/check-docs.mjs`, Vite, TypeScript y Vitest. |
| **pnpm** | `12.21.0` (declarado en `packageManager` de `package.json` vía Corepack) | Gestión determinista del workspace (`pnpm-workspace.yaml` y `pnpm-lock.yaml`). |
| **Rust (`rustc` / `cargo`)** | `>= 1.77.2` (target `x86_64-pc-windows-msvc` en Windows) | Compilación del contenedor nativo en `apps/desktop/src-tauri` (`cargo check --manifest-path apps/desktop/src-tauri/Cargo.toml`). |
| **Visual Studio C++ Build Tools & WebView2 SDK** | Windows 10/11 SDK + MSVC v143 | Requeridos por `tauri` y `tauri-build` al compilar en `windows-latest`. |
| **.NET SDK** | `.NET 8.0 LTS` (a partir de Etapa 4 / `F2`) | No requerido en Etapa 2 (`F1a`); necesario cuando se inicialice `engine/` en la Etapa 4 (`F2`). |

---

## 4. Garantías de aislamiento en la Etapa 2 (`F1a`)

De acuerdo con [ADR-0018](../adr/0018-tauri-surface-and-frontend-dependency-policy.md):

1. El proceso Tauri (`evo-desktop`) se ejecuta siempre con los privilegios estándar del usuario interactivo; no solicita elevación UAC.
2. No existe subproceso hijo ni comunicación por `stdio` o Named Pipes.
3. El WebView aplica una Content Security Policy (CSP) sin orígenes remotos (`default-src 'self'`), impidiendo conexiones salientes (`connect-src 'self'`) y carga de recursos externos.
