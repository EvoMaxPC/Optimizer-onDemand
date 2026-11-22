# Índice de Registros de Decisiones de Arquitectura (ADRs)

Todos los ADRs de la Etapa F0 siguen la plantilla obligatoria: **Estado**, **Fecha**, **Contexto**, **Decisión**, **Alternativas descartadas**, **Consecuencias**, **Límites y riesgos conocidos** y **Preguntas abiertas**.

| Número | Archivo | Título | Estado |
|---|---|---|---|
| 0001 | [0001-monorepo-and-stack.md](0001-monorepo-and-stack.md) | Monorepo, Stack Tecnológico y Contrato por Esquema | Propuesto |
| 0002 | [0002-privilege-separation.md](0002-privilege-separation.md) | Separación de Privilegios en Dos Procesos (`Engine.Host` y `Engine.Elevated`) | Propuesto |
| 0003 | [0003-elevated-ipc-channel.md](0003-elevated-ipc-channel.md) | Canal IPC Elevado Autenticado sin Secretos en Línea de Comandos | Propuesto |
| 0004 | [0004-approval-token.md](0004-approval-token.md) | Token de Autorización de Un Solo Uso (`ApprovalToken`) | Propuesto |
| 0005 | [0005-storage-and-evidence-chain.md](0005-storage-and-evidence-chain.md) | Almacenamiento SQLite, Muestras Individuales y Cadena de Evidencias | Propuesto |
| 0006 | [0006-restoration-strategy.md](0006-restoration-strategy.md) | Estrategia de Restauración y Cobertura Declarada de Snapshots | Propuesto |
| 0007 | [0007-optimization-intelligence.md](0007-optimization-intelligence.md) | Inteligencia de Optimización y `ApplicabilityProfile` | Propuesto |
| 0008 | [0008-adaptive-core-authority.md](0008-adaptive-core-authority.md) | `Adaptive Core` como Único Decisor y Defensa en Profundidad | Propuesto |
| 0009 | [0009-component-classification.md](0009-component-classification.md) | Clasificación de Componentes en Cinco Clases y Reglas de Propuesta | Propuesto |
| 0010 | [0010-reboot-transactions-and-recovery-guard.md](0010-reboot-transactions-and-recovery-guard.md) | Transacciones con Reinicio y `Recovery Guard` | Propuesto |
| 0011 | [0011-usage-observation-privacy.md](0011-usage-observation-privacy.md) | Observación Local de Uso Opt-In y Límites de Privacidad | Propuesto |
| 0012 | [0012-versioned-closed-catalog.md](0012-versioned-closed-catalog.md) | Catálogo como Dato Versionado y Primitivas Tipadas Cerradas | Propuesto |
| 0013 | [0013-consent-levels-and-expert-mode.md](0013-consent-levels-and-expert-mode.md) | Niveles de Consentimiento y Modo Experto | Propuesto |
| 0014 | [0014-provisioning-and-image-out-of-core.md](0014-provisioning-and-image-out-of-core.md) | Aprovisionamiento (`WinGet`) e Imágenes (`Evo.Image`) Fuera del Núcleo | Propuesto |
| 0015 | [0015-source-and-reference-policy.md](0015-source-and-reference-policy.md) | Política de Fuentes Técnicas y No Copia de Terceros | Propuesto |
| 0016 | [0016-user-catalog-extensions.md](0016-user-catalog-extensions.md) | Catálogo de Usuario Restringido a Primitivas Cerradas | Propuesto |
| 0017 | [0017-user-scope-in-elevated-executor.md](0017-user-scope-in-elevated-executor.md) | Resolución del Ámbito de Usuario (`HKCU`) ante Elevación Cruzada | Propuesto |
| 0018 | [0018-tauri-surface-and-frontend-dependency-policy.md](0018-tauri-surface-and-frontend-dependency-policy.md) | Superficie Mínima de Tauri v2 y Política de Dependencias del Frontend | Aceptado |
| 0019 | [0019-modules-manifest.md](0019-modules-manifest.md) | Manifiesto de Módulos | Aceptado |
| 0020 | [0020-ipc-host-readonly-ndjson.md](0020-ipc-host-readonly-ndjson.md) | Contrato IPC de Solo Lectura por NDJSON (`Evo.Engine.Host`) | Aceptado |
