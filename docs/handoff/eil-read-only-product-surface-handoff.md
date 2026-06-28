# EIL Read-Only Product Surface Handoff

## Summary

The Evidence Intelligence Layer now has a read-only product surface presenter.

The surface turns EIL index, search, claim scan, action scan, graph and readiness matrix data into deterministic viewmodels for human audit review.

## Entry Point

Use:

- `EvidenceIntelligenceReadOnlyPresenter.CreateFixture()`
- `EvidenceIntelligenceReadOnlyPresenter.Create(index, request)`

Namespace:

- `OneBrain.Core.Evidence`

## Safety Boundary

The presenter is read-only and local-only.

It does not:

- execute actions;
- launch browser/CDP/Cloak;
- inspect desktop/UIA/Win32 live;
- activate OCR or screenshots;
- run recorder or sandbox;
- call provider/LLM/cloud/network;
- start shell/process runners;
- write product files.

## Product Copy Requirements

The surface must continue to show:

- semantic backend disabled;
- lexical deterministic retrieval;
- contradiction-first behavior;
- fail-closed action scan;
- runtime not enabled;
- local-only evidence.

## Tests

Focused tests:

- `EvidenceIntelligenceProductSurfaceTests`
- category `EvidenceIntelligenceProductSurface`
- category `EvidenceIntelligence`

These tests cover:

- lexical search rendering;
- claim scan states;
- contradiction-first action scan;
- missing/stale/policy/live fail-closed states;
- readiness matrix;
- graph summary;
- no-runtime/no-provider/no-network/no-live flags;
- no raw secret leakage;
- no fake semantic/fake success copy.

## Next Safe Step

Render this presenter in a read-only UI host only if the host can guarantee no action handlers, no live capture and no runtime launch. Runtime remains blocked.
