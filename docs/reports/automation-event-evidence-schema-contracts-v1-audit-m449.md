# NODAL OS / NODRIX - M449 Automation Event/Evidence Schema Contracts V1 Audit

## Scope

M449 reviewed the existing Agent Operations contracts and services that future NODRIX automation must use before any recorder, replay, queue, scheduler, browser automation, or UI implementation exists.

This milestone remains contract-only.

## Existing Event And Evidence Building Blocks

- `NodalOsEvidenceBridgeRef` already provides no-authority evidence references, source/use kind, sensitivity, redaction state, ledger reference, provenance, and timestamp metadata.
- `NodalOsEvidenceRefBridge` validates no-authority semantics, sensitivity/redaction combinations, redaction-required states, rejected-sensitive states, and sensitive provenance/ref content.
- `NodalOsRedactionService` provides common redaction and sensitive-content detection for secrets, authorization headers, cookies, tokens, passwords, private bodies, and similar fields.
- `NodalOsRunReport` and `NodalOsAgentProgressReport` already provide report/progress surfaces for Mission Control style visibility.
- Scheduled read-only and orchestration contracts already enforce runtime-deferred flags, global policy evaluation, manual trigger, dry-run-only, and no execution.

## Gaps For Automation

Future automation needs a specific contract surface for:

- automation events that can be placed on a Mission Control / Timeline stream;
- automation evidence that is safe to persist or bridge to the ledger;
- human handoff states with explicit blocker and user options;
- DOM, selector, screenshot-reference, network-metadata, human-note, and step-log evidence kinds;
- validation that rejects raw secrets/cookies/headers/bodies before future automation runtime exists.

## EvidenceRefBridge Relationship

Automation events, automation evidence, and automation handoff states must carry only `NodalOsEvidenceBridgeRef` references. Those references must validate through the existing bridge. They must not grant action authority and must not carry unredacted sensitive data.

## CommonRedaction Relationship

All human summaries, technical summaries, selector paths, redacted DOM snippets, redacted step logs, redacted network metadata, and human notes must pass through common redaction validation. M449 does not introduce a separate automation redaction engine.

## RunReport / ProgressReport Relationship

Automation event/evidence contracts are designed to be report-compatible, not runtime-authoritative. Future RunReport and ProgressReport integration should consume sanitized event/evidence metadata and evidence refs only. Producing a report must not imply execution.

## Timeline / Mission Control Relationship

Events and evidence include Mission/Task/Recipe/Step identifiers where available. Generic events are allowed only with warnings so Mission Control can still display diagnostic items without inventing authority.

## Secret Risks

Primary risks are raw DOM containing credentials, cookies in network metadata, Authorization headers, private request/response bodies, tokenized selectors, and human notes that include secrets. V1 treats these as invalid unless already redacted.

## DOM / Screenshot / Network Risks

- DOM snapshots must be redacted text snippets, not raw page dumps.
- Screenshot evidence is reference-only and future/policy-bound, not binary inline content.
- Network metadata must exclude Authorization, Cookie, Set-Cookie, and body content.
- Selectors must not contain secret-like material.

## V1 Contract Scope

M449-M451 adds:

- `NodalOsAutomationEvent`;
- `NodalOsAutomationEvidence`;
- `NodalOsAutomationHandoffState`;
- event/evidence/handoff enums;
- validator;
- JSON serializer;
- fixtures;
- tests, report, artifact, and roadmap update.

## Not Implemented

No recorder, replay, queue, scheduler, trigger, browser automation, browser action, desktop action, workflow designer, DSL parser, API/HTTP/gRPC, UI, worker runtime, persistence DB, cloud runtime, or external RPA dependency is implemented.

## Decision

Proceed with Automation Event and Evidence Schema Contracts V1 as a contract-only layer subordinate to NODRIX Mission Control, policy, approval, evidence, and timeline boundaries.
