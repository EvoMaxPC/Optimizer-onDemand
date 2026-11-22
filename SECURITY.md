# Política de Seguridad (`SECURITY`)

## 1. Estado Actual del Proyecto

EVO OPTIMIZER se encuentra en fase temprana de desarrollo (**Etapa 2 / `F1a`: shell de escritorio sin motor; no lee ni modifica Windows**). Actualmente no se distribuyen binarios compilados ni instaladores para usuarios finales.

---

## 2. Cómo Reportar una Vulnerabilidad

Si identificas un fallo de diseño en el modelo de amenazas ([`docs/security/THREAT_MODEL.md`](docs/security/THREAT_MODEL.md)), una debilidad en los protocolos proyectados ([`docs/adr/README.md`](docs/adr/README.md)) o una vulnerabilidad en la superficie de escritorio:

- **Canal previsto:** **GitHub Private Vulnerability Reporting** (a activar por el propietario en la configuración del repositorio en GitHub, según consta en [`docs/process/DECISIONS_PENDING.md`](docs/process/DECISIONS_PENDING.md)).
- **Contacto alternativo:** **`PENDIENTE`** (no se inventan ni publican direcciones de correo electrónico hasta decisión expresa del propietario).
- **Regla de divulgación responsable:** por favor, **no abras un Issue público** ni un Pull Request público para reportar vulnerabilidades explotables. Emplea el canal privado cuando esté habilitado e incluye el componente o ADR afectado, pasos de reproducción e impacto potencial.

---

## 3. Alcance de Seguridad

### Dentro del alcance
- Superficie mínima de Tauri v2, políticas CSP y archivo de capacidades (`capabilities/default.json`) en `apps/desktop` ([ADR-0001](docs/adr/0001-monorepo-and-stack.md)).
- Diseño de separación de privilegios entre `Engine.Host` y `Engine.Elevated` ([ADR-0002](docs/adr/0002-privilege-separation.md)).
- Seguridad del canal IPC por Named Pipe, validación Authenticode y acuerdo ECDH ([ADR-0003](docs/adr/0003-elevated-ipc-channel.md)).
- Integridad y resistencia a TOCTOU del `ApprovalToken` ([ADR-0004](docs/adr/0004-approval-token.md)).
- Restricciones estructurales del catálogo cerrado y `schemas/action.schema.json` ([ADR-0012](docs/adr/0012-versioned-closed-catalog.md)).
- Transacciones con reinicio y `Recovery Guard` ([ADR-0010](docs/adr/0010-reboot-transactions-and-recovery-guard.md)).

### Fuera del alcance / Riesgos residuales declarados
- Sistemas donde el atacante ya dispone de ejecución de código como Administrador o `SYSTEM`.
- Consecuencias derivadas de reducciones de seguridad autorizadas expresamente por el propietario del equipo en **Modo Experto** tras superar todas las confirmaciones ([ADR-0013](docs/adr/0013-consent-levels-and-expert-mode.md)).

---

## 4. Plazos Orientativos de Respuesta (Sin Compromiso Legal)

Los siguientes tiempos son **objetivos operativos orientativos de buena fe** y no constituyen garantías contractuales, acuerdos de nivel de servicio (SLA) ni compromisos legales:

- **Acuse de recibo inicial:** objetivo orientativo de 3 a 5 días hábiles.
- **Evaluación técnica y clasificación:** objetivo orientativo de 7 a 14 días hábiles.
- **Resolución o actualización de diseño/parche:** según la severidad técnica y la etapa activa del proyecto.
