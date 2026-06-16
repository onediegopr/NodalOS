# ADR: DOM + Screenshot Grounding Snapshot M169-M171

## Decision

NODAL OS adds a local/private-preview browser grounding snapshot model for operator debug and evidence review. The snapshot combines redacted DOM metadata, screenshot metadata, page health, focused element metadata, visible interactables and evidence refs.

This is not an authority surface. The screenshot is evidence/debug only, DOM/CDP/Core policy remain preferred, and Core remains authoritative for every action decision.

## Captured

- Snapshot id, runtime id, tab id and step id.
- URL, title and timestamp after redaction.
- DOM hash and screenshot hash.
- Screenshot reference only when redaction succeeds.
- Focused element metadata, visible interactables, bounds, confidence and source.
- Page health, redaction status, evidence refs, source summary and risk summary.

## Not Captured

- Raw DOM/body.
- Cookies, tokens, headers, credentials or secrets.
- Screenshots when redaction failed or sensitive content is detected.
- Sensitive UIA trees or unredacted logs.
- Any submit/pay/sign/delete payloads.

## Redaction Policy

`RedactedSafe` and `RedactedWithWarnings` can produce metadata-only evidence refs. `RedactionFailed` and `BlockedSensitive` block persistence and clear screenshot refs. Sensitive elements are redacted and downgrade the snapshot to warning/blocking status.

If redaction fails, the snapshot is unusable for persistence and appears in the timeline as a warning/blocker requiring Core/human review.

## Relationship To Stagnation

Grounding snapshots feed the Runtime Stagnation Detector as redacted progress snapshots:

- repeated DOM hash
- repeated screenshot hash
- loading/not-loaded page
- blocked page
- no visual change when represented by runtime progress

The detector can recommend retry, replan, ask human or stop with evidence. It does not execute recovery automatically.

## Timeline Binding

The sidepanel reuses the existing vertical timeline renderer. Grounding is rendered as a card inside an existing timeline step. No second timeline is created.

The card can show:

- safe thumbnail reference when redacted
- focused element
- visible interactables
- page health
- confidence/risk
- redaction status
- evidence refs

## Scope Lock

No scope expansion. Grounding does not enable production, SaaS public, public API, real billing/email, real credentials, sensitive sites, submit/pay/sign/delete, recorder/replay productive, external CDP general-ready, embedded runtime or Chromium fork.

## Non-Goals

- No OpenComet dependency/fork.
- No research mode.
- No visual overlay engine.
- No screenshot-only action approval.
- No productive recorder/replay.
- No OCR/vision productive path in this milestone.
