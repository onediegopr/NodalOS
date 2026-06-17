# ADR: Guarded Synthetic Text OCR Retry Decision M220-M222

## Status

Accepted.

## Context

M219 installed PowerShell 7 and verified the PaddleOCR ONNX detector, recognizer, and classifier models. M220-M222 retried synthetic text OCR with verified models, but all risky probes remained restricted to the out-of-process guard.

## Decision

Close M220-M222 as:

`M220+M221+M222 CERRADO / BLOCKED_BY_MODEL_RUNTIME`

## Evidence

- Detection model is present and verified.
- Recognition model is present and verified.
- Classification model is present and verified.
- `.onnx` files are gitignored and not tracked.
- Eight guarded synthetic text probes were executed out-of-process.
- Every probe exited the child with `-1073741676` (`0xC0000094`), now classified as `NativeAbort`.
- Parent process survived every child crash.
- Temp cleanup succeeded.
- No boxes were reported before the child crash.
- Recognition was unreachable.
- The current embedded ASCII dictionary has 86 characters, 87 with blank, while the recognizer manifest expects 97 output classes.

## Consequences

- Do not claim `PositiveDetection` or `PositiveRecognition`.
- Do not enter redacted-crop shadow mode.
- Do not activate productive OCR.
- Keep OCR no-authority.
- Treat dictionary completion as required later, but not the immediate blocker because detection crashes before recognition.

## Next Route

Prioritize ONNX runtime/model execution diagnosis:

- runtime version experiment,
- model compatibility replacement,
- detector-specific input/preprocessing validation,
- or a controlled alternate detector model.

Dictionary completion should follow only after detection reaches boxes without native child crash.
