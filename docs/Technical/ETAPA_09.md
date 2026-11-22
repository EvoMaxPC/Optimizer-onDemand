# ETAPA 09 — M09 DRIVER INTELLIGENCE (Auditoría de controladores)

> Estado: 🟢 En progreso — compila y pasa CI (run 44988109674, CI_RUNNER). Tests de CIM corren en `windows-latest`.
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
| E09-01 | `EVO.Collectors.Drivers.DriverAnalyzerCollector` (M09) creado y compila | ✅ CI_RUNNER (run 44988109674) |
| E09-02 | Tests xUnit de M09 (5 Facts) — pasan en CI (`windows-latest`, CIM real) | ✅ CI_RUNNER (run 44988109674) — workflow verde; contadores exactos no citados (host de .trx bloqueado en este sandbox) |
| E09-03 | Docs commiteados; PR #1 continúa | ✅ CI_RUNNER (run 44988109674) |

## 6. Decisiones de diseño concretas

- **Instantánea, no serie.**
- **Best-effort:** si la consulta falla, la sección se omite sin abortar.
- **Agregado, no detalle:** los drivers se cuentan por estado/iniciar/status; el detalle por driver es trabajo del dashboard (M15).
- **Sin hallazgos:** auditoría de integridad; los verdicts son de M14.
- **Sin métricas string.**

## 7. Estado del módulo

🟢 **CI verde (CI_RUNNER)** — run 44988109674, Windows runner.

- Compilación Release: OK (EVO.sln con 5 proyectos).
- `dotnet test`: OK (5 Facts de M09 + los existentes de M01/M02/M03/M04/M06/M07/M08).
- `Verify-Foundation.ps1`: OK (incluye la extensión de M09).

**Pendiente:** E09-04 — PC del usuario (verificar M09 en máquina real del dueño).
El check requiere que el dueño lo ejecute localmente; no se puede afirmar sin
evidencia real de la PC.

Nada pasa a "verificado en PC" sin ese check explícito.
