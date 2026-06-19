# NODAL OS - M452 Selector Safety / Human Handoff Discovery

## Scope

M452 reviewed the current Automation Layer ADR, Automation Event/Evidence contracts, EvidenceRefBridge, common redaction, scheduled read-only contracts, orchestration contracts, and roadmap before creating Selector Safety Policy and Human Handoff Contract V1.

This milestone remains contract-only. It does not implement UI, recorder, replay, browser automation, scheduler, queue, or runtime execution.

## Existing Selector / Evidence / Handoff Surface

- `NodalOsAutomationEvidence` already supports `SelectorEvidence`, `DomSnapshotRedacted`, `ScreenshotReferenceFuture`, `NetworkMetadataRedacted`, `HumanNote`, `FallbackEvidence`, and `HandoffEvidence`.
- `NodalOsAutomationHandoffState` already captures handoff reason, blocker text, user options, evidence refs, runtime-deferred flags, and no execution authority.
- `NodalOsEvidenceRefBridge` validates no-authority evidence refs.
- `NodalOsRedactionService` detects common secrets, cookies, headers, tokens, passwords, and private-body indicators.

## Missing For Selector Safety

- A selector strategy hierarchy.
- Explicit semantic/DOM/CDP-first preference.
- Explicit visual/OCR fallback/evidence-only rule.
- Selector stability score and risk evaluation.
- Selector decisions that cannot authorize runtime actions.
- Dedicated rejection paths for sensitive, unstable, mutable, or unsupported selectors.

## Missing For Human Handoff

- A formal handoff contract separate from the broader automation event state.
- Explicit user option enum.
- Specific blocker requirement.
- Rejection of generic `blocked`.
- Explicit no-authority and runtime-deferred validation.
- Timeline/Mission Control correlation fields.

## Selector Secret Risks

Selectors can accidentally capture credentials, cookies, authorization headers, tokens, private bodies, hidden inputs, or account data. V1 treats these as invalid and requires redacted selector paths and labels.

## Visual / OCR Fallback Risks

Visual/OCR selectors are brittle and can overfit to screenshots or text fragments. V1 allows them only as future fallback/evidence strategies and never as the first strategy in policy order.

## Login / Captcha / 2FA Risks

Login, captcha, 2FA, and credentials are explicit human handoff reasons. They do not authorize the system to act. They must produce an exact blocker and user options.

## Automation Event/Evidence Relationship

Selector safety and handoff contracts are designed to feed automation event/evidence records later. They reuse evidence refs and common redaction but remain separate policy/contract boundaries.

## EvidenceRefBridge Relationship

Selector candidates, evaluations, and human handoff contracts validate all evidence refs through the existing bridge. Evidence remains no-authority.

## CommonRedaction Relationship

Selector paths, labels, blocker text, technical logs, reasons, warnings, and forbidden selector material are validated with common redaction.

## V1 Scope

M452-M454 adds selector policy, selector candidate, selector evaluation, human handoff contract, validator, serializer, fixtures, tests, report, artifact, and roadmap update.

## Not Implemented

No UI, recorder, replay, browser automation, workflow designer, DSL parser, queue, trigger, scheduler, timer, background worker, API/HTTP/gRPC, worker runtime, browser action, desktop action, recipe/skill/step execution, persistence DB, cloud runtime, package install/uninstall, external RPA dependency, or visual/OCR productive automation is implemented.

## Decision

Proceed with Selector Safety Policy + Human Handoff Contract V1 as NODAL OS contract-only safety scaffolding subordinate to Mission Control, approval, evidence, timeline, and policy gates.
