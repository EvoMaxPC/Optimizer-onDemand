# ETAPA 04 — M03 RAM ANALYZER (Presión y paginación)

> Estado: 🟡 En progreso (ver §7).
> Fecha: 2026-11-22.

## 1. Objetivo

Medir **presión y paginación de la RAM** en el momento de la recolección:
memoria disponible, memoria comprometida (committed) y actividad de paginación
(page faults y pages por segundo). El módulo **no declara causas** — eso
pertenece a M14 — Bottleneck Engine.

**Alcance de la métrica:** snapshot puntual, no serie temporal. La evolución en el
tiempo es M16 (EVO Live).

## 2. M03 — Alcance

**Dentro:**

- Memoria disponible (`ram.available_mb`, unidad `MB`).
- Memoria comprometida (`ram.committed_mb`, unidad `MB`).
- Porcentaje de memoria disponible relativa al total (`ram.available_percent`, unidad `%`).
- Page faults/sec (`ram.page_faults_per_sec`, unidad `/sec`) — best-effort.
- Pages/sec (`ram.pages_per_sec`, unidad `/sec`) — best-effort.
- Hallazgo de advertencia si memoria disponible < 10 % del total.

**Fuera (no implementado en esta etapa):**

- Correlación causal y hallazgos confirmados (M14).
- Serie temporal / monitoreo vivo (M16).
- Detalle por proceso de memoria (M06).
- Swap/paginación persistida (M04 — storage).

## 3. Contratos usados

```
ICollector
  ModuleId: "M03"
  CollectAsync(CancellationToken) -> CollectResult
    Metrics   ← ram.available_mb, ram.committed_mb, ram.available_percent,
               ram.page_faults_per_sec, ram.pages_per_sec (best-effort)
    Findings  ← 0 o 1 hallazgo (solo si memoria disponible < 10 % del total)
```

## 4. Modelo de diagnóstico del hallazgo

- **Category:** `"RAM"`
- **Title:** "Presión de memoria detectada (memoria disponible baja)" — **nunca** "cuello de botella".
- **Severity:** `Medium`
- **Confidence:** `Warning` (nunca `Confirmed`)
- **Evidence:** memoria disponible relativa al total (dato que respalda)
- **SuspectedSources:** procesos de alto consumo y servicios en background (nunca confirmados)
- **Impact / Recommendation / VerificationMethod:** cadena de verificación honesta

El hallazgo **no es un cuello de botella**: jamás se declara un bottleneck por
uso elevado de un componente (regla de la especificación). Solo indica que hay
que investigar.

## 5. Criterios de verificación (consistentes con ETAPA 01/02)

| Checkpoint | Resultado exigido | Estado |
|------------|-------------------|--------|
| E04-01 | `EVO.Collectors.Memory.RamAnalyzerCollector` (M03) creado y compila | ⏳ |
| E04-02 | Tests xUnit de M03 — pasan en CI (`windows-latest`, CIM real) | ⏳ |
| E04-03 | Docs commiteados; PR #1 continúa | ⏳ |

## 6. Decisiones de diseño concretas

- **Umbral estático** (`PressureThresholdPercent = 10.0`): constante pública en el
  collector, no un archivo de configuración. Razón: M03 produce datos; la política
  de umbrales es un tema de M14/análisis, no del recolector.
- **Instantánea, no serie:** un único snapshot CIM por recolección. M16 cubrirá
  la evolución.
- **Best-effort:** si `Win32_PerfFormattedData_PerfOS_Memory` no existe o devuelve
  vacío, la sección se omite sin fallar toda la recolección.
- **Métricas de tasa** (Page Faults/sec, Pages/sec) se emiten como instantáneas —
  no son un promedio, no son una serie. Documentado como limitación conocida.
- **M03 no emite métricas string** — las métricas de análisis son numéricas.

## 7. Estado del módulo

🟡 En progreso. Pendiente: CI verde.
