# ADR M55 — Private Preview Feedback/Audit Loop

## Estado

Aceptado.

## Contexto

La private preview local necesita un ciclo operativo para registrar feedback, issues, diagnostics y audit review sin introducir usuarios externos ni datos reales.

## Decision

M55 define contratos para feedback local, issue report, audit review, session summary, feedback loop y decision. Todo feedback debe ser tenant/workspace scoped, role-aware y redacted.

El evaluator falla cerrado si:

- El feedback contiene secretos, cookies, bodies o paths sensibles.
- Diagnostics no estan redacted.
- Audit export no esta redacted.
- El actor no esta autorizado.
- Hay acceso cross-tenant.

## Evidencia

Diagnostics y audit se adjuntan como refs, no como payload. La session summary registra conteos y accion recomendada sin contenido sensible.

## Fuera de alcance

- SaaS publico.
- Usuarios externos reales.
- Emails reales.
- Sitios reales.
- Soporte viendo secretos.
