# Iconos del Shell Tauri (`apps/desktop/src-tauri/icons`)

## Fuente oficial provisional (`D6`)

El icono fuente vectorial del proyecto se encuentra en:

- [`source/evo-icon.svg`](source/evo-icon.svg) — SVG cuadrado de `1024x1024` definido por el propietario (`D6`). Es un diseño provisional revisable antes de la Etapa 10 (`F7`, empaquetado y firma).

## Generación de iconos derivados (`B6`)

La versión instalada de `@tauri-apps/cli` (`2.12.6`) soporta archivos `.svg` directamente en el subcomando `tauri icon` (verificado mediante `pnpm --filter @evo/desktop tauri icon --help`).

Comando exacto para regenerar los iconos derivados desde la raíz del monorepo:

```bash
pnpm --filter @evo/desktop tauri icon src-tauri/icons/source/evo-icon.svg -o src-tauri/icons
rm -rf apps/desktop/src-tauri/icons/android apps/desktop/src-tauri/icons/ios
```

## Archivos generados para escritorio

- `icon.ico`: icono multi-resolución para Windows.
- `icon.icns`: contenedor de icono requerido por el esquema de bundle de Tauri.
- `icon.png`, `32x32.png`, `64x64.png`, `128x128.png`, `128x128@2x.png`: iconos PNG estándar de ventana y bandeja.
- `StoreLogo.png` y `Square*Logo.png`: recursos visuales de escritorio para Windows.

> Nota de Etapa 2 (`F1a`): En [`../tauri.conf.json`](../tauri.conf.json), `bundle.active` se mantiene en `false` porque la generación de instaladores firmados corresponde a la Etapa 10 (`F7`).
