# Evidence Intelligence Read-Only UI Mount

Decision target: `GO_EIL_READ_ONLY_UI_MOUNT_AUDIT_SAFE_READY`

## Architecture

The EIL UI mount uses the existing read-only presenter/viewmodel as the source of truth:

- Core source: `EvidenceIntelligenceReadOnlyPresenter`
- Mount model: `EvidenceIntelligenceReadOnlyUiMount`
- Product surface: installed sidepanel Mission Control section `#evidenceIntelligenceSurface`
- Data source: deterministic local fixture from `EvidenceIntelligenceSurfaceFixtureCatalog`

The sidepanel mount mirrors the deterministic presenter fixture for a visible, packaged product surface. It does not call the bridge, browser runtime, CDP harness, provider, cloud service, semantic backend, durable store, or filesystem writer.

## Visible Sections

- Evidence Index Summary
- Lexical search results
- Claim scan verdict
- Action scan verdict
- Contradictions first
- Missing / stale / low-confidence evidence
- Typed evidence graph
- Action readiness matrix
- Required human actions
- Safe next step
- Semantic backend disabled notice
- Local-only / no-runtime notices

## Safety Contract

EIL UI mount is read-only and audit-safe:

- Runtime actions are not enabled.
- Semantic/vector backend is not enabled.
- Browser/CDP automation is not enabled.
- WCU and OCR live automation are not enabled.
- Provider/cloud calls are not enabled.
- New durable evidence persistence is not enabled.
- Product filesystem writes are not enabled.
- Human approval remains required for any real-world action.

## Alternatives Rejected

- New app route/router: rejected because the sidepanel already has a tested Mission Control product surface seam.
- Runtime bridge feed: rejected because this hito is local-only and no-runtime.
- Semantic/vector backend integration: rejected because EIL currently exposes lexical deterministic retrieval with semantic disabled notice.
- Durable evidence store: rejected because persistence is a later design hito.
