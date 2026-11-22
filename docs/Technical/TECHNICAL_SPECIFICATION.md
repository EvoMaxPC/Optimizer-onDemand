# EVO BOTTLENECK DETECTOR — Especificación técnica definitiva

> Versión 1.0 — Definición aprobada 2026-11-22
> Producto de EVO LABS — Diagnóstico de rendimiento de Windows

**NO ADIVINAMOS. INVESTIGAMOS.**

---

## 1. Identidad y propósito

**EVO BOTTLENECK DETECTOR** es un sistema de reconocimiento, auditoría,
monitorización y diagnóstico inteligente de Windows.

**Pregunta que responde:** ¿qué está frenando realmente mi Windows y por qué?

**Misión**

Identificar qué está perjudicando el rendimiento de un equipo Windows, localizar
las evidencias, establecer causas probables y proporcionar soluciones
técnicamente justificadas.

**Principio fundamental**

No nos resignamos a mostrar porcentajes. Distinguimos entre:

- Problema **confirmado** (evidencia directa).
- Problema **probable** (correlación con múltiples evidencias).
- **Advertencia** (comportamiento fuera de lo esperado, no decisivo).
- Comportamiento **normal** (dentro de lo esperado).
- **Datos insuficientes** (no podemos afirmar nada).

Nunca presentaremos una causa como confirmada cuando solamente exista una
correlación.

---

## 2. Arquitectura tecnológica definitiva

| Componente          | Tecnología              | Función                          |
|---------------------|-------------------------|----------------------------------|
| Aplicación          | C#                      | Lenguaje principal               |
| Framework           | .NET 10 LTS             | Plataforma                       |
| Interfaz            | WPF                     | Escritorio nativo                |
| Patrón              | MVVM                    | Separación visual y lógica       |
| Motor               | C#                      | Diagnóstico y correlación        |
| Recolección Windows | PowerShell 5.1          | Auditoría del sistema            |
| Hardware            | CIM / WMI / PnP          | Inventario                       |
| Rendimiento         | Performance Counters    | Métricas                         |
| Diagnóstico avanzado| ETW                     | Trazas y latencias               |
| Base de datos       | SQLite                  | Historial local                  |
| Formato intercambio | JSON                    | Datos e informes                  |
| Pruebas             | xUnit                   | Validación automatizada           |

**Decisión de implementación:** arquitectura inicialmente dentro de un único
proceso, con interfaces desacopladas. Los recolectores que necesiten aislamiento
podrán ejecutarse posteriormente mediante procesos auxiliares y Named Pipes.
Evitamos complejidad innecesaria sin cerrar posibilidades futuras.

### Distribución portable

La aplicación se publicará como:

- Ejecutable Windows x64.
- Publicación self-contained.
- Runtime incluido (no requiere instalación de .NET en la máquina destino).
- Datos, configuración e informes locales.
- Sin servicios residentes obligatorios.
- Sin conexión a servidores externos (sin telemetría propia).

La publicación es portable, aunque Windows seguirá requiriendo sus componentes
nativos y permisos correspondientes.

---

## 3. Los 16 sectores funcionales

| ID  | Módulo              | Objetivo                                      |
|-----|---------------------|-----------------------------------------------|
| M01 | System Intelligence | Inventario completo                           |
| M02 | CPU Analyzer        | Saturación y limitaciones                     |
| M03 | RAM Analyzer        | Presión y paginación                           |
| M04 | Storage Analyzer    | Latencia e I/O                                 |
| M05 | GPU Analyzer        | Motores y memoria gráfica                      |
| M06 | Process Intelligence| Consumo por proceso                           |
| M07 | Services Auditor    | Auditoría de servicios                         |
| M08 | Startup Analyzer    | Arranque de Windows                            |
| M09 | Driver Intelligence | Controladores                                  |
| M10 | Latency Lab         | DPC, ISR y ETW                                 |
| M11 | Network Analyzer    | Red y conectividad                             |
| M12 | Windows Health      | Integridad y eventos                           |
| M13 | Background Activity | Actividad en segundo plano                    |
| M14 | Bottleneck Engine   | Correlación y causas                           |
| M15 | EVO Reports         | Informes y comparativas                        |
| M16 | EVO Live            | Monitorización temporal                        |

Todos serán módulos independientes, con contratos de entrada y salida definidos.

**Hoja de desarrollo (V0.1 → V1.0):**

| Versión | Funcionalidad                                        |
|---------|------------------------------------------------------|
| V0.1    | Inventario y reconocimiento del hardware              |
| V0.2    | Detector CPU, RAM y disco                            |
| V0.3    | Procesos y servicios                                 |
| V0.4    | Inicio y tareas programadas                          |
| V0.5    | Drivers, latencia y eventos                          |
| V0.6    | Motor de correlación de evidencias                   |
| V1.0    | Dashboard e informe completo EVO                     |

