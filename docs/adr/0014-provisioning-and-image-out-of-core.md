# ADR-0014: Aprovisionamiento (`WinGet`) e Imágenes (`Evo.Image`) Fuera del Núcleo

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
Muchas herramientas populares combinan en una misma pantalla la optimización del sistema en vivo con instaladores masivos de programas de terceros y generadores de imágenes ISO de Windows recortadas (*custom ISOs*). Mezclar estas funciones con el motor de optimización en vivo introduce graves riesgos de cadena de suministro (descarga de binarios externos cambiantes) y de irreproducibilidad (modificaciones offline con DISM que eliminan componentes sin posibilidad de reversión en caliente).

## Decisión
Separar arquitectónica y temporalmente el aprovisionamiento de software y la preparación de imágenes fuera del núcleo de *"optimizar el equipo en ejecución"*:
1. **Aprovisionamiento declarativo mediante `WinGet` (Oleada 5, fuera del núcleo):**
   - Cuando se implemente en fases tardías, utilizará exclusivamente **WinGet con manifiestos fijados criptográficamente por hash SHA-256** para proteger la cadena de suministro.
   - **Chocolatey queda fuera de `v1`** para no duplicar gestores de paquetes ni depender de scripts de instalación comunitarios de difícil auditoría.
2. **`Evo.Image` como subsistema separado y fase tardía (Oleada 6):**
   - La personalización de imágenes ISO o despliegue offline mediante **DISM** se diseña como un subsistema independiente (`Evo.Image`), nunca mezclado con el flujo de `Adaptive Core` sobre el sistema en ejecución.
   - No forma parte del ciclo ordinario de diagnóstico y optimización del equipo activo.

## Alternativas descartadas
- **Descarga directa de instaladores desde URLs dinámicas o scripts de terceros:** descartada por riesgo crítico de compromiso de cadena de suministro (*supply-chain attack*).
- **Integrar el recorte de componentes de imagen (estilo NTLite / ISO custom) sobre el sistema operativo en vivo:** descartado porque eliminar paquetes base del almacén WinSxS en caliente rompe las actualizaciones acumulativas y carece de reversibilidad limpia.

## Consecuencias
- El núcleo de EVO (`Engine.Host` + `Engine.Elevated`) se mantiene libre de descargas de red y enfocado al 100 % en diagnosticar, adaptar, medir y recuperar el Windows instalado.
-La Categoría 18 (*Instalación y preparación*) se gobierna bajo sus propias reglas de cadena de suministro en las oleadas 5 y 6 ([CATEGORIES.md](../catalog/CATEGORIES.md)).

## Límites y riesgos conocidos
- Incluso fijando el hash del manifiesto o del instalador en WinGet, los servidores upstream de los fabricantes de software de terceros pueden retirar versiones antiguas y provocar que una instalación fijada por hash falle por enlace caído (*404*) hasta actualizar y reauditar el hash.
- Las imágenes offline generadas con DISM en `Evo.Image` (cuando se implemente en la Oleada 6) no cuentan con el `Recovery Guard` en tiempo real durante la fase de instalación de Windows.

## Preguntas abiertas
- Determinar si `Evo.Image` se distribuirá en el futuro como binario/herramienta separada dentro del monorepo o como módulo opcional descargable.
