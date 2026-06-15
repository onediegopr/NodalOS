# ADR M54 — Private Preview Operational Runbook

## Estado

Aceptado.

## Contexto

La private preview local necesita un runbook operativo para evitar expectativas falsas. El objetivo es operar el producto/admin shell local con soporte, diagnostics y audit export redacted, sin activar producción real.

## Decision

M54 define `NexaPrivatePreviewRunbook` con:

- Operational checklist.
- Support procedure.
- Manual rollback procedure.
- Known limitations.

El runbook declara explícitamente:

- M51 external target proof deferred.
- No SaaS público.
- No billing real.
- No email real.
- No credenciales reales.
- No sitios sensibles reales.
- No AFIP, bancos, ERP, submit, pay, sign o delete.
- Recorder/replay productivo bloqueado.

## Operación

La operación local debe verificar perfil `LocalSandbox`, licencia mock, diagnostics redacted, audit key custody M50, public API listener deshabilitado y billing/email mock-only. El soporte sólo puede recolectar metadata redacted y audit export con manifest.

## Rollback

El rollback es manual/model-only:

- Detener shell local.
- Descartar estado in-memory.
- Generar rollback dry-run report.
- Re-ejecutar diagnostics para confirmar que no hay listener público ni billing/email real.

## Fuera de alcance

- No rollback automático.
- No deploy real.
- No auto-update real.
- No soporte con acceso a secretos.
- No operación externa.
