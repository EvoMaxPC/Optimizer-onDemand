# Política Operativa de Fuentes y Referencias (`REFERENCE_POLICY`)

> Este documento traduce [ADR-0015](../adr/0015-source-and-reference-policy.md) en reglas operativas de cumplimiento obligatorio para cualquier contribuidor humano o agente de IA que redacte documentación o fichas del catálogo.

---

## 1. Distinción entre Hechos de Windows y Material de Terceros

1. **Hechos técnicos (libres de propiedad exclusiva):**
   - Nombres e identificadores de servicios de Windows en el Service Control Manager (SCM).
   - Rutas de claves y nombres de valores del Registro de Windows definidos por el sistema operativo.
   - GUIDs de esquemas y subgrupos de energía de Windows.
   - Nombres de tareas programadas y características opcionales nativas de Windows.
2. **Expresión y compilación protegida (PROHIBIDO COPIAR):**
   - Código fuente, funciones o scripts PowerShell/Batch/C#/Rust de otros proyectos.
   - Archivos JSON, YAML o XML que estructuran listas de cambios en proyectos como **WinUtil**, **AtlasOS** u otros optimizadores.
   - Textos descriptivos, advertencias o traducciones redactadas por terceros.

---

## 2. Reglas para Crear una Nueva Ficha del Catálogo (Etapa 6+)

Al proponer una nueva ficha en `catalog/`, el autor debe cumplir estos pasos:

1. **Uso de proyectos externos solo como mapa temático:** puedes observar qué áreas (por ejemplo, "servicios de impresión" o "configuración de Game DVR") preocupan a los usuarios en proyectos como WinUtil o AtlasOS, pero **jamás** copiar su implementación, sus valores sin verificar ni su redacción.
2. **Investigación desde fuentes primarias:**
   - Consultar documentación técnica de Microsoft (referencias de políticas ADMX, documentación de Windows Client/Server, guías de rendimiento de Microsoft, herramientas Sysinternals).
   - Si en fase de borrador (`status: "draft"`) una referencia documental aún no ha sido verificada, es obligatorio escribir literalmente **`PENDIENTE: fuente`** en el campo `sources[].reference`. **Está prohibido inventar URLs, códigos de artículos KB o citas inexistentes.**
3. **Redacción original bilingüe:** escribir desde cero los campos `title`, `technicalDescription`, `rationale`, `impactIfDisabled` y `restorationNote` en español (`es`) e inglés (`en`).
4. **Verificación en el laboratorio propio de EVO:** ejecutar la prueba `apply → verify → revert` en las máquinas virtuales de referencia antes de solicitar el cambio de estado a `lab-tested`.

---

## 3. Regla ante Cualquier Reutilización Externa

En la Etapa F0 y en el catálogo de EVO **no se reutiliza código ni contenido de terceros**. Si en una etapa futura se propusiera incorporar una biblioteca o artefacto externo:

1. Verificar primero su licencia exacta y su compatibilidad con la licencia que defina el propietario en [`DECISIONS_PENDING.md`](../process/DECISIONS_PENDING.md).
2. Documentar la justificación técnica, el origen, el hash y las obligaciones de atribución en el Pull Request correspondiente.
3. Obtener aprobación explícita de revisión humana antes de fusionar.
