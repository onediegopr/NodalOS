# Evidence Intelligence Read-Only Product Surface v1

Decision ID: `GO_EIL_READ_ONLY_PRODUCT_SURFACE_INTEGRATION_READY`

## Purpose

This surface exposes the Evidence Intelligence Layer (EIL) as a read-only product/presenter model for human audit review.

It helps an operator inspect:

- local evidence index summary;
- deterministic lexical search results;
- claim scan verdicts;
- action scan verdicts;
- contradiction, stale, missing and low-confidence signals;
- typed evidence graph summaries;
- action readiness matrix rows;
- required human review;
- safe next step copy;
- disabled semantic backend status;
- no-runtime and local-only boundaries.

## Boundary

The integration is presenter/viewmodel only. No UI route is mounted.

It does not add or enable:

- browser runtime, CDP or Cloak live paths;
- desktop automation or UIA/Win32 live paths;
- OCR live activation or screenshot capture;
- recorder runtime;
- sandbox, VM or container runtime;
- provider, LLM, cloud or network call;
- shell/process runner;
- action execution;
- durable persistence;
- product filesystem mutation.

Existing protected runtime/OCR/browser scopes remain present in the repo and untouched.

## Implementation

The product surface lives in:

- `src/OneBrain.Core/Evidence/EvidenceIntelligenceProductSurface.cs`

Main types:

- `EvidenceIntelligenceSurfaceViewModel`
- `EvidenceIntelligenceReadOnlyPresenter`
- `EvidenceIntelligenceSurfaceFixtureCatalog`
- `EvidenceIntelligenceSearchPanel`
- `EvidenceIntelligenceClaimScanPanel`
- `EvidenceIntelligenceActionScanPanel`
- `EvidenceIntelligenceGraphSummary`
- `EvidenceIntelligenceReadinessMatrixPanel`

The default fixture builds an in-memory local index with DOM, OCR, UIA, policy, stale validation and redacted observation signals. It is deterministic and local-only.

## Product Semantics

Allowed copy:

- `Read-only`
- `Local-only`
- `Runtime not enabled`
- `Semantic backend disabled`
- `Contradiction-first`
- `Lexical deterministic retrieval`
- `Action scan evaluates readiness only`

Forbidden interpretation:

- EIL does not execute actions.
- EIL does not prove runtime readiness.
- EIL does not provide semantic/vector search.
- EIL does not capture live evidence.

## Semantic Backend Status

Semantic/vector search remains disabled. The surface explicitly says:

- semantic backend status is `Disabled`;
- retrieval mode is deterministic lexical retrieval;
- lexical fallback is real;
- no semantic capability is claimed.

## Fail-Closed Behavior

The surface preserves EIL fail-closed decisions:

- contradiction blocks;
- missing required evidence blocks;
- stale evidence blocks sensitive/non-read action;
- policy block wins;
- unsafe live action is blocked;
- approval remains human review, not execution.

## Integration Path

This line is safe for later rendering in a read-only UI host. Any mounted UI must only render the viewmodel and must not add action handlers that run, apply, patch, record, capture or launch anything.

## Future Work

Safe next work:

- read-only UI mount that renders this presenter;
- fixture demo curation;
- external audit packet.

Still blocked:

- live evidence capture;
- runtime adapter wiring;
- provider/LLM augmentation;
- semantic/vector backend unless implemented honestly and separately audited.
