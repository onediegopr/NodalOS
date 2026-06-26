# WCU-008-014 UIA/OCR/Robust Perception Interop Inventory

Status: audit-only inventory for Windows Computer Use fixture-safe interop.

Branch worktree: `chrome-lab-001-extension-local-ai-bridge`
Initial expected HEAD: `1aadf7543afa25c50816bf05b93cf4f9d8c53d8c`

## Guard Summary

- Protected scope diff: none detected before implementation.
- Browser live/CDP/live bridge gates: not run.
- Windows live actions: not run.
- OCR engine creation: not performed.
- Raw screenshot persistence: not introduced.
- Existing dirty worktree before this block:
  - `OneBrain.slnx`
  - `tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj`
  - `src/OneBrain.DocumentIntelligence/**`
  - `tests/OneBrain.Safety.Tests/MistralOcrProviderRouterDesignOnlyTests.cs`

## Inventory

| Area | Module | Evidence path | Classification | WCU use |
| --- | --- | --- | --- | --- |
| Robust perception | Window liveness, overlay, empty surface, semantic fallback | `src/OneBrain.BrowserExecutor.Cdp/NodalOsRobustPerceptionServices.cs`; `src/OneBrain.BrowserExecutor.Contracts/NodalOsRobustPerceptionContracts.cs` | `WRAP_ONLY` | Consume redacted readiness/blocker signals through WCU bridge contracts. Do not move provider logic into WCU. |
| Robust perception tests | Robust perception no-authority fixtures | `tests/OneBrain.Safety.Tests/NodalOsRobustPerceptionM136M138Tests.cs` | `REUSE_AS_IS` | Policy precedent: high confidence does not grant action authority. |
| OCR/Vision router | OCR/Vision provider and activation contracts | `src/OneBrain.BrowserExecutor.Contracts/NodalOsOcrVisionContracts.cs`; `src/OneBrain.BrowserExecutor.Cdp/NodalOsOcrVisionServices.cs` | `WRAP_ONLY` | Treat as existing provider line. WCU only consumes redacted observations. |
| Local OCR runtime | ONNX Runtime .NET OCR pipeline | `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxOcrSyntheticInferencePipeline.cs`; `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxOcr*`; `tools/onnx-ocr-probe-runner/**` | `REUSE_AS_IS` | Existing OCR runtime remains outside WCU. WCU tests do not invoke it. |
| PaddleOCR worker | Legacy/diagnostic worker and contracts | `src/OneBrain.BrowserExecutor.Cdp/NodalOsPaddleOcr*`; `tools/ocr-worker/**`; `docs/runbooks/paddleocr-local-worker-operations-m191-m193.md` | `LEGACY_REFERENCE_ONLY` | Historical/diagnostic path. Do not route WCU through it directly. |
| Low-risk screen OCR observation | Evidence-only OCR observation evaluator | `src/OneBrain.BrowserExecutor.Cdp/NodalOsLowRiskOcrObservationServices.cs`; `src/OneBrain.BrowserExecutor.Contracts/NodalOsLowRiskOcrObservationContracts.cs` | `REUSE_AS_IS` | Best matching existing screen OCR contract. WCU bridge can map accepted/rejected evidence results into `VisualPerceptionSignal`. |
| OCR observation isolation | Confidence gate, wrong-window, not-foreground, bounds mismatch | `src/OneBrain.BrowserExecutor.Contracts/NodalOsLowRiskOcrObservationIsolationContracts.cs`; related tests under `tests/OneBrain.Safety.Tests/NodalOsOcrObservationIsolationM322M324Tests.cs` | `REUSE_AS_IS` | Use as policy precedent for no raw image, low confidence handoff, and isolated region evidence. |
| OCR evidence ledger | OCR evidence envelopes and ledger integration | `src/OneBrain.BrowserExecutor.Contracts/NodalOsOcrEvidenceLedgerContracts.cs`; `src/OneBrain.BrowserExecutor.Cdp/NodalOsOcrEvidenceIntegrationServices.cs` | `WRAP_ONLY` | Map evidence refs and redaction status into WCU evidence packs without storing raw screenshots. |
| OCR assisted verification | OCR-assisted verification fixtures and services | `src/OneBrain.BrowserExecutor.Cdp/NodalOsAssistedVerificationServices.cs`; `src/OneBrain.BrowserExecutor.Cdp/NodalOsAssistedVerificationFixtureServices.cs` | `EXTEND_LATER` | Future read-only verification aid only. Not an action-authority source. |
| Mistral OCR provider router | Design-only document intelligence router | `src/OneBrain.DocumentIntelligence/OcrDocumentProviderRouter.cs`; `tests/OneBrain.Safety.Tests/MistralOcrProviderRouterDesignOnlyTests.cs`; `docs/architecture/document-intelligence/ocr-document-intelligence-provider-router.md` | `WRAP_ONLY` | Optional document intelligence provider candidate. Screen-control authority remains blocked. Current files were preexisting dirty worktree inputs for this block. |
| Pixel/redaction preconditions | Pixel redaction and pre-OCR isolation | `src/OneBrain.BrowserExecutor.Cdp/NodalOsPixelRedactionServices.cs`; `docs/adr/ocr-real-redaction-precondition-m181.md` | `REUSE_AS_IS` | Policy precedent for redacted visual evidence and no raw persistence. |
| Browser perception router | CBPR fixture-safe perception/action planner | `src/OneBrain.BrowserPerception/**`; `tests/OneBrain.Safety.Tests/CloakBrowserPerceptionRouterFoundationTests.cs` | `DO_NOT_TOUCH` | Browser line remains fixture-safe/design-only. WCU did not modify or invoke live browser paths. |
| WCU evidence redaction | WCU redactor/evidence pack | `src/OneBrain.WindowsComputerUse/WindowsComputerUseControlPlane.cs` | `REUSE_AS_IS` | Reused for redacting visual text observations in WCU bridge fixtures. |

## Not Found

No production-ready desktop UIA live collector was found in WCU. This block added only a disabled read-only skeleton in WCU. No FlaUI adapter was added.

## Canonical Policy Mapping

Perception is not authorization. Existing OCR/vision modules expose many `NoAuthority` and `ActionAuthority=false` fields. WCU interop keeps the same boundary:

- OCR/Vision may observe visible text, visual hints, blocked states, overlays, and sensitive surfaces.
- OCR/Vision may contribute redacted metadata and evidence refs.
- OCR/Vision may not authorize click, type, submit, login, captcha, payment, delete, overwrite, UAC/admin, clipboard, keyboard, mouse, or live UIA actions.
