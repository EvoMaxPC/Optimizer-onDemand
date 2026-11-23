# EVO BOTTLENECK DETECTOR

**EVO LABS** — Windows Performance Intelligence.

Identificamos qué está perjudicando el rendimiento de un equipo Windows,
localizamos las evidencias, establecemos causas probables y proporcionamos
soluciones técnicamente justificadas.

> **NO ADIVINAMOS. INVESTIGAMOS.**

## Identidad

| Atributo   | Valor                                                        |
|------------|--------------------------------------------------------------|
| Resumen    | Sistema de reconocimiento, auditoría, monitorización y diagnóstico inteligente de Windows |
| Lenguaje   | C# (.NET 10 LTS)                                              |
| UI         | WPF — escritorio nativo (MVVM)                                |
| Colección  | PowerShell 5.1, CIM/WMI/PnP, Performance Counters, ETW         |
| Persistencia | SQLite (historial) + JSON (intercambio de datos e informes) |
| Distribución | Portable self-contained, Windows x64                        |
| Pruebas    | xUnit                                                         |
| Identidad visual | Negro profesional, amarillo EVO, blanco y cyan          |

## Arquitectura

Híbrida y modular, un único proceso con interfaces desacopladas; recolectores
aíslados futuros mediante procesos auxiliares y Named Pipes. Las decisiones
completas están registradas en [`docs/Architecture/DECISIONS.md`](docs/Architecture/DECISIONS.md).

Un único proceso **sin** servicios residentes obligatorios y **sin** conexión a
servidores externos; diagnóstico **solo lectura** por defecto.

## Estado del proyecto

**V1.0 — CERRADA (CI_RUNNER); verificación en PC del usuario pendiente**

| Milestone | Módulos | Estado |
|-----------|---------|--------|
| V1.0 | M01, M02, M03, M04, M05, M06, M07, M08, M09, M11, M12, M14, M15, M16 | ✅ CI_RUNNER |
| V1.0 | M10 — Latency Lab (DPC/ISR/ETW) | ⏳ Diferido explícitamente |
| V1.0 | M13 — Background Activity | ⏳ NO IMPLEMENTADO |
| V1.0 | EVO.Desktop (dashboard WPF) | ⏳ NO VERIFICADO (work-in-progress) |

**CI_RUNNER, no PC del usuario.** La verificación en PC del usuario sigue pendiente. Ver [`docs/Technical/V1_0_STATUS.md`](docs/Technical/V1_0_STATUS.md).

Guía de verificación en PC: [`docs/Technical/PC_VERIFICATION.md`](docs/Technical/PC_VERIFICATION.md).

## ETAPA 02 — M01 System Intelligence

Primero módulo real de la arquitectura: inventario completo del sistema Windows
(OS, CPU, RAM, almacenamiento, GPU) mediante CIM / WMI.

Ver [`docs/Technical/ETAPA_02.md`](docs/Technical/ETAPA_02.md).

Proyecto: `src/EVO.Collectors/` (`SystemIntelligenceCollector`, módulo M01).

## Estructura del repositorio

```
EVO.sln
├── config/                  # Configuración real (appsettings.json)
├── docs/
│   ├── Architecture/DECISIONS.md        # Registro de decisiones
│   └── Technical/
│       ├── TECHNICAL_SPECIFICATION.md   # Especificación definitiva
│       └── FOUNDATION.md                # Checkpoints verificados (F01-F06)
├── scripts/
│   ├── Build/              # Scripts de compilación
│   ├── Publish/            # Script de publicación portable
│   └── Validation/         # Verificación de checkpoints
├── data/, logs/, reports/  # Artefactos locales (ignorados por git)
├── src/
│   ├── EVO.Core/          # Contratos, modelos, configuración, logging (F04/F05)
│   └── EVO.Desktop/       # Shell WPF (shell mínimo, identidad dark)
└── tests/
    └── EVO.Core.Tests/    # Tests xUnit de EVO.Core
```

## Cómo empezar (Windows)

Requisitos: Windows x64, .NET 10 SDK, PowerShell 5.1+.

```powershell
# Compilar
scripts\Build\Build-Solution.ps1

# Verificar FOUNDATION (F01-F05, chequeos de F06)
scripts\Validation\Verify-Foundation.ps1

# Publicar portable (self-contained, win-x64)
scripts\Publish\Publish-Portable.ps1
```

## Reglas permanentes

- Solo lectura por defecto.
- Sin telemetría propia ni envío externo.
- Sin modificaciones automáticas de registro, drivers, archivos ni servicios.
- Sin optimizaciones ocultas.
- Elevación solamente cuando una función la necesite.
- Registro de las operaciones realizadas por EVO.

## Licencia y créditos

Producto **EVO LABS**. Todos los derechos reservados.
