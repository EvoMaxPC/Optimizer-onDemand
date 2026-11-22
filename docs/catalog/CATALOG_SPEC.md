# Especificación del Catálogo de Acciones (`CATALOG_SPEC`)

> **Estado actual:** Especificación y borrador de esquema (`schemas/action.schema.json`). El directorio `catalog/` no contiene fichas operativas hasta la **Etapa 6 (`F4a`)**.

---

## 1. Estructura de la Ficha de Acción (19 Campos Fundamentales + Metadatos de Gobernanza)

Toda ficha del catálogo es un documento JSON estructurado según [`schemas/action.schema.json`](../../schemas/action.schema.json) que contiene obligatoriamente los 19 conceptos fundamentales y los metadatos de ciclo de vida y consentimiento:

| # | Campo Técnico (`action.schema.json`) | Concepto Obligatorio | Descripción |
|---|---|---|---|
| 1 | `id` | ID único | Identificador jerárquico inmutable (ej. `system.services.example-draft-action`). |
| 2 | `category` | Categoría | Una de las 18 categorías cerradas definidas en [`CATEGORIES.md`](CATEGORIES.md). |
| 3 | `i18n.{es,en}.technicalDescription` | Descripción técnica | Explicación rigurosa en español e inglés de qué hace el componente y qué modifica la acción. |
| 4 | `i18n.{es,en}.rationale` | Motivo | Razón técnica por la que el cambio puede ser deseable y bajo qué condiciones (`impactIfDisabled` y `restorationNote` incluidos). |
| 5 | `windowsCompatibility` | Windows compatible | Versiones (`Win10-22H2`, `Win11-23H2`, `Win11-24H2` — correspondencia de builds `PENDIENTE: fuente`), ediciones y `minBuild`/`maxBuild`. |
| 6 | `hardwareCompatibility` | Hardware compatible | Arquitecturas (`x64`, `arm64`), factores de forma, tipos de almacenamiento y RAM mínima. |
| 7 | `changes[].expectedOriginal*` | Estado original | Estado original esperado dentro de cada operación tipada de la unión discriminada `changes[]` antes de aplicar. |
| 8 | `changes[].proposed*` | Estado propuesto | Estado propuesto mediante la unión discriminada `changes[]` por `primitive` (`oneOf` con `const`), con campos tipados y patrones restringidos sin cadenas libres. |
| 9 | `dependencies` | Dependencias | Servicios, controladores o acciones de los que depende este componente. |
| 10 | `conflicts` | Conflictos | Otras fichas o condiciones de software incompatibles con esta acción. |
| 11 | `riskLevel` | Nivel de riesgo | `low`, `medium`, `high` o `critical`. |
| 12 | `reversibility` | Reversibilidad | `Full` (total), `Partial` (parcial) o `None` (no reversible). |
| 13 | `snapshotRequired` + `snapshotCoverage` | Snapshot requerido | Booleano más declaración explícita de qué cubre (`covered`) y qué no cubre (`notCovered`) el snapshot propio ([ADR-0006](../adr/0006-restoration-strategy.md)). |
| 14 | `rebootRequired` | Reinicio requerido | Booleano que activa la máquina de estados y `Recovery Guard` si es `true` ([ADR-0010](../adr/0010-reboot-transactions-and-recovery-guard.md)). |
| 15 | `verificationMethod` | Método de verificación | Primitiva de lectura (`readbackPrimitive`) y `verifyAllChangesMatchProposed: true` para comprobar que cada elemento de `changes[]` quedó aplicado. |
| 16 | `restorationMethod` | Método de restauración | Estrategia, primitiva de reversión y si exige Punto de Restauración de Windows o confirmación de medio de arranque. |
| 17 | `evidenceBeforeAfter` | Evidencia antes/después | Sondas de evidencia local requeridas (`localEvidenceProbeIds`), métricas a muestrear (`metricsToMeasure`), mínimo de muestras (`minSampleCount`) y criterio de veredicto. |
| 18 | `consentLevel` + `componentClass` | Consentimiento y Clase | Nivel de consentimiento (`standard`, `advanced`, `reinforced`, `expert`) y clase (`Expendable`, `Optional`, `FeatureDependent`, `Critical`, `Undetermined`). |
| 19 | `version`, `status`, `sources`, `scope`, `requiredByFeatures`, `goals` | Gobernanza y Ámbito | Versión SemVer, estado del ciclo (`draft`, `lab-tested`, `stable`, `deprecated`), fuentes documentales, ámbito (`user`/`machine`), funciones que lo requieren y objetivos. |

---

## 2. Unión Discriminada `changes[]` y Lista Cerrada de 13 Primitivas (`primitive`)

Ninguna ficha puede invocar lógica fuera de estas **13 primitivas cerradas** ([ADR-0012](../adr/0012-versioned-closed-catalog.md)). En `schemas/action.schema.json`, la propiedad `changes` es un array cuyos elementos se validan mediante una **unión discriminada (`oneOf` con `const` sobre `primitive`)**, exigiendo `additionalProperties: false` y patrones estrictos sin rutas ni comandos libres:

