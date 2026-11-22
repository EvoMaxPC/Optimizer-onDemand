# REVISIÓN — ETAPA 01 FOUNDATION

> Revisión del checkpoint FOUNDATION (F01–F06). Cada punto se reporta como:
> **correcto** / **corregido** / **requiere decisión**.
> Fecha: 2026-11-22.

## a) Paquetes NuGet y versiones en los .csproj

**Estado: requiere decisión (sin nuget.org desde este entorno)**

- `Microsoft.NET.Test.Sdk` **17.12.0** — esperado compatible con tests .NET 10.
- `xunit` **2.9.2** — **xUnit v2** (no v3). Es la versión mayor estabilizada del ecosistema actual.
- `xunit.runner.visualstudio` **2.8.2** — complementa xUnit v2.
- `Microsoft.NET.Sdk` 10 (implícito) — el CI lo confirma vía `dotnet --list-sdks`.

⚠️ **No hay acceso a nuget.org desde este sandbox** (solo github.com / registry.npmjs.org / pypi.org / files.pythonhosted.org). Las versiones exactas *no están verificadas*; el CI (paso `dotnet restore`) lo confirmará.
**Acción: CI confirma; si falla, se corrige la versión en un commit nuevo.**

## b) `EVO.sln` — regenerar o mantener?

**Estado: justificado — se mantiene el manual**

`dotnet new sln` + `dotnet sln add` **no son ejecutables desde este sandbox** (no hay .NET SDK disponible). El `.sln` se generó a mano siguiendo el formato exacto de `dotnet` / Visual Studio (encabezados, `Project("{9A19103F-16F7-4668-BFE3-9A1E7F4F8D33}")`, secciones `SolutionConfigurationPlatforms`, `ProjectConfigurationPlatforms`). Se validó estáticamente:
- El encabezado y el formato `Format Version 12.00` son correctos.
- GUIDs bien formados (proj + global) para los 3 proyectos.
- El path de cada proyecto existe.
- `Debug|Any CPU` / `Release|Any CPU` cubiertos en ambas propiedades (ActiveCfg + Build.0).

El CI (`dotnet restore EVO.sln`, `dotnet build EVO.sln`) confirma la validez real del archivo. Si falla, se regenera con dotnet en PC Windows y se documenta.

## c) Rutas de config, logs y data — `AppContext.BaseDirectory`

**Estado: corregido**

- `AppSettings.LoadDefault(solutionRoot)` (raíz del repo) ❌ **ya no existe**.
- `AppSettings.LoadDefault()` → ahora resuelve `AppContext.BaseDirectory / config / appsettings.json` (portable, self-contained).
- `SimpleFileLogger` → resuelve rutas relativas contra `AppContext.BaseDirectory`; rutas absolutas sin cambios.
- `EVO.Desktop.csproj` → copia `config/appsettings.json` al output con `CopyToOutputDirectory="PreserveNewest"`, de modo que la publicación portable la lleva junto al ejecutable.
- Nada más se implementa aún (lógica de settings/logging no se usa en el shell): el Desktop sigue siendo shell mínimo.

## d) Exigencia de `readOnly=true` en `AppSettings`

**Estado: requiere decisión — PROPUESTA, no implementada**

Actualmente `AppSettings.Validate` bloquea cualquier config con `ReadOnly != true`. Esto bloquearía funciones futuras que requieren elevación explícita de permisos (el producto dice *"elevación solamente cuando una función lo necesite"*).

**PROPUESTA (no implementada):** cambiar la exigencia de *throw* por:
- Valor por defecto `ReadOnly = true` (conserva la seguridad por defecto).
- Si la config declara `ReadOnly = false`, registrar una advertencia explícita en el log (`SimpleFileLogger.Warning`) + advertencia visible en la UI/historial de operaciones, sin abortar el arranque.
- Función de elevación marcada explícitamente en el código de cada tarea Elevated (no existe aún).

**Pendiente de aprobar por el usuario.**

## e) `FindingConfidence`: "Normal" y "Warning" no son niveles de confianza

**Estado: requiere decisión — PROPUESTA, no implementada**

El modelo actual (D-013) clasifica el hallazgo con 5 estados: `Confirmed / Probable / Warning / Normal / InsufficientData`. Pero `Warning` y `Normal` no son niveles de confianza — son clasificaciones de severidad/estatus.

**PROPUESTA (no implementada) — separar dos conceptos:**
- **`Confidence`** (probabilidad de la causa): `Confirmed / Probable / InsufficientData` (con `Probable` reservado para correlación sin evidencia concluyente).
- **`FindingStatus`** (estatus comunicable): `Confirmed / Probable / Warning / Normal / InsufficientData` (lo que se muestra como mensaje al usuario).
- Los dos campos coexisten en `DiagnosticFinding` como `Confidence` + `Status` con un `Severity` de impacto.

**Pendiente de aprobar por el usuario. Consecuencia: D-013 queda PROPUESTA PENDIENTE DE APROBACIÓN.**

## f) `Publish-Portable.ps1` — single-file vs self-contained

**Estado: justificado**

La especificación dice "Ejecutable Windows x64. Publicación self-contained. Runtime incluido." — **una sola ejecutable para distribución portable**. Single-file es la implementación más literal de eso (un solo ejecutable, sin instalar .NET en destino, sin instalar .NET ni los componentes; todo en un paquete portable).

Trade-offs aceptados: startup más lento y tamaño mayor por single-file. Alternativa válida: self-contained sin single-file (~40–80 archivos, sin instalador). Esa alternativa es una **PROPUESTA** para que el usuario elija; hoy se mantiene single-file como lo pide la especificación.

`Publish-Portable.ps1` **no fue ejecutado** desde este sandbox (Linux). Debe ejecutarse en Windows (`scripts\Publish\Publish-Portable.ps1`) antes de cualquier release.

## g) `.gitkeep`

**Estado: documentado (D-015)**

Directorios `data/`, `logs/`, `reports/` se preservan en git con `.gitkeep` vacío; `.gitignore` ignora su contenido. Sin esto, la estructura portable no se preservaría en el paquete. Excepción documentada en `DECISIONS.md` (D-015) como PROPUESTA (no aprobada aún por el usuario).
