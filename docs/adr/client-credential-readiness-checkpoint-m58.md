# ADR M58 — Client Credential Readiness Checkpoint

## Estado

Aceptado.

## Contexto

El vault y la private preview local avanzaron, pero las credenciales reales de cliente siguen prohibidas. M58 crea un checkpoint formal para decidir readiness futura sin habilitar credenciales reales.

## Decision

M58 define `NexaClientCredentialReadinessReport`, checks, risk register, blockers y recomendacion.

El reporte evalua:

- Audit key custody M50.
- Vault OS-backed M39.
- Vault threat tests M56.
- Rotation/recovery/export policy M57.
- Leak-hardening M52.
- Diagnostics/support redaction.
- Tenant governance.
- License gating.
- Core-only boundary.
- Companion no authority.
- Profile raw blocked.
- Public API not exposed.
- M51 external proof status.

## Blockers

Real client credentials quedan bloqueadas si:

- M51 sigue deferred.
- Falta auditoria externa post vault hardening.
- No existe policy aprobada para credenciales reales.
- No existe proceso de soporte/incidente para credenciales reales.

## Fuera de alcance

- Credenciales reales de cliente.
- Sitios reales.
- SaaS publico.
- Soporte viendo secretos.