1. `registry.setValue`: `hive` (`HKCU` | `HKLM`), `subkey` (patrón acotado bajo `SOFTWARE`, `SYSTEM`, `Control Panel` o `Environment`), `valueName`, `valueType` (`REG_DWORD` | `REG_QWORD` | `REG_SZ`), `expectedOriginalValue` y `proposedValue`.
2. `registry.deleteValue`: `hive` (`HKCU` | `HKLM`), `subkey` (patrón acotado), `valueName`, `valueType` y `expectedOriginalValue`.
3. `service.setStartType`: `serviceName` (patrón `^[A-Za-z0-9_-]{2,64}$`), `expectedOriginalStartType` y `proposedStartType` (`Automatic` | `AutomaticDelayedStart` | `Manual` | `Disabled`).
4. `service.stop`: `serviceName` (patrón acotado), `expectedOriginalStatus` (`Running`) y `proposedStatus` (`Stopped`).
5. `task.setEnabled`: `taskPath` (patrón acotado bajo `\Microsoft\Windows\...`), `taskName` (patrón acotado), `expectedOriginalEnabled` y `proposedEnabled` (booleanos).
6. `startup.setEnabled`: `startupLocation` (`HKCU_Run` | `HKLM_Run` | `StartupFolder_User` | `StartupFolder_Common`), `entryName` (patrón acotado), `expectedOriginalEnabled` y `proposedEnabled`.
7. `power.setValue`: `schemeGuid`, `subgroupGuid` y `settingGuid` (patrón UUID/GUID de 36 caracteres), `powerAcDc` (`AC` | `DC` | `Both`), `expectedOriginalIndexValue` y `proposedIndexValue` (enteros).
8. `network.setDns`: `adapterScope` (`active-default-gateway-adapter` | `all-physical-ethernet` | `all-physical-wifi`), `addressFamily` (`IPv4` | `IPv6`), `primaryDns` y `secondaryDns` (patrón IPv4/IPv6 acotado).
9. `network.flushDns`: `operation` (`flush-resolver-cache`).
10. `feature.setState`: `featureName` (patrón `^[A-Za-z0-9_.-]{3,96}$`), `expectedOriginalState` y `proposedState` (`Enabled` | `Disabled`).
11. `file.cleanup`: `cleanupTargetId` como **enum cerrado de identificadores compilados en el motor** (`user-temp-dir`, `system-temp-dir`, `windows-update-download-cache`, `directx-shader-cache`, `delivery-optimization-cache`, `thumbnail-cache`; **nunca una ruta libre**) y `minAgeMinutes` (entero).
12. `repair.run`: `repairOperation` como **enum de operaciones fijas sin parámetros libres** (`sfc-scannow`, `dism-check-health`, `dism-scan-health`, `dism-restore-health`, `dism-start-component-cleanup`).
13. `appx.remove`: `packageName` (patrón `^[A-Za-z0-9._-]{5,128}$`) y `removalScope` (`current-user` | `provisioned-and-current-user`).

---

## 3. Reglas del Validador de CI del Catálogo (Etapa 6 / `F4a`+)

El validador estático de CI rechazará con fallo bloqueante cualquier ficha que incurra en cualquiera de las siguientes condiciones:

1. **Sin método de reversión válido:** carece de `restorationMethod` o define `restorePrimitive: "none"`, **salvo** que declare explícitamente `reversibility: "None"` y exija `consentLevel: "reinforced"` o `"expert"`.
2. **Sin medición declarando beneficio:** incluye objetivos de rendimiento/recursos en `goals` pero no especifica métricas verificables en `evidenceBeforeAfter.metricsToMeasure` o tiene `minSampleCount < 1`.
3. **Primitiva fuera de la lista cerrada:** utiliza un valor de `primitive` o una rama de `changes[]` no perteneciente a las 13 primitivas cerradas.
4. **Comandos o rutas libres:** incluye propiedades no admitidas (`additionalProperties: false`) o incumple los patrones acotados de `changes[]`.
5. **Sin traducciones completas `es` y `en`:** falta cualquiera de los bloques `i18n.es` o `i18n.en` o alguno de sus campos obligatorios está vacío.
6. **Componente crítico sin Modo Experto:** tiene `componentClass: "Critical"` o `"Undetermined"` pero su `consentLevel` es distinto de `"expert"`.

---

## 4. Criterios Obligatorios para Pasar de `draft` a `lab-tested`

Ninguna ficha puede promocionarse del estado `draft` a `lab-tested` (ni mucho menos a `stable`) sin cumplir y adjuntar evidencia de los cuatro requisitos siguientes:

