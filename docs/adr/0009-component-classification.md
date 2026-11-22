# ADR-0009: Clasificación de Componentes en Cinco Clases y Reglas de Propuesta

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
No todos los servicios, tareas o componentes de Windows tienen el mismo impacto si se desactivan. Deshabilitar un servicio accesorio de telemetría o de mapas descargados no conlleva el mismo riesgo que deshabilitar la cola de impresión o un servicio nuclear del sistema como `RpcSs`. Tratar todos los componentes por igual lleva o bien a romper Windows o bien a impedir cualquier personalización avanzada.

## Decisión
1. Clasificar todo componente de Windows dentro de una de **cinco clases (`componentClass`)**:
   - **`Expendable` (Prescindible):** componente accesorio cuya desactivación no afecta a funciones operativas estándar. Se propone cuando existe evidencia local de no uso o de coste medido.
   - **`Optional` (Opcional):** función secundaria útil solo en ciertos escenarios. Se propone cuando existe evidencia local de no uso o coste medido.
   - **`FeatureDependent` (Necesario para ciertas funciones):** componente del que dependen capacidades concretas (impresión, Bluetooth, Xbox, Hyper-V, etc.). Requiere evidencia local **más confirmación explícita del usuario** de que no utiliza dicha función.
   - **`Critical` (Crítico para Windows):** componente esencial de seguridad, estabilidad o infraestructura base del sistema. Nunca es propuesto por un perfil genérico; **solo es accesible de forma individual en Modo Experto** con todos sus prerrequisitos ([ADR-0013](0013-consent-levels-and-expert-mode.md)).
   - **`Undetermined` (Sin determinar):** **cualquier componente que no tenga una entrada explícita y auditada en el catálogo recibe automáticamente la clase `Undetermined`**. No puede ser recomendado por perfiles y solo puede inspeccionarse o gestionarse bajo las restricciones de Modo Experto.
2. **Componentes que impiden arrancar Windows** (por ejemplo, `RpcSs`, `DcomLaunch`, controladores de volumen de arranque): se mantienen **solo como informativos por defecto**, sin acción de desactivación habilitada (registrado en [DECISIONS_PENDING.md](../process/DECISIONS_PENDING.md)).

## Alternativas descartadas
- **Clasificación binaria ("Seguro" / "Peligroso"):** descartada por ser incapaz de expresar que un servicio como `Spooler` es totalmente seguro de desactivar para quien no tiene impresora, pero rompe el flujo de trabajo de quien sí imprime.
- **Asumir como `Optional` los servicios desconocidos de terceros:** descartado; todo componente no catalogado debe tratarse como `Undetermined` por prudencia.

## Consecuencias
- Los perfiles automáticos nunca tocarán componentes `Critical` ni `Undetermined`.
- La distinción entre `Optional` y `FeatureDependent` obliga a que la interfaz pregunte al usuario en lenguaje claro (*"¿Utilizas impresoras locales o en red?"*) antes de proponer desactivar el servicio asociado.

## Límites y riesgos conocidos
- La clasificación `componentClass` representa conocimiento curado humano; si una actualización mayor de Windows añade una nueva dependencia oculta sobre un servicio antes catalogado como `Optional`, la clasificación podría quedar desactualizada hasta que se detecte en las pruebas de laboratorio de la nueva build ([ADR-0012](0012-versioned-closed-catalog.md)).
- Los servicios instalados por software de terceros (antivirus, controladores de periféricos, anticheats de juegos) aparecerán inicialmente como `Undetermined`.

## Preguntas abiertas
- Confirmar la decisión final del propietario sobre si los componentes que impiden el arranque de Windows (`RpcSs`, `DcomLaunch`) permanecerán perpetuamente como solo informativos incluso en Modo Experto (ver [DECISIONS_PENDING.md](../process/DECISIONS_PENDING.md)).
