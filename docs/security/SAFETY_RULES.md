# Reglas Inviolables de Seguridad (`SAFETY_RULES`)

> **Principio Rector:** Las restricciones en EVO OPTIMIZER son **niveles de consentimiento y prerrequisitos de recuperación**, no prohibiciones paternalistas. El usuario es dueño de su sistema; la obligación de EVO es garantizar información veraz, compatibilidad, evidencia local, snapshot previo, verificación y vuelta atrás.

---

## 1. Las 12 Reglas Inviolables del Producto

1. **Ningún cambio sin diagnóstico:** está prohibido proponer o habilitar la ejecución de una acción sin haber leído previamente el estado real del componente en el equipo.
2. **Ningún cambio sin verificar compatibilidad:** ninguna acción puede ejecutarse si la compilación/edición de Windows o el hardware del equipo no cumplen el `ApplicabilityProfile` declarado.
3. **Ninguna recomendación sin evidencia local:** EVO jamás recomienda una acción basándose en listas genéricas; requiere evidencia local medida en el equipo (coste de recursos o evidencia de no uso).
4. **Ninguna desactivación sin revisar dependencias:** antes de desactivar un servicio, tarea o característica, el motor debe inspeccionar y mostrar la cadena completa de componentes dependientes.
5. **Ningún cambio administrativo sin autorización:** toda escritura en el sistema requiere consentimiento explícito del usuario vinculado a un `ApprovalToken` de un solo uso ([ADR-0004](../adr/0004-approval-token.md)).
6. **Ningún cambio reversible sin snapshot propio:** ninguna acción con reversibilidad `Full` o `Partial` puede aplicarse sin haber persistido y verificado antes el snapshot propio de EVO ([ADR-0006](../adr/0006-restoration-strategy.md)).
7. **Ningún cambio irreversible sin advertencia y consentimiento reforzado:** toda acción con reversibilidad `Partial` o `None` exige mostrar la lista exacta de elementos que no se recuperarán y superar el flujo de consentimiento `reinforced` o `expert`.
8. **Ninguna optimización presentada como exitosa sin medición:** está prohibido mostrar mensajes de "optimización exitosa" o porcentajes de mejora sin mediciones locales antes y después respaldadas por muestras individuales ([ADR-0005](../adr/0005-storage-and-evidence-chain.md)).
9. **Ningún componente crítico modificado por un perfil genérico:** los perfiles (`Gaming`, `Oficina`, etc.) tienen prohibido incluir componentes de clase `Critical` o `Undetermined`. Estos solo pueden modificarse de forma **individual** dentro del **Modo Experto** ([ADR-0009](../adr/0009-component-classification.md), [ADR-0013](../adr/0013-consent-levels-and-expert-mode.md)).
10. **Ninguna modificación de GPU, audio o red sin autorización específica adicional:** los cambios que afecten al controlador gráfico, subsistema de audio o pila de red nunca se aplican silenciosamente dentro de un lote general; exigen una confirmación específica e independiente del usuario.
11. **Ninguna telemetría externa por defecto:** EVO no envía datos a servidores externos; toda observación de uso local es estrictamente *opt-in* ([ADR-0011](../adr/0011-usage-observation-privacy.md)).
12. **Ningún reinicio sin informar al usuario y guardar el estado de recuperación:** el sistema nunca reinicia por sorpresa ni sin haber escrito y verificado previamente el plan de recuperación en `%ProgramData%\EVO\recovery` bajo custodia de `Recovery Guard` ([ADR-0010](../adr/0010-reboot-transactions-and-recovery-guard.md)).

---

## 2. Reglas Inviolables de Arquitectura

- **Modo lectura por defecto:** al abrirse la aplicación y durante todo el diagnóstico y planificación, solo corre `Engine.Host` sin privilegios administrativos ([ADR-0002](../adr/0002-privilege-separation.md)).
- **Escritura exclusiva mediante primitivas cerradas en el ejecutor:** solo `Engine.Elevated` escribe en ámbito de máquina, y únicamente ejecuta las 13 primitivas tipadas del catálogo cerrado ([ADR-0012](../adr/0012-versioned-closed-catalog.md)). **Nunca ejecuta PowerShell libre, nunca importa archivos `.reg` opacos, nunca invoca `cmd.exe` libre y nunca acepta rutas de archivo o registro arbitrarias.**
- **Revalidación independiente en el ejecutor (Defensa en Profundidad):** `Engine.Elevated` revalida de forma autónoma el nivel de consentimiento, la validez criptográfica del `ApprovalToken`, la existencia del snapshot previo y los prerrequisitos antes de ejecutar cada primitiva ([ADR-0008](../adr/0008-adaptive-core-authority.md)).
- **Sin descargas dinámicas:** el motor de optimización no descarga scripts, definiciones ejecutables ni binarios desde Internet en tiempo de ejecución ([ADR-0014](../adr/0014-provisioning-and-image-out-of-core.md)).
- **Sin cifras de mejora sin mediciones locales:** ninguna pantalla, ficha o documento puede prometer ganancias fijas (por ejemplo, *"+15 % FPS"* o *"-500 MB RAM garantizados"*); todo beneficio debe medirse en la máquina del usuario.

---

## 3. Tabla de Niveles de Consentimiento (`consentLevel`)

| Nivel | Identificador | Ámbito y Tipo de Acciones | Prerrequisitos Obligatorios Antes de Aplicar |
|---|---|---|---|
| **Estándar** | `standard` | Ajustes totalmente reversibles (`Full`) en ámbito de usuario (`scope: user`), sin impacto en servicios del sistema ni necesidad de reinicio. | Diagnóstico previo, compatibilidad verificada, previsualización del cambio, snapshot propio de EVO y autorización explícita del usuario. |
| **Avanzado** | `advanced` | Ajustes reversibles (`Full`) en ámbito de máquina (`scope: machine`): servicios no críticos, tareas programadas, energía. | Todo lo del nivel Estándar + elevación UAC bajo demanda (`Engine.Elevated`), revisión de árbol de dependencias y **autorización específica adicional separada si toca GPU, audio o red**. |
| **Reforzado** | `reinforced` | Acciones con reversibilidad parcial (`Partial`) o nula (`None`), como limpieza de cachés temporales o desinstalación de paquetes `AppX`. | Todo lo del nivel Avanzado + visualización obligatoria de la **lista exacta de lo que NO se recupera** (`snapshotCoverage.notCovered`) y casilla de aceptación expresa de pérdida de esos elementos. |
| **Experto (Modo Experto)** | `expert` | Reducción de protecciones (Defender, Firewall, UAC, VBS), componentes de clase `Critical` o `Undetermined`, o cambios con impacto en la ruta de arranque. | Ejecución estrictamente **individual** (prohibido en perfiles), **Punto de Restauración de Windows confirmado**, **confirmación de medio de recuperación externo** si afecta al arranque, **escritura manual del nombre del cambio** y activación del **indicador persistente `"protección reducida"`** en la UI ([ADR-0013](../adr/0013-consent-levels-and-expert-mode.md)). |
