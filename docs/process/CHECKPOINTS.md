# Gobernanza de Proceso y Hoja de Ruta por Etapas (`CHECKPOINTS`)

## 1. Reglas de Gobernanza del Repositorio y Control de Cambios

1. **Rama `main` protegida:** quedan prohibidos los *pushes* directos a `main`. Todo cambio requiere un **Pull Request (PR)** obligatorio.
2. **Puertas de fusión en cada PR:**
   - Ejecución en verde de todos los workflows de CI requeridos (`.github/workflows/ci.yml`).
   - Al menos **1 revisión humana explícita** antes de fusionar.
   - Plantilla de PR (`.github/PULL_REQUEST_TEMPLATE.md`) completada sin casillas de seguridad omitidas.
3. **Límites para Agentes de IA:** un agente de IA solo puede proponer cambios estrictamente dentro del alcance de la etapa o tarea asignada en el PR. Tiene prohibido adelantar código de etapas futuras, inventar resultados de pruebas o modificar reglas de seguridad sin solicitud expresa.
4. **Rutas de Revisión Crítica Explícita:** cualquier modificación sobre los siguientes elementos exige revisión humana específica y justificación arquitectónica:
   - `docs/adr/` y `docs/security/`
   - `schemas/`
   - `capabilities/` y políticas CSP de Tauri (desde Etapa 2 / `F1a`)
   - `.github/workflows/` y scripts de CI (`tools/`)
   - Implementación de `SafetyPolicy`, `ApprovalToken` y `Recovery Guard`
5. **Control de Dependencias y Cadena de Suministro:**
   - **Ninguna dependencia externa** (`npm`/`pnpm`, `cargo`, `NuGet`) puede añadirse sin justificación técnica escrita en `docs/process/DEPENDENCIES.md` y en el PR.
   - En la Etapa 1 (`F0`) existen **cero dependencias externas**.
   - A partir de la Etapa 2 (`F1a`), todos los archivos de bloqueo (**`lockfiles`**) generados por la herramienta oficial deben estar versionados en Git y validados en CI.
   - Las acciones de GitHub Actions se fijan mediante el **SHA de commit completo de 40 caracteres** (mientras no pueda verificarse el commit exacto en un entorno sin acceso al historial de la acción, se mantiene con `# TODO: fijar por SHA de commit completo (40 caracteres)`).

---

## 2. Regla Bloqueante de Seguridad Operativa

> **REGLA INVIOLABLE:** Ninguna acción real de escritura sobre Windows podrá ejecutarse ni habilitarse en la aplicación hasta que la **Etapa 9 (`F6`: `Recovery Guard`, reinicio y rollback)** esté completamente implementada y verificada con **pruebas automatizadas de rollback** (incluyendo simulación de fallo y reversión post-reinicio).

---

## 3. Mapa Oficial de Numeración (`Etapa ↔ Sprint`)

Esta tabla es la referencia única de numeración en todos los documentos del repositorio:

| Etapa | Sprint | Contenido | Modifica Windows | Puerta de Salida (Checkpoint) |
|---|---|---|---|---|
| **1** | **F0** | **Repositorio, documentación, CI de docs** | **NO** | `node tools/check-docs.mjs` en verde + auditoría documental F0. |
| **2** | **F1a** | **Tauri + React, tema, layout, i18n, router** | **NO** | Pruebas de UI, i18n y guardias de arquitectura pasando; superficie Tauri mínima sin comandos ni plugins. |
| **3** | **F1b** | **6 módulos por manifiesto, estado honesto** | **NO** | Navegación declarativa de los 6 módulos mostrando estado honesto sin conexión al motor. |
| **4** | **F2** | **Motor de lectura .NET + JSON-RPC stdio** | **NO (Solo lectura)** | Creación de `Engine.Host` (.NET self-contained) sin elevación y contrato JSON-RPC `stdio` con la UI. |
| **5** | **F3** | **SQLite, migraciones, evidence chain** | **NO (Solo lectura)** | Base de datos SQLite en modo WAL, migraciones numeradas con checksum y `Evidence Chain` (HMAC+DPAPI). |
| **6** | **F4a** | **`Adaptive Core`, catálogo, clasificador, `Planner`** | **NO (Solo lectura)** | `SystemProfile`, `UserProfile`, `ApplicabilityProfile`, validador de fichas en `catalog/` y pruebas del `Planner`. |
| **7** | **F4b** | **Acciones, `SafetyPolicy`, snapshots, transacciones** | **NO (En aislamiento)** | Lógica de evaluación de `SafetyPolicy`, niveles de consentimiento, `ApprovalToken` y captura de snapshots propios. |
| **8** | **F5** | **Ejecutor elevado, canal protegido** | **NO (En aislamiento)** | `Engine.Elevated`, Named Pipe con ACL por SID, verificación PID/ruta/Authenticode, acuerdo ECDH y resolución de [ADR-0017](../adr/0017-user-scope-in-elevated-executor.md). |
| **9** | **F6** | **`Recovery Guard`, reinicio, rollback** | **Solo en VMs de laboratorio** | Máquina de estados de reinicio, persistencia en `%ProgramData%\EVO\recovery` y **tests automatizados de rollback superados**. |
| **10** | **F7** | **Instalador, firma, WebView2** | **SÍ (Bajo autorización, snapshot y Recovery Guard)** | Empaquetado final, detección/instalación de WebView2 Runtime, firma Authenticode y validación en Win10 22H2 (build 19045 — `PENDIENTE: fuente`) y Win11 (propuesta provisional build 22621 — `PENDIENTE: fuente`). |
