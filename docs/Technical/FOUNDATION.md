# ETAPA 01 — FOUNDATION

> Registro verificable de checkpoints F01–F06.
> Toda afirmación aquí es comprobable contra los archivos del repositorio y
> contra el CI (enlace al run cuando exista evidencia).

## Contexto del entorno

- Fecha: 2026-11-22.
- Entorno de creación de este checkpoint: Linux (sandbox). El producto objetivo es
  **Windows x64 + .NET 10 LTS + WPF**. Por tanto:
  - Los checks infraestructurales que requieren Windows/.NET SDK/PowerShell son
    **pendientes** y están marcados como tales, sin ocultarlos.
  - Verificaciones de archivos, estructuras, XML/JSON/sln son **estáticas** y están
    listadas por checkpoint.
  - **Nada** pasa a estado "verificado" sin evidencia real en el CI (enlaces de run
    en la tabla) o en la PC del usuario.

## Niveles de estado (sin sinonimia vaga)

| Nivel | Significado |
|-------|-------------|
| GENERADO | El archivo/código existe, está escrito, no verificado |
| VERIFICADO ESTÁTICAMENTE | Validación estática (XML/JSON/sln parseables, GUIDs válidos, paths existen, código revisado) |
| VERIFICADO EN CI | Verificado por el CI con enlace al run de GitHub Actions |
| VERIFICADO EN PC DEL USUARIO | Verificación ejecutada en la PC real del usuario |

## Checkpoint F01 — Entorno y estructura verificados

| Comprobación | Estado |
|--------|--------|
| Carpetas `src/`, `tests/`, `scripts/`, `data/`, `logs/`, `reports/`, `config/`, `docs/` | GENERADO + VERIFICADO ESTÁTICAMENTE |
| `docs/Architecture/DECISIONS.md` existe | GENERADO + VERIFICADO ESTÁTICAMENTE |
| Estructura de destino alineada con PARTE II | VERIFICADO ESTÁTICAMENTE (con nota de graduación D-012) |
| PowerShell / Windows (`Get-CimInstance`) | ⛔ Pendiente — requiere PC Windows |
| .NET SDK .NET 10 (`dotnet --list-sdks`) | ⛔ Pendiente — verificado indirectamente por el CI (enlace al run) |

**Estado consolidado F01: VERIFICADO EN CI** — estructura, decisiones y config verificados por CI.
**VERIFICADO EN CI:** https://github.com/EvoMaxPC/Optimizer-onDemand/actions/runs/44981262446
**Pendiente de PC del usuario:** validación de infraestructura PowerShell (script Windows).

## Checkpoint F02 — Solución y proyectos creados

| Comprobación | Estado |
|--------|--------|
| `EVO.sln` creada con 3 proyectos | GENERADO + VERIFICADO ESTÁTICAMENTE |
| Formatos y GUIDs validados (script de validación) | VERIFICADO ESTÁTICAMENTE |
| `EVO.Core.csproj`, `EVO.Desktop.csproj`, `EVO.Core.Tests.csproj` | GENERADO |
| Archivos sin placeholders/archivos ficticios | VERIFICADO ESTÁTICAMENTE |
| Proyectos diferidos (`EVO.Collectors`, `EVO.Data`, `EVO.Reports`, `EVO.PowerShell`) | ⏭ diferidos (D-012) |

**Estado consolidado F02: VERIFICADO EN CI** — restore y build (Release) OK.
**VERIFICADO EN CI:** https://github.com/EvoMaxPC/Optimizer-onDemand/actions/runs/44981262446

## Checkpoint F03 — Referencias y dependencias correctas

| Comprobación | Estado |
|--------|--------|
| `EVO.Desktop` → `EVO.Core` (solo) | VERIFICADO ESTÁTICAMENTE |
| `EVO.Core.Tests` → `EVO.Core` | VERIFICADO ESTÁTICAMENTE |
| `EVO.Core` sin dependencias externas | VERIFICADO ESTÁTICAMENTE |
| Sin ciclos de referencia | VERIFICADO ESTÁTICAMENTE |

