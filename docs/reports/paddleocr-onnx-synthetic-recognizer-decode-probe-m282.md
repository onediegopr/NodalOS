# PaddleOCR ONNX Synthetic Recognizer Decode Probe - M282

## Decision

`M280+M281+M282 CERRADO / READY_FOR_SYNTHETIC_IMAGE_RECOGNIZER_CROP_FIXTURES`

## Scope

This block ran a recognizer-only ONNX probe with a synthetic tensor through the out-of-process guard. It did not use real images, real crops, screenshots, documents, full-screen OCR, sensitive data, SaaS OCR, CDP, shadow mode, or productive OCR.

## Probe Result

PP-OCRv5 English candidate:

- model: `tools/ocr-worker/models/onnx/candidates/en_PP-OCRv5_rec_mobile.onnx`
- dictionary: `tools/ocr-worker/models/onnx/dictionaries/ppocrv5_en_dict.txt`
- input tensor: deterministic `Gradient`
- input shape: `[1,3,48,320]`
- input metadata: `x=[-1,3,-1,-1]`
- output metadata: `fetch_name_0=[-1,-1,438]`
- output shape: `[1,40,438]`
- expected classes: `438`
- observed classes: `438`
- blank index: `0`
- space index: `437`
- output layout: `[B,T,C]`
- softmax reapplied: `false`
- softmax evidence: rows sum approximately to `1.0`
- decode consumed output: yes, non-authoritative preview only
- useful OCR claimed: no
- exit code: `0`
- parent survived: yes

PP-OCRv4 current recognizer was not attempted because `ch_PP-OCRv4_rec.onnx` was not present locally.

## Safety

- Out-of-process guard used
- Parent process survived
- Temp files cleaned
- No raw tensors persisted
- No real image/screen/document used
- No ONNX model committed
- No dictionary committed
- No-authority preserved
- Productive OCR blocked
- Shadow mode blocked
