# ETAPA 13 — M05 GPU + M11 NETWORK (V0.5 completado parcialmente)

> Estado: 🟡 En progreso (ver §7).
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
| E13-01 | M05 y M11 creados y compilan | ⏳ |
| E13-02 | Tests xUnit de M05/M11 — pasan en CI | ⏳ |
| E13-03 | Docs commiteados; PR #1 continúa | ⏳ |

## Decisiones de diseño

- **M05 es agregación de inventario:** el detalle por adaptador (nombre, RAM por
  adaptador) vive en M01; M05 agrega métricas de resumen.
- **M11 es auditoría:** sin hallazgos — los verdicts son de M14.
- **Sin métricas string:** D-016 sigue propuesta, sin aprobación.
- **M10/M13 diferidos** (ver V0_5_STATUS.md y § M13).

## Estado del módulo

🟡 En progreso. Pendiente: CI verde.

## Estado del dashboard WPF (EVO.Desktop)

> 2026-11-22 — El shell WPF se simplificó (sin Commands/Converters custom;
> patrón StartupUri + parameterless ctor + DataContext en code-behind).
> HEAD: 6636a4d. **CI NO CONFIRMADO**: el run solo existe para 99c5c56
> (éxito); este push no dispara checks en PR #1 (plataforma/Actions pendiente).
> No reclamamos el dashboard como verificado.
