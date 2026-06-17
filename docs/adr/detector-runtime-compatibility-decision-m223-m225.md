# ADR: Detector Runtime Compatibility Decision M223-M225

## Status

Accepted.

## Context

M220-M222 closed as `BLOCKED_BY_MODEL_RUNTIME` because full guarded synthetic OCR crashed in the child process during the detection path, with exit `-1073741676` / `0xC0000094`.

M223-M225 isolated detector runtime execution from the full OCR pipeline.

## Decision

Close M223-M225 as:

`M223+M224+M225 CERRADO / BLOCKED_BY_MODEL_RUNTIME`

This decision means the overall OCR path remains blocked by model runtime, but the detector-only experiment did not reproduce the crash and does not justify a detector model replacement or ONNX Runtime version experiment yet.

## Evidence

- Detector model is present and verified.
- Detector session creation succeeds.
- Detector input metadata is `x=[-1,3,-1,-1]`.
- Detector output metadata is `sigmoid_0.tmp_0=[-1,1,-1,-1]`.
- Detector-only `session.Run` succeeds for zero, ones, gradient, direct synthetic text, current preprocessed synthetic text, safe rectangle, and safe circle tensors.
- Tested session options do not change outcome because no detector-only crash is present.
- NHWC is skipped because metadata/manifest expect NCHW.
- Detector postprocessing is reached and succeeds with current preprocessed synthetic text.
- The full guarded OCR probe still crashes with `0xC0000094`.

## Consequences

- Do not claim positive OCR.
- Do not enable shadow mode.
- Do not enable productive OCR.
- Do not change ONNX Runtime version in this block.
- Do not replace detector model based on current evidence.
- Do not treat dictionary completion as next until the downstream runtime crash is isolated.

## Next Route

Create a follow-up isolation block for recognition runtime/full-pipeline handoff:

- run recognizer-only out-of-process with manual synthetic safe crop,
- isolate recognition `session.Run`,
- isolate crop extraction and detector-to-recognizer handoff,
- then return to dictionary completion only if recognition runtime does not crash.
