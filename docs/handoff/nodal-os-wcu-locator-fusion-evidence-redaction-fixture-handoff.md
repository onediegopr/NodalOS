# NODAL OS WCU Locator Fusion Evidence/Redaction Fixture Handoff

Decision: `GO_WCU_LOCATOR_FUSION_EVIDENCE_REDACTION_FIXTURE_READY`

## Delivered

- Added canonical fixture-safe locator fusion contracts.
- Added `ComputerUseLocatorFusionEngine`.
- Added unified evidence/redaction pack builder.
- Extended evidence kinds for locator fusion, ambiguity, stale risk, selector confidence, and unified evidence.
- Added adversarial redaction and no-authority tests.
- Did not add live Windows reads, P/Invoke, FlaUI, UIA live events, input injection, clipboard, raw screenshots, browser live, WebSocket, CDP, or OCR duplication.

## Key Files

- `src/OneBrain.WindowsComputerUse/ComputerUseLocatorFusion.cs`
- `src/OneBrain.WindowsComputerUse/ComputerUseUnifiedEvidence.cs`
- `src/OneBrain.WindowsComputerUse/WindowsComputerUseControlPlane.cs`
- `tests/OneBrain.Safety.Tests/WindowsComputerUseLocatorFusionEvidenceTests.cs`
- `docs/architecture/computer-use/windows-computer-use-locator-fusion-v1.md`
- `docs/architecture/computer-use/windows-computer-use-evidence-redaction-unification-v1.md`

## Boundary

Locator confidence, OCR/visual hints, UIA events, Win32 context, and evidence packs remain observation-only. They cannot authorize actions.

## Recommended Next Block

`WCU-031-036 -- READ-ONLY LIVE DESIGN GATE + AUDIT PACK`

Do not proceed directly to controlled actions. The next work should design and audit gates for any future read-only live adapter.
