# ETAPA 07 — M07 SERVICES AUDITOR (Auditoría de servicios)

> Estado: 🟢 En progreso — compila y pasa CI (run 44985309215, CI_RUNNER). Tests de CIM corren en `windows-latest`.
> Fecha: 2026-11-22.

## 1. Objetivo

Auditar el inventario de servicios de Windows: total, running/stopped y
auto/manual/disabled. M07 es una **auditoría**, no un detector: no correlaciona
ni declara cuellos de botella. Los verdicts pertenecen a M14 — Bottleneck Engine.

**Alcance de la métrica:** auditoría instantánea del estado de los servicios.

## 2. M07 — Alcance

**Dentro:**

- `services.count` (count) — total de servicios.
- `services.running` (count) — servicios en estado running/startpending/continuepending.
- `services.stopped` (count) — servicios en otros estados.
- `services.auto_start` (count) — servicios de inicio automático.
- `services.manual` (count) — servicios de inicio bajo demanda.
- `services.disabled` (count) — servicios deshabilitados.

**Fuera (no implementado en esta etapa):**

- Hallazgos/diagnósticos (M14 — correlación).
- Detalle por servicio (trabajo del dashboard, M15).
- Serie temporal / monitoreo vivo (M16).
- Auditoría de servicios de terceros o servicios sensibles — fuera de alcance.

## 3. Contratos usados

```
ICollector
  ModuleId: "M07"
  CollectAsync(CancellationToken) -> CollectResult
    Metrics   ← services.* (best-effort, agregado)
    Findings  ← vacío (M07 es auditoría; ver §5)
```

## 4. Modelo de diagnóstico del hallazgo (M07: ninguno)

M07 produce solo métricas de inventario. La decisión de emitir hallazgos con
confianza honesta ya está diseñada para M14 (correlación); M07 aporta la evidencia
que M14 consume.

## 5. Criterios de verificación (consistentes con ETAPA 01–06)

| Checkpoint | Resultado exigido | Estado |
|------------|-------------------|--------|
| E07-01 | `EVO.Collectors.Services.ServicesAuditorCollector` (M07) creado y compila | ✅ CI_RUNNER (run 44985309215) |
| E07-02 | Tests xUnit de M07 (5 Facts) — pasan en CI (`windows-latest`, CIM real) | ✅ CI_RUNNER (run 44985309215) — workflow verde; contadores exactos no citados (host de .trx bloqueado en este sandbox) |
| E07-03 | Docs commiteados; PR #1 continúa | ✅ CI_RUNNER (run 44985309215) |

## 6. Decisiones de diseño concretas

- **Instantánea, no serie:** un único snapshot CIM por recolección.
- **Best-effort:** si la consulta `Win32_Service` falla, la sección se omite sin abortar.
- **Agregado, no detalle:** los servicios individuales se cuentan por estado/modo de inicio; el detalle por servicio es trabajo del dashboard (M15).
- **Sin hallazgos:** M07 produce métricas de auditoría; M14 emite los verdicts con evidencia.
- **Sin métricas string:** las métricas de auditoría son numéricas.

## 7. Estado del módulo

🟢 **CI verde (CI_RUNNER)** — run 44985309215, Windows runner.

- Compilación Release: OK (EVO.sln con 5 proyectos).
- `dotnet test`: OK (5 Facts de M07 + los existentes de M01/M02/M03/M04/M06).
- `Verify-Foundation.ps1`: OK (incluye la extensión de M07).

**Pendiente:** E07-04 — PC del usuario (verificar M07 en máquina real del dueño).
El check requiere que el dueño lo ejecute localmente; no se puede afirmar sin
evidencia real de la PC.

Nada pasa a "verificado en PC" sin ese check explícito.
