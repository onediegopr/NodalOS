# PaddleOCR Low-Risk OCR Observation Isolation Gate M324

Base commit: `d756d0c`

Decision: `M322+M323+M324 CERRADO / READY_FOR_INTERNAL_OCR_EVIDENCE_INTEGRATION`

## Scope

This block hardened low-risk OCR observation so bounded QA-window captures are not accepted as evidence unless the capture source and OCR result pass explicit verification and confidence gates.

## What changed

- Added isolation and verification contracts for low-risk OCR observation.
- Added confidence gate and acceptance-state model.
- Switched QA host capture from `Graphics.CopyFromScreen` to `window-client-bitmap-region` capture from the QA window surface.
- Added non-sensitive visual markers:
  - `NODAL-QA-OCR`
  - fixture id
- Added capture fingerprint emission from the QA host.
- Added runner-side fingerprint verification and acceptance gating.

## Verification model

Accepted evidence now requires:

- expected window title and process match
- bounded region inside client bounds
- trusted capture technique: `window-client-bitmap-region`
- fingerprint parity between host metadata and captured region bytes
- `ActionsAllowed=false`
- `NoAuthority=true`
- `EvidenceOnly=true`
- confidence gate passed

Foreground was not made a hard blocker when the capture source is the QA window surface itself. The envelope still records `window-not-foreground-but-window-surface-capture` as a warning.

## Probe result

- observations: `3`
- envelopes created: `3`
- region verified: `3`
- confidence gate passed: `3`
- accepted evidence: `3`
- rejected evidence: `0`
- uncertain evidence: `0`
- wrong window detections: `0`

OCR results:

- `PVC WALL -> PVC WALI`, edit distance `1`, accepted via bounded edit-distance gate
- `ROMA -> ROMA`, exact
- `12 34 -> |2 34`, edit distance `1`, accepted via bounded edit-distance gate

Aggregate:

- exact: `1`
- normalized: `0`
- mismatches: `2`
- total edit distance: `2`

This satisfies the gate because verified observations stayed within the configured edit-distance envelope and all evidence remained non-authoritative.

## Safety

- no SaaS
- no API keys
- no full-screen
- no real documents
- no sensitive data
- no raw sensitive persistence
- out-of-process ONNX guard used
- parent survived
- host cleaned up