1. **Fuentes citadas y verificadas:** referencias reales a documentación técnica de Microsoft y acta de prueba en laboratorio propio (sin marcadores `PENDIENTE: fuente`).
2. **Prueba automatizada `apply → verify → revert` en máquina virtual:** ejecución automatizada en entorno aislado que aplica la acción, verifica el cambio, ejecuta la reversión desde el snapshot de EVO y comprueba que **el estado final es bit a bit idéntico al estado inicial**.
3. **Matriz de compilaciones de Windows superada:** verificación en máquinas virtuales con **Windows 10 22H2 (build 19045 — `PENDIENTE: fuente`)** y **Windows 11 (`23H2`, `24H2` y posteriores; propuesta provisional de build mínima 22621 — `PENDIENTE: fuente`)**.
4. **Métricas y veredicto documentados:** registro de muestras individuales antes y después en laboratorio demostrando que el protocolo de medición produce un veredicto reproducible (`mejora medible` o `sin cambio medible`, sin regresiones).

---

## 5. Ejemplo de Ficha en Estado Borrador (`status: "draft"`)

> **ADVERTENCIA — ILUSTRATIVO, NO VERIFICADO: no usar.**
> El siguiente JSON muestra únicamente la conformidad estructural con `schemas/action.schema.json` y su unión discriminada `changes[]`. Los identificadores de objetivo, números de compilación (`"minBuild": 19045` — `PENDIENTE: fuente`) y referencias están marcados como borrador F0 y no han sido ejecutados ni probados en laboratorio.

```json
{
  "id": "system.services.example-draft-action",
  "version": "0.1.0",
  "status": "draft",
  "category": "services",
  "scope": "machine",
  "componentClass": "FeatureDependent",
  "consentLevel": "advanced",
  "primitive": "service.setStartType",
  "i18n": {
    "es": {
      "title": "ILUSTRATIVO, NO VERIFICADO: no usar - Inicio manual de servicio dependiente de función",
      "technicalDescription": "Ficha de ejemplo ilustrativo que muestra la unión discriminada changes[] para el cambio de tipo de inicio de un servicio.",
      "rationale": "Ejemplo estructural para comprobar los campos de CATALOG_SPEC sin representar una recomendación real verificada.",
      "impactIfDisabled": "Si el servicio de ejemplo se pasa a manual o deshabilitado, la función asociada no iniciará automáticamente.",
      "restorationNote": "Se restaura el tipo de inicio original exacto guardado en el snapshot propio de EVO."
    },
    "en": {
      "title": "ILLUSTRATIVE, UNVERIFIED: do not use - Manual start for feature-dependent service",
      "technicalDescription": "Illustrative example card demonstrating the discriminated union changes[] for changing a Windows service start type.",
      "rationale": "Structural example to check CATALOG_SPEC fields without representing a verified recommendation.",
      "impactIfDisabled": "If the example service is set to manual or disabled, the associated feature will not start automatically.",
      "restorationNote": "Restores the exact original start type stored in EVO's own snapshot."
    }
  },
  "windowsCompatibility": {
    "supportedReleases": [
      "Win10-22H2",
      "Win11-23H2",
      "Win11-24H2"
    ],
    "editions": [
      "Home",
      "Pro",
      "Enterprise",
      "Education"
    ],
    "minBuild": 19045
  },
  "hardwareCompatibility": {
    "architectures": [
      "x64",
      "arm64"
    ],
    "formFactors": [
      "desktop",
      "laptop",
      "workstation"
    ],
    "storageTypes": [
      "any"
    ],
    "minRamGb": 4
  },
  "changes": [
    {
      "primitive": "service.setStartType",
      "serviceName": "ExampleServiceNotVerified",
      "expectedOriginalStartType": "Automatic",
      "proposedStartType": "Manual"
    }
  ],
  "dependencies": [],
  "conflicts": [],
  "requiredByFeatures": [
    "example-optional-peripheral"
  ],
  "goals": [
    "memory-footprint",
    "background-noise-reduction"
  ],
  "riskLevel": "low",
  "reversibility": "Full",
  "snapshotRequired": true,
  "snapshotCoverage": {
    "covered": [
      "Tipo de inicio original del servicio en el Service Control Manager (Start)"
    ],
    "notCovered": [
      "Estado en memoria de trabajos en curso si el servicio se detiene durante la sesión"
    ]
  },
  "rebootRequired": false,
  "verificationMethod": {
    "readbackPrimitive": "service.readState",
    "verifyAllChangesMatchProposed": true
  },
  "restorationMethod": {
    "strategy": "restore-from-evo-snapshot",
    "restorePrimitive": "service.setStartType",
    "requiresSystemRestorePoint": false,
    "requiresBootMediaConfirmation": false
  },
  "evidenceBeforeAfter": {
    "requiresLocalEvidenceToRecommend": true,
    "localEvidenceProbeIds": [
      "probe.system.service-resource-and-usage"
    ],
    "metricsToMeasure": [
      "service_working_set_bytes"
    ],
    "minSampleCount": 5,
    "expectedVerdictCriterion": "median_reduction_required"
  },
  "sources": [
    {
      "kind": "pending-verification",
      "reference": "PENDIENTE: fuente - Ficha ilustrativa de Etapa F0 no verificada en laboratorio"
    }
  ]
}
```
