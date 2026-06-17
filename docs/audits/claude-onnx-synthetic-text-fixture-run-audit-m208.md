# Claude Audit Prompt — ONNX Synthetic Text Fixture Run (M208)

## Scope
Audit the M206–M208 synthetic text fixture generator, ONNX text run service, and readiness recheck after discovering that synthetic text-like fixtures crash the native ONNX Runtime host.

## Files to Review
- `src/OneBrain.BrowserExecutor.Contracts/NodalOsSyntheticOcrTextFixtureContracts.cs`
- `src/OneBrain.BrowserExecutor.Cdp/NodalOsSyntheticOcrTextFixtureGenerator.cs`
- `src/OneBrain.BrowserExecutor.Contracts/NodalOsOnnxOcrSyntheticInferenceContracts.cs`
- `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxOcrSyntheticTextRunService.cs`
- `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxSyntheticOcrReadinessReview.cs`
- `tests/OneBrain.Safety.Tests/NodalOsSyntheticOcrTextFixtureM206Tests.cs`
- `tests/OneBrain.Safety.Tests/NodalOsOnnxOcrSyntheticTextRunM207Tests.cs`
- `docs/reports/onnx-synthetic-text-fixture-run-m208.md`
- `artifacts/ocr-vision-onnx/m208/onnx-synthetic-text-fixture-run-summary.json`

## Audit Questions
1. Does the M206 fixture generator produce only synthetic, non-sensitive, non-full-screen fixtures with no raw persistence?
2. Are the default texts (`TEST`, `NODAL`, `HELLO`, `SAFE`, `ABC123`, `12345`) free of sensitive keywords?
3. Does the M207 run service honestly map `ModelMissing`, `ModelUnverified`, `SessionLoadFailed`, and `BlockedByModelRuntime` to the run decision `BlockedByModelRuntime`?
4. Are `RecognitionEmpty` and `DictionaryMismatch` reported honestly without inventing successful recognition?
5. Are quarantined M207 tests marked with the exact ignore message naming the native runtime crash?
6. Do safe-shape diagnostics remain enabled and not claim a positive text OCR run?
7. Do unit-level tests cover no raw persistence, no full-screen, no sensitive, no SaaS, no authority, model missing/unverified handling, and no-boxes-does-not-invoke-recognition?
8. Does the M208 readiness review refuse to advance to `ReadyForRedactedCropShadow` when the run result is `BlockedByModelRuntime`?
9. Is the M208 report honest about the detector crash and the lack of a positive text detection/recognition run?
10. Are all required TestCategory filters present on M206/M207 test classes?

## Expected Decision
Confirm whether M206–M208 should close as `BlockedByModelRuntime` with quarantined text-fixture tests, or if additional work is required before any shadow mode.
