# ETAPA 10 — M12 WINDOWS HEALTH (Integridad y eventos)

> Estado: 🟡 En progreso (ver §7).
> Fecha: 2026-11-22.

## 1. Objetivo

Auditar el estado de salud del equipo desde el log de eventos Windows: conteo de
events de error y advertencia en los logs System y Application (ventana de 24 h).
M12 es una **auditoría**, no un detector. NO declara causas ni cuellos de botella:
los verdicts pertenecen a M14 — Bottleneck Engine.

**Alcance de la métrica:** auditoría instantánea, con ventana temporal acotada.

## 2. M12 — Alcance

**Dentro (vía `Win32_NTLogEvent`):**

- `health.system.errors` (count) — eventos de Error (EventType=1) en System log, últimos 24 h.
- `health.system.warnings` (count) — eventos de Warning (EventType=2) en System log, últimos 24 h.
- `health.application.errors` (count) — eventos de Error en Application log, últimos 24 h.
- `health.application.warnings` (count) — eventos de Warning en Application log, últimos 24 h.
- `health.events_24h` (count) — total de errores + advertencias.

**Fuera (no implementado en esta etapa):**

- Hallazgos/diagnósticos (M14 — correlación).
- Eventos individuales (detalle, dashboard — M15).
- Monitoreo en vivo (M16).

## 3. Contratos usados

```
ICollector
  ModuleId: "M12"
  CollectAsync(CancellationToken) -> CollectResult
    Metrics   ← health.* (best-effort, agregado, 24 h)
    Findings  ← vacío (M12 es auditoría; ver §4)
```

## 4. Modelo de diagnóstico del hallazgo (M12: ninguno)

M12 produce solo métricas de auditoría. Los verdicts con confianza honesta
pertenecen a M14 (correlación); M12 aporta la evidencia que M14 consume.

## 5. Criterios de verificación (consistentes con ETAPA 01–09)

| Checkpoint | Resultado exigido | Estado |
|------------|-------------------|--------|
| E10-01 | `EVO.Collectors.Windows.WindowsHealthCollector` (M12) creado y compila | ⏳ |
| E10-02 | Tests xUnit de M12 — pasan en CI (`windows-latest`, CIM real) | ⏳ |
| E10-03 | Docs commiteados; PR #1 continúa | ⏳ |

## 6. Decisiones de diseño concretas

- **Ventana 24 h** (`WindowHours = 24`): acotada para evitar escanear logs completos.
  M12 no inventa "integridad" más allá del recuento agregado; el análisis de eventos
  es trabajo de M14.
- **Instantánea, no serie:** un único snapshot CIM por recolección.
- **Best-effort:** si la consulta falla, la sección se omite sin abortar.
- **Sin hallazgos:** M12 produce métricas de auditoría; M14 emite los verdicts.
- **Sin métricas string.**

## 7. Estado del módulo

🟡 En progreso. Pendiente: CI verde.
