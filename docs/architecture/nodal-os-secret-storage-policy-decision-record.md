# NODAL OS Secret Storage Policy Decision Record

## Decision

`NODAL_OS_SECRET_STORAGE_POLICY_DEFINED`

NODAL OS will not store raw secrets in JSON, logs, artifacts, reports, screenshots, observability output, handoff packages, timeline entries, evidence records, prompt context, provider settings serialization, or test fixtures.

Future BYOK API keys and provider credentials must be represented in product contracts as references, not values. The future implementation must write credential material only to an approved secure store or vault after explicit user consent and policy review.

This decision record is policy-only. It does not implement secure storage, OS keychain access, local encrypted vaults, provider calls, network access, BYOK runtime, prompt creation, LLM routing, or cloud sync.

## Context

M510-M512 defined Project Understanding policy, future real scan preconditions, and context-to-LLM governance. M513-M515 prepares BYOK settings and future provider test connection UX without enabling real BYOK.

BYOK is sensitive because provider keys can unlock user-funded model access, cost exposure, data exfiltration risk, and prompt-context leakage. NODAL OS therefore requires a strict storage policy before any provider settings are allowed to become operational.

## Principles

- Redaction first.
- Reference values, never raw credential values.
- No raw secrets in JSON/logs/artifacts/reports.
- No secrets in tests.
- No secrets in screenshots.
- No secrets in observability reports.
- No secrets in handoff/export.
- No secrets in timeline/evidence.
- No secrets in prompt context.
- No secrets in provider settings serialization.
- Provider test connection uses a credential reference, never a raw value in UI or logs.
- Errors must be redacted before display, logs, timeline, evidence, or handoff.
- No managed cloud secret sync by default.
- No BYOK runtime until policy, consent, prompt governance, budget guardrails, and audit evidence exist.

## Future Storage Options

Allowed future options require separate implementation review:

- OS keychain or credential manager.
- Local encrypted vault.
- Hardware-backed secure storage if available.
- Enterprise-managed secure store only after explicit policy.

Cloud secret storage is prohibited by default. It requires a separate cloud policy, consent model, data residency review, threat model, and legacy sensitive subsystem quarantine/removal decision.

## Provider Settings Serialization

Provider settings may include:

- Provider kind.
- Model selection placeholder.
- Endpoint policy placeholder.
- Credential reference placeholder.
- Capability declarations.
- Budget policy reference.
- Prompt governance reference.
- Consent reference.
- Redaction policy reference.
- Safe Context Boundary reference.
- Evidence, timeline, and guardrail references.

Provider settings must not include raw credential values, API keys, bearer tokens, cookies, authorization headers, private keys, connection strings, or environment variable values.

## Provider Test Connection Policy

Future provider test connection must:

- Use a credential reference.
- Require explicit user consent.
- Require network/provider policy.
- Require prompt governance if prompt-like payloads are involved.
- Require budget guardrails.
- Record timeline/evidence refs.
- Redact every error before display or logging.
- Never expose credential values in UI, logs, screenshots, observability, handoff, timeline, or evidence.

M515 only models the UX contract. It must remain disabled/mock-only and must not perform a real connection.

## Paste Handling Future

If a future UI allows a user to paste a provider credential, the value must be immediately converted into a secure-store reference and removed from UI state, logs, reports, diagnostics, and in-memory display surfaces wherever possible.

The paste flow itself requires a separate implementation milestone and security review.

## Rotation, Deletion, And Audit

Future secret handling requires:

- Rotation flow.
- Deletion flow.
- Access audit.
- Failed-access audit.
- Provider test audit.
- Redacted error audit.
- Evidence refs for settings changes.
- Timeline refs for user-visible actions.

## Non-Goals For M513-M515

- No secure store implementation.
- No OS keychain implementation.
- No local encrypted vault implementation.
- No provider calls.
- No provider SDK.
- No network or HTTP.
- No BYOK runtime.
- No prompt creation.
- No LLM routing.
- No cloud sync.
- No raw credential persistence.

## Relationship To Existing Boundaries

- Safe Context Boundary decides what context can ever be displayed, exported, or considered for future provider use.
- Context-to-LLM Governance requires future BYOK, consent, prompt policy, and budget guardrails before provider use.
- Project Understanding Policy blocks LLM-assisted understanding until BYOK and prompt governance are ready.
- Mission Control remains read-only/no-op until positive execution authorization exists.
- Evidence and timeline remain ref-only for sensitive material.

## Final Decision

`NODAL_OS_SECRET_STORAGE_POLICY_DEFINED`

NODAL OS will use reference-only BYOK/provider settings until a secure-store implementation is approved. Raw secrets are never allowed in serializable product surfaces.
