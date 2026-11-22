# ADR-0010: Transacciones con Reinicio y `Recovery Guard`

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
Algunas optimizaciones legítimas de Windows (cambios en el inicio de servicios del sistema, parámetros de memoria o red) solo surten efecto completo tras reiniciar el sistema operativo. Si un cambio provoca inestabilidad tras el reinicio o el usuario no llega a confirmar que el sistema funciona correctamente, dejar el cambio aplicado sin supervisión violaría el principio *"Nadie queda atrapado"*.

## Decisión
1. **Máquina de estados formal de transacciones con reinicio:**
   - Camino nominal: `Planned → Approved → SnapshotTaken → Applying → PendingReboot → Rebooting → PostBootVerification → AwaitingConfirmation → Committed`.
   - Camino de reversión: desde cualquier fallo o expiración de ventana transita a `RollingBack → RolledBack` o, si falla la reversión automática, a `RollbackFailed` mostrando una **guía manual paso a paso** (incluyendo comandos para WinRE).
2. **Doble persistencia del plan de recuperación:** además de SQLite, antes de aplicar el cambio se guarda el manifiesto de reversión firmado en **`%ProgramData%\EVO\recovery`** con una ACL restringida exclusivamente a `SYSTEM` y `Administrators`.
3. **`Recovery Guard`:** tarea programada mínima y firmada que se ejecuta como `SYSTEM` al iniciar Windows, equipada con un **contador de arranques** y una **ventana de confirmación con valor por defecto seguro**:
   - Al arrancar en estado `Rebooting`/`PendingReboot`, ejecuta `PostBootVerification`.
   - Si el usuario no confirma que el sistema opera correctamente dentro de la ventana de confirmación o si se supera el límite del contador de arranques sin llegar a `Committed`, `Recovery Guard` ejecuta automáticamente la reversión (`RollingBack → RolledBack`) usando el snapshot en `%ProgramData%\EVO\recovery`.
4. **Regla bloqueante de ingeniería:** ninguna acción real de escritura sobre Windows se habilitará en el proyecto antes de que esta máquina de estados y `Recovery Guard` estén implementados y probados en la Etapa 9.

## Alternativas descartadas
- **Aplicar cambios que requieren reinicio y darlos por terminados (`Committed`) antes de reiniciar:** descartado porque si el cambio degrada el inicio de sesión o la red tras el reinicio, no habría mecanismo automático para deshacerlo.
- **Depender de que el usuario abra manualmente la interfaz de EVO tras el reinicio para revertir:** descartado porque si el escritorio o WebView2 fallan, el usuario no podría abrir la aplicación.

## Consecuencias
- Los cambios con reinicio se comportan como la confirmación de cambio de resolución de pantalla: si el usuario no confirma tras el reinicio dentro de la ventana segura, el ajuste vuelve automáticamente a su valor anterior.
- La carpeta `%ProgramData%\EVO\recovery` permite que `Recovery Guard` revierta los cambios incluso sin acceso al perfil de usuario ni a la base de datos de la interfaz.

## Límites y riesgos conocidos
- **Límite honesto y crítico de arranque:** `Recovery Guard` es una tarea programada de Windows que corre en modo usuario bajo la cuenta `SYSTEM`; **si un cambio impide que el kernel de Windows, el subsistema de servicios (`services.exe`) o el Programador de Tareas lleguen a iniciarse (fallo antes del escritorio / BSOD temprano), `Recovery Guard` NO llegará a ejecutarse**.
- Por este motivo, cualquier cambio que pueda afectar a la ruta de arranque exige obligatoriamente **Modo Experto**, un **Punto de Restauración del Sistema confirmado** y la confirmación de que el usuario dispone de un **medio de recuperación externo (WinRE / USB de instalación)** desde el cual revertir offline ([ADR-0013](0013-consent-levels-and-expert-mode.md)).

## Preguntas abiertas
- Fijar los valores numéricos por defecto de la ventana de confirmación post-arranque (por ejemplo, 10 minutos tras inicio de sesión) y del máximo del contador de arranques fallidos (por ejemplo, 2 arranques).
