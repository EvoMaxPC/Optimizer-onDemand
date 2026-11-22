# ADR-0012: Catálogo como Dato Versionado y Primitivas Tipadas Cerradas

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
Si el catálogo de optimizaciones permitiera incluir bloques de código arbitrario, comandos de PowerShell libres, invocaciones a `cmd.exe` o rutas del sistema sin restringir, cualquier error en una ficha o una modificación maliciosa del catálogo convertiría a `Engine.Elevated` en un vector de ejecución remota o local de código arbitrario como Administrador, además de hacer imposible garantizar la reversión automática.

## Decisión
1. **El catálogo es dato declarativo versionado, nunca código ejecutable:** todas las fichas deben ajustarse a `schemas/action.schema.json` con `additionalProperties: false`, expresando sus operaciones en `changes[]` como una unión discriminada por `primitive` (`oneOf` con `const`) cuyos campos están tipados y acotados por enums o patrones estrictos.
2. **Conjunto cerrado de 13 primitivas tipadas:** `Engine.Elevated` (y `Engine.Host` para ámbito de usuario) solo implementan las siguientes primitivas cerradas:
   - `registry.setValue` y `registry.deleteValue`
   - `service.setStartType` y `service.stop`
   - `task.setEnabled`
   - `startup.setEnabled`
   - `power.setValue`
   - `network.setDns` y `network.flushDns`
   - `feature.setState`
   - `file.cleanup` (restringido a identificadores cerrados de directorios temporales/caché)
   - `repair.run` (restringido a herramientas del sistema con **argumentos fijos predefinidos**, como `sfc /scannow` o `dism /Online /Cleanup-Image /CheckHealth`, nunca argumentos libres)
   - `appx.remove`
3. **Contrato obligatorio por primitiva:** cada una de las 13 primitivas implementa en el motor cinco operaciones tipadas: **`read` (leer)**, **`snapshot` (capturar estado previo)**, **`apply` (aplicar)**, **`verify` (verificar lectura posterior)** y **`revert` (restaurar desde snapshot)**.
4. **Firma y ciclo de vida:** el catálogo oficial se distribuye versionado (SemVer) y firmado criptográficamente. Cada ficha atraviesa el ciclo formal: `draft → lab-tested → stable → deprecated`.

## Alternativas descartadas
- **Fichas con scripts `applyScript` / `revertScript` en PowerShell o Batch:** descartadas rotundamente porque rompen el aislamiento de seguridad, son sensibles a políticas de ejecución (`ExecutionPolicy`), localización de idioma de consola e inyección de comandos.
- **Permitir rutas arbitrarias de borrado en `file.cleanup`:** descartado porque un error tipográfico o manipulación en una ruta libre podría destruir archivos del usuario o del sistema operativo.

## Consecuencias
- Un catálogo comprometido o mal formado no puede ejecutar comandos arbitrarios fuera de las 13 primitivas y de los identificadores de objetivo permitidos por el motor.
- Si una futura optimización requiere un mecanismo técnico nuevo no cubierto por las 13 primitivas, exige actualizar el código del motor, auditar la nueva primitiva y publicar una nueva versión del esquema.

## Límites y riesgos conocidos
- Aunque las primitivas son cerradas, primitivas como `service.setStartType`, `registry.setValue` o `appx.remove` siguen teniendo capacidad de degradar funciones de Windows si se parametrizan incorrectamente en una ficha; por ello la seguridad depende también de la clasificación de componentes ([ADR-0009](0009-component-classification.md)), de la verificación en laboratorio y de la firma del catálogo oficial.
- `repair.run` y `network.flushDns` son acciones operativas cuyo efecto transitorio no es "deshacible" como un valor de registro; su método de reversión y cobertura de snapshot deben declararlo explícitamente ([ADR-0006](0006-restoration-strategy.md)).

## Preguntas abiertas
- Definir el formato del manifiesto de firma criptográfica del paquete del catálogo oficial (por ejemplo, firma Ed25519 o CMS/PKCS#7 Authenticode).
