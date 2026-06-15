# ADR M56 — Vault Threat-Test / Core-only Boundary

## Estado

Aceptado.

## Contexto

M39 introdujo vault OS-backed minimal con DTOs reference-only. M56 agrega pruebas de amenaza para rutas que podrian intentar usar el vault fuera del boundary core-only.

## Decision

M56 define `BrowserVaultThreatRequest`, `BrowserVaultThreatDecision` y `BrowserVaultCoreOnlySecretHandle`.

El boundary bloquea:

- Companion recuperando secretos.
- Support recuperando secretos.
- Admin dashboard mostrando raw secret.
- Public API incluyendo secreto.
- Diagnostics o audit export incluyendo secreto.
- Cross-tenant retrieval.
- Worker no autorizado.
- Falta de entitlement ProductiveVault.
- Gate failed.
- Serializacion/export del handle.

## Core-only handle

El handle interno puede existir solo con actor Core, mismo tenant, worker autorizado, entitlement activo y gate passed. No es DTO publico, no es serializable y no es exportable.

## Fuera de alcance

- Credenciales reales.
- Export de secreto.
- Companion con autoridad.
- Public API real.
