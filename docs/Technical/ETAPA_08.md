# ETAPA 08 — M08 STARTUP ANALYZER (Arranque de Windows)

> Estado: 🟢 En progreso — compila y pasa CI (run 44987684312, CI_RUNNER). Tests de CIM corren en `windows-latest`.
> Fecha: 2026-11-22.

## 1. Objetivo

Auditar el inventario de elementos de arranque de Windows: total, fuente
(registro vs. carpetas de inicio) y elementos sin descripción. M08 es una
**auditoría**, no un detector: no declara cuellos de botella ni causas. Los
verdicts pertenecen a M14 — Bottleneck Engine.

**Alcance de la métrica:** auditoría instantánea del estado de arranque.

## 2. M08 — Alcance

**Dentro (vía `Win32_StartupCommand`):**

- `startup.count` (count) — total de elementos de arranque.
- `startup.registry` (count) — elementos desde claves de registro (Run/Startup.exe/Wow6432Node/etc.).
- `startup.folder` (count) — elementos desde carpetas de inicio.
- `startup.no_description` (count) — elementos sin descripción (señal de auditoría).

**Fuera (no implementado en esta etapa):**

- Hallazgos/diagnósticos (M14 — correlación).
- Detalle por elemento (nombre, comando, usuario — dashboard, M15).
- Medición de tiempo de arranque (M10 — Latency Lab / M16 — Live).

## 3. Contratos usados

```
ICollector
  ModuleId: "M08"
  CollectAsync(CancellationToken) -> CollectResult
    Metrics   ← startup.* (best-effort, agregado)
    Findings  ← vacío (M08 es auditoría; ver §4)
```

## 4. Modelo de diagnóstico del hallazgo (M08: ninguno)

M08 produce solo métricas de auditoría. La decisión de emitir hallazgos con
confianza honesta ya está diseñada para M14 (correlación); M08 aporta la evidencia
que M14 consume.

## 5. Criterios de verificación (consistentes con ETAPA 01–07)

| Checkpoint | Resultado exigido | Estado |
|------------|-------------------|--------|
| E08-01 | `EVO.Collectors.Startup.StartupAnalyzerCollector` (M08) creado y compila | ✅ CI_RUNNER (run 44987684312) |
| E08-02 | Tests xUnit de M08 (5 Facts) — pasan en CI (`windows-latest`, CIM real) | ✅ CI_RUNNER (run 44987684312) — workflow verde; contadores exactos no citados (host de .trx bloqueado en este sandbox) |
| E08-03 | Docs commiteados; PR #1 continúa | ✅ CI_RUNNER (run 44987684312) |

## 6. Decisiones de diseño concretas

- **Instantánea, no serie:** un único snapshot CIM por recolección.
- **Best-effort:** si la consulta falla, la sección se omite sin abortar.
- **Agregado, no detalle:** los elementos se cuentan por fuente y descripción;
  el detalle por elemento es trabajo del dashboard (M15).
- **Sin hallazgos:** M08 produce métricas de auditoría; M14 emite los verdicts.
- **Sin métricas string:** las métricas de auditoría son numéricas.

## 7. Estado del módulo

🟢 **CI verde (CI_RUNNER)** — run 44987684312, Windows runner.

- Compilación Release: OK (EVO.sln con 5 proyectos).
- `dotnet test`: OK (5 Facts de M08 + los existentes de M01/M02/M03/M04/M06/M07).
- `Verify-Foundation.ps1`: OK (incluye la extensión de M08).

**Pendiente:** E08-04 — PC del usuario (verificar M08 en máquina real del dueño).
El check requiere que el dueño lo ejecute localmente; no se puede afirmar sin
evidencia real de la PC.

Nada pasa a "verificado en PC" sin ese check explícito.
