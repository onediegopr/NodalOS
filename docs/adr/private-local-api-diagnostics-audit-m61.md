# ADR M61 — Private Local API Diagnostics/Audit

## Estado

Aceptado.

## Contexto

La Private Local API existe como servicio in-process/local. M61 agrega diagnostics y audit para operar la private preview sin exponer datos sensibles.

## Decision

M61 define reportes de diagnostics, request audit events, usage report tenant-scoped, route health, security summary e incident candidates.

El reporte registra:

- Ruta.
- Actor role.
- Tenant/workspace.
- Auth status.
- Tenant decision.
- Rate-limit status.
- License decision.
- Response status.
- Reason codes.
- Audit refs.

## Redaction

No se registran secretos, cookies, session material, request/response bodies, sensitive header values, vault raw, payment card data ni paths sensibles.

## Fuera de alcance

- API publica.
- Listener publico.
- Usuarios externos reales.