**Estado consolidado F03: VERIFICADO EN CI** — referencias correctas (build Release OK).
**VERIFICADO EN CI:** https://github.com/EvoMaxPC/Optimizer-onDemand/actions/runs/44981262446

## Checkpoint F04 — Contratos y modelos implementados

| Archivo | Contenido | Estado |
|--------|-----------|--------|
| `src/EVO.Core/Contracts/ICollector.cs` | `ICollector` + `CollectResult` | GENERADO |
| `src/EVO.Core/Contracts/Metric.cs` | `Metric` | GENERADO |
| `src/EVO.Core/Contracts/Anomaly.cs` | `Anomaly` | GENERADO |
| `src/EVO.Core/Findings/DiagnosticFinding.cs` | Modelo completo | GENERADO |
| `src/EVO.Core/Findings/Evidence.cs` | `Evidence` | GENERADO |
| `src/EVO.Core/Findings/FindingConfidence.cs` | 5 estados | GENERADO |
| `src/EVO.Core/Findings/Severity.cs` | 5 niveles | GENERADO |

**Estado consolidado F04: VERIFICADO EN CI** — contratos/modelos compilan y sus tests pasan en CI.
**VERIFICADO EN CI:** https://github.com/EvoMaxPC/Optimizer-onDemand/actions/runs/44981262446

## Checkpoint F05 — Configuración y logging funcionales

| Archivo | Contenido | Estado |
|--------|-----------|--------|
| `config/appsettings.json` | Configuración real | GENERADO |
| `src/EVO.Core/Configuration/AppSettings.cs` | Carga + validación; `LoadDefault()` resuelve contra `AppContext.BaseDirectory` | GENERADO |
| `src/EVO.Core/Logging/SimpleFileLogger.cs` | Logger de archivo thread-safe; rutas relativas resueltas contra `AppContext.BaseDirectory` | GENERADO |
| `src/EVO.Core/Logging/LogLevel.cs` | enum niveles | GENERADO |
| `src/EVO.Desktop/EVO.Desktop.csproj` | Copia `config/appsettings.json` al output (publish portable) | GENERADO |

**Estado consolidado F05: VERIFICADO EN CI** — AppSettings carga/valida y SimpleFileLogger loggea (tests xUnit verdes en CI).
**VERIFICADO EN CI:** https://github.com/EvoMaxPC/Optimizer-onDemand/actions/runs/44981262446

## Checkpoint F06 — Pruebas aprobadas y compilación exitosa

| Comprobación | Estado |
|--------|--------|
| 12 tests xUnit escritos (`DiagnosticFindingTests`, `CollectResultTests`/`Metric/Anomaly`, `AppSettingsTests`, `SimpleFileLoggerTests`) | GENERADO |
| `dotnet build -c Release` | ⛔ Pendiente de CI |
| `dotnet test` (trx) | ⛔ Pendiente de CI |
| Artefactos .trx subidos por CI | ⛔ Pendiente de CI |

**Estado consolidado F06: VERIFICADO EN CI** — build Release, 12 tests xUnit (trx) y verificación PowerShell 5.1 pasan en CI.
**VERIFICADO EN CI:** https://github.com/EvoMaxPC/Optimizer-onDemand/actions/runs/44981262446
**CI_RUNNER, no PC del usuario** — PC del usuario pendiente (F06 en su entorno).

## Criterios de cierre

- **F06 aprobado:** ✅ CI verde (build + test) con enlace al run · ⏳ PC del usuario pendiente.
- **F01–F05:** VERIFICADO EN CI (enlace al run); VERIFICADO EN PC DEL USUARIO pendiente.
- **F06:** VERIFICADO EN CI; VERIFICADO EN PC DEL USUARIO pendiente.

**ETAPA 01 FOUNDATION — F01–F06 VERIFICADOS EN CI (Run: https://github.com/EvoMaxPC/Optimizer-onDemand/actions/runs/44981262446). PC DEL USUARIO pendiente.**
