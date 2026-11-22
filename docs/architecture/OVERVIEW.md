# Resumen de Arquitectura (`OVERVIEW`)

> **Estado de implementación actual (Etapa 2 / `F1a`):** Existe únicamente el **shell de escritorio Tauri v2 + React sin motor** en [`../../apps/desktop/`](../../apps/desktop/) ([ADR-0018](../adr/0018-tauri-surface-and-frontend-dependency-policy.md), [RUNTIME_REQUIREMENTS.md](RUNTIME_REQUIREMENTS.md)). El motor nativo, los módulos especialistas, el almacenamiento SQLite y el ejecutor elevado permanecen **"No implementado"**. Esta versión no lee ni modifica Windows.

## 1. Diagrama General de Componentes y Fronteras de Privilegio

```text
+-----------------------------------------------------------------------------------+
| CONTEXTO DE USUARIO ESTÁNDAR (Sin privilegios de administrador)                   |
|                                                                                   |
|  +---------------------------+          stdio (JSON-RPC)                          |
|  | UI (React + TypeScript)   | <-------------------------------+                  |
|  | [apps/desktop]            |                                 |                  |
|  +-------------+-------------+                                 |                  |
|                | IPC Tauri (Comandos tipados cerrados)         |                  |
|  +-------------v-------------+                                 v                  |
|  | Tauri Shell               |                       +---------+---------------+  |
|  | [apps/desktop]            |                       | Engine.Host (.NET)      |  |
|  +---------------------------+                       | Solo lectura / Sin UAC  |  |
|                                                      |                         |  |
|  +---------------------------------------------------+ Adaptive Core:          |  |
|  | Subsistema de Datos Locales (SQLite WAL)          | - FactStore /           |  |
|  | - Muestras individuales (antes/después)           |   ProfileBuilder        |  |
|  | - Evidence Chain (HMAC-SHA256 + DPAPI)            | - KnowledgeCatalog +    |  |
|  | - Snapshots de usuario                            |   Classifier            |  |
|  +---------------------------------------------------+ - Planner               |  |
|                                                      | - SafetyPolicy          |  |
|                                                      | - MeasurementService    |  |
|                                                      | - Módulos Especialistas |  |
|                                                      +------------+------------+  |
+-------------------------------------------------------------------|---------------+
                                                                    |
                     Named Pipe aleatorio + ACL por SID de usuario  |
                     Verificación PID + Ruta + Authenticode         |
                     Sesión ECDH efímera + Nonce + ApprovalToken    |
                                                                    v
+-----------------------------------------------------------------------------------+
| CONTEXTO ELEVADO BAJO DEMANDA / SYSTEM (Privilegios Administrativos)              |
|                                                                                   |
|  +---------------------------------------+     Plan persistido con ACL restringida|
|  | Engine.Elevated (.NET)                |     (%ProgramData%\EVO\recovery)       |
|  | - Verificador de política indep.      | -------------------+                   |
|  | - Validador de ApprovalToken (HMAC)   |                    |                   |
|  | - Gestor de Snapshots propios         |                    v                   |
|  | - Ejecutor de catálogo cerrado        |     +--------------+----------------+  |
|  | - Diario de transacciones             |     | Recovery Guard (SYSTEM Task)  |  |
|  +---------------------------------------+     | - Contador de arranques       |  |
|                                                | - Verificación post-boot      |  |
|                                                | - Rollback si expira ventana  |  |
|                                                +-------------------------------+  |
+-----------------------------------------------------------------------------------+
```

---

## 2. Inventario de Módulos y Subsistemas

