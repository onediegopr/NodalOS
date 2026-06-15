# ADR: Browser Vault Real Minimal M23

## Status

Accepted for M23 sandbox only.

## Context

M20 defined the productive vault design but kept all OS-backed providers fail-closed. M23 needs a first operational vault boundary so the runtime can prove scoped store/retrieve/revoke behavior without using personal credentials, external sites, raw Chrome profiles, cookies from third parties, or product authentication.

## Decision

M23 adds `BrowserVaultMinimalSandboxProvider` as a sandbox-only local encrypted provider. It stores only synthetic fixture material and returns public `BrowserVault*Result` DTOs that contain references, decisions, audit events, and evidence refs, never values.

The provider uses an in-memory AES key and encrypted blobs held by the provider instance. This is intentionally not a productive vault, not DPAPI, and not Windows Credential Manager. It proves the boundary and policy path without introducing durable key custody risk.

## Provider Choice

Chosen provider:

- `SandboxLocalEncrypted`

Deferred providers:

- DPAPI CurrentUser
- Windows Credential Manager
- External vault

Reasoning:

- A minimal local encrypted sandbox provider is sufficient for fixture login proof.
- DPAPI/Credential Manager require separate key custody, backup, migration, and operational policy work.
- No personal credentials are stored or read.
- The provider is blocked by policy/gate outside sandbox.

## Security Rules

- Reference-only public DTOs.
- Core is the only authority.
- Companion never receives values.
- Retrieval requires valid consent, policy, gate, scope, known reference, and non-revoked state.
- Unknown references fail closed.
- Revocation blocks future retrieval.
- Rotation is modeled and audited without value exposure.
- Audit/evidence/export/logs never include values.

## Integration

M23 integrates with:

- M21 consent grants for `SecretRetrieval`.
- M19 phase gate observed state via `BrowserRuntimeVaultState.MinimalSandboxActive`.
- M17/M15 audit redaction rules.
- M24 authenticated sandbox scenario through an internal core-only handle.

## Explicitly Out Of Scope

- Productive vault.
- DPAPI/Credential Manager real storage.
- External vault.
- Real credentials.
- Real login to external sites.
- Companion secret display or transport.

## Future Work

Before productive vault activation, implement OS-backed key custody, emergency revoke, backup/restore policy, rotation, operational consent UI, and post-implementation audit.
