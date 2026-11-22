# ETAPA 05 — M04 STORAGE ANALYZER (Latencia e I/O)

> Estado: 🟡 En progreso (ver §7).
> Fecha: 2026-11-22.

## 1. Objetivo

Medir **latencia e I/O por disco lógico**: tiempo medio de lectura/escritura
(ms), longitud de cola, tiempo de disco (%) y tasas de lectura/escritura
(MB/s). El módulo **no declara causes ni cuellos de botella** — eso pertenece a
M14 — Bottleneck Engine, que correlaciona evidencia.

**Alcance de la métrica:** snapshot puntual, no serie temporal. La evolución en el
tiempo es M16 (EVO Live).

## 2. M04 — Alcance

**Dentro (por disco lógico):**

- `storage.<disco>.percent_disk_time` (%)
- `storage.<disco>.avg_sec_read_ms` / `avg_sec_write_ms` (ms)
- `storage.<disco>.avg_queue_length` (jobs)
- `storage.<disco>.read_mb_sec` / `write_mb_sec` (MB/s)

**Fuera (no implementado en esta etapa):**

- Hallazgos/diagnósticos de almacenamiento (M14 — correlación).
- Serie temporal / monitoreo vivo (M16).
- Detalle por proceso de I/O (M06).
- Compactación/defragmentación (no es diagnóstico).

## 3. Contratos usados

```
ICollector
  ModuleId: "M04"
  CollectAsync(CancellationToken) -> CollectResult
    Metrics   ← storage.<disco>.* (best-effort, por disco)
    Findings  ← vacío (M04 produce solo datos; ver §5)
```

## 4. Decisión de diseño: M04 no emite hallazgos

Las métricas de M04 son **datos crudos**. Los umbrales de latencia y colas dependen
del tipo de disco (SSD/HDD), del workload y del equipo — inventar un umbral y
llamarlo "verificado" sería presentar una conclusión que no existe. La decisión de
emitir hallazgos con confianza honesta ya está diseñada para M14 (correlación);
M04 aporta la evidencia que M14 consume.

## 5. Criterios de verificación (consistentes con ETAPA 01–04)

| Checkpoint | Resultado exigido | Estado |
|------------|-------------------|--------|
| E05-01 | `EVO.Collectors.Storage.StorageAnalyzerCollector` (M04) creado y compila | ⏳ |
| E05-02 | Tests xUnit de M04 — pasan en CI (`windows-latest`, CIM real) | ⏳ |
| E05-03 | Docs commiteados; PR #1 continúa | ⏳ |

## 6. Decisiones de diseño concretas

- **Best-effort por disco:** cualquier falla en la consulta (driver raro, contador
  deshabilitado) omite la sección sin abortar la recolección.
- **Lectura defensiva de propiedades:** CIM expone nombres con capitalización
  variable; se accede por nombre directo y se omite lo ausente/parseable.
- **Instantánea, no serie:** un único snapshot CIM por recolección. M16 cubrirá la evolución.
- **Sin hallazgos:** M04 produce métricas; M14 emite los verdicts con evidencia.
- **M04 no emite métricas string** — las métricas de análisis son numéricas.

## 7. Estado del módulo

🟡 En progreso. Pendiente: CI verde.
