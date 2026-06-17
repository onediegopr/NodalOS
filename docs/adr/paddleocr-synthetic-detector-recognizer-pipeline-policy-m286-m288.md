# ADR: Synthetic Detector-To-Recognizer Pipeline Policy M286-M288

## Status

Accepted as blocked by detector model availability.

## Context

M283-M285 proved that the PP-OCRv5 recognizer can consume synthetic crop tensors under the official PaddleOCR space-token policy. The next step is a synthetic full-image pipeline that includes detector boxes and crop extraction.

## Decision

Add a guarded runner mode for synthetic detector-to-recognizer pipeline fixtures, but fail closed when the detector model is absent.

The pipeline must use only synthetic images and must not read real screens, documents, or sensitive inputs. ONNX execution must happen in the child process via the existing guard when models are available.

## Policy

- Detector model: `tools/ocr-worker/models/onnx/ch_PP-OCRv4_det.onnx`.
- Recognizer model: `tools/ocr-worker/models/onnx/candidates/en_PP-OCRv5_rec_mobile.onnx`.
- Dictionary: `tools/ocr-worker/models/onnx/dictionaries/ppocrv5_en_dict.txt`.
- Recognizer layout: `[B,T,C]`.
- Recognizer tensor: `[1,3,48,320]`.
- Decode policy: `blank(0) + dictionary(1..436) + space(437)`.
- Softmax must not be reapplied.

## Consequences

Because the detector model is unavailable locally in this environment, the block closes with:

`BLOCKED_BY_MODEL_OR_DICTIONARY_AVAILABILITY`

No synthetic detector boxes, crops, or recognizer previews are claimed for this block.

## Prohibitions

- No public product readiness.
- No uncontrolled OCR.
- No real screen OCR.
- No real document OCR.
- No SaaS OCR.
- No raw persistence of real data.
- No OCR-based authority or actions.
