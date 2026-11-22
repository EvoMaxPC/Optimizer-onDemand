# Esquemas de Contrato (`schemas`)

**Estado actual:** Borrador F0 (`action.schema.json`), no validado contra Windows real.

## Propósito

Contiene los contratos formales JSON Schema (Draft 2020-12) que gobiernan la comunicación y validación estática entre el catálogo declarativo, el motor (`Engine.Host` / `Engine.Elevated`) y la interfaz de usuario ([ADR-0001](../docs/adr/0001-monorepo-and-stack.md), [ADR-0012](../docs/adr/0012-versioned-closed-catalog.md)).

## Archivos

- [`action.schema.json`](action.schema.json): borrador de esquema para las fichas del catálogo de acciones. Impone `additionalProperties: false`, lista cerrada de primitivas (`primitive`), niveles de consentimiento (`consentLevel`), clases de componente (`componentClass`), reversibilidad (`reversibility`), estados del ciclo de vida (`status`) y textos bilingües (`es` y `en`), prohibiendo cualquier campo de comando libre, script o ruta arbitraria.
