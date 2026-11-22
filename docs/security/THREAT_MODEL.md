# Modelo de Amenazas (`THREAT_MODEL`)

## 1. Activos Protegidos

| ID | Activo | Impacto en caso de compromiso |
|---|---|---|
| **A-01** | **Arranque y estabilidad de Windows** | Sistema inoperativo (*bootloop* o BSOD) o pérdida de conectividad/servicios básicos. |
| **A-02** | **Snapshots y manifiestos de recuperación (`%ProgramData%\EVO\recovery`)** | Imposibilidad de revertir cambios aplicados ("usuario atrapado"). |
| **A-03** | **Ejecutor privilegiado (`Engine.Elevated`)** | Escalada de privilegios local (LPE) o ejecución arbitraria como Administrador. |
| **A-04** | **Integridad del catálogo (`catalog/`)** | Aplicación de cambios no auditados o con clasificaciones de riesgo falsificadas. |
| **A-05** | **Base de datos SQLite y Cadena de Evidencias** | Falsificación de mediciones antes/después, borrado de historial o exposición de hábitos de uso. |
| **A-06** | **Soberanía y consentimiento del usuario** | Modificaciones silenciosas o desactivación involuntaria de funciones críticas. |

---

## 2. Actores de Amenaza

1. **Malware del mismo usuario (sin privilegios de administrador):** proceso malicioso que se ejecuta bajo el mismo SID que `Engine.Host` e intenta usar a EVO como puente para escalar privilegios o modificar el sistema sin permiso.
2. **Malware con privilegios de Administrador / SYSTEM:** código malicioso que ya posee control privilegiado sobre el sistema operativo.
3. **Catálogo manipulado:** alteración maliciosa o accidental de archivos JSON del catálogo en disco o durante su distribución.
4. **Proceso local que suplanta al cliente del pipe:** binario no autorizado que intenta conectarse al Named Pipe de `Engine.Elevated` o interceptar tokens en línea de comandos.
5. **Usuario engañado o apresurado:** usuario que importa recomendaciones externas engañosas o intenta desactivar subsistemas críticos sin comprender las consecuencias.
6. **Agente de IA o colaborador con alcance excesivo:** asistente automatizado o contribuidor durante el desarrollo que intenta introducir comandos libres, dependencias no auditadas, permisos excesivos o datos simulados.

---

## 3. Superficies de Ataque y Mitigaciones por ADR

| Superficie | Amenaza Principal | Mitigación Arquitectónica | ADR de Referencia |
|---|---|---|---|
| **Canal IPC (`Named Pipe` y `argv`)** | Lectura de secretos en `argv` por otros procesos o conexión de un cliente suplantador al proceso elevado. | Cero secretos en `argv` o variables de entorno; pipe con nombre aleatorio y ACL restringida al SID del usuario; verificación de PID, ruta de imagen y firma Authenticode; sesión ECDH efímera con `nonce` y cierre por inactividad; `ApprovalToken` HMAC de un solo uso vinculado a `previewHash` y `snapshotHash`. | [ADR-0002](../adr/0002-privilege-separation.md), [ADR-0003](../adr/0003-elevated-ipc-channel.md), [ADR-0004](../adr/0004-approval-token.md) |
| **Catálogo de acciones (`catalog/`)** | Inyección de comandos PowerShell/cmd, rutas arbitrarias o degradación de nivel de consentimiento. | Catálogo puramente declarativo validado por JSON Schema (`additionalProperties: false`); lista cerrada de 13 primitivas tipadas; catálogo oficial firmado; revalidación independiente de política en `Engine.Elevated`; fichas de usuario marcadas como no firmadas y sujetas a lista de objetivos críticos protegidos. | [ADR-0008](../adr/0008-adaptive-core-authority.md), [ADR-0012](../adr/0012-versioned-closed-catalog.md), [ADR-0016](../adr/0016-user-catalog-extensions.md) |
| **Actualizaciones y aprovisionamiento** | Compromiso de cadena de suministro o descarga dinámica de scripts en caliente. | Prohibición de descargas dinámicas en el motor; aprovisionamiento `WinGet` fuera del núcleo con manifiestos fijados por hash SHA-256. | [ADR-0014](../adr/0014-provisioning-and-image-out-of-core.md) |
| **Base de datos SQLite y observabilidad** | Alteración de muestras antes/después, corrupción por apagado o recolección excesiva de datos privados. | SQLite en modo WAL con transacciones y migraciones con checksum; muestras individuales auditables; `Evidence Chain` con HMAC-SHA256 y clave protegida en DPAPI con entropía propia; observación opt-in sin títulos de ventana, rutas de archivos, URLs ni pulsaciones. | [ADR-0005](../adr/0005-storage-and-evidence-chain.md), [ADR-0011](../adr/0011-usage-observation-privacy.md) |
| **Transacciones de reinicio y `Recovery Guard`** | Sistema inestable tras reinicio o borrado del plan de rollback por proceso no privilegiado. | Máquina de estados persistida en `%ProgramData%\EVO\recovery` con ACL restringida a `SYSTEM`/Administradores; tarea `Recovery Guard` firmada corriendo como `SYSTEM` con contador de arranques y ventana de confirmación; prerrequisitos de Modo Experto. | [ADR-0006](../adr/0006-restoration-strategy.md), [ADR-0010](../adr/0010-reboot-transactions-and-recovery-guard.md), [ADR-0013](../adr/0013-consent-levels-and-expert-mode.md) |
| **Repositorio y pipeline de desarrollo** | Agente de IA o PR que introduce código fuera de alcance, permisos ocultos o datos simulados. | `CHECKPOINTS.md`, `CODEOWNERS`, plantilla de PR con checklist de seguridad y validador determinista `tools/check-docs.mjs` en CI. | [ADR-0001](../adr/0001-monorepo-and-stack.md), [ADR-0015](../adr/0015-source-and-reference-policy.md) |

