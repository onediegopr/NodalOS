# ADR M60 — Private Local API Auth/Tenant/Rate Limit

## Estado

Aceptado.

## Contexto

La private local API requiere pruebas live-local de auth, tenant isolation y rate limits antes de cualquier preview local operativa.

## Decision

M60 define:

- `NexaPrivateLocalApiAuthToken`
- `NexaPrivateLocalApiAuthContext`
- `NexaPrivateLocalApiAuthPolicy`
- `NexaPrivateLocalApiAuthDecision`
- `NexaPrivateLocalApiRateLimitPolicy`
- `NexaPrivateLocalApiRateLimitCounter`
- `NexaPrivateLocalApiRateLimitDecision`

Tokens permitidos son sinteticos: owner/admin/viewer/worker/support test tokens.

## Enforcement

La API bloquea:

- Missing token.
- Unknown token.
- Cross-tenant request.
- Unknown tenant.
- Unauthorized worker.
- Support secret access.
- Viewer mutation.
- License feature disabled.
- Rate limit exceeded.

## Fuera de alcance

- Tokens reales.
- Auth externa.
- Listener publico.
- Credenciales reales de cliente.
- Soporte con acceso a vault raw.
