# M573+M574+M575 - Productive Consent Design Review

Decision target: `PRODUCTIVE_CONSENT_DESIGN_REVIEW_READY`.

## Scope

This block reviews the productive consent design, models disabled storage boundaries, and records consent audit acceptance as governance baseline only.

## M573 - Productive Consent Design Review

The review is static, no-op, and non-authorizing.

- `IsReviewOnly=true`.
- `IsNoOp=true`.
- `CanApproveImplementation=false`.
- `CanAuthorizeProductiveConsent=false`.
- `CanPersistConsent=false`.
- `CanEnforceConsent=false`.
- `CanAuthorizeCapability=false`.
- `CanAuthorizeFilesystemAccess=false`.
- `CanAuthorizeLlmContext=false`.
- `CanUseCloud=false`.

Review sections cover storage boundary, scope model, freshness, revocation, per-capability requirements, disclosures, audit trail, evidence/timeline requirements, fail-closed behavior, rollback strategy, local-only default, no cloud default, and no LLM implication.

## M574 - Disabled Consent Storage Contract

The storage contract is disabled by default and does not create productive persistence.

- `DisabledByDefault=true`.
- `IsContractOnly=true`.
- `UsesProductivePersistence=false`.
- `PersistsConsent=false`.
- `ReadsProductiveConsent=false`.
- `WritesProductiveConsent=false`.
- `DeletesProductiveConsent=false`.
- `MigratesConsent=false`.
- `SyncsConsentToCloud=false`.
- `CanAuthorizeCapability=false`.
- `CanAuthorizeFilesystemAccess=false`.
- `CanAuthorizeLlmContext=false`.

Record drafts are fixture-safe, draft-only, non-authoritative, redacted, and cannot authorize real use.

Safety rules block sensitive material, content payloads, unredacted broad paths, cloud sync, provider sync, runtime access, automatic migration, implicit consent inheritance, cross-capability implication, representation implication, and stale or revoked consent.

## M575 - Consent Audit Acceptance

The acceptance pack accepts the governance baseline only.

- `IsAcceptanceOnly=true`.
- `CanApproveImplementation=false`.
- `CanEnableProductiveConsent=false`.
- `CanAuthorizeCapability=false`.
- `CanAccessFilesystem=false`.
- `CanBuildLlmContext=false`.
- `CanUseCloud=false`.

Acceptance criteria require design review, disabled storage, no productive persistence, no enforcement, no capability authorization, no implicit inheritance, no LLM/cloud implication, fail-closed consent states, redaction rules, audit trail requirements, evidence/timeline requirements, rollback strategy, adversarial tests, and continued implementation blocking.

## Artifacts

- `artifacts/agent-operations/m575/productive-consent-design-review.json`.
- `artifacts/agent-operations/m575/disabled-consent-storage-contract.json`.
- `artifacts/agent-operations/m575/consent-audit-acceptance.json`.
- `artifacts/agent-operations/m575/productive-consent-design-review-preview.html`.

## Validation

Executed at block close:

- Restore: passed.
- Build: passed.
- Build without restore: passed.
- Filtered safety tests: 12 passed, 0 failed.
- Full suite: 4251 passed, 37 skipped, 0 failed.
- First full-suite attempt reached the command timeout before returning a test result; rerun with a larger timeout passed unchanged.
- Guard checks over new files, reports, previews, and artifacts: passed.

## Guardrail Confirmation

This block does not implement productive consent, productive persistence, real consent enforcement, capability enablement, path jail activation, OS path resolution, operational scan behavior, content access, content fingerprinting, indexing, representation build, LLM context, provider activity, cloud, or runtime.

## Progress Estimate

- NODAL OS global: 99.9%
- Agent Operations / Automation Layer: 99.2%
- Core Runtime: 76%
- Evidence/Timeline foundation: 93%
- Approval foundation: 89%
- Redaction/Safety foundation: 97%
- Productization foundation: 85%
- Mission Control UX: 80%
- Workspace Local: 82%
- Project Understanding foundation: 91%
- LLM/Assignment: 74%
- Cloud optional: 10%

## Decision

Closed after validation: `PRODUCTIVE_CONSENT_DESIGN_REVIEW_READY`.
