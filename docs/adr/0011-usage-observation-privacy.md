# ADR-0011: Observación Local de Uso Opt-In y Límites de Privacidad

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
Para responder con rigor a la pregunta *"¿Se utiliza realmente este componente?"* y evitar recomendar la desactivación de servicios o programas de inicio que el usuario emplea de forma intermitente, EVO se beneficia de observar qué procesos y servicios se activan a lo largo del tiempo. Sin embargo, monitorizar la actividad del equipo plantea riesgos graves de privacidad si no se acota de forma estricta y transparente.

## Decisión
1. **Opt-in explícito por defecto desactivado:** la observación continua de uso local está desactivada al instalar. El usuario debe activarla expresamente, existiendo además un **interruptor independiente y separado** para permitir o no la detección del proceso en primer plano (*foreground process*).
2. **Lista blanca estricta de datos permitidos:** cuando está activa, solo se registran localmente:
   - Nombre del ejecutable (por ejemplo, `spoolsv.exe`, `game.exe`).
   - Sujeto firmante del certificado Authenticode (si existe).
   - Duración acumulada de ejecución y recuento de sesiones.
   - Agregados estadísticos de uso de CPU, RAM, E/S de disco y red.
   - Estado de programas de inicio y transiciones de estado de servicios.
3. **Lista negra absoluta (NUNCA se registra ni lee):**
   - **NUNCA** títulos de ventana (`Window Title`).
   - **NUNCA** nombres o rutas de documentos/archivos abiertos por el usuario.
   - **NUNCA** direcciones URL, dominios ni tráfico de red inspeccionado.
   - **NUNCA** pulsaciones de teclado, movimientos de ratón, contenido del portapapeles ni capturas de pantalla.
4. **Retención y control del usuario:** retención local configurable con valor por defecto de **30 días**. La aplicación incluirá la pantalla **"Qué sabe EVO de mí"** desde la cual el usuario puede inspeccionar todos los registros, exportarlos en JSON/CSV o borrarlos instantáneamente.
5. **Ventana mínima de evidencia y sin ML:** antes de afirmar que un componente *"no se usa"*, EVO exige una ventana mínima de observación de **14 días y N sesiones registradas**, mostrando siempre el **nivel de confianza visible** de la estimación. Todo el cálculo se realiza con **estadística descriptiva simple y reglas deterministas, sin modelos de Machine Learning**.

## Alternativas descartadas
- **Observación activada por defecto (*opt-out*):** descartada por ser contraria al principio de privacidad y consentimiento explícito de EVO.
- **Registrar títulos de ventana para distinguir qué hace el usuario:** descartado porque los títulos de ventana suelen contener nombres de documentos confidenciales, pestañas de navegador, correos o conversaciones privadas.

## Consecuencias
- Si el usuario decide no activar la observación de uso, EVO sigue funcionando mediante diagnósticos puntuales bajo demanda, pero indicará con honestidad menor nivel de confianza sobre el uso histórico de componentes intermitentes.
- El diseño técnico impide por construcción que EVO actúe como *spyware* o *keylogger*.

## Límites y riesgos conocidos
- Incluso registrando únicamente nombres de ejecutables y duración en la base de datos local, si un tercero obtiene acceso al archivo SQLite del usuario podría inferir qué aplicaciones o juegos están instalados y con qué frecuencia se abren.
- Una ventana de 14 días no detecta programas de uso estacional o mensual (por ejemplo, software contable trimestral); por ello el usuario siempre ve el periodo observado y tiene la última palabra antes de autorizar cualquier cambio.

## Preguntas abiertas
- Definir el número mínimo exacto `N` de sesiones de encendido requeridas junto a los 14 días naturales para otorgar nivel de confianza alto (propuesta inicial: `N = 10` sesiones).
