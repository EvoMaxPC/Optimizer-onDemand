# ADR-0017: Resolución del Ámbito de Usuario (`HKCU`) ante Elevación Cruzada

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
En entornos Windows donde el usuario opera habitualmente con una **cuenta de usuario estándar (no administradora)**, cuando UAC solicita credenciales para lanzar `Engine.Elevated` (*Over-the-Shoulder elevation*), el usuario introduce las credenciales de una cuenta Administradora distinta. Como resultado, el proceso `Engine.Elevated` corre bajo el token y perfil del Administrador, por lo que su rama `HKEY_CURRENT_USER` (`HKCU`) y sus carpetas `%APPDATA%` apuntan al Administrador y **no** al usuario estándar que inició sesión en el escritorio.

## Decisión
Documentar el problema arquitectónico de elevación cruzada y acotar la resolución a dos opciones técnicas válidas cuya elección definitiva debe cerrarse **antes de iniciar la Etapa 8 (Ejecutor Elevado)**:
- **Opción A (Operación por SID explícito en `Engine.Elevated`):** `Engine.Elevated` recibe como parte de la solicitud autenticada el SID del usuario interactivo original (`Engine.Host`), valida que corresponde al cliente autenticado del pipe ([ADR-0003](0003-elevated-ipc-channel.md)) y opera explícitamente sobre el hive cargado `HKEY_USERS\<SID_Usuario>` en lugar de usar el alias `HKEY_CURRENT_USER`.
- **Opción B (Ejecución en proceso no elevado `Engine.Host` para `scope: user`):** dado que las acciones con `scope: "user"` modifican únicamente recursos pertenecientes al propio usuario interactivo, las ejecuta directamente `Engine.Host` bajo el token del usuario actual sin pasar por `Engine.Elevated`, reservando `Engine.Elevated` exclusivamente para acciones con `scope: "machine"`.

## Alternativas descartadas
- **Usar `HKEY_CURRENT_USER` directamente dentro de `Engine.Elevated` asumiendo que el usuario siempre es un Administrador con *Split Token*:** descartado porque falla silenciosamente en todas las cuentas estándar de Windows, modificando el registro del administrador equivocado y rompiendo la verificación y el rollback.

## Consecuencias
- El esquema del catálogo (`schemas/action.schema.json`) exige desde la Etapa F0 que toda ficha declare obligatoriamente `"scope": "user" | "machine"`.
- Se evita uno de los errores más comunes en herramientas de administración de Windows bajo cuentas sin privilegios administrativos.

## Límites y riesgos conocidos
- En la **Opción A**, si un cambio de ámbito de usuario requiere notificar a la sesión interactiva o modificar recursos por fuera del registro `HKEY_USERS\<SID>`, el proceso elevado sigue corriendo en una sesión/contexto distinto.
- En la **Opción B**, un lote mixto que combine acciones `scope: user` y `scope: machine` requiere coordinar la transacción y el snapshot entre `Engine.Host` y `Engine.Elevated` para que el rollback siga siendo atómico.

## Preguntas abiertas
- Tomar la decisión definitiva entre la Opción A, la Opción B o un modelo híbrido (Opción B por defecto para `scope: user` y Opción A para `Recovery Guard` corriendo como `SYSTEM`) antes de comenzar la Etapa 8.
