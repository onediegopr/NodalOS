# Guarded Synthetic Text OCR Diagnosis M214

## Scope

M212-M214 extends the M210 out-of-process guard path for synthetic text OCR probes only. No SaaS OCR, no production OCR, no real documents, no full-screen OCR, no sensitive data, no raw image persistence, and no OCR-as-authority behavior were enabled.

## M212 Probe Matrix

Implemented `NodalOsGuardedSyntheticTextOcrProbeMatrix` with explicit fixture, render, dimension, preprocessing, and safety metadata.

Fixture/render variants represented:

- large centered text
- small centered text
- wide padding text
- soft-border text metadata
- white background
- light-gray background metadata
- pure black text
- dark-gray text metadata
- rectangular pixel-font text
- numeric text
- letters text
- alphanumeric text
- system-font text is represented but blocked/skipped because deterministic safety is not established

Dimensions represented:

- 256x96
- 320x128
- 480x160
- 640x160
- 640x320
- 640x640

Preprocessing variants represented:

- current mean/std
- keep aspect with white padding
- keep aspect with black padding
- threshold/binarization
- RGB order
- BGR order
- channel layout validation

All risky text probes are modeled as requiring the out-of-process guard. The matrix rejects in-process execution for risky text.

## M213 Compatibility Diagnosis

Detection diagnosis now records:

- ONNX Runtime assembly version
- CPU provider intent
- model path
- model opset placeholder from manifest/metadata
- input tensor shape
- input tensor min/max/mean
- NaN/Infinity validation before runtime
- channel layout and color order
- output names/shapes when available
- session.Run crash mapping from guarded child results
- postprocessing crash flag
- boxes detected

Recognition diagnosis now records:

- whether recognition is reachable
- recognizer input shape when reachable
- crop extraction status
- CTC decoding attempt status
- dictionary compatibility status
- dictionary id
- confidence when available

Dictionary/CTC status: the current implementation uses a local `en-ascii` dictionary. The manifest describes `ch_PP-OCRv4_rec.onnx` output as CTC logits `[T,1,VocabSize+1]` with expected output `[40,1,97]`. The local ASCII dictionary is not treated as verified PaddleOCR dictionary compatibility, so positive recognition must not be claimed.

## Runtime And Model Status

ONNX Runtime package:

- `Microsoft.ML.OnnxRuntime` 1.18.1
- provider: CPUExecutionProvider

Model files observed in this checkout:

- present: `ch_ppocr_mobile_v2.0_cls.onnx`
- missing: `ch_PP-OCRv4_det.onnx`
- missing: `ch_PP-OCRv4_rec.onnx`

Because detector and recognizer model files are missing, M212-M214 cannot produce a real positive synthetic OCR run in this checkout. Guarded risky probes are still constrained to the child guard, but the expected real outcome is model missing / blocked by model runtime rather than positive recognition.

## Readiness

Requirements status:

- guard exists: yes
- risky synthetic text never runs in-process: enforced by matrix/tests
- parent survives child crash/timeout: covered by M210 guard tests
- child cleanup works: covered by guard tests
- temp cleanup works: covered by guard tests
- synthetic fixtures remain non-sensitive: yes
- no raw persistence: yes
- no full-screen: yes
- no SaaS: yes
- no-authority: yes
- detection compatibility diagnosed: yes, but real detector model is missing
- recognition compatibility diagnosed: yes, unreachable without detector boxes or safe manual crop; dictionary compatibility remains unverified
- dictionary status documented: yes
- model/runtime/render decision documented: yes
- shadow mode: blocked
- production OCR: blocked

## Decision

`M212+M213+M214 CERRADO / READY_FOR_MODEL_COMPATIBILITY_FIX`

Rationale: the guarded probe expansion and diagnosis contracts are in place, but the detector and recognizer model files required for a true positive synthetic OCR run are absent in this checkout, and dictionary compatibility for `ch_PP-OCRv4_rec.onnx` is not verified. No positive OCR is claimed.
