# ETAPA 05 — M04 STORAGE ANALYZER (Latencia e I/O)

> Estado: 🟢 En progreso — compila y pasa CI (run 44984575119, CI_RUNNER). Tests de CIM corren en `windows-latest`.
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
| E05-01 | `EVO.Collectors.Storage.StorageAnalyzerCollector` (M04) creado y compila | ✅ CI_RUNNER (run 44984575119) |
| E05-02 | Tests xUnit de M04 (6 Facts) — pasan en CI (`windows-latest`, CIM real) | ✅ CI_RUNNER (run 44984575119) — workflow verde; contadores exactos no citados (host de .trx bloqueado en este sandbox) |
| E05-03 | Docs commiteados; PR #1 continúa | ✅ CI_RUNNER (run 44984575119) |

## 6. Decisiones de diseño concretas

- **Best-effort por disco:** cualquier falla en la consulta (driver raro, contador
  deshabilitado) omite la sección sin abortar la recolección.
- **Lectura defensiva de propiedades:** CIM expone nombres con capitalización
  variable; se accede por nombre directo y se omite lo ausente/parseable.
- **Instantánea, no serie:** un único snapshot CIM por recolección. M16 cubrirá la evolución.
- **Sin hallazgos:** M04 produce métricas; M14 emite los verdicts con evidencia.
- **M04 no emite métricas string** — las métricas de análisis son numéricas.

## 7. Estado del módulo

🟢 **CI verde (CI_RUNNER)** — run 44984575119, Windows runner.

- Compilación Release: OK (EVO.sln con 5 proyectos).
- `dotnet test`: OK (6 Facts de M04 + los existentes de M01/M02/M03).
- `Verify-Foundation.ps1`: OK (incluye la extensión de M04).

**Pendiente:** E05-04 — PC del usuario (verificar M04 en máquina real del dueño).
El check requiere que el dueño lo ejecute localmente; no se puede afirmar sin
evidencia real de la PC.

Nada pasa a "verificado en PC" sin ese check explícito.
