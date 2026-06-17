# ADR: Guarded Synthetic Text OCR Runtime Decision M212-M214

## Status

Accepted.

## Context

M207-M211 established that some synthetic text fixtures can crash native ONNX Runtime during `session.Run`, primarily during detection. M210 introduced an out-of-process guard so risky text probes can be attempted without killing the parent process.

M212-M214 expands the guarded synthetic text probe matrix and adds detector/recognizer compatibility diagnosis before deciding the next runtime path.

## Decision

Keep all risky synthetic text OCR attempts out-of-process only. Do not enable in-process synthetic text OCR, redacted-crop shadow mode, productive OCR, or OCR-as-authority behavior.

Final decision:

`M212+M213+M214 CERRADO / READY_FOR_MODEL_COMPATIBILITY_FIX`

## Rationale

The required detector and recognizer files are not present in this checkout:

- `tools/ocr-worker/models/onnx/ch_PP-OCRv4_det.onnx`
- `tools/ocr-worker/models/onnx/ch_PP-OCRv4_rec.onnx`

Only the classifier model is present. Therefore, a real guarded positive synthetic text OCR run cannot be claimed. The recognizer dictionary is also not verified as PaddleOCR-compatible; the local ASCII dictionary is sufficient for controlled decoding tests but not sufficient for a product claim against `ch_PP-OCRv4_rec.onnx`.

## Consequences

- Shadow mode remains blocked.
- Production OCR remains blocked.
- No SaaS OCR is used.
- No real documents or screens are processed.
- No raw image persistence is allowed.
- Next work should restore/verify detector and recognizer model compatibility and dictionary compatibility before repeating guarded synthetic positive probes.
