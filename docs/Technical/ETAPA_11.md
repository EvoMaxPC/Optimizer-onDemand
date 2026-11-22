# ETAPA 11 — M14 BOTTLENECK ENGINE (Correlación de evidencias)

> Estado: 🟢 En progreso — compila y pasa CI (run 44989438084, CI_RUNNER).
> Fecha: 2026-11-22.

## 1. Objetivo

El **Motor de correlación** (V0.6): consume las métricas emitidas por los módulos
(M01–M12), las agrega y produce **hallazgos finales con confianza honesta**.

M14 es el corazón del proyecto: es donde las detecciones se convierten en
diagnósticos honestos.

## 2. M14 — Alcance

**Dentro:**

- Consume el conjunto de métricas de M02/M03/M06/M12 (y otros módulos).
- Evaluación de 3 áreas: CPU, MEMORY, HEALTH.
- Hallazgos finales con confianza Warning / Probable (nunca Confirmed).
- Métricas de correlación: areas / suspected / corroborated.
- Best-effort por fuente: los módulos fallidos se omiten sin abortar.

**Fuera (no implementado en esta etapa):**

- Hallazgos de áreas no evaluadas (STORAGE: umbrales de trabajo-dependientes — ver §6).
- Causas confirmadas — M14 produce causas probables, nunca confirmadas.
- Serie temporal / monitoreo vivo (M16).

## 3. Contratos usados

```
ICollector
  ModuleId: "M14"
  ctor(IEnumerable<ICollector> sources)  ← inyección de los módulos productores
  CollectAsync(CancellationToken) -> CollectResult
    Metrics   ← bottleneck.areas / bottleneck.suspected / bottleneck.corroborated
    Findings  ← 0..3 (CPU, MEMORY, HEALTH) — Confidence Warning / Probable
```

## 4. Modelo de correlación — reglas (triage, no diagnóstico)

### CPU (M02 + M06)
- Suspecta: `cpu.usage_percent` ≥ 80 % (M02)
- Corroborada: `process.top_cpu_percent` ≥ 80 % (M06) — Confidence Probable
- Confidence Warning si solo la fuente única aporta evidencia
- Severity Medium (siempre)

### MEMORY (M03)
- Suspecta: `ram.available_percent` < 10 % (M03)
- Corroborada: `ram.page_faults_per_sec` ≥ 100 /sec (M03) — Confidence Probable
- Confidence Warning si solo la fuente única aporta evidencia

### HEALTH (M12)
- Suspecta: `health.events_24h` ≥ 50 (M12)
- Corroborada: `health.system.errors` ≥ 10 (M12) — Confidence Probable
- Confidence Warning si solo la fuente única aporta evidencia

**STORAGE:** NO se emite hallazgo en esta etapa — los umbrales de latencia/cola dependen del workload y del tipo de disco. M04 aporta evidencia cruda; M14 la consume en una etapa posterior (V0.6+) cuando los umbrales estén calibrados.

## 5. Criterios de verificación

| Checkpoint | Resultado exigido | Estado |
|------------|-------------------|--------|
| E11-01 | `EVO.Core.Correlation.BottleneckEngineCollector` (M14) creado y compila | ✅ CI_RUNNER (run 44989438084) |
| E11-02 | Tests xUnit de correlación (sin CIM — stub collectors) — pasan en CI | ✅ CI_RUNNER (run 44989438084) — workflow verde; contadores exactos no citados (host de .trx bloqueado en este sandbox). Nota: la primera iteración falló en build por `_sources.Count` en `IEnumerable`; la corrección usa `List<ICollector>` |
| E11-03 | Docs commiteados; PR #1 continúa | ✅ CI_RUNNER (run 44989438084) |

## 6. Decisiones de diseño concretas

- **Motor en EVO.Core** (namespace `EVO.Core.Correlation`): la correlación es lógica pura — no depende de CIM ni de Windows. Evaluable en cualquier plataforma con stub collectors.
- **Sin cálculo de causas confirmadas:** las reglas son triage. Todo hallazgo es Warning o Probable, nunca Confirmed.
- **Inyección de fuentes:** el motor no instancia módulos — recibe `IEnumerable<ICollector>` (inyección de dependencias explícita). Facilita tests y desacoplamiento.
- **"Último gana":** si dos módulos emiten la misma métrica, el más reciente prevalece (documentado).
- **Threshold constants:** constantes públicas en el motor, documentadas aquí como umbrales de triage, no diagnósticos.

## 7. Estado del módulo

🟢 **CI verde (CI_RUNNER)** — run 44989438084, Windows runner.

- Compilación Release: OK (EVO.sln con 5 proyectos).
- `dotnet test`: OK (12 Facts de correlación + los existentes).
- `Verify-Foundation.ps1`: OK (incluye la extensión de M14).

**Pendiente:** E11-04 — PC del usuario (verificar M14 en máquina real del dueño).
El check requiere que el dueño lo ejecute localmente; no se puede afirmar sin
evidencia real de la PC.

Nada pasa a "verificado en PC" sin ese check explícito.
