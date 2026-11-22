# Verificación en PC del usuario — EVO BOTTLENECK DETECTOR

> Fecha: 2026-11-22.
> Estado de este documento: guía de verificación — NO es una evidencia
> verificada. Las verificaciones en este documento son checkpoints humanos.

Este documento describe cómo ejecutar la verificación local en una máquina Windows.
**NO es una evidencia**: solo define qué hacer y qué esperar. La evidencia real se
registra al completar este documento (fecha, máquina, resultados).

## Requisitos

- Windows 10/11 (x64) — CIM/WMI es Windows-only.
- .NET 10 SDK (LTS) — LTS de .NET 10.
- Git — para clonar el repositorio.

## Pasos

### 1. Clonar / actualizar

```powershell
git clone -b arena/f3b590dd-optimizer-ondemand https://github.com/EvoMaxPC/Optimizer-onDemand.git
# o, desde el repositorio existente:
# git pull origin arena/f3b590dd-optimizer-ondemand
cd Optimizer-onDemand
```

### 2. Verificar SDK

```powershell
dotnet --list-sdks
# Esperado: 10.0.x (LTS)
```

### 3. Compilar

```powershell
dotnet build EVO.sln -c Release
# Esperado: 5 proyectos compilan (EVO.Core, EVO.Collectors, EVO.Desktop, EVO.Core.Tests, EVO.Collectors.Tests)
```

### 4. Ejecutar tests

```powershell
dotnet test EVO.sln -c Release
# Esperado: todos los tests pasan (incluyendo 5 Facts de M15/M16)
```

### 5. Verificar checkpoints estructurales

```powershell
.\scripts\Validation\Verify-Foundation.ps1
# Esperado: PASS verde en consola; foundation-results.txt generado con éxito
```

### 6. Qué esperar por módulo (honestidad)

| Momento | Módulos | Qué se emite |
|---------|---------|--------------|
| Inventario (M01) | `SystemIntelligenceCollector` | Métricas de SO/CPU/memoria/disco/GPU |
| Mensuración (M02/M03/M04) | CPU/RAM/Storage | Uso de CPU, presión de RAM, latencia de disco |
| Auditorías (M06/M07/M08/M09/M12) | Procesos/servicios/arranque/drivers/eventos | Conteos de procesos, servicios, arranque, drivers, eventos 24h |
| Correlación (M14) | `BottleneckEngineCollector` | Hallazgos con confianza Warning/Probable (nunca Confirmed) |
| Informe (M15) | `ReportBuilder` | JSON de resumen del motor M14 |
| Monitorización (M16) | `LiveMonitorCollector` | Métricas temporales agregadas (avg/max/min) |

**Honestidad importante:** los módulos M14/M15/M16 **no tienen CLI/GUI**
(EVO.Desktop es un shell WPF mínimo). La verificación humana de estos módulos es
**vía tests xUnit** (`dotnet test`). Si querés una CLI end-to-end (`dotnet run`
con salida de métricas/informes), es una etapa de producto dedicada — NO está
incluida en este milestone. No es un requisito de verificación de CI.

### 7. Registrar evidencia

Al terminar cada check, documentar:

- Fecha y máquina (modelo, Windows, CPU, RAM)
- Resultado (build / test / Verify-Foundation)
- Hallazgos observados (o ausencia de ellos)

## Pendiente conocido

- M10 — Latency Lab (DPC/ISR/ETW) — **diferido** (ver `V0_5_STATUS.md`).
- D-016 (`Metric.StringValue`) — **propuesta**, sin aprobación.
