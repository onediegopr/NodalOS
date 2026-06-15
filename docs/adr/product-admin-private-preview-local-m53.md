# ADR M53 — Product/Admin Private Preview LOCAL

## Estado

Aceptado.

## Contexto

M51 external read-only proof permanece deferred. La decisión de roadmap permite una private preview local single-tenant, sin usuarios externos reales ni exposición pública.

## Decision

M53 introduce `NexaPrivatePreviewLocalProfile`, `NexaPrivatePreviewLocalSession`, `NexaPrivatePreviewLocalReadiness` y `NexaPrivatePreviewLocalResult`.

La private preview local sólo es permitida si:

- Corre en máquina local.
- Es single-tenant.
- Usa datos sintéticos.
- Usa billing mock y email mock.
- Mantiene public API como design-only sin listener.
- Mantiene SaaS público deshabilitado.
- Mantiene sensitive real pilot deshabilitado.
- Mantiene recorder/replay productivo bloqueado.
- Requiere audit key custody M50.
- Requiere diagnostics redaction.
- Requiere tenant governance.
- Requiere leak-hardening y skipped tests audit completos.
- Declara M51 external proof como deferred.

## Gate

El phase gate agrega señales `LeakHardeningCompleted`, `SkippedTestsAuditCompleted`, `PrivatePreviewLocalAllowed`, `PrivatePreviewLocalSafe` y `M51ExternalProofDeferred`. Si la preview intenta SaaS público, billing/email real, sensitive pilot, recorder/replay productivo, listener público, diagnostics leak o falta de audit key custody, el gate falla.

## Fuera de alcance

- No SaaS público.
- No API pública real.
- No usuarios externos reales.
- No cobro real.
- No emails reales.
- No credenciales reales.
- No sitios sensibles reales.
- No M51 validado externo.
