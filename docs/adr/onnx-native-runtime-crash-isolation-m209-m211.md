# ADR — ONNX native runtime crash isolation (M209–M211)

- **Status:** Accepted
- **Date:** 2026-06-17
- **Milestones:** M209, M210, M211
- **Base:** `chrome-lab-001-extension-local-ai-bridge` @ `be6b5b3`

## Context

The native ONNX Runtime host crashes inside `InferenceSession.Run` on synthetic text-like fixtures
(M206–M208). The crash is native, so the managed `try/catch` in the synthetic inference pipeline
cannot contain it: the process dies before any result is returned. This blocks any honest synthetic
text OCR validation and threatens the test host.

## Decision

1. **Model the crash, never run it in-process (M209).** A crash reproduction matrix classifies each
   `fixture × dimension × stage` probe as safe-in-process, quarantined-out-of-process-only, or
   blocked-before-runtime — without executing the runtime in the suite.
2. **Isolate risky execution in a child process (M210).** `NodalOsOnnxOutOfProcessGuard` launches
   `tools/onnx-ocr-probe-runner` with a timeout, maps a native child crash / timeout / invalid
   output to a controlled result, kills the process tree on timeout, and always cleans temp files.
   The parent (and test host) always survives.
3. **Decide honestly (M211).** The readiness review yields `ReadyForOutOfProcessOnly` when the guard
   demonstrably contains a native crash with the parent alive. Redacted-crop shadow mode stays
   blocked.

## Consequences

- Risky synthetic text OCR may proceed in M212+ **only** through the out-of-process guard.
- No production OCR, no SaaS, no raw persistence, no full-screen, no sensitive surfaces, no-authority.
- A new tool project (`OneBrain.Tools.OnnxOcrProbeRunner`) is added to the solution; it is a probe
  runner only and is not wired into the production CDP pipeline.
- Parallel investigations remain open: alternative detector model export, ONNX Runtime version, and
  real font rendering for detector-friendly fixtures.

## Alternatives considered

- **AppDomain / managed isolation:** insufficient — a native crash kills the whole process regardless.
- **Disable ONNX entirely:** rejected — safe-shape diagnostics prove the runtime loads and runs.
- **Claim shadow readiness:** rejected — no successful synthetic text run has been observed.
