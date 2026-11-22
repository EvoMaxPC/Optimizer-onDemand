# ADR-0007: Inteligencia de Optimización y `ApplicabilityProfile`

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
Los optimizadores tradicionales aplican listas fijas de cambios sin importar si el equipo es un portátil con batería o una estación de escritorio, si tiene un SSD NVMe o un disco mecánico HDD, si corre Windows 10 Home o Windows 11 Pro, o si el componente que van a desactivar ni siquiera está consumiendo recursos. Eso genera cambios inútiles o roturas de funciones que el usuario sí utilizaba.

## Decisión
1. Definir para cada acción del catálogo un **`ApplicabilityProfile`** declarativo compuesto por:
   - **Builds y edición de Windows compatibles** (`windowsCompatibility`).
   - **Requisitos de hardware** (`hardwareCompatibility`: arquitectura, factor de forma, tipo de almacenamiento, memoria mínima).
   - **Estado actual esperado** (`originalState`): predicado sobre el valor actual leído por sonda.
   - **Objetivos de intención** (`goals`): metas del `UserProfile` a las que contribuye la acción.
   - **Conflictos y dependencias** (`conflicts`, `dependencies`, `requiredByFeatures`).
   - **Evidencia local requerida** (`evidenceBeforeAfter.localEvidenceProbeIds`).
2. **Regla estricta de recomendación:** sin compatibilidad verificada **y** sin evidencia local recolectada en la máquina, EVO **no recomienda** la acción.
3. El resultado de evaluar una acción produce siempre uno de cuatro estados formales explicables en la UI: `Applicable`, `NotApplicable(razón)`, `Unknown(razón)` o `Conflict(con)`.

## Alternativas descartadas
- **Listas de *tweaks* por perfil ("Perfil Gaming = aplicar estas 40 claves"):** descartado porque ignora el hardware real y recomienda cambios redundantes o dañinos en máquinas donde no aplican.
- **Modelos de aprendizaje automático (*Machine Learning*) opacos ("caja negra"):** descartados porque el usuario y el auditor no podrían verificar de forma determinista por qué se recomendó un cambio concreto.

## Consecuencias
- Cada recomendación presentada al usuario viene acompañada de la evidencia local concreta (por ejemplo, *"El servicio X está en ejecución, consume N MB y no se han detectado dispositivos asociados"*).
- Cuando una sonda no puede determinar un dato, el motor se abstiene de recomendar (`Unknown(razón)`) en lugar de adivinar.

## Límites y riesgos conocidos
- Una instantánea puntual de diagnóstico puede no reflejar un periférico o función que el usuario conecta de forma esporádica (por ejemplo, una impresora USB que solo enciende una vez al mes). Por ello, las acciones de clase `FeatureDependent` exigen además confirmación explícita de que el usuario no necesita dicha función ([ADR-0009](0009-component-classification.md)) o una ventana mínima de observación ([ADR-0011](0011-usage-observation-privacy.md)).
- Si Microsoft introduce cambios no documentados de comportamiento en una compilación futura de Windows, el `ApplicabilityProfile` deberá bloquear la recomendación hasta validar la nueva compilación en laboratorio.

## Preguntas abiertas
- Determinar los umbrales numéricos mínimos por categoría (RAM, CPU, latencia) a partir de los cuales la evidencia local se considera significativa en la Etapa 6.