**Única característica diferencial más allá del análisis actual:** detección de
cuellos de botella *temporales*. No solo el estado actual, sino la evolución
(arranque → picos de disco → paginación → recuperación) para identificar qué
frena y en qué momento.

---

## 4. Modelo de diagnóstico

Cada hallazgo debe producir un objeto estructurado (`DiagnosticFinding`):

```
DiagnosticFinding
│
├── Id
├── Category
├── Title
├── Description
├── Severity
├── Confidence
├── Evidence[]
├── SuspectedSources[]
├── Impact
├── Recommendation
├── VerificationMethod
└── Timestamp
```

La arquitectura distingue cinco conceptos:

| Concepto         | Definición                                            |
|------------------|------------------------------------------------------|
| **Métrica**      | Dato medido (nombre, unidad, valor, timestamp)       |
| **Anomalía**     | Comportamiento que merece investigación               |
| **Evidencia**    | Dato que respalda una conclusión                      |
| **Causa probable** | Explicación que relaciona evidencias (nunca confirmada si solo hay correlación) |
| **Recomendación** | Acción propuesta para verificar o resolver            |

`Confidence` codifica los cinco estados del producto: `Confirmed`,
`Probable`, `Warning`, `Normal`, `InsufficientData`. Nunca se declara un
cuello de botella solo por uso elevado de un componente.

---

## 5. Seguridad y privacidad (reglas permanentes)

- Diagnóstico de **solo lectura** por defecto.
- **Sin telemetría propia** y sin envío externo de datos.
- **Sin** modificaciones automáticas del registro.
- **Sin** eliminación de archivos.
- **Sin** desactivación automática de servicios.
- **Sin** modificación de drivers.
- **Sin** optimizaciones ocultas.
- Elevación de permisos solamente cuando una función lo necesite.
- Registro de las operaciones realizadas por EVO.

---

## 6. Estructura maestra del proyecto (destino final)

```
EVO_BOTTLENECK/
│
├── EVO.sln
├── src/
│   ├── EVO.Desktop/          Views, ViewModels, Components, Themes, Assets, Resources
│   ├── EVO.Core/             Diagnostics, Analysis, Correlation, Rules, Findings, Recommendations
│   ├── EVO.Collectors/       Hardware, CPU, Memory, Storage, GPU, Network, Processes, Services, Startup, Drivers, Windows, ETW
│   ├── EVO.PowerShell/       Inventory, Audits, Diagnostics, Common
│   ├── EVO.Data/             Models, DTO, Serialization, Database, Migrations
│   └── EVO.Reports/          PDF, JSON, Templates
├── tests/
│   ├── EVO.Core.Tests/
│   ├── EVO.Collectors.Tests/
│   └── EVO.Integration.Tests/
├── scripts/
│   ├── Build/
│   ├── Publish/
│   └── Validation/
├── data/
├── logs/
├── reports/
├── config/
├── docs/
│   ├── Architecture/
│   ├── Technical/
│   └── UserManual/
└── README.md
```

> **Nota de gobernanza:** esta estructura es el destino final. NO se crean
> carpetas vacías sin sentido ni archivos ficticios. Cada módulo se incorpora
> cuando tenga una responsabilidad y una implementación real. Durante FOUNDATION
> se crean: `EVO.Core` (contratos, modelos, configuración, logging),
> `EVO.Desktop` (shell mínimo) y `tests/EVO.Core.Tests`. `EVO.Collectors`,
> `EVO.Data`, `EVO.Reports` y `EVO.PowerShell` se incorporan cuando reciban
> responsabilidades reales. Ver DECISIONS.md.

---

## 7. Checkpoints FOUNDATION (ETAPA 01)

| Checkpoint | Resultado exigido                                | Estado |
|------------|---------------------------------------------------|--------|
| F01 | Entorno y estructura verificados                  | ⬜ ver FOUNDATION.md |
| F02 | Solución y proyectos creados                      | ⬜ ver FOUNDATION.md |
| F03 | Referencias y dependencias correctas              | ⬜ ver FOUNDATION.md |
| F04 | Contratos y modelos implementados                 | ⬜ ver FOUNDATION.md |
| F05 | Configuración y logging funcionales               | ⬜ ver FOUNDATION.md |
| F06 | Pruebas aprobadas y compilación exitosa            | ⬜ ver FOUNDATION.md |

---

## 8. Principios de ingeniería

1. Cada módulo tendrá código real, pruebas reproducibles y resultados
   comprobables.
2. No se dará por terminada una función porque aparezca un indicador verde:
   se verificará.
3. Cambios de arquitectura se documentan en `docs/Architecture/DECISIONS.md`.
4. La especificación es la referencia técnica; cambios se documentan, no
   silenciosos.

**Última regla:** EVO se construye como una herramienta que podamos utilizar,
auditar y defender técnicamente.
