# ETAPA 06 — M06 PROCESS INTELLIGENCE (Consumo por proceso)

> Estado: 🟢 En progreso — compila y pasa CI (run 44985121178, CI_RUNNER). Tests de CIM corren en `windows-latest`.
> Fecha: 2026-11-22.

## 1. Objetivo

Medir el **consumo por proceso** para identificar los principales consumidores:
total de procesos, máximo uso de CPU (%) y máximo working set (MB). El módulo **no
declara causas ni cuellos de botella** — eso pertenece a M14 — Bottleneck Engine.

**Alcance de la métrica:** instantánea puntual, no serie temporal. La evolución en el
tiempo es M16 (EVO Live).

## 2. M06 — Alcance

**Dentro:**

- `process.count` (count) — total de procesos (`Win32_PerfFormattedData_PerfProc_Process`).
- `process.top_cpu_percent` (%) — máximo uso de CPU entre procesos.
- `process.top_memory_mb` (MB) — máximo working set entre procesos.
- Hallazgo de advertencia si el máximo CPU >= 80 %.

**Fuera (no implementado en esta etapa):**

- Ranking completo por proceso (trabajo del dashboard, M15).
- Correlación causal y hallazgos confirmados (M14).
- Serie temporal / monitoreo vivo (M16).
- Detalle por proceso de otros atributos (I/O, threads, handles) — evidencia a la espera.

## 3. Contratos usados

```
ICollector
  ModuleId: "M06"
  CollectAsync(CancellationToken) -> CollectResult
    Metrics   ← process.count, process.top_cpu_percent, process.top_memory_mb
    Findings  ← 0 o 1 hallazgo (solo si máximo CPU >= 80 %)
```

## 4. Modelo de diagnóstico del hallazgo

- **Category:** `"Process"`
- **Title:** "Proceso de alto consumo de CPU detectado" — **nunca** "cuello de botella".
- **Severity:** `Medium`
- **Confidence:** `Warning` (nunca `Confirmed`)
- **Evidence:** nombre del proceso y su uso (dato que respalda)
- **SuspectedSources:** procesos de alto consumo y servicios en background
- **Impact / Recommendation / VerificationMethod:** cadena honesta de verificación

El hallazgo **no es un cuello de botella**: jamás se declara uno por uso elevado
(regla de la especificación). Solo indica que hay que investigar.

## 5. Criterios de verificación (consistentes con ETAPA 01–05)

| Checkpoint | Resultado exigido | Estado |
|------------|-------------------|--------|
| E06-01 | `EVO.Collectors.Processes.ProcessAnalyzerCollector` (M06) creado y compila | ✅ CI_RUNNER (run 44985121178) |
| E06-02 | Tests xUnit de M06 (6 Facts) — pasan en CI (`windows-latest`, CIM real) | ✅ CI_RUNNER (run 44985121178) — workflow verde; contadores exactos no citados (host de .trx bloqueado en este sandbox). Nota: la primera iteración falló en CI (`CountsProcesses`) por depender del contador de perf para el conteo; la corrección usa Win32_Process para el conteo y perf solo para uso de CPU/memoria |
| E06-03 | Docs commiteados; PR #1 continúa | ✅ CI_RUNNER (run 44985121178) |

## 6. Decisiones de diseño concretas

- **Umbral estático** (`HighCpuThresholdPercent = 80.0`): constante pública en el
  collector. La política de umbrales es un tema de M14/análisis, no del recolector.
- **Instantánea, no serie:** un único snapshot CIM por recolección.
- **Best-effort:** si `Win32_PerfFormattedData_PerfProc_Process` no existe o
  devuelve vacío, la sección se omite sin abortar.
- **Solo máximos por atributo:** el ranking completo es trabajo del dashboard (M15)
  y de la correlación (M14). Se emite lo que el módulo certifica: el máximo.
- **Sin métricas string:** las métricas de análisis son numéricas (D-016 sigue
  propuesta, no aprobada).

## 7. Estado del módulo

🟢 **CI verde (CI_RUNNER)** — run 44985121178, Windows runner.

- Compilación Release: OK (EVO.sln con 5 proyectos).
- `dotnet test`: OK (6 Facts de M06 + los existentes de M01/M02/M03/M04).
- `Verify-Foundation.ps1`: OK (incluye la extensión de M06).

**Pendiente:** E06-04 — PC del usuario (verificar M06 en máquina real del dueño).
El check requiere que el dueño lo ejecute localmente; no se puede afirmar sin
evidencia real de la PC.

Nada pasa a "verificado en PC" sin ese check explícito.
