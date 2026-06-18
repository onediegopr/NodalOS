# ADR — Low-Risk OCR Observation Policy

## Decision

Internal screen OCR observation is allowed only as bounded, evidence-only, non-authoritative output.

## Rules

- `lowRiskObservationOnly = true`
- `actionsAllowed = false`
- `noAuthority = true`
- `evidenceOnly = true`
- `fullScreen = false`
- `containsSensitiveData = false`
- `containsDocumentData = false`
- `containsCredentials = false`
- `officialSpacePolicy = true`
- `softmaxReapplied = false`

## Accepted source in this block

- `real-qa-window-region`

## Rejected in this block

- full-screen
- sensitive regions
- document regions
- credential/password regions
- action requests
- authority requests

## Consequence

OCR output can be logged as supporting evidence, but cannot drive clicks, typing, submit, send, delete, pay, sign, or any authoritative decision.
