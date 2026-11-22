# ADR-0002: Separación de Privilegios en Dos Procesos (`Engine.Host` y `Engine.Elevated`)

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
La mayoría de las herramientas de optimización de Windows solicitan elevación UAC (`requireAdministrator`) desde el arranque de la aplicación. Esto hace que toda la interfaz gráfica (incluido el motor de renderizado web) y las tareas de diagnóstico corran con privilegios de administrador todo el tiempo, ampliando innecesariamente la superficie de ataque.

## Decisión
Dividir el motor en dos procesos independientes con privilegios diferenciados:
1. **`Engine.Host` (no elevado, solo lectura):** se inicia junto con la aplicación en el contexto de seguridad del usuario actual. Ejecuta las sondas de diagnóstico, aloja el `Adaptive Core`, consulta SQLite y calcula planes de optimización sin requerir elevación UAC.
2. **`Engine.Elevated` (elevado bajo demanda, catálogo cerrado):** solo se invoca cuando el usuario autoriza explícitamente aplicar o revertir cambios que requieren permisos administrativos. Ejecuta únicamente primitivas tipadas y cerradas del catálogo ([ADR-0012](0012-versioned-closed-catalog.md)), nunca comandos arbitrarios, y finaliza al completar la operación o tras un periodo corto de inactividad.

## Alternativas descartadas
- **Ejecutar toda la aplicación como Administrador desde el inicio:** descartado por violar el principio de mínimo privilegio y exponer WebView2 en un contexto elevado.
- **Instalar un servicio de Windows permanente corriendo como `SYSTEM`:** descartado porque deja abierta de forma continua una superficie privilegiada en segundo plano aunque el usuario no esté realizando cambios.

## Consecuencias
- El usuario puede abrir EVO, diagnosticar su hardware, explorar el catálogo y simular perfiles sin recibir ningún aviso de UAC.
- Incluso si ocurriese un fallo en la capa de interfaz gráfica, el proceso afectado carece de permisos administrativos directos sobre el sistema operativo.
- Se requiere un protocolo IPC seguro y autenticado entre `Engine.Host` y `Engine.Elevated` ([ADR-0003](0003-elevated-ipc-channel.md)).

## Límites y riesgos conocidos
- Algunas lecturas profundas de diagnóstico (por ejemplo, ciertos datos SMART de controladores específicos o políticas de auditoría restringidas a administradores) no están disponibles sin elevación; en esos casos `Engine.Host` debe reportar el estado honesto `Unknown(requires-elevation)` en lugar de adivinar el valor.
- La elevación bajo demanda introduce una latencia perceptible cuando aparece el diálogo UAC de Windows.

## Preguntas abiertas
- Determinar el tiempo exacto de expiración por inactividad (*idle timeout*) de `Engine.Elevated` durante una misma sesión de cambios por lotes.
