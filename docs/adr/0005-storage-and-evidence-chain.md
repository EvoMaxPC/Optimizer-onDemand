# ADR-0005: Almacenamiento SQLite, Muestras Individuales y Cadena de Evidencias

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
EVO OPTIMIZER necesita almacenar diagnósticos, snapshots, transacciones de cambios, mediciones antes/después y eventos de auditoría de forma atómica, auditable y resistente a cierres inesperados. Asimismo, para cumplir el principio *"No lo creas, compruébalo"*, no basta con guardar un promedio opaco de rendimiento ni un registro de texto plano fácil de alterar sin dejar rastro.

## Decisión
1. **Motor de almacenamiento:** usar **SQLite** configurado obligatoriamente con `PRAGMA foreign_keys = ON;` y modo de diario **`PRAGMA journal_mode = WAL;`**.
2. **Transacciones y migraciones:** toda operación de cambio o captura de evidencias se ejecuta dentro de una transacción explícita. El esquema evoluciona mediante **migraciones SQL numeradas secuencialmente con verificación de checksum SHA-256** al iniciar.
3. **Tabla de muestras individuales:** las mediciones antes y después se guardan como filas individuales con su marca de tiempo y contexto en una tabla dedicada; la **mediana** y la **desviación** (por ejemplo, MAD o rango intercuartílico) se derivan de dichas muestras verificables, nunca se guardan como cifras agregadas sin respaldo.
4. **Cadena de evidencias (*Evidence Chain*):** el historial de auditoría y cambios forma una cadena encadenada donde cada registro calcula:
   `entry_hash = HMAC-SHA256(clave_local, prev_hash ‖ JSON_canónico_del_evento)`.
5. **Custodia de clave con DPAPI:** la clave del HMAC se genera localmente por instalación y se almacena cifrada mediante **Windows DPAPI (`ProtectedData`) con entropía adicional propia**.

## Alternativas descartadas
- **Archivos JSON sueltos en disco:** descartados por carecer de transacciones ACID, integridad referencial y resistencia ante cortes de energía durante escrituras concurrentes.
- **Guardar únicamente el porcentaje de mejora o la media aritmética:** descartado porque la media es sensible a valores atípicos (*outliers*) y ocultar las muestras individuales impide auditar la medición.
- **Cadena de hashes simples SHA-256 sin clave (sin HMAC):** descartada porque cualquier proceso que modifique la base de datos podría recalcular trivialmente todos los hashes posteriores sin conocer ningún secreto.

## Consecuencias
- Cualquier edición manual, borrado de filas intermedias o alteración de una muestra histórica en el archivo SQLite rompe la verificación de la `Evidence Chain` y es detectada por EVO.
- El usuario puede inspeccionar o exportar las muestras individuales que justifican cada veredicto (`mejora medible`, `sin cambio medible` o `empeora`).

## Límites y riesgos conocidos
- **Límite honesto sobre la cadena de evidencias:** la `Evidence Chain` con HMAC y DPAPI **detecta la manipulación accidental o no autorizada, pero no la impide físicamente**. Además, un atacante o proceso que corra bajo el mismo usuario de Windows (o como administrador capaz de suplantarlo) y conozca la entropía estática del binario podría invocar DPAPI para descifrar la clave HMAC y reconstruir la cadena completa, o bien eliminar la base de datos entera. Esto se declara expresamente en el modelo de amenazas ([THREAT_MODEL.md](../security/THREAT_MODEL.md)).
- Los archivos `-wal` y `-shm` de SQLite deben incluirse siempre en las operaciones de copia de seguridad de la base de datos.

## Preguntas abiertas
- Definir la política de compactación (`VACUUM`) y rotación del historial de muestras brutas cuando la base de datos supere un umbral de tamaño configurable.
