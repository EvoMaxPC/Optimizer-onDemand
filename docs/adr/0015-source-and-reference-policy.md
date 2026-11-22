# ADR-0015: Política de Fuentes Técnicas y No Copia de Terceros

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
En el ecosistema de optimización de Windows existen proyectos conocidos como WinUtil, AtlasOS, entre otros. Copiar sus scripts, archivos de configuración, listas literales o textos descriptivos generaría contaminación de propiedad intelectual y licencias, además de propagar *tweaks* heredados por tradición oral cuyo efecto real en versiones actuales de Windows 10 y Windows 11 nunca fue medido con rigor.

## Decisión
1. **Hechos técnicos vs. expresión protegida:** los puntos de configuración de Windows (nombres de claves del Registro, identificadores de servicios del sistema, GUIDs de planes de energía, nombres de tareas programadas documentadas) son hechos técnicos de la plataforma Windows y no son propiedad intelectual exclusiva de ningún tercero.
2. **Prohibición estricta de copia:** queda terminantemente prohibido copiar código fuente, scripts PowerShell/Batch, archivos `.reg`/`.yml`, listas literales o textos explicativos de WinUtil, AtlasOS o cualquier otro proyecto externo.
3. **Derivación desde fuentes primarias y laboratorio propio:** cada ficha del catálogo de EVO debe redactarse desde cero a partir de **documentación técnica de Microsoft** (Hardware Dev Center, Windows Server/Client documentation, Sysinternals, esquemas ADMX oficiales) y **pruebas empíricas en el laboratorio propio de EVO**.
4. **Uso permitido de proyectos externos:** proyectos como WinUtil o AtlasOS pueden consultarse únicamente como **mapa conceptual de áreas temáticas** (para identificar qué subsistemas suelen preocupar a los usuarios), nunca como fuente de implementación ni de copia.
5. **Revisión previa de licencia:** si en algún momento futuro se considerara reutilizar cualquier artefacto externo, es obligatorio verificar primero su licencia y compatibilidad legal antes de introducirlo al repositorio.

## Alternativas descartadas
- **Importar y convertir automáticamente las listas JSON/YAML de WinUtil o los playbooks de AtlasOS:** descartado por infringir la autoría y licencia de terceros y por introducir cientos de cambios sin verificación de reversibilidad ni medición propia.

## Consecuencias
- El catálogo de EVO crecerá de manera gradual y controlada según la capacidad real de documentar y probar cada ficha en laboratorio (`draft → lab-tested → stable`), pero cada entrada tendrá trazabilidad técnica limpia y textos 100 % originales.
- El riesgo legal e intelectual sobre el catálogo se reduce drásticamente.

## Límites y riesgos conocidos
- Dado que el nombre de una clave de registro de Windows (por ejemplo, `HKLM\SYSTEM\CurrentControlSet\Services\...`) es único e impuesto por el sistema operativo, dos proyectos independientes que ajusten el mismo servicio inevitablemente harán referencia a la misma ruta técnica; la diferenciación auditable reside en la estructura tipada propia de EVO, sus textos originales, sus condiciones de aplicabilidad y sus pruebas de laboratorio documentadas.
- Algunos comportamientos internos de Windows carecen de documentación pública exhaustiva de Microsoft; cuando una fuente oficial no esté verificada, la ficha debe marcarse explícitamente como `PENDIENTE: fuente` y permanecer en estado `draft`.

## Preguntas abiertas
- Establecer el formato de archivo de las actas de prueba de laboratorio (*lab test reports*) que respaldarán el paso de cada ficha de `draft` a `lab-tested`.
