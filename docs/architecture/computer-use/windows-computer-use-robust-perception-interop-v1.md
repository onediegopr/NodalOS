# Windows Computer Use Robust Perception Interop v1

Status: fixture-safe design and passive-contract implementation.

Decision: WCU consumes existing Robust Perception/OCR outputs through passive, redacted bridge contracts. WCU does not own an OCR engine and does not treat visual perception as action authority.

## Desktop Perception Priority

1. UIA/FlaUI semantic tree: primary source for accessible role, name, automation id, runtime id, control type, pattern metadata, and bounds. Read-only only.
2. Win32 window context: process name, title, class name, modality, allowlist, DPI, foreground/liveness metadata. Read-only only.
3. UIA events: future passive stream for focus/tree/lifecycle metadata. No Invoke, Click, SetValue, keyboard, mouse, or clipboard.
4. Existing Robust Perception OCR/Vision fallback: redacted observations, visual text hints, blocked/empty/overlay signals, and sensitive-surface signals from existing OCR/Robust Perception modules.
5. Human handoff: required when semantic identity is insufficient, visual-only targeting is needed, sensitive or destructive surfaces appear, confidence is low, or system/admin blockers are detected.

## Authority Boundary

Perception is not authorization.

OCR/Vision can:

- observe visible text;
- detect visual labels and approximate regions;
- detect blocked, empty, loading, overlay, UAC/admin-like, payment, login, OTP, or destructive signals;
- provide confidence, source, hash, evidence refs, and redaction metadata;
- support human review and read-only context.

OCR/Vision cannot:

- authorize click, type, submit, login, captcha, payment, delete, overwrite, UAC/admin, clipboard, keyboard, mouse, shell, browser live, WebSocket bridge, or live UIA actions;
- bypass WCU policy;
- upgrade a visual-only target into an executable target;
- persist raw screenshots.

## Screenshot and Evidence Policy

WCU bridge contracts represent metadata and redacted text only:

- `RawScreenshotStored=false` is required for WCU visual observations.
- Source images are represented as fixture ids, source hashes, region metadata, and evidence refs.
- Text is redacted before storage in `VisualTextObservation.TextRedacted` and `VisualElementObservation.LabelRedacted`.
- Evidence packs may store redacted summaries and refs, not raw screenshots or raw OCR text.
- Sensitive field categories are retained for audit without retaining values.

## WCU Contract Surface

WCU adds passive contracts:

- `VisualPerceptionSignal`
- `VisualTextObservation`
- `VisualElementObservation`
- `VisualSurfaceRisk`
- `VisualSignalSource`
- `VisualSignalConfidence`
- `RedactedVisualObservation`
- `RobustPerceptionBridgeResult`
- `IComputerUseVisualPerceptionBridge`

These are provider-neutral DTOs. Tests use fixture bridges only; they do not call ONNX, PaddleOCR, Mistral, screen capture, UIA live, CDP live, browser live, network, or WebSocket paths.

## Existing Robust Perception Mapping

| Existing module | WCU bridge target | Notes |
| --- | --- | --- |
| `NodalOsWindowLivenessMonitor` | `VisualPerceptionSignal` reasons and `VisualSurfaceRisk.EmptyOrBlocked` | Read-only liveness/stability context. |
| `NodalOsSystemOverlayDetector` | `VisualSurfaceRisk.ModalOverlay`, `UacAdmin`, `EmptyOrBlocked` | Handoff/blocker signal only. |
| `NodalOsEmptySurfaceDetector` | `VisualSurfaceRisk.EmptyOrBlocked` | Blocks visual targeting. |
| `NodalOsSemanticAccessFallbackService` | `VisualSignalSource.ExistingRobustPerception` | Auxiliary semantic fallback; no action authority. |
| `NodalOsLowRiskOcrObservationEvaluator` | `VisualTextObservation` | Accepted evidence-only OCR can be translated to redacted text observation. Rejected observations map to blockers/handoff. |
| OCR evidence ledger | `RedactedVisualObservation.SourceHash`, evidence refs | Preserve hashes and metadata only. |

## Mistral OCR Provider Router Fit

The Mistral OCR Provider Router belongs to document intelligence, not desktop control. In WCU it may be referenced only as:

- optional document OCR or document AI candidate;
- design-only/live-blocked provider descriptor;
- source for redacted document evidence metadata;
- provider selection context for non-screen document workflows.

It must not:

- control screen perception priority;
- read live desktop screenshots for WCU;
- authorize screen or browser actions;
- bypass local-first screen OCR policy;
- make network calls in fixture-safe validation.

## UIA Read-Only Collector Design

The WCU skeleton exposes:

- `IWindowsUiAutomationReadOnlyCollector`
- `WindowsUiAutomationReadOnlySnapshotOptions`
- `WindowsUiAutomationReadOnlyResult`
- `WindowsUiAutomationReadOnlyCollectorDisabled`

The disabled collector returns `SkippedDisabled`, never captures screenshots, and never uses Invoke, Click, SetValue, keyboard, mouse, or clipboard. A future FlaUI adapter can be added only behind the same read-only contract and must prove no action patterns are called.

## Fusion Policy

`ComputerUsePerceptionFusionClassifier` combines:

- UIA fixture snapshot;
- current WCU Win32-like window context metadata;
- passive visual/OCR fixture signals.

Outputs include:

- capability classification;
- `VisualFallbackRequired`;
- sensitive surface detected;
- blockage detected;
- low confidence reason;
- human handoff reason;
- `ActionAuthorityGranted=false`.

Visual hints can increase diagnostic context for UIA-poor surfaces but cannot raise the system to action-ready. OCR-only targets and low-confidence visual targets require human handoff.

## Evidence/Redaction Mapping

WCU visual bridge fixtures reuse `ComputerUseEvidenceRedactor` for:

- passwords;
- OTP/MFA;
- API keys/tokens/JWTs;
- email addresses;
- payment card-like values;
- SSN-like values.

WCU evidence packs remain metadata-only and redacted. Future integration with OCR evidence ledger should map refs, confidence, acceptance state, and redaction categories, not raw images.
