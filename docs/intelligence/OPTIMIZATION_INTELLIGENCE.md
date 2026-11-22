# Inteligencia de Optimización (`OPTIMIZATION_INTELLIGENCE`)

> **Alcance en Etapa F0:** Especificación conceptual del motor de decisión en el `Adaptive Core` ([ADR-0007](../adr/0007-optimization-intelligence.md), [ADR-0008](../adr/0008-adaptive-core-authority.md)). No incluye código de aplicación.

---

## 1. `SystemProfile` (Hechos Medidos del Equipo)

El `SystemProfile` es una estructura inmutable por sesión diagnóstica construida exclusivamente con hechos verificados por sondas de solo lectura en `Engine.Host`:

| Dominio | Hechos recolectados por sonda |
|---|---|
| **CPU y Arquitectura** | Arquitectura (`x64`, `arm64`), fabricante, modelo, topología de núcleos físicos/lógicos (P-cores/E-cores si aplica), soporte de virtualización asistida por hardware. |
| **RAM y Archivo de Paginación** | Memoria física instalada y disponible, presión de memoria actual, estado de compresión de memoria, ubicación y tamaño configurado/efectivo de `pagefile.sys`. |
| **GPU y Controlador Gráfico** | Adaptadores integrados y dedicados, fabricante, versión y fecha del controlador WDDM, estado de programación de GPU acelerada por hardware (HAGS). |
| **Discos y Estado SMART** | Unidades lógicas y físicas, tipo de medio (`NVMe`, `SATA SSD`, `HDD`), espacio libre, estado de TRIM y atributos de salud SMART disponibles. |
| **Red e Interfaces** | Adaptadores activos (Ethernet, Wi-Fi), servidores DNS configurados, latencia base de resolución y estado de controladores de red. |
| **Windows (Build y Edición)** | Rama (`Win10`, `Win11`), versión comercial (`22H2`, `23H2`, `24H2`), número de compilación (`Build` y `UBR`) y edición (`Home`, `Pro`, `Enterprise`, `Education`, `IoT-LTSC`). |
| **Factor de Forma y Energía** | Chasis (`desktop`, `laptop`, `handheld`, `workstation`), presencia de batería, estado AC/DC y plan de energía activo. |
| **Servicios, Procesos e Inicio** | Tipo de inicio y estado de ejecución de cada servicio del SCM, consumo de Working Set/CPU por servicio, entradas de inicio (`Run`, carpeta Startup, Task Scheduler) y datos históricos opt-in ([ADR-0011](../adr/0011-usage-observation-privacy.md)). |

---

## 2. `UserProfile` (Intención y Restricciones del Usuario)

El `UserProfile` captura la voluntad explícita del propietario del equipo:

1. **Intención principal y pesos de objetivo (`goals`):** selección de un perfil base (**Gaming**, **Oficina**, **Desarrollo**, **Multimedia**, **Bajo consumo**, **PC antigua** o **Personalizado**) que asigna pesos relativos a objetivos como `gaming-latency`, `memory-footprint`, `boot-time`, `battery-life`, `privacy-hardening`, `security-hardening`, `storage-reclaim` o `system-stability`.
2. **Restricciones explícitas ("no tocar X"):** lista de subsistemas, servicios o categorías que el usuario prohíbe modificar (por ejemplo, *"No tocar configuración de red"* o *"No tocar plan de energía"*).
3. **Funciones declaradas como necesarias (`requiredByFeatures`):** confirmación explícita sobre capacidades del sistema que el usuario utiliza (impresión local/red, Bluetooth, Xbox/Game Bar, WSL/Hyper-V, escritorio remoto, lectores biométricos, etc.).
4. **Tolerancia al riesgo / Nivel de consentimiento máximo:** filtro de qué niveles (`standard`, `advanced`, `reinforced`) pueden proponerse en un plan general. El nivel `expert` queda siempre excluido de planes automáticos ([ADR-0013](../adr/0013-consent-levels-and-expert-mode.md)).
5. **Permiso de reinicio:** indica si el usuario admite en la sesión actual incluir transacciones que requieran reiniciar Windows (`rebootRequired: true`).

