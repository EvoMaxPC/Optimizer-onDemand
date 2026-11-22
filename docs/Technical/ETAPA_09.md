# ETAPA 09 — M09 DRIVER INTELLIGENCE (Auditoría de controladores)

> Estado: 🟡 En progreso (ver §7).
> Fecha: 2026-11-22.

## 1. Objetivo

Auditar el inventario de controladores de Windows: total, estado (running/stopped),
modo de inicio (auto/manual/disabled) e integridad (status degrada/error). M09 es
una **auditoría**, no un detector de fallos. NO declara causas ni cuellos de botella.

**Alcance de la métrica:** auditoría instantánea del inventario de drivers.

## 2. M09 — Alcance

**Dentro (vía `Win32_SystemDriver`):**

- `drivers.count` (count) — total de controladores.
- `drivers.running` / `drivers.stopped` (count) — estado.
- `drivers.auto_start` / `drivers.manual` / `drivers.disabled` (count) — modo de inicio.
- `drivers.degraded` (count) — drivers con status en {Error, Degraded, Pred Fail, Stressed, NonRecover, No Contact, Lost Comm}.

**Fuera (no implementado en esta etapa):**

- Hallazgos/diagnósticos (M14 — correlación).
- Detalle por driver (nombre, ruta — dashboard, M15).
- Firmware/hardware de drivers específicos (M09 lo cubre solo como inventario de servicio).

## 3. Contratos usados

```
ICollector
  ModuleId: "M09"
  CollectAsync(CancellationToken) -> CollectResult
    Metrics   ← drivers.* (best-effort, agregado)
    Findings  ← vacío (M09 es auditoría; ver §4)
```

## 4. Modelo de diagnóstico del hallazgo (M09: ninguno)

M09 produce solo métricas de auditoría. Los verdicts con confianza honesta pertenecen
a M14 (correlación). M09 aporta la evidencia que M14 consume.

## 5. Criterios de verificación (consistentes con ETAPA 01–08)

| Checkpoint | Resultado exigido | Estado |
|------------|-------------------|--------|
| E09-01 | `EVO.Collectors.Drivers.DriverAnalyzerCollector` (M09) creado y compila | ⏳ |
| E09-02 | Tests xUnit de M09 — pasan en CI (`windows-latest`, CIM real) | ⏳ |
| E09-03 | Docs commiteados; PR #1 continúa | ⏳ |

## 6. Decisiones de diseño concretas

- **Instantánea, no serie.**
- **Best-effort:** si la consulta falla, la sección se omite sin abortar.
- **Agregado, no detalle:** los drivers se cuentan por estado/iniciar/status; el detalle por driver es trabajo del dashboard (M15).
- **Sin hallazgos:** auditoría de integridad; los verdicts son de M14.
- **Sin métricas string.**

## 7. Estado del módulo

🟡 En progreso. Pendiente: CI verde.
