# EVO OPTIMIZER (`Optimizer-onDemand`)

> **Lema:** *"Tu Windows. Tus reglas. Tu hardware. No lo creas, compruébalo."*

**Estado actual:** **Etapa 2: shell de escritorio sin motor; no lee ni modifica Windows.**

> **Aviso de Licencia y Contribuciones (D2):**
> *"Licencia prevista: GPL-3.0-or-later. El archivo LICENSE aún no está añadido. No se aceptan contribuciones externas hasta definir el mecanismo de cesión (CLA)."*

---

## 1. ¿Qué es EVO OPTIMIZER?

**EVO OPTIMIZER** (alojado en el repositorio `Optimizer-onDemand`) es un sistema de adaptación técnica para Windows basado en una premisa central: **Windows debe adaptarse al usuario, no el usuario a Windows**.

No es una lista estática de *tweaks*, ni un script opaco de desactivación masiva, ni un clon de WinUtil o AtlasOS. Su principio operativo recorre seis fases verificables:

**diagnosticar → recomendar → autorizar → ejecutar → verificar → revertir**

- **CONOCER:** inspeccionar en modo de solo lectura el hardware real, la compilación de Windows, los servicios activos y sus dependencias.
- **ADAPTAR:** traducir la intención del usuario (**Gaming**, **Oficina**, **Desarrollo**, **Multimedia**, **Bajo consumo**, **PC antigua**, **Personalizado**) y las capacidades de su máquina en un plan individualizado.
- **OPTIMIZAR:** aplicar cambios reales únicamente con autorización explícita del usuario mediante un catálogo cerrado de primitivas tipadas, nunca ejecutando scripts arbitrarios.
- **RECUPERAR:** conservar el estado exacto anterior mediante snapshots propios granulares y un guardián de transacciones con reinicio (`Recovery Guard`).
- **DEMOSTRAR:** contrastar con mediciones antes y después (muestras individuales, mediana y desviación) si el cambio produjo una `mejora medible`, `sin cambio medible` o `empeora`.

---

## 2. Estado Actual del Proyecto y Soporte Objetivo

- **Estado actual:** **Etapa 2: shell de escritorio sin motor; no lee ni modifica Windows** (Sprint `F1a`, ver [`docs/process/CHECKPOINTS.md`](docs/process/CHECKPOINTS.md)).
- [`apps/desktop/`](apps/desktop/README.md): Shell de escritorio Tauri v2 + React + TypeScript con navegación base (`Resumen`, `Historial`, `Ajustes`), tema oscuro e internacionalización `es`/`en` en memoria. **Sin conexión a motor y sin módulos especialistas (los 6 módulos se abordan en la Etapa 3 / `F1b` y el motor de lectura en la Etapa 4 / `F2`).**
- [`engine/`](engine/README.md): **No implementado** (se implementa desde la Etapa 4 / `F2`).
- [`catalog/`](catalog/README.md): **Catálogo vacío** (sin fichas hasta la Etapa 6 / `F4a`).
- [`schemas/action.schema.json`](schemas/action.schema.json): **Borrador F0**, con unión discriminada `changes[]`, no validado contra Windows real.
- **Visibilidad del repositorio:** **PÚBLICO** (decidido por el propietario).
- **Soporte objetivo de Windows:** **Windows 10 22H2 (build 19045 — `PENDIENTE: fuente`) y Windows 11** (con propuesta provisional de build mínima 22621 a validar en laboratorio — `PENDIENTE: fuente`).
- **Licencia:** Licencia prevista: GPL-3.0-or-later. El archivo LICENSE aún no está añadido. No se aceptan contribuciones externas hasta definir el mecanismo de cesión (CLA).

---

## 3. Mapa de Documentación

- **Principios y Filosofía:** [`docs/PRINCIPLES.md`](docs/PRINCIPLES.md)
- **Resumen de Arquitectura:** [`docs/architecture/OVERVIEW.md`](docs/architecture/OVERVIEW.md)
- **Requisitos de Tiempo de Ejecución:** [`docs/architecture/RUNTIME_REQUIREMENTS.md`](docs/architecture/RUNTIME_REQUIREMENTS.md)
- **Índice de ADRs (0001 a 0018):** [`docs/adr/README.md`](docs/adr/README.md)
- **Modelo de Amenazas:** [`docs/security/THREAT_MODEL.md`](docs/security/THREAT_MODEL.md)
- **Reglas Inviolables de Seguridad:** [`docs/security/SAFETY_RULES.md`](docs/security/SAFETY_RULES.md)
- **Inteligencia de Optimización:** [`docs/intelligence/OPTIMIZATION_INTELLIGENCE.md`](docs/intelligence/OPTIMIZATION_INTELLIGENCE.md)
- **Especificación del Catálogo:** [`docs/catalog/CATALOG_SPEC.md`](docs/catalog/CATALOG_SPEC.md)
- **Categorías y Oleadas (1 a 18):** [`docs/catalog/CATEGORIES.md`](docs/catalog/CATEGORIES.md)
- **Política de Fuentes y Referencias:** [`docs/catalog/REFERENCE_POLICY.md`](docs/catalog/REFERENCE_POLICY.md)
- **Hoja de Ruta y Mapa Etapa↔Sprint:** [`docs/process/CHECKPOINTS.md`](docs/process/CHECKPOINTS.md)
- **Definición de Terminado (DoD):** [`docs/process/DEFINITION_OF_DONE.md`](docs/process/DEFINITION_OF_DONE.md)
- **Política e Inventario de Dependencias:** [`docs/process/DEPENDENCIES.md`](docs/process/DEPENDENCIES.md)
- **Decisiones del Propietario y Pendientes:** [`docs/process/DECISIONS_PENDING.md`](docs/process/DECISIONS_PENDING.md)
- **Política de Seguridad y Reporte:** [`SECURITY.md`](SECURITY.md)

---

## 4. Validación y Construcción (Etapa 2 / `F1a`)

```bash
node tools/check-docs.mjs
pnpm --filter @evo/desktop typecheck
pnpm --filter @evo/desktop test
pnpm --filter @evo/desktop build
```
