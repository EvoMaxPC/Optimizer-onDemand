# ETAPA 02 — M01 SYSTEM INTELLIGENCE (Inventario completo)

> Estado: 🟡 En progreso (ver §7).
> Fecha: 2026-11-22.

## 1. Objetivo

Inventario del sistema operativo, procesador, memoria, almacenamiento y GPU del
equipo Windows. Este es el primer módulo real de la arquitectura: M01 — System
Intelligence.

El módulo **no detecta anomalías todavía** (eso es M14 — Bottleneck Engine) ni
correlaciona evidencias. Produce solo métricas inventario, con formato y
timestamp consistentes.

Misión del módulo: **saber qué es el equipo**, con confianza alta en lo que
produce.

## 2. M01 — Alcance

**Dentro:**

- SO: caption, versión, build, arquitectura, RAM total (vía `Win32_OperatingSystem`).
- CPU: nombre, cores lógicos/físicos, reloj máximo (vía `Win32_Processor`).
- RAM: total (vía `Win32_OperatingSystem.TotalVisibleMemorySize`).
- Almacenamiento: tamaño y espacio libre por letra de disco (vía `Win32_LogicalDisk`).
- GPU: nombre y memoria de video (vía `Win32_VideoController`).
- `Metric` con `StringValue` opcional para hechos de inventario string.

**Fuera (no implementado en esta etapa):**

- Detección/análisis de anomalías (M14).
- Sistema de alertas por umbral.
- Recolección en vivo continua (eso es M16 — EVO Live).
- Inventario de servicios, drivers, startup y procesos (M06-M13).
- Informes y dashboard (M15 / V1.0).

## 3. Contratos usados

```
ICollector
  ModuleId: "M01"
  CollectAsync(CancellationToken) -> CollectResult
    ModuleId
    CollectedAt  (timestamp UTC)
    Metrics      (IReadOnlyList<Metric>)  ← M01 produce aquí los hechos de inventario
    Findings     (IReadOnlyList<DiagnosticFinding>)  ← vacío en esta etapa; M14 tendrá su propia lógica
```

## 4. Extender Metric — decisión D-016 (PROPUESTA)

`Metric` era solo numérico: `(Name, Unit, Value, Timestamp)`. Un inventario
completo necesita hechos string (ej. "Intel Core i7-12700H", "Windows 11 Pro").

**D-016 (PROPUESTA — pendiente de aprobación):** extender la métrica con un
campo `StringValue` opcional:

```csharp
public sealed record Metric(string Name, string Unit, double Value,
    DateTimeOffset Timestamp, string? StringValue = null);
```

- Dato numérico: `Value` = número, `StringValue` = null (sin cambios de uso).
- Dato string: `StringValue` = el string, `Value` = 0 (o 0.0).

**Nada más se cambia en los contratos:** `ICollector`, `CollectResult`,
`DiagnosticFinding`, `FindingConfidence` y `FindingStatus` son idénticos a F04.

## 5. Criterios de verificación (consistentes con ETAPA 01)

| Checkpoint | Resultado exigido | Estado |
|------------|-------------------|--------|
| E02-01 | Proyecto `EVO.Collectors` creado con `SystemIntelligenceCollector` M01 — compila | ⏳ |
| E02-02 | Tests xUnit de M01 escritos — pasan en CI (`windows-latest` — CIM real) | ⏳ |
| E02-03 | Doc/README actualizados y commiteados; PR #1 continúa | ⏳ |

**Nada pasa a "verificado" sin evidencia real** — ver CI run y artifact, no la
palabra del agente.

## 6. Decisiones de diseño concretas

- La recolección es **best-effort por sección**: si una consulta falla (driver
  raro, máquina sin GPU, inventario no disponible), se omite esa sección sin
  abortar todo. Esto es M01 (inventario), no M14 (diagnóstico de anomalías).
- CIM vía `System.Management` (Windows-only por contrato de la especificación).
- Las métricas string pueden ser null indefinidas: solo se emiten hechos
  presentes (`if (os["Caption"] != null)`).

## 7. Estado del módulo

🟡 En progreso. Pendiente: compilación real, CI verde, M06+.
