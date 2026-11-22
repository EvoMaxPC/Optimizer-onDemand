# ETAPA 03 — M02 CPU ANALYZER (Saturación y limitaciones)

> Estado: 🟢 En progreso — compila y pasa CI (run 44983845008, CI_RUNNER). Tests de CIM corren en `windows-latest`.
> Fecha: 2026-11-22.

## 1. Objetivo

Medir **saturación y limitaciones del procesador** en el momento de la
recolección: uso instantáneo de CPU y longitud de cola. El módulo **no correlaciona
ni declara causas** — eso pertenece a M14 — Bottleneck Engine.

**Alcance de la métrica:** snapshot puntual, no serie temporal. La evolución en el
tiempo es M16 (EVO Live).

## 2. M02 — Alcance

**Dentro:**

- Uso instantáneo del procesador (`cpu.usage_percent`, unidad `%`).
- Longitud de cola de trabajos (`cpu.queue_length`, unidad `jobs`).
- Hallazgo de advertencia si el uso supera el umbral (90 %).

**Fuera (no implementado en esta etapa):**

- Correlación causal y hallazgos confirmados (M14).
- Serie temporal / monitoreo vivo (M16).
- Inventario por core del CPU (mueve sota M01/M06).
- Detección de limitaciones térmicas o por reloj (M05/M06).

## 3. Contratos usados

```
ICollector
  ModuleId: "M02"
  CollectAsync(CancellationToken) -> CollectResult
    Metrics   ← cpu.usage_percent, cpu.queue_length (best-effort)
    Findings  ← 0 o 1 hallazgo (solo si uso >= 90 %)
```

## 4. Modelo de diagnóstico del hallazgo

- **Category:** `"CPU"`
- **Title:** "CPU saturada en el instante de la medición" — **nunca** "cuello de botella".
- **Severity:** `Medium`
- **Confidence:** `Warning` (nunca `Confirmed`)
- **Evidence:** uso% en el instante (dato que respalda)
- **SuspectedSources:** procesos de alto consumo y servicios en background (nunca confirmados)
- **Impact / Recommendation / VerificationMethod:** cadena de verificación honesta

El hallazgo **no es un cuello de botella**: jamás se declara un bottleneck por uso
elevado (regla de la especificación). Solo indica que hay que investigar.

## 5. Criterios de verificación (consistentes con ETAPA 01/02)

| Checkpoint | Resultado exigido | Estado |
|------------|-------------------|--------|
| E03-01 | `EVO.Collectors.CPU.CpuAnalyzerCollector` (M02) creado y compila | ✅ CI_RUNNER (run 44983845008) |
| E03-02 | Tests xUnit de M02 (5 Facts) — pasan en CI (`windows-latest`, CIM real) | ✅ CI_RUNNER (run 44983845008) — workflow verde; contadores exactos no citados (host de artifacts bloqueado en este sandbox) |
| E03-03 | Docs commiteados; PR #1 continúa | ✅ CI_RUNNER (run 44983845008) |

## 6. Decisiones de diseño concretas

- **Umbral estático** (`SaturationThresholdPercent = 90.0`): constante pública en el
  collector, no un archivo de configuración. Razón: M02 produce datos; la política
  de umbrales es un tema de M14/análisis, no del recolector.
- **Instantánea, no serie:** un único snapshot CIM por recolección. M16 cubrirá
  la evolución.
- **Best-effort:** si `Win32_PerfFormattedData_PerfOS_Processor` no existe o
  devuelve vacío, la sección se omite sin fallar toda la recolección.
- **M02 no emite métricas string** — las métricas de análisis son numéricas.

## 7. Estado del módulo

🟢 **CI verde (CI_RUNNER)** — run 44983845008, Windows runner.

- Compilación Release: OK (EVO.sln con 5 proyectos).
- `dotnet test`: OK (5 Facts de M02 + los existentes de M01).
- `Verify-Foundation.ps1`: OK (incluye la extensión de M02).

**Pendiente:** E03-04 — PC del usuario (verificar M02 en máquina real del dueño).
Ese check requiere que el dueño lo ejecute localmente; no se puede afirmar sin
evidencia real de la PC.

Nada pasa a "verificado en PC" sin ese check explícito.
