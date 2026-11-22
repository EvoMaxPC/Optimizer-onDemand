# ADR-0003: Canal IPC Elevado Autenticado sin Secretos en Línea de Comandos

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
Cuando `Engine.Host` (no elevado) lanza `Engine.Elevated` (elevado) y se comunica con él en Windows, pasar claves, tokens o secretos a través de argumentos de línea de comandos (`argv`) o variables de entorno es inseguro: cualquier proceso que corra bajo el mismo usuario puede consultar la línea de comandos y el bloque de entorno de otros procesos de su sesión mediante APIs estándar de Windows o WMI (`Win32_Process`).

## Decisión
Establecer el canal IPC entre `Engine.Host` y `Engine.Elevated` con las siguientes medidas obligatorias:
1. **Cero secretos en `argv` o entorno:** por línea de comandos solo se pasa el identificador público efímero del punto de conexión.
2. **Named Pipe con nombre aleatorio y ACL estricta:** el servidor del pipe se crea con un nombre criptográficamente aleatorio por sesión y un descriptor de seguridad (DACL) restringido exclusivamente al SID del usuario invocador y a `SYSTEM`/Administradores, rechazando clientes remotos (`PIPE_REJECT_REMOTE_CLIENTS`).
3. **Verificación del proceso par:** al conectarse el par sobre el pipe, se obtiene su PID real (`GetNamedPipeClientProcessId` / `GetNamedPipeServerProcessId`), se resuelve la ruta completa de su imagen ejecutable y se verifica su **firma Authenticode**.
4. **Acuerdo de claves ECDH efímero:** una vez verificado el binario par, ambos procesos ejecutan un intercambio **ECDH efímero** sobre el pipe para derivar claves de sesión simétricas autenticadas (AEAD).
5. **Protección contra repetición y cierre automático:** cada mensaje incluye un `nonce` monotónico/único por sesión; el canal y el proceso elevado se cierran automáticamente tras inactividad.

## Alternativas descartadas
- **Pasar un token compartido en `argv` al lanzar `Engine.Elevated`:** descartado porque cualquier proceso del mismo usuario puede leer `CommandLine` vía `NtQueryInformationProcess` o WMI.
- **Socket TCP en `127.0.0.1`:** descartado porque expone el puerto a cualquier cuenta local de la máquina y carece de las primitivas nativas de ACL por SID e inspección de PID de los Named Pipes de Windows.

## Consecuencias
- Se bloquea el secuestro oportunista del canal por procesos de otros usuarios locales o binarios no firmados.
- La implementación de la capa IPC requiere pruebas específicas de rechazo de clientes no firmados, tokens repetidos y sesiones caducadas.

## Límites y riesgos conocidos
- **Límite honesto del modelo de amenazas:** un malware que ya se ejecute bajo el mismo usuario y en el mismo nivel de integridad que `Engine.Host` puede intentar inyectar código o manipular la memoria de `Engine.Host` antes de que se emita la solicitud al proceso elevado. Este canal IPC mitiga la suplantación externa del pipe y la lectura pasiva de `argv`, pero **el malware residente en el mismo contexto de usuario queda parcialmente fuera del modelo de defensa IPC** y depende de las confirmaciones del usuario en UAC y de la restricción del ejecutor a un catálogo cerrado de primitivas ([ADR-0012](0012-versioned-closed-catalog.md)).
- La validación de firma Authenticode exige que los binarios estén firmados incluso en entornos de preproducción (con certificado de laboratorio) para probar la ruta real sin desactivar controles.

## Preguntas abiertas
- Definir la política de validación del certificado Authenticode durante el desarrollo local frente a compilaciones de lanzamiento (ver [DECISIONS_PENDING.md](../process/DECISIONS_PENDING.md)).
