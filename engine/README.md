# Motor de Diagnóstico y Ejecución (`engine`)

**Estado actual:** No implementado. Se implementa desde Etapa 4 (`F2`).

## Propósito

Contendrá los procesos en **C# / .NET (self-contained)** definidos en [ADR-0001](../docs/adr/0001-monorepo-and-stack.md) y [ADR-0002](../docs/adr/0002-privilege-separation.md):

1. **`Engine.Host`** (desde Etapa 4 / `F2`, con SQLite en Etapa 5 / `F3` y `Adaptive Core` en Etapa 6 / `F4a`): proceso sin privilegios de administrador, ejecutado bajo el contexto del usuario estándar, encargado de sondas de solo lectura, `Adaptive Core`, persistencia en SQLite ([ADR-0005](../docs/adr/0005-storage-and-evidence-chain.md)) y cálculo de planes ([ADR-0007](../docs/adr/0007-optimization-intelligence.md), [ADR-0008](../docs/adr/0008-adaptive-core-authority.md)).
2. **`Engine.Elevated`** (Etapa 8 / `F5`, tras implementar `SafetyPolicy` y snapshots en Etapa 7 / `F4b`): proceso bajo demanda con privilegios administrativos que valida de forma independiente el `ApprovalToken` ([ADR-0004](../docs/adr/0004-approval-token.md)), toma snapshots propios ([ADR-0006](../docs/adr/0006-restoration-strategy.md)) y ejecuta exclusivamente primitivas cerradas del catálogo ([ADR-0012](../docs/adr/0012-versioned-closed-catalog.md)).
3. **`Recovery Guard`**: componente de verificación post-reinicio y reversión automática implementado en la Etapa 9 (`F6`, [ADR-0010](../docs/adr/0010-reboot-transactions-and-recovery-guard.md)).

## Alcance en Etapas 1–2 (`F0`–`F1a`)

- No existen proyectos `.csproj`, soluciones `.sln` ni paquetes NuGet en estas etapas.
- Ninguna acción de escritura sobre Windows se implementará antes de completar y probar la Etapa 9 (`F6`, ver [CHECKPOINTS.md](../docs/process/CHECKPOINTS.md)).
