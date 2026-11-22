# Principios Fundamentales de EVO OPTIMIZER

> **Lema:** *"Tu Windows. Tus reglas. Tu hardware. No lo creas, compruébalo."*

## 1. Filosofía del Sistema

Windows debe adaptarse al usuario, no el usuario a Windows. **EVO OPTIMIZER** no es una colección estática de *tweaks*, ni un script masivo de desactivación, ni un clon de WinUtil o AtlasOS. Es un sistema de adaptación técnica que:

**diagnosticar → recomendar → autorizar → ejecutar → verificar → revertir**

Los ajustes técnicos sobre Windows (claves de registro, servicios, tareas programadas, directivas de energía) son hechos operativos del sistema y no pertenecen a nadie. El valor de ingeniería de EVO reside en aplicarlos únicamente con diagnóstico previo, snapshot propio, verificación determinista, vuelta atrás garantizada y medición empírica. Las barreras del sistema son **niveles de consentimiento y prerrequisitos de recuperación**, nunca vetos paternalistas.

---

## 2. Los Cinco Pilares Operativos

1. **CONOCER:** Antes de sugerir cualquier cambio, EVO inspecciona en modo de solo lectura el hardware real, la edición y compilación de Windows, los servicios activos, los procesos de inicio y las dependencias instaladas.
2. **ADAPTAR:** Traduce la intención del usuario y las capacidades físicas de su máquina en un plan individualizado. Dos equipos distintos con el mismo perfil reciben propuestas distintas.
3. **OPTIMIZAR:** Aplica cambios reales sobre el sistema mediante primitivas cerradas y auditables, únicamente bajo autorización explícita del usuario.
4. **RECUPERAR:** Todo cambio reversible exige un snapshot propio previo y un procedimiento probado de restablecimiento automático o asistido.
5. **DEMOSTRAR:** Contrasta el estado y las métricas antes y después de cada intervención para emitir un veredicto objetivo basado en muestras reales.

---

## 3. Principio de Autorización y Reinicio ("EVO prepara y propone; con tu autorización, aplica")

**EVO prepara y propone; con tu autorización, aplica.** Nada se modifica en el sistema operativo sin autorización expresa e informada del usuario.

- Ningún diagnóstico, escaneo o cambio de perfil ejecuta escrituras por sí mismo.
- El **reinicio** forma parte legítima del proceso de optimización cuando el subsistema de Windows lo exige (por ejemplo, cambios en servicios de arranque o subsistema de memoria), pero **siempre es informado de antemano** y **con el estado de recuperación y snapshot guardados en disco antes de reiniciar** (ver [ADR-0010](adr/0010-reboot-transactions-and-recovery-guard.md)).

---

## 4. Las 7 Preguntas Obligatorias por Componente

Para cada servicio, tarea, clave o componente analizado, el catálogo y el motor de EVO deben responder explícitamente a estas siete preguntas antes de presentar una propuesta al usuario:

1. **¿Qué función cumple?** Explicación técnica clara del propósito del componente en Windows.
2. **¿Quién la necesita?** Escenarios de uso, periféricos o flujos de trabajo que dependen de él.
3. **¿Se utiliza realmente?** Evidencia local observada en el equipo concreto (ejecución histórica, llamadas, dispositivos presentes).
4. **¿Depende otro componente de ella?** Cadena ascendente y descendente de servicios, controladores o funciones del sistema.
5. **¿Qué recursos consume?** Impacto medido en memoria (Working Set), CPU, E/S de disco, red o tiempo de arranque.
6. **¿Qué sucede si se desactiva?** Efectos secundarios exactos y funciones que dejarán de operar.
7. **¿Puede restaurarse?** Grado de reversibilidad (`Full`, `Partial`, `None`) y mecanismo exacto de vuelta atrás.

---

## 5. Clasificación de Componentes: Clase vs. Relevancia

EVO separa estrictamente dos dimensiones que otros optimizadores confunden:

