# ADR-0019: Manifiesto de Módulos

## Estado

Aceptado

## Fecha

2026-11-22

## Contexto

En la Etapa 3 (`F1b`), la interfaz de escritorio (`apps/desktop`) debe presentar los 6 módulos funcionales de EVO Optimizer (`gaming`, `memory`, `system`, `network`, `storage`, `security`) sin motor conectado, sin datos simulados y sin botones de acción. Para evitar listas duplicadas entre la documentación, las pruebas y los componentes React, se necesita una única fuente declarativa que defina los módulos, su reparto sobre las 17 categorías operativas y sus capacidades planificadas.

## Decisión

1. **Manifiesto como fuente única:** Se define `apps/desktop/src/modules/modules.manifest.json`, validado estructuralmente por `schemas/modules-manifest.schema.json` (Draft 2020-12) y por la regla `R12-MODULES-MANIFEST` en `tools/check-docs.mjs`.
2. **Reparto cerrado de categorías 1..17:** `gaming` (`[5, 7, 8]`), `memory` (`[6]`), `system` (`[1, 2, 3, 11, 13, 14, 15, 16, 17]`), `network` (`[10]`), `storage` (`[9]`), `security` (`[4, 12]`). La categoría 18 queda fuera del manifiesto.
3. **UI generada:** Los componentes de React (`ModulesPage`, `ModuleDetailPage`) iteran exclusivamente el manifiesto validado en tiempo de arranque; si el manifiesto es inválido, se renderiza una pantalla de error honesta.
4. **Estado real en Etapa 4:** El manifiesto no contiene ningún campo `estado`. Las capacidades (`probes`, `dossiers`, `actions`) representan lo planificado; el estado real vendrá del motor `.NET` en la Etapa 4 (`F2`).

## Alternativas descartadas

1. **Definir los 6 módulos como constantes TypeScript dispersas en componentes:**
   - *Motivo de descarte:* Genera duplicación, dificulta la validación documental en CI y rompe la trazabilidad con el esquema.
2. **Incluir textos visibles o estado simulado dentro del manifiesto:**
   - *Motivo de descarte:* Viola la separación i18n (`es`/`en` en `src/i18n/locales/`) y la regla de honestidad de Etapa 3.

## Consecuencias

- **Positivas:** Fuente única auditable en CI (`R12-MODULES-MANIFEST`) y en Vitest; cero listas hardcodeadas en la UI.
- **Negativas / Coste asumido:** Cualquier cambio de módulos o categorías exige actualizar el manifiesto, el esquema y los locales `es`/`en`.

## Límites y riesgos conocidos

- `R12-MODULES-MANIFEST` realiza validación estructural sin dependencias externas; no sustituye a un validador formal completo de JSON Schema Draft 2020-12.
- Hasta la Etapa 4 (`F2`), todos los módulos muestran únicamente `"No disponible: motor no conectado"`.

## Preguntas abiertas

- Definir en la Etapa 4 (`F2`) cómo se mapea el handshake del motor `.NET` sobre las capacidades planificadas de cada módulo.