| Componente / Módulo | Tipo | Responsabilidad Principal | Estado Actual (Etapa 2 / `F1a`) |
|---|---|---|---|
| **UI (`apps/desktop`)** | Interfaz | Shell base con tema oscuro, i18n (`es`/`en`) en memoria y rutas `/`, `/history`, `/settings` sin motor ([ADR-0018](../adr/0018-tauri-surface-and-frontend-dependency-policy.md)). | **Implementado en Etapa 2 (`F1a`) — sin motor** |
| **Tauri Shell (`apps/desktop`)** | Contenedor | Anfitrión nativo v2 con CSP estricta, ventana única `main`, permiso `core:default` y cero comandos/plugins ([ADR-0018](../adr/0018-tauri-surface-and-frontend-dependency-policy.md)). | **Implementado en Etapa 2 (`F1a`) — sin puente IPC** |
| **Engine.Host (`engine`)** | Proceso usuario | Hospeda el `Adaptive Core`, ejecuta sondas de solo lectura y gestiona SQLite sin requerir UAC ([ADR-0002](../adr/0002-privilege-separation.md)). | **No implementado** |
| **Adaptive Core** | Transversal | Único decisor del sistema: construye perfiles, evalúa aplicabilidad, resuelve dependencias/conflictos, emite `ApprovalToken` y coordina reinicios ([ADR-0008](../adr/0008-adaptive-core-authority.md)). | **No implementado** |
| **Módulo `Gaming`** | Especialista | Sondas y expedientes de CPU/planificación, GPU (con autorización específica adicional) y latencia de juego. | **No implementado** |
| **Módulo `Memory`** | Especialista | Sondas y expedientes de memoria física, Working Set de servicios, compresión de memoria y archivo de paginación. | **No implementado** |
| **Módulo `System`** | Especialista | Sondas y expedientes de servicios de Windows, inicio/segundo plano, registro, interfaz, energía, funciones opcionales, Windows Update y mantenimiento. | **No implementado** |
| **Módulo `Network`** | Especialista | Sondas de latencia/resolución y expedientes de DNS y pila de red (respetando el driver y con consentimiento específico). | **No implementado** |
| **Módulo `Storage`** | Especialista | Sondas de espacio, tipo de medio (NVMe/SSD/HDD), salud SMART y limpieza acotada de cachés temporales. | **No implementado** |
| **Módulo `Security`** | Especialista | Sondas y expedientes de privacidad/telemetría y auditoría/endurecimiento de seguridad (y reducción solo en Modo Experto). | **No implementado** |
| **EVO Recovery (`Engine.Elevated` + `Recovery Guard`)** | Transversal | Snapshots propios ([ADR-0006](../adr/0006-restoration-strategy.md)), máquina de estados de transacciones con reinicio y reversión automática post-boot ([ADR-0010](../adr/0010-reboot-transactions-and-recovery-guard.md)). | **No implementado** |
| **Measurement (`MeasurementService`)** | Transversal | Captura de muestras individuales antes/después, cálculo de mediana y desviación, y emisión de veredicto ([ADR-0005](../adr/0005-storage-and-evidence-chain.md)). | **No implementado** |
| **WinGet Provisioning & `Evo.Image`** | Fuera del núcleo | Aprovisionamiento declarativo con hashes fijados e imágenes offline DISM en fases tardías ([ADR-0014](../adr/0014-provisioning-and-image-out-of-core.md)). | **No implementado** |

---

## 3. Flujo de una Operación de Extremo a Extremo

1. **Sondeo en solo lectura:** `Engine.Host` ejecuta las sondas de los 6 módulos especialistas y construye el `SystemProfile` en `FactStore`.
2. **Evaluación y planificación:** `Adaptive Core` cruza el `SystemProfile` y el `UserProfile` contra el `KnowledgeCatalog` ([ADR-0007](../adr/0007-optimization-intelligence.md)), produciendo estados `Applicable`, `NotApplicable`, `Unknown` o `Conflict`.
3. **Previsualización y consentimiento:** La UI presenta el plan explicando las 7 preguntas por acción, lo que cubre y no cubre el snapshot y el nivel de consentimiento requerido ([ADR-0013](../adr/0013-consent-levels-and-expert-mode.md)).
4. **Snapshot y token de aprobación:** Al autorizar el usuario, se calcula el hash de la previsualización y del snapshot previo y se emite un `ApprovalToken` de un solo uso ([ADR-0004](../adr/0004-approval-token.md)).
5. **Ejecución aislada y revalidada:** `Engine.Elevated` recibe la petición por el pipe autenticado ([ADR-0003](../adr/0003-elevated-ipc-channel.md)), revalida por su cuenta la política de seguridad, el token y los prerrequisitos, ejecuta la primitiva cerrada ([ADR-0012](../adr/0012-versioned-closed-catalog.md)) y verifica el estado resultante.
6. **Medición post-aplicación:** `MeasurementService` toma las muestras posteriores y registra el veredicto en la cadena de evidencias ([ADR-0005](../adr/0005-storage-and-evidence-chain.md)).

---

## 4. Detección de Deriva (*Configuration Drift Detection*)

Las actualizaciones acumulativas de Windows (*Windows Update*), las actualizaciones de controladores o los instaladores de terceros pueden revertir silenciosamente claves de registro, estados de servicios o tareas programadas que el usuario había ajustado con EVO.

Para gestionar este escenario sin violar el principio de consentimiento:

1. Al iniciar sesión o ejecutar un diagnóstico, `Engine.Host` compara el estado real actual de cada objetivo con el último estado confirmado (`Committed`) registrado en la base de datos local.
2. Si detecta una discrepancia no originada por el usuario dentro de EVO, marca la acción con estado **`Drifted` (Deriva detectada)**.
3. EVO informa al usuario qué ajustes cambiaron externamente y le ofrece dos opciones explícitas: **volver a aplicar el ajuste** (pasando de nuevo por previsualización, snapshot y autorización) o **aceptar el estado actual como nueva línea base**.
4. **Regla estricta:** EVO **nunca reaplica cambios en segundo plano por su cuenta** ante una deriva.
