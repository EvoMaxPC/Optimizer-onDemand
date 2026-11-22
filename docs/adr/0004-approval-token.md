# ADR-0004: Token de Autorización de Un Solo Uso (`ApprovalToken`)

## Estado
Propuesto

## Fecha
2026-11-21

## Contexto
Incluso con un canal IPC autenticado ([ADR-0003](0003-elevated-ipc-channel.md)), existe el riesgo de condiciones de carrera del tipo *Time-of-Check to Time-of-Use* (TOCTOU): el estado del sistema o la previsualización mostrada al usuario podría cambiar entre el instante en que el usuario pulsa "Autorizar" y el instante en que `Engine.Elevated` aplica la modificación, o un mensaje podría reenviarse accidentalmente.

## Decisión
Toda solicitud de escritura enviada a `Engine.Elevated` debe ir acompañada de un **`ApprovalToken`** con las siguientes propiedades:
1. **Un solo uso y expiración corta:** cada token posee un tiempo de vida breve (TTL estricto) y su `nonce` se invalida inmediatamente tras ser consumido una vez.
2. **Vinculación criptográfica mediante HMAC:** el token se firma con HMAC-SHA256 sobre la tupla canónica:
   `actionId + sessionId + previewHash + snapshotHash + nonce + expiresAtUtc`.
3. **Verificación independiente en el ejecutor:** antes de aplicar la primitiva, `Engine.Elevated` recalcula por su cuenta el estado actual del objetivo, verifica que el snapshot referenciado por `snapshotHash` existe e íntegro en disco, y comprueba que el estado actual coincide con el representado en `previewHash`.
4. **Invalidación inmediata ante cambios:** si el estado del sistema cambió desde la previsualización o si el snapshot previo no coincide con `snapshotHash`, `Engine.Elevated` **rechaza la ejecución** y solicita una nueva evaluación.

## Alternativas descartadas
- **Autorización booleana implícita por sesión ("sesión ya autorizada"):** descartada porque permitiría ejecutar acciones cuya previsualización el usuario no vio o cuyo estado base varió.
- **Token vinculado solo a `actionId`:** descartado porque no protege contra cambios de parámetros ni garantiza que el snapshot previo haya sido efectivamente persistido antes de escribir.

## Consecuencias
- Ninguna acción reversible puede ejecutarse en `Engine.Elevated` si el snapshot previo no fue creado y verificado criptográficamente antes (`snapshotHash`).
- El usuario tiene la garantía técnica de que lo que se aplica corresponde exactamente a la previsualización que aprobó.

## Límites y riesgos conocidos
- El `ApprovalToken` garantiza la integridad del flujo entre la previsualización, el snapshot y la ejecución, pero no sustituye la atención del usuario: si el usuario aprueba conscientemente una acción sin leer las advertencias en pantalla, el token será técnicamente válido.
- En sistemas bajo carga extrema o con relojes alterados durante la operación, un TTL demasiado corto puede provocar rechazos por expiración que obliguen al usuario a confirmar de nuevo.

## Preguntas abiertas
- Ajustar empíricamente en la Etapa 8 la duración exacta del TTL del `ApprovalToken` para equilibrar seguridad TOCTOU y usabilidad en lotes de acciones.
