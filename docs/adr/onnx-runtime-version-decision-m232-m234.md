# ADR - M232-M234 ONNX Runtime Version Decision

## Status

Accepted.

## Context

With `Microsoft.ML.OnnxRuntime 1.18.1`, detector `ch_PP-OCRv4_det.onnx` runs successfully, but recognizer `ch_PP-OCRv4_rec.onnx` crashes during `session.Run`.

Baseline crash:

```text
-1073741676 / 0xC0000094
stage: RecognitionRun/session.Run
```

The recognizer model metadata is:

```text
input: x=[-1,3,-1,-1]
output: softmax_2.tmp_0=[-1,-1,97]
```

Dictionary remains incompatible (`97` output classes vs current ASCII+blank `87`), but dictionary is secondary until recognizer runtime succeeds.

## Decision

Proceed to a controlled ONNX Runtime package upgrade.

Minimum successful candidate:

```text
Microsoft.ML.OnnxRuntime 1.22.1
```

All tested candidates `1.22.1`, `1.23.2`, and `1.25.0` restored, built, preserved detector sanity, and allowed recognizer runtime to produce output shapes for zero/ones/gradient/crop tensors.

The branch remains at baseline `1.18.1` after this experiment. This ADR does not itself perform the permanent package upgrade.

## Consequences

- Next block should upgrade ONNX Runtime, preferably starting with `1.22.1` as the minimal proven candidate.
- After upgrade, guarded synthetic OCR should be re-run.
- Dictionary/CTC completion remains required before claiming recognition success.
- Productive OCR and shadow mode remain blocked.
- No SaaS OCR, raw persistence, sensitive/full-screen OCR, or OCR-as-authority was introduced.

## Decision Text

```text
M232+M233+M234 CERRADO / READY_FOR_ONNX_RUNTIME_UPGRADE
```
