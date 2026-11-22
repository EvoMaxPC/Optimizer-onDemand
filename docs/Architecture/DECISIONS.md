# EVO BOTTLENECK DETECTOR — Architecture Decisions

> Registro de decisiones técnicas de la arquitectura.
> Toda decisión de arquitectura se registra aquí. Cambios posteriores deben
> actualizar este documento para evitar inconsistencias.

> **STATUS DE APROBACIÓN (2026-11-22):** SOLO D-001, D-002 y D-003 están
> aprobadas por el usuario. Las demás decisiones (D-004 … D-015) son
> **propuestas** documentadas por el autor y quedan sin aprobación hasta que
> el usuario las valide explícitamente. Ninguna se trata como aprobada.

| ID     | Decisión                                                                            | Estado                    | Fecha       |
|--------|--------------------------------------------------------------------------------------|---------------------------|-------------|
| D-001  | Arquitectura **híbrida y modular**, un único proceso con interfaces desacopladas.   | ✅ Aprobada               | 2026-11-22  |
| D-002  | Identidad visual **dark**: negro profesional, amarillo EVO, blanco y cyan.          | ✅ Aprobada               | 2026-11-22  |
| D-003  | Distribución **portable**, self-contained, sin instalación tradicional.            | ✅ Aprobada               | 2026-11-22  |
| D-004  | **C# / .NET 10 LTS** como lenguaje y plataforma.                                     | ⚠️ Propuesta — no aprobada | 2026-11-22  |
| D-005  | UI **WPF** con patrón **MVVM**.                                                      | ⚠️ Propuesta — no aprobada | 2026-11-22  |
| D-006  | Recolección de Windows: **PowerShell 5.1** + **CIM/WMI/PnP**.                        | ⚠️ Propuesta — no aprobada | 2026-11-22  |
| D-007  | Métricas: **Performance Counters**. Diagnóstico avanzado: **ETW**.                   | ⚠️ Propuesta — no aprobada | 2026-11-22  |
| D-008  | Persistencia: **SQLite** (historial) + **JSON** (intercambio de datos e informes).  | ⚠️ Propuesta — no aprobada | 2026-11-22  |
| D-009  | Pruebas con **xUnit**.                                                               | ⚠️ Propuesta — no aprobada | 2026-11-22  |
| D-010  | **Solo lectura** por defecto. Sin telemetría propia. Sin envío externo. Sin modificaciones automáticas de registro / drivers / archivos / servicios. | ⚠️ Propuesta — no aprobada | 2026-11-22  |
| D-011  | **Un único proceso** inicial; recolectores aislados (futuro) mediante procesos auxiliares y Named Pipes.  | ⚠️ Propuesta — no aprobada | 2026-11-22  |
| D-012  | Estructura final según PARTE II de la especificación; **creación gradual** de módulos (sin carpetas vacías ni archivos ficticios). FOUNDATION crea `EVO.Core`, `EVO.Desktop` y `tests/EVO.Core.Tests`. | ⚠️ Propuesta — no aprobada | 2026-11-22  |
| D-013  | Modelo de diagnóstico: `DiagnosticFinding` con cinco conceptos (Métrica, Anomalía, Evidencia, Causa probable, Recomendación) y `FindingConfidence` de 5 estados (Confirmed / Probable / Warning / Normal / InsufficientData). **PROPUESTA PENDIENTE DE APROBACIÓN** — ver revisión 3e. | ⚠️ PROPUESTA PENDIENTE DE APROBACIÓN | 2026-11-22  |
| D-014  | Nombre del producto: **EVO BOTTLENECK DETECTOR** (EVO LABS). **PROPUESTA PENDIENTE DE APROBACIÓN**. | ⚠️ PROPUESTA PENDIENTE DE APROBACIÓN | 2026-11-22  |
| D-015  | Excepción de gobernanza: directorios `data/`, `logs/`, `reports/` se preservan en git con `.gitkeep` vacío y `.gitignore` ignora su contenido. Sin esta excepción, la estructura portable no se preservaría en el paquete. | ⚠️ Propuesta — no aprobada | 2026-11-22  |

## Regla de cambios

Si se modifica una decisión de arquitectura durante el desarrollo:

1. Registrar el cambio aquí (nueva ID o actualización con fecha).
2. Actualizar la especificación técnica correspondiente.
3. Documentar el motivo del cambio.

## Status de distribución portable

- Un solo ejecutable Windows x64 self-contained (runtime incluido).
  **Propuesta D-003 (aprobada):** distribución portable self-contained.
  *Propuesta:* single-file como modo de publicación — justificada en la revisión (sección f); no es una decisión nueva, es la implementación de D-003.
- Sin servicios residentes obligatorios.
- Datos, configuración e informes locales (resolver rutas contra `AppContext.BaseDirectory`).
- `.gitkeep` en directorios de artefactos: excepción documentada (D-015).

## Pendientes de aprobación del usuario

D-004 … D-012, D-015 (propuestas), así como D-013 y D-014 (PROPUESTA PENDIENTE DE APROBACIÓN).
