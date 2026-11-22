# ETAPA 12 — M15 REPORTS + M16 LIVE (V1.0)

> Estado: 🟢 En progreso — compila y pasa CI (run 44990354058, CI_RUNNER).
> Fecha: 2026-11-22.

## 1. Objetivo

V1.0 (informe + monitorización): **M15 — EVO Reports** construye el documento de
informe a partir del resultado del motor M14; **M16 — EVO Live** muestra la evolución
temporal en una ventana acotada. Ambos son módulos de análisis, no motores de
diagnóstico: los verdicts siguen siendo de M14.

## 2. M15 — Reports

**Dentro:**

- `ReportBuilder.Build(CollectResult)` → `ReportDocument` (Id, GeneratedAt, ModuleId, FindingsTotal, Corroborated, Warning, MetricsTotal, Findings[]).
- `ReportDocument.ToJson()` — serialización indentada ( System.Text.Json ).
- Tests xUnit sin CIM (lógica pura).

**Fuera (no implementado en esta etapa):**

- Renderizado a PDF/HTML (EVO.Reports — futuro, requiere librería de plantillas).
- Escritura del archivo a disco (responsabilidad del caller/dashboard).

## 3. M16 — Live Monitor

**Dentro:**

- `LiveMonitorCollector` (M16) — muestrea los módulos fuente en N muestras con intervalo.
- Emite estadísticas temporales agregadas: samples, cpu.usage_avg/max, ram.available_avg/min, health.events_max, process.top_cpu_max.
- Tests xUnit sin CIM (stub collectors).

**Fuera (no implementado en esta etapa):**

- Streaming continuo real (M16 es muestreo acotado; la monitorización continua es dashboard).
- Tendencia/historial persistido.
- Filtro por ventana/ésimo análisis (M14 + M16).

## 4. Criterios de verificación

| Checkpoint | Resultado exigido | Estado |
|------------|-------------------|--------|
| E12-01 | `EVO.Core.Reports.ReportBuilder` + `EVO.Core.Live.LiveMonitorCollector` creados y compilan | ✅ CI_RUNNER (run 44990354058) |
| E12-02 | Tests xUnit de M15/M16 (sin CIM) — pasan en CI | ✅ CI_RUNNER (run 44990354058) — workflow verde; contadores exactos no citados (host de .trx bloqueado en este sandbox). Nota: la primera iteración falló en test por namingPolicy (PascalCase vs camelCase); la corrección usa JsonNamingPolicy.CamelCase |
| E12-03 | Docs commiteados; PR #1 continúa | ✅ CI_RUNNER (run 44990354058) |

## 5. Decisiones de diseño concretas

- **M15/M16 en EVO.Core:** lógica pura (sin I/O, sin CIM). Testable sin hardware.
- **M15 no escribe archivos:** produce el JSON; la escritura queda en el caller.
- **M16 es una ventana acotada:** no hay stream indefinido — fin explícito del muestreo.
- **Sin hallazgos en M16:** los verdicts pertenecen a M14.
- **Naming:** M15 / M16 como en la especificación original.
- **Sin métricas string en M16:** los agregados son numéricos.

## 7. Estado del módulo

🟢 **CI verde (CI_RUNNER)** — run 44990354058, Windows runner.

- Compilación Release: OK (EVO.sln con 5 proyectos).
- `dotnet test`: OK (5 Facts de M15 + 9 Facts de M16 + los existentes).
- `Verify-Foundation.ps1`: OK (incluye la extensión de M15/M16).

**Pendiente:** E12-04 — PC del usuario (verificar M15/M16 en máquina real del dueño).
El check requiere que el dueño lo ejecute localmente; no se puede afirmar sin
evidencia real de la PC.

Nada pasa a "verificado en PC" sin ese check explícito.
