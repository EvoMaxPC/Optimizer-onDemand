# ADR-0006: Estrategia de Restauración y Cobertura Declarada de Snapshots

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
Muchas utilidades de modificación de Windows delegan toda la responsabilidad de recuperación en la creación de un Punto de Restauración del Sistema (*System Restore Point*) de Windows. Sin embargo, la Restauración del Sistema puede estar deshabilitada por política o por el usuario, falla con frecuencia, no cubre todas las claves de usuario (`HKCU`) ni archivos específicos, tarda varios minutos en aplicarse y revierte globalmente todo el sistema en lugar de deshacer un cambio puntual.

## Decisión
1. **El Punto de Restauración de Windows NO sustituye al snapshot propio de EVO:** toda acción reversible exige que EVO lea y persista su propio snapshot granular y determinista del estado exacto anterior (valor de registro, tipo de inicio de servicio, estado de tarea, configuración de energía, etc.) antes de ejecutar el cambio.
2. **Declaración obligatoria de cobertura (`snapshotCoverage`):** cada ficha del catálogo debe declarar explícitamente dos listas visibles para el usuario antes de autorizar:
   - **`covered` (qué cubre su snapshot):** los elementos exactos cuyo valor previo se guarda y se restaurará automáticamente al revertir.
   - **`notCovered` (qué NO cubre su snapshot):** efectos colaterales o datos transitorios que el snapshot no puede reconstruir (por ejemplo, cachés temporales vaciadas, sesiones de red reiniciadas o paquetes eliminados que requieran descarga externa).
3. **Punto de restauración como capa complementaria:** para acciones de nivel Experto o que afecten a la ruta de arranque ([ADR-0010](0010-reboot-transactions-and-recovery-guard.md), [ADR-0013](0013-consent-levels-and-expert-mode.md)), se exige confirmar además un Punto de Restauración de Windows como red de seguridad adicional, nunca como reemplazo del snapshot de EVO.

## Alternativas descartadas
- **Confiar únicamente en `Checkpoint-Computer` / System Restore:** descartado por ser lento, todo-o-nada, propenso a estar desactivado en equipos con poco espacio y opaco respecto a qué valores concretos restaura.
- **Snapshots basados en exportar ramas completas `.reg` sin tipar:** descartados porque al reimportar un `.reg` masivo se pueden sobrescribir valores intermedios modificados legítimamente por Windows u otras aplicaciones después del snapshot.

## Consecuencias
- El usuario puede revertir individualmente cualquier cambio reversible en segundos restaurando únicamente el atributo afectado.
- La interfaz muestra con total transparencia qué se recupera al 100 % y qué tiene reversibilidad parcial (`Partial`) o nula (`None`).

## Límites y riesgos conocidos
- Un snapshot granular de EVO restaura el estado de configuración (claves, servicios, tareas), pero **no puede recuperar archivos eliminados en una acción de limpieza de disco (`file.cleanup`)**. Por ello, toda acción de borrado de archivos debe declarar en `notCovered` los datos eliminados.
- Si el sistema operativo sufre una corrupción grave del archivo hive del Registro o del sistema de archivos NTFS que impida iniciar los servicios básicos de Windows, el snapshot local de EVO no bastará por sí solo y será necesario acudir al Punto de Restauración desde WinRE o al medio de recuperación externo ([ADR-0010](0010-reboot-transactions-and-recovery-guard.md)).

## Preguntas abiertas
- Definir el formato de exportación cifrada o firmada de los snapshots de EVO para que el usuario pueda guardarlos también en una unidad USB externa.
