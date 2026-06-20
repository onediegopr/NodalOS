# NODAL OS Project Understanding Policy Decision Record

Decision: `NODAL_OS_PROJECT_UNDERSTANDING_POLICY_DEFINED`

Status: Accepted for policy foundation.

## Context

NODAL OS has a safe Mission Control foundation for workspace models, path jail contracts, metadata mocks, user-provided context, context review cards, evidence refs, validation summaries, and readiness reports. None of those layers perform real project understanding.

Project Understanding is the future capability that may classify and summarize a local workspace from approved, bounded, redacted, provenance-labeled context. It must remain separate from runtime execution, browser automation, cloud, provider calls, and assignment/runtime authority.

## Definition

Project Understanding means a governed, evidence-aware interpretation of a workspace and mission context for display, planning, and future advisory flows.

Project Understanding is not:

- Runtime execution.
- Approval authority.
- Browser automation.
- Cloud sync.
- Billing, licensing, or BYOK implementation.
- A provider prompt.
- A filesystem scan by default.
- A guarantee that user-provided claims are true.

## Context Stages

- User-provided context: text or metadata supplied by the user or fixture; unverified until future policy permits verification.
- Mock metadata: fixture-safe index data created without real scans, file reads, directory listing, git commands, embeddings, provider calls, or cloud.
- Safe project summary: redacted, display-safe summary derived from workspace and mock/user-provided context.
- Future real scan: disabled until all scan preconditions are satisfied.
- Future real project understanding: disabled until real scan, redaction, evidence, review, and governance preconditions are satisfied.
- Future LLM-assisted understanding: disabled until BYOK/policy/consent, prompt governance, budget policy, redaction, and human review are satisfied.

## Principles

- Local-first by default.
- Explicit consent before any future scan or provider usage.
- Path jail first before any filesystem-related capability.
- Redaction first before display, export, prompt preparation, or evidence reporting.
- Evidence first for traceability; raw evidence payloads remain blocked.
- Approval before any sensitive future operation.
- No cloud by default.
- BYOK and policy before any future LLM usage.
- No execution authority. Project Understanding cannot authorize execution.

## Allowed Data By Stage

- Display: public-safe, user-provided-safe, workspace-metadata-safe, evidence-ref-only, and redacted-only data.
- Export: display-safe context plus redaction summaries and evidence refs.
- Future real scan: only path-jail-scoped metadata after consent and precondition review.
- Future LLM-assisted understanding: only redacted, approved, consented, provenance-labeled context after BYOK and prompt governance.

## Prohibited Data

- Raw credentials, tokens, private keys, cookies, headers, request/response bodies, DOM raw, network raw, screenshots inline, unredacted sensitive paths, and unknown sensitivity context.
- Raw filesystem payloads before real scan policy exists.
- Any context that bypasses Safe Context Boundary.

## Sensitivity, Provenance, Confidence, Freshness

Every context item must carry:

- Sensitivity classification.
- Provenance label.
- Confidence label.
- Freshness label.
- Evidence refs when available.
- Timeline refs when available.

Unknown sensitivity requires review. Sensitive, credential-like, and raw payload context is blocked.

## Required Review

Human review is required when:

- Context is unknown, sensitive, credential-like, raw, stale, unprovenanced, or intended for future provider usage.
- Scan scope is broad or includes risky patterns.
- Evidence refs contradict or fail to support a claim.
- Future Advisor or Assignment Engine would use the context.

## Blockers

Real understanding is blocked by:

- Missing workspace.
- Missing validated path jail.
- Missing user consent.
- Missing redaction policy.
- Missing evidence/timeline linkage.
- Missing scan preconditions.
- Sensitive or credential-like context.
- Cloud quarantine unresolved.
- Recipe Risk Classifier hardening incomplete.
- Positive execution gate missing for any future runtime operation.

LLM-assisted understanding is blocked by:

- BYOK/provider policy missing.
- Prompt governance missing.
- Budget/cost guardrails missing.
- User consent missing.
- Human review missing.
- Sensitive, credential-like, raw, unknown, or unredacted context.

Filesystem scan is blocked by:

- Missing path jail.
- Missing explicit scan scope.
- Missing consent.
- Symlink policy unresolved.
- Binary/size/secret policies unresolved.
- Preview-before-scan missing.

Export is blocked by:

- Secrets, raw payloads, sensitive data, unknown context, or missing redaction.

## System Relationships

Safe Context Boundary decides whether context is display/export/future-use eligible.

Workspace Readiness Gate decides whether the workspace is ready for safe next steps.

Evidence/Timeline provide traceability and must remain ref-only unless policy permits safe metadata.

Future Assignment Engine and Expert Advisor may consume only governed, provenance-labeled, redacted context.

Runtime execution gate remains separate. Project Understanding cannot authorize execution, bypass approval, run automation, call providers, mutate files, or start browser/runtime work.

## Consequences

NODAL OS can continue adding readiness and governance surfaces without implementing real scan, provider usage, prompt creation, cloud, or runtime. Any future move toward real Project Understanding must satisfy this ADR and pass safety tests.