---

## 4. Riesgos Residuales Declarados (Honestidad de Seguridad)

Ningún software de administración de sistemas ofrece seguridad absoluta. EVO declara explícitamente los siguientes riesgos residuales que no se ocultan al usuario ni al auditor:

1. **Malware residente en el mismo contexto de usuario:** si el equipo ya está infectado por malware que corre bajo el mismo usuario e integridad media que `Engine.Host`, dicho malware puede intentar manipular la memoria de `Engine.Host` o automatizar la interfaz gráfica. Aunque no puede inyectar comandos arbitrarios porque `Engine.Elevated` solo acepta las 13 primitivas cerradas y exige elevación UAC, este escenario queda parcialmente fuera del aislamiento IPC ([ADR-0003](../adr/0003-elevated-ipc-channel.md)).
2. **Malware con privilegios de Administrador o SYSTEM:** si el atacante ya posee permisos de Administrador o `SYSTEM`, tiene control total del sistema operativo, puede alterar `%ProgramData%\EVO\recovery`, desinstalar `Recovery Guard` o modificar Windows directamente sin pasar por EVO.
3. **Detección vs. prevención en `Evidence Chain`:** la cadena HMAC en SQLite protegida con DPAPI ([ADR-0005](../adr/0005-storage-and-evidence-chain.md)) **detecta** modificaciones no autorizadas de registros individuales, pero un proceso corriendo como el mismo usuario que extraiga la clave desde DPAPI podría recalcular la cadena completa o borrar el archivo `.db` entero.
4. **Fallo de arranque anterior al subsistema de servicios/tareas:** si un cambio crítico impide que Windows llegue a iniciar el Programador de Tareas, `Recovery Guard` no podrá ejecutarse automáticamente y el usuario deberá emplear el Punto de Restauración o su medio externo WinRE ([ADR-0010](../adr/0010-reboot-transactions-and-recovery-guard.md)).

---

## 5. Falsos Positivos de Antivirus / SmartScreen y Firma de Código

Dado que EVO consulta y modifica servicios de Windows, claves del Registro y tareas programadas mediante un ejecutor elevado, los motores heurísticos de antivirus (incluido Microsoft Defender) y **Microsoft Defender SmartScreen** pueden clasificar binarios no firmados o recién emitidos como potencialmente no deseados (PUA) o sospechosos.

- **Mitigación técnica:** prohibición absoluta de técnicas de ofuscación, empaquetadores opacos (*packers*), inyección de procesos o ejecución dinámica de scripts; uso exclusivo de APIs documentadas de Windows y primitivas cerradas.
- **Requisito operativo:** necesidad de **firma de código Authenticode** (certificado estándar, EV o Azure Trusted Signing) para todos los ejecutables (`apps/desktop`, `Engine.Host`, `Engine.Elevated`, `Recovery Guard`) antes de su distribución pública, registrada en [DECISIONS_PENDING.md](../process/DECISIONS_PENDING.md).
