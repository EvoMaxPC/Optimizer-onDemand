# ADR-0008: `Adaptive Core` como Único Decisor y Defensa en Profundidad

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
En un sistema modular con seis dominios especialistas (`Gaming`, `Memory`, `System`, `Network`, `Storage`, `Security`), permitir que cada módulo decida y ejecute cambios por su cuenta provocaría decisiones contradictorias (por ejemplo, el módulo `Gaming` buscando máxima frecuencia mientras el perfil del usuario prioriza `Bajo consumo`, o dos módulos modificando servicios interdependientes sin coordinación).

## Decisión
1. Establecer el **`Adaptive Core`** (alojado en `Engine.Host`) como el **único decisor arquitectónico** del sistema, responsable exclusivo de:
   - Consolidar los hechos del sistema y del usuario (`FactStore` / `ProfileBuilder`).
   - Resolver el grafo de dependencias y conflictos entre módulos (`Planner`).
   - Evaluar las reglas de seguridad y niveles de consentimiento (`SafetyPolicy`).
   - Gestionar la emisión de `ApprovalToken`, las ventanas de reinicio y la coordinación de recuperación.
2. **Módulos como especialistas sin autoridad de ejecución directa:** los seis módulos especialistas únicamente proveen **sondas de lectura**, **expedientes técnicos** y **definiciones de acciones**, sin invocar jamás a `Engine.Elevated` por su cuenta.
3. **Defensa en profundidad en el ejecutor:** `Engine.Elevated` no confía ciegamente en `Adaptive Core`; revalida de forma independiente la política de seguridad, el nivel de consentimiento, el `ApprovalToken`, el snapshot previo y la pertenencia de la primitiva al catálogo cerrado antes de escribir en Windows.

## Alternativas descartadas
- **Arquitectura de plugins autónomos donde cada módulo aplica sus propios cambios:** descartada porque impide garantizar transacciones atómicas globales, resolución de conflictos cruzados y orden correcto de reinicio.
- **Confiar únicamente en la validación del `Adaptive Core` sin revalidar en `Engine.Elevated`:** descartada porque un compromiso de memoria en el proceso no elevado permitiría enviar órdenes inválidas al proceso privilegiado.

## Consecuencias
- Existe un punto único, determinista y auditable donde se explica por qué se aprobó, pospuso o rechazó cada acción.
- La duplicación deliberada de la validación de seguridad entre `Adaptive Core` y `Engine.Elevated` garantiza que ninguna orden fuera de política se ejecute aunque falle la primera barrera.

## Límites y riesgos conocidos
- Centralizar la decisión en `Adaptive Core` exige mantener sincronizadas las reglas de `SafetyPolicy` entre `Engine.Host` y `Engine.Elevated` mediante una biblioteca común de dominio compilada en ambos binarios.
- Un error lógico en el algoritmo de resolución de dependencias del `Planner` afectaría a todos los módulos; por ello requiere pruebas unitarias exhaustivas de grafos cíclicos y conflictos en la Etapa 6 (`F4a`).

## Preguntas abiertas
- Definir la prioridad de desempate cuando dos acciones válidas del mismo plan compiten por modificar parámetros relacionados con pesos idénticos en el perfil de usuario.
