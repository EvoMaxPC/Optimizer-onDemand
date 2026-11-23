# ETAPA 13 — M05 GPU + M11 NETWORK (V0.5 completado parcialmente)

> Estado: 🟢 CERRADO — CI_RUNNER (ver §4).
> Fecha: 2026-11-22.

## M05 — GPU Analyzer

**Dentro:**
- `gpu.adapters` (count) — total de adaptadores gráficos.
- `gpu.total_adapter_ram_mb` / `gpu.max_adapter_ram_mb` (MB).

**Fuera (diferido, documentado):**
- Utilización/uso GPU % — los contadores de GPU son vendor-específicos; no hay una vía CIM confiable. El análisis GPU profundo queda pendiente de diseño explícito.

## M11 — Network Analyzer

**Dentro:**
- `network.adapters.count` / `network.adapters.enabled` (count).
- `network.bytes_sent_mb_sec` / `network.bytes_received_mb_sec` (MB/s, suma sobre interfaces).
- `network.packets_sent_per_sec` / `network.packets_received_per_sec` (/sec).

**Fuera (no implementado en esta etapa):**
- Desglose por interfaz (nombre, IP) — dashboard lo consume.
- Hallazgos de red — M14.

## Criterios de verificación

| Checkpoint | Resultado exigido | Estado |
|------------|-------------------|--------|
| E13-01 | M05 y M11 creados y compilan | ✅ CI_RUNNER — run 44991419059 (head 99c5c56) |
| E13-02 | Tests xUnit de M05/M11 — pasan en CI | ✅ CI_RUNNER — run 44991419059 (head 99c5c56) |
| E13-03 | Docs commiteados; PR #1 continúa | ✅ (docs commiteados c9c47e1/7c55ea2; commits de dashboard arriba, ver §4) |

## Decisiones de diseño

- **M05 es agregación de inventario:** el detalle por adaptador (nombre, RAM por
  adaptador) vive en M01; M05 agrega métricas de resumen.
- **M11 es auditoría:** sin hallazgos — los verdicts son de M14.
- **Sin métricas string:** D-016 sigue propuesta, sin aprobación.
- **M10/M13 diferidos** (ver V0_5_STATUS.md y § M13).

## Estado del módulo

🟢 CI verde para el código de M05/M11 (run 44991419059, head 99c5c56: build +
test + Verify-Foundation verdes, c9c47e1 + 7c55ea2 commiteados).

## Estado del dashboard WPF (EVO.Desktop)

> 2026-11-22 — El shell WPF se simplificó (sin Commands/Converters custom;
> patrón StartupUri + parameterless ctor + DataContext en code-behind).
> **El dashboard NO está CI-VERIFICADO**: el único run verde (44991419059)
> cubre el shell original, no el dashboard. El dashboard compilado no fue
> confirmado en CI (ambas versiones anteriores fallaron el Release build;
> la versión simplificada no tiene run asociado).
>
> **Causa raíz de por qué CI no dispara en el branch:** otro sesión mergeó
> PR #7 y #8 y main fue reescrito a un monorepo pnpm ("Etapa 2 / F1a");
> `.github/workflows/foundation.yml` fue ELIMINADO de main. Desde el 8/12/2025,
> GitHub lee el workflow de `pull_request` desde la rama DEFAULT (main) —
> sin el archivo en main no hay workflow que corra, y por eso no se disparan
> checks para PR #1 desde antes de este punto. Ninguna rama nuestra puede
> restaurar ese workflow en main: requiere a la sesión que dueña de main
> (no se merge ni se pushea a main desde aquí).
>
> No reclamamos el dashboard como verificado; queda como work-in-progress
> en el branch con honestidad explícita.
