# ADR-0016: Catálogo de Usuario Restringido a Primitivas Cerradas

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
Los usuarios avanzados y administradores de sistemas a menudo desean añadir sus propios ajustes personalizados que aún no forman parte del catálogo oficial de EVO. Si para ello se les ofreciera un gancho de "ejecutar script personalizado", se destruirían las garantías de seguridad, snapshot y reversibilidad del motor.

## Decisión
1. Permitir un **Catálogo de Usuario** donde el propietario del equipo pueda definir fichas propias, pero **compuestas única y exclusivamente con las mismas 13 primitivas cerradas** ([ADR-0012](0012-versioned-closed-catalog.md)) y validadas contra el mismo esquema `schemas/action.schema.json`.
2. **Mismas garantías operativas:** toda ficha de usuario recibe exactamente el mismo ciclo de lectura previa, snapshot propio obligatorio, validación de `ApprovalToken`, verificación post-aplicación, medición y reversión que una ficha oficial.
3. **Etiquetado y distinción criptográfica:** las fichas del catálogo de usuario se marcan visiblemente en la interfaz y en la base de datos como **`"de usuario"` y `"no firmadas por EVO"`**, impidiendo que se confundan con recomendaciones auditadas por el proyecto.

## Alternativas descartadas
- **No permitir ninguna extensión del usuario:** descartado porque resta flexibilidad al usuario avanzado que desea gestionar sus propios ajustes bajo la protección de snapshots y rollback de EVO.
- **Permitir fichas de usuario con comandos PowerShell o ejecutables libres:** descartado porque convertiría a `Engine.Elevated` en un lanzador privilegiado de código arbitrario (*LOLBin*).

## Consecuencias
- El usuario puede extender EVO para sus necesidades particulares manteniendo intactas la verificación, el snapshot y la vuelta atrás.
- Incluso si un usuario importa una ficha personalizada compartida por un tercero, dicha ficha no puede ejecutar código arbitrario ni salirse de las primitivas tipadas y niveles de consentimiento de `Engine.Elevated`.

## Límites y riesgos conocidos
- Aunque una ficha de usuario esté restringida a las primitivas cerradas y validada contra `action.schema.json`, el autor de la ficha podría clasificar erróneamente un componente delicado como `Optional` en lugar de `Critical`; para mitigar este riesgo, el motor debe aplicar una lista interna de protección sobre objetivos conocidos de sistema (servicios y claves críticas) que fuerce el nivel `expert` independientemente de lo que declare una ficha no firmada.

## Preguntas abiertas
- Definir si la importación de fichas de usuario desde archivos externos requerirá siempre al menos nivel de consentimiento `advanced` o `expert` en su primera ejecución.
