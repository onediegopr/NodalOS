# Claude audit prompt — ONNX native runtime crash isolation (M209 + M210 + M211)

You are auditing the ONNX native runtime crash isolation work delivered in M209–M211 of NODAL OS.
Be skeptical and honest. The goal is to confirm that a native ONNX Runtime crash on synthetic
text-like fixtures can never kill the main/test process, that it is contained as a controlled
result, and that all safety invariants hold.

## Context

- Worktree: `Codigo-m12-audit`, branch `chrome-lab-001-extension-local-ai-bridge`, base `be6b5b3`.
- M206–M208 established: native crash inside `InferenceSession.Run` on text-like synthetic fixtures
  (pixel-font / anti-aliased / thick bars); safe shapes (stripe/rectangle/circle) run cleanly.

## Files to review

- `src/OneBrain.BrowserExecutor.Contracts/NodalOsOnnxNativeRuntimeCrashContracts.cs`
- `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxNativeRuntimeCrashProbeMatrixBuilder.cs`
- `src/OneBrain.BrowserExecutor.Contracts/NodalOsOnnxOutOfProcessGuardContracts.cs`
- `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxOutOfProcessGuard.cs`
- `tools/onnx-ocr-probe-runner/Program.cs`
- `src/OneBrain.BrowserExecutor.Contracts/NodalOsOnnxNativeRuntimeCrashReadinessContracts.cs`
- `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxNativeRuntimeCrashReadinessReview.cs`
- `tests/OneBrain.Safety.Tests/NodalOsOnnxNativeRuntimeCrashProbeMatrixM209Tests.cs`
- `tests/OneBrain.Safety.Tests/NodalOsOnnxOutOfProcessGuardM210Tests.cs`
- `tests/OneBrain.Safety.Tests/NodalOsOnnxNativeRuntimeCrashReadinessM211Tests.cs`
- `docs/reports/onnx-native-runtime-crash-isolation-m211.md`
- `artifacts/ocr-vision-onnx/m211/onnx-native-runtime-crash-isolation-summary.json`

## What to audit

1. **Crash isolation.** Confirm no test path runs a crash-prone probe in-process. Confirm the
   matrix builder never touches the native runtime. Confirm quarantined fixtures are guard-only.
2. **Out-of-process guard.** Confirm child crash / timeout / invalid output / non-zero exit are all
   mapped to controlled results and that the parent always survives. Check the exit-code → crash-kind
   mapping for correctness on Windows (and reasonableness on Unix). Confirm timeout kills the entire
   process tree and that no orphan child is left.
3. **Cleanup.** Confirm the temp working dir is always deleted (including on launch failure / timeout)
   and that no raw image bytes are persisted.
4. **No raw / no sensitive / no full-screen.** Confirm the pre-launch safety gate (parent and child)
   rejects full-screen / sensitive / raw-persisted / non-synthetic requests before any runtime touch.
5. **No-authority.** Confirm results are never usable to approve clicks/submit/pay/sign/delete and
   that the runner is not wired into the production CDP pipeline.
6. **Honest decision.** Confirm `ReadyForOutOfProcessOnly` is justified and that shadow mode
   (`ReadyForRedactedCropShadow`) remains blocked. Confirm the report answers the crash-location
   questions honestly and does not overclaim a positive OCR result.

## Deliverable

Recommend the safest route to achieve a positive **synthetic** OCR text result in M212+:
in-process safe diagnostics only, out-of-process for risky fixtures, render/font strategy change,
ONNX model change, ONNX Runtime version change, or continued block. Flag any place where the code or
docs overclaim, where containment could leak, or where a safety invariant is not actually enforced.
