## Alcance y Etapa

- **Etapa del plan:** <!-- Ej.: Etapa 1 (F0 - Gobernanza y Documentación) -->
- **Issue / Tarea relacionada:** <!-- Referencia o N/A -->

## Qué cambia

- <!-- Lista concisa de archivos o decisiones añadidas/modificadas -->

## Qué NO cambia

- <!-- Declaración explícita de lo que queda fuera de este PR -->

## Checklist de seguridad y gobernanza

- [ ] No añade comandos Tauri ni permisos (`capabilities`) no revisados.
- [ ] No modifica Windows ni ejecuta acciones sobre el sistema operativo sin la Etapa 9 (EVO Recovery) probada.
- [ ] No presenta datos simulados, mocks ni cifras inventadas como mediciones reales.
- [ ] No añade dependencias externas (`npm`, `NuGet`, `cargo`) sin justificación documentada.
- [ ] Se ha creado o actualizado el ADR correspondiente si cambia una decisión arquitectónica o de seguridad.
- [ ] `node tools/check-docs.mjs` finaliza con código `0`.

## Evidencia de verificación

### VERIFICADO (ejecutado localmente / en CI)
```text
<!-- Pegar salida literal de los comandos ejecutados -->
```

### NO VERIFICADO
- <!-- Indicar explícitamente cualquier elemento escrito pero no ejecutado -->
