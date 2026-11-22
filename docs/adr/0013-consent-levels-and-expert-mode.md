# ADR-0013: Niveles de Consentimiento y Modo Experto

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
El lema de EVO OPTIMIZER es *"Tu Windows. Tus reglas. Tu hardware"*. Prohibir paternalistamente ajustes avanzados empuja a los usuarios expertos a usar scripts opacos sin red de recuperación; por el contrario, presentar cambios de alto riesgo con un simple casillero junto a ajustes triviales de interfaz expone a usuarios generales a perder funciones críticas o protecciones de seguridad sin comprender las consecuencias.

## Decisión
Establecer que las restricciones de EVO son **niveles de consentimiento y prerrequisitos de recuperación**, estructurados en cuatro niveles (`consentLevel`):
1. **`standard` (Estándar):** cambios reversibles en el ámbito del usuario actual (`scope: user`), sin elevación UAC ni reinicio. Requiere autorización normal e instantánea de snapshot de usuario.
2. **`advanced` (Avanzado):** cambios reversibles a nivel de máquina (`scope: machine`), servicios no críticos, tareas programadas, planes de energía y ajustes de red. Requiere elevación UAC (`Engine.Elevated`), snapshot propio y, para **GPU, audio o red, autorización específica adicional** separada del lote general.
3. **`reinforced` (Reforzado):** cambios con **reversibilidad parcial (`Partial`) o nula (`None`)**, tales como eliminación de paquetes `AppX` o limpieza de cachés. Exige mostrar la **lista exacta de lo que NO se recupera** (`snapshotCoverage.notCovered`) y una confirmación explícita dedicada antes de continuar.
4. **`expert` (Experto / Modo Experto):** reservado a la reducción de protecciones de seguridad (Windows Defender, Firewall, UAC, VBS/HVCI), modificación de componentes de clase `Critical` o `Undetermined`, o cambios que toquen la ruta de arranque. Exige simultáneamente:
   - Activación consciente del Modo Experto y ejecución individual (nunca en lotes de perfil).
   - **Punto de Restauración del Sistema confirmado** además del snapshot propio de EVO.
   - **Confirmación de medio de recuperación externo** si el cambio afecta al arranque ([ADR-0010](0010-reboot-transactions-and-recovery-guard.md)).
   - **Confirmación escrita manual del nombre técnico del cambio** por parte del usuario.
   - **Indicador visual persistente de `"protección reducida"`** en la interfaz principal mientras una protección permanezca desactivada.
5. **Caso específico de Windows Update:** pausar o deshabilitar Windows Update es posible bajo consentimiento correspondiente, ofreciendo una **fecha de fin opcional** (reactivación programada) y manteniendo un **aviso persistente** en la interfaz sobre el estado de actualizaciones.

## Alternativas descartadas
- **Bloquear por completo cualquier ajuste sobre Windows Update o Windows Defender:** descartado porque contradice la filosofía *"Tu Windows. Tus reglas"* y empuja al usuario a herramientas sin snapshot ni reversión.
- **Permitir que un perfil predefinido ("Perfil Gaming") desactive seguridad o componentes críticos en bloque:** descartado rotundamente; los componentes críticos o de seguridad nunca pueden alterarse mediante un perfil genérico.

## Consecuencias
- El usuario conserva la soberanía sobre su máquina en todo momento, pero la fricción de consentimiento y los prerrequisitos de recuperación escalan proporcionalmente al riesgo real del cambio.
- El indicador persistente de `"protección reducida"` evita que el usuario olvide que desactivó temporalmente una barrera de seguridad o las actualizaciones del sistema.

## Límites y riesgos conocidos
- Si el usuario en Modo Experto desactiva protecciones como Defender, Firewall o VBS tras cumplir todos los pasos de confirmación escrita, el sistema quedará objetivamente más expuesto a malware externo; EVO informa y mantiene la alerta persistente, pero no puede impedir las consecuencias de seguridad de una decisión expresa del propietario del equipo.
- Si el usuario declara tener un medio de recuperación USB sin poseerlo realmente, la salvaguarda procedimental frente a fallos de arranque temprano pierde eficacia.

## Preguntas abiertas
- Definir si el Modo Experto se desactiva automáticamente al cerrar la sesión de la aplicación (manteniendo el indicador de `"protección reducida"` si ya hay cambios activos).