- **CLASE (`componentClass`):** Conocimiento curado y universal sobre el rol estructural del componente dentro de Windows ([ADR-0009](adr/0009-component-classification.md)):
  - **Prescindible (`Expendable`):** Componentes accesorios sin impacto en funciones principales del sistema.
  - **Opcional (`Optional`):** Funciones secundarias desactivables si el usuario no las emplea.
  - **Necesario para ciertas funciones (`FeatureDependent`):** Requerido por subsistemas específicos (impresión, Bluetooth, virtualización, Xbox, etc.).
  - **Crítico para Windows (`Critical`):** Esencial para la estabilidad, seguridad base o arranque del sistema operativo.
  - **Sin determinar (`Undetermined`):** Todo componente que aún no posee ficha auditada en el catálogo recibe esta clasificación por defecto.
- **RELEVANCIA:** Evidencia local empírica de uso y coste medido en la máquina específica del usuario.

---

## 6. Regla de Evidencia

Sin evidencia local recolectada por sondas y sin compatibilidad verificada con la compilación de Windows y el hardware presente, **EVO no recomienda un cambio**. Toda evaluación de una ficha frente a un equipo produce uno de cuatro estados formales ([ADR-0007](adr/0007-optimization-intelligence.md)):

- `Applicable`: compatible, sin conflictos y respaldado por evidencia local de coste o no uso.
- `NotApplicable(razón)`: el hardware, la edición de Windows o el estado actual hacen innecesaria o incompatible la acción.
- `Unknown(razón)`: faltan datos de sonda o ventana mínima de observación para emitir juicio fundamentado.
- `Conflict(con)`: entra en colisión con una restricción del usuario, una dependencia activa o una acción ya seleccionada.

---

## 7. Las Mejoras se Demuestran

EVO no anuncia mejoras sin medir. Cada optimización que declara beneficio de rendimiento o recursos se somete a mediciones **antes y después** conservando las **muestras individuales** en base de datos, a partir de las cuales se calculan la **mediana** y la **desviación** ([ADR-0005](adr/0005-storage-and-evidence-chain.md)).

El resultado se presenta al usuario con uno de tres veredictos honestos:

- **`mejora medible`**: la mediana posterior supera el umbral técnico respecto a la línea base considerando la dispersión.
- **`sin cambio medible`**: el ajuste de configuración se aplicó correctamente, pero no produjo diferencia estadística en las métricas observadas. El usuario puede decidir mantener un cambio aunque no arroje mejora medible (por preferencia funcional o privacidad), y EVO lo informará con transparencia.
- **`empeora`**: las métricas posteriores degradan la línea base; EVO lo señala explícitamente y **recomienda revertir** el cambio con un clic.

---

## 8. "Nadie Queda Atrapado"

Ningún usuario puede quedar encerrado en una configuración opaca o irreversible sin saberlo. Cada cambio ejecutado registra de forma inmutable en el diario local:

1. Valor o estado anterior exacto (snapshot propio).
2. Valor o estado nuevo aplicado.
3. Motivo de la aplicación (recomendación por evidencia o selección manual).
4. Fecha, hora e identificador de sesión (`sessionId`).
5. Riesgos declarados y árbol de dependencias evaluado.
6. Método tipado de reversión.
7. Resultado de la verificación post-aplicación.

---

## 9. Perfiles de Usuario como Intenciones

Los perfiles de EVO (**Gaming**, **Oficina**, **Desarrollo**, **Multimedia**, **Bajo consumo**, **PC antigua**, **Personalizado**) no son listas fijas de cambios ni recetas universales. Son **intenciones declarativas** compuestas por:

- Pesos de prioridad sobre objetivos (latencia, memoria libre, autonomía de batería, privacidad, estabilidad).
- Restricciones duras de funciones que no deben alterarse.
- Nivel máximo de consentimiento admitido en propuestas automáticas.

Por ello, **el mismo perfil produce planes de adaptación distintos en equipos distintos**, y la interfaz de usuario explica siempre por qué cada acción fue incluida o descartada en ese hardware concreto.

---

## 10. Privacidad por Diseño

- Todo el análisis, perfilado y toma de decisiones ocurre **100 % en local** y de forma transparente para el usuario.
- La telemetría externa está **desactivada por defecto y no implementada** en el código.
- La observación continua de hábitos de uso local es estrictamente **opt-in**, auditable y revocable en cualquier momento desde la vista *"Qué sabe EVO de mí"* (ver [ADR-0011](adr/0011-usage-observation-privacy.md)).