---

## 3. `ApplicabilityProfile` (Condiciones de cada Ficha)

Cada acción del catálogo define declarativamente su `ApplicabilityProfile`:

- **Compatibilidad de plataforma:** versiones, ediciones y rango de compilaciones de Windows (`windowsCompatibility`).
- **Compatibilidad de hardware:** arquitecturas, factores de forma, tipos de almacenamiento y RAM mínima (`hardwareCompatibility`).
- **Predicado de estado actual (`originalState`):** valor en que debe encontrarse actualmente el componente para que tenga sentido proponer el cambio (si ya está en el estado propuesto, no es aplicable).
- **Evidencia local exigida (`localEvidenceProbeIds`):** sondas que deben aportar evidencia de no uso o de coste real de recursos en este equipo.
- **Vínculos funcionales (`dependencies`, `conflicts`, `requiredByFeatures`, `goals`).**

---

## 4. Algoritmo Conceptual de Decisión

El `Adaptive Core` evalúa cada ficha del catálogo siguiendo una cadena determinista de seis filtros (**Clase × Relevancia × Compatibilidad × Dependencias × Conflictos × Perfil**) sin emplear modelos opacos:

1. **Filtro de Clase (`componentClass`):**
   - Si el componente es `Critical` o `Undetermined` (incluyendo cualquier componente sin ficha en el catálogo), queda excluido de toda recomendación de perfil; solo es accesible individualmente en Modo Experto ([ADR-0009](../adr/0009-component-classification.md)).
2. **Filtro de Compatibilidad (`windowsCompatibility` y `hardwareCompatibility`):**
   - Si la edición/build de Windows o el hardware del `SystemProfile` no coinciden (por ejemplo, ajuste específico de HDD en un equipo con NVMe), la evaluación termina en **`NotApplicable(razón)`**.
   - Si el componente ya se encuentra en el `proposedState`, termina en **`NotApplicable(already-in-desired-state)`**.
3. **Filtro de Relevancia y Evidencia Local:**
   - Se consultan los datos de las sondas requeridas en `localEvidenceProbeIds` y, si está habilitada, la ventana mínima de observación ([ADR-0011](../adr/0011-usage-observation-privacy.md)).
   - Si faltan lecturas de sonda (por falta de elevación o ventana de observación insuficiente), la evaluación termina honestamente en **`Unknown(razón)`**.
   - Si la evidencia demuestra que el componente está en uso activo por el usuario o su coste es nulo/irrelevante, termina en **`NotApplicable(component-in-active-use)`**.
4. **Filtro de Funciones Necesarias y Dependencias (`requiredByFeatures` y `dependencies`):**
   - Si la ficha es `FeatureDependent` y el usuario aún no ha confirmado si necesita esa función, queda en **`Unknown(awaiting-user-feature-confirmation)`**.
   - Si el usuario confirmó que necesita la función o existe un servicio activo dependiente que quedaría roto, termina en **`Conflict(dependent-feature-or-service)`**.
5. **Filtro de Conflictos y Restricciones (`conflicts` y restricciones del `UserProfile`):**
   - Si el objetivo está en la lista *"no tocar X"* del usuario, requiere reinicio cuando el usuario desautorizó reinicios, o choca con otra acción de mayor prioridad en el plan, termina en **`Conflict(con)`**.
6. **Alineación con el Perfil (`goals` y `consentLevel`):**
   - Si supera todos los filtros anteriores y contribuye al menos a un objetivo (`goals`) activo en el `UserProfile` dentro del `consentLevel` permitido, la acción se clasifica como **`Applicable`** y pasa a la previsualización explicando exactamente qué evidencia local la respalda.
