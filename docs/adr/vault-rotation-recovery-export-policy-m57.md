# ADR M57 — Vault Rotation/Recovery/Export Policy

## Estado

Aceptado.

## Contexto

El vault productivo minimal requiere politicas de ciclo de vida antes de usar credenciales reales. M57 modela rotacion, recuperacion y export sin exponer secretos.

## Decision

Se agregan politicas y decisiones para:

- Rotation: requiere policy, audit y owner/admin approval. Nunca expone old/new secret.
- Recovery: requiere owner/admin approval, binding local machine/user para DPAPI y fail-closed si provider o key material no esta disponible. Audit sin valor.
- Export: disabled by default. Puede quedar design-only/manifest-only. Cleartext siempre bloqueado.

## Export

No se implementa export real de secretos. El resultado permitido en este hito es manifest-only o design-only, siempre sin raw secret y con cleartext bloqueado. Export real requiere una politica de cifrado fuerte futura.

## Gate

El phase gate distingue `VaultRotationPolicyDefined`, `VaultRecoveryPolicyDefined`, `VaultExportPolicyDefined`, `VaultExportCleartextBlocked`, `VaultRecoveryFailsClosed` y `VaultRotationExposesSecret`.

## Fuera de alcance

- Export cleartext.
- Recovery con key material no disponible.
- Credenciales reales de cliente.
- External vault.
