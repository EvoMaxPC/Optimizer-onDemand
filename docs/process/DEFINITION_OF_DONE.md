# Definición de Terminado (`DEFINITION_OF_DONE`)

Ninguna tarea, ficha del catálogo, módulo o etapa de EVO OPTIMIZER puede marcarse como completada ni fusionarse en `main` si no satisface simultáneamente los cinco pilares de esta Definición de Terminado (DoD):

---

## 1. Compilado y Validado Estáticamente
- El código o artefacto de la etapa correspondiente compila sin errores ni advertencias (*warnings*) bloqueantes.
- Todos los esquemas JSON, tipos de TypeScript y contratos de C# validan sin discrepancias.
- El script de validación del repositorio (`node tools/check-docs.mjs` y los validadores de etapas posteriores) finaliza con código de salida `0`.

## 2. Probado y Verificado con Evidencia Real
- Toda lógica añadida cuenta con pruebas automatizadas verificables en CI.
- En el informe del Pull Request se separa rigurosamente:
  - **`VERIFICADO`**: comandos efectivamente ejecutados adjuntando su salida literal.
  - **`NO VERIFICADO`**: cualquier componente escrito que no haya podido ejecutarse en el entorno actual.
- Para fichas del catálogo (Etapa 6+), ninguna ficha pasa a `lab-tested` sin haber superado la prueba automatizada `apply → verify → revert` dejando el sistema en estado idéntico al original ([CATALOG_SPEC.md](../catalog/CATALOG_SPEC.md)).

## 3. Auditado en Seguridad y Gobernanza
- Cumple íntegramente las 12 reglas de producto y las reglas de arquitectura de [`SAFETY_RULES.md`](../security/SAFETY_RULES.md).
- No introduce ejecución de comandos libres, scripts PowerShell/Batch ni rutas arbitrarias.
- No añade permisos de Tauri (`capabilities`), cambios de CSP ni dependencias externas sin justificación explícita y revisión humana ([`CHECKPOINTS.md`](CHECKPOINTS.md)).
- Si altera o introduce una decisión arquitectónica, incluye la actualización o creación del ADR correspondiente siguiendo la plantilla oficial, con la sección **"Límites y riesgos conocidos"** redactada con honestidad.

## 4. Documentado con Rigor
- La documentación de usuario y arquitectura está escrita en **español** claro y preciso, manteniendo los identificadores, nombres de archivos y claves técnicas en **inglés**.
- Todos los enlaces relativos entre documentos funcionan y son verificados automáticamente por `tools/check-docs.mjs`.
- Las fuentes no verificadas se marcan explícitamente como `PENDIENTE: fuente`, sin inventar jamás enlaces ni referencias.

## 5. Cero Simulación Presentada como Real
- **Prohibición absoluta de engaño:** ninguna pantalla, comando o informe puede presentar datos falsos (*mocks*, cifras aleatorias, porcentajes de mejora prefabricados o estados "Implementado" ficticios) como si fueran lecturas o resultados reales del sistema operativo.
- Todo componente aún no construido debe declararse explícitamente como **`"No implementado"`** junto con la etapa en la que se abordará.
