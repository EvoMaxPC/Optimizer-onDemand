# ADR-0001: Monorepo, Stack Tecnológico y Contrato por Esquema

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
EVO OPTIMIZER requiere una interfaz de escritorio moderna y auditable junto con un motor nativo capaz de consultar APIs de administración de Windows (WMI/CIM, Service Control Manager, Registro, ETW, Task Scheduler, DPAPI) sin depender de scripts frágiles de PowerShell. Además, la interfaz de usuario y el motor deben evolucionar de forma desacoplada pero garantizando coherencia estricta en los datos intercambiados.

## Decisión
1. Organizar el proyecto en un **monorepo en GitHub** que aloja la interfaz (`apps/desktop`), el motor (`engine`), el catálogo declarativo (`catalog`), los esquemas (`schemas`) y la documentación (`docs`).
2. Implementar la capa de presentación con **React + TypeScript** empaquetada dentro de **Tauri** como contenedor de escritorio ligero.
3. Implementar el motor del sistema (`Engine.Host`, `Engine.Elevated` y `Recovery Guard`) en **C# / .NET compilado como despliegue autocontenido (*self-contained*)**, eliminando la necesidad de que el usuario instale un runtime externo de .NET.
4. Utilizar **SQLite** como base de datos embebida local para estado, historial, snapshots y mediciones.
5. Separar estrictamente la UI del motor: la UI nunca accede directamente al sistema operativo y se comunica mediante un **contrato único gobernado por JSON Schema** (`schemas/`).

## Alternativas descartadas
- **Electron + scripts de PowerShell:** descartado por elevado consumo de RAM base y por la fragilidad y superficie de inyección de invocar `powershell.exe` para leer o modificar el sistema.
- **Aplicación monolítica en WPF/WinUI sin separación de procesos:** descartada porque acopla la interfaz gráfica al contexto de ejecución del motor y dificulta el aislamiento estricto de privilegios.
- **Todo el motor en Rust puro:** descartado para la primera iteración porque el ecosistema .NET en Windows ofrece acceso maduro, tipado y mantenido por Microsoft para WMI/CIM, SCM, EventLog, Registro, DPAPI y certificados Authenticode.

## Consecuencias
- El repositorio mantiene en un único historial atómico los cambios de esquema, catálogo, motor e interfaz.
- El empaquetado *self-contained* de .NET incrementa el tamaño en disco del instalador a cambio de garantizar una versión exacta y probada del runtime.
- Cualquier discrepancia entre UI y motor es detectada en tiempo de validación contra `schemas/`.

## Límites y riesgos conocidos
- Tauri depende de **Microsoft Edge WebView2 Runtime** instalado en el sistema; en instalaciones de Windows modificadas donde se haya eliminado WebView2, la interfaz gráfica no podrá iniciar sin restaurar primero dicho componente.
- La coexistencia de tres ecosistemas de herramientas en etapas posteriores (Node.js/TypeScript, Rust/Cargo para el shell de Tauri y .NET SDK para el motor) aumenta la complejidad del pipeline de compilación y la superficie de cadena de suministro.

## Preguntas abiertas
- Definir en la Etapa 2 y Etapa 4 las versiones exactas fijadas (LTS) de Node.js, Rust y .NET y la estrategia de bloqueo de dependencias (`lockfiles`).
