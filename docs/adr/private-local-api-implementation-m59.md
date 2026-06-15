# ADR M59 — Private Local API Implementation

## Estado

Aceptado.

## Contexto

M47 definio la frontera API publica como design-only. M59 implementa una API privada/local in-process para private preview local, sin exponer red publica.

## Decision

M59 define `NexaPrivateLocalApiService`, host, routes, request, response y audit event.

Rutas iniciales:

- `GET /runtime/status`
- `GET /admin/dashboard`
- `GET /license/status`
- `GET /diagnostics/summary`
- `POST /audit/export`
- `POST /onboarding/free/mock`
- `POST /support/bundle/mock`

El host es in-process, loopback/local-only, sin public listener. Las respuestas son redacted, tenant-scoped, role-aware, license-aware y auditadas.

## Safety

DTOs no contienen secretos, cookies, bodies, vault raw data, payment data, document contents ni paths locales sensibles.

## Fuera de alcance

- API publica real.
- SaaS publico.
- Usuarios externos reales.
- Credenciales reales.
- Billing/email real.
