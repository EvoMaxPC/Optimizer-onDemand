# ETAPA 08 — M08 STARTUP ANALYZER (Arranque de Windows)

> Estado: 🟡 En progreso (ver §7).
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
| E08-01 | `EVO.Collectors.Startup.StartupAnalyzerCollector` (M08) creado y compila | ⏳ |
| E08-02 | Tests xUnit de M08 — pasan en CI (`windows-latest`, CIM real) | ⏳ |
| E08-03 | Docs commiteados; PR #1 continúa | ⏳ |

## 6. Decisiones de diseño concretas

- **Instantánea, no serie:** un único snapshot CIM por recolección.
- **Best-effort:** si la consulta falla, la sección se omite sin abortar.
- **Agregado, no detalle:** los elementos se cuentan por fuente y descripción;
  el detalle por elemento es trabajo del dashboard (M15).
- **Sin hallazgos:** M08 produce métricas de auditoría; M14 emite los verdicts.
- **Sin métricas string:** las métricas de auditoría son numéricas.

## 7. Estado del módulo

🟡 En progreso. Pendiente: CI verde.
