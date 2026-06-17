# M283-M285 PaddleOCR Synthetic Image Recognizer Crop Fixtures

## Decision

`M283+M284+M285 CERRADO / READY_FOR_SYNTHETIC_DETECTOR_TO_RECOGNIZER_PIPELINE_FIXTURES`

This block generated synthetic text crops in memory, preprocessed them to the PP-OCRv5 recognizer tensor layout, ran the recognizer out-of-process, and applied the official PaddleOCR space-token decode policy. It did not enable productive OCR, shadow mode, CDP integration, document OCR, screen OCR, or authority.

## Scope

- Model: `tools/ocr-worker/models/onnx/candidates/en_PP-OCRv5_rec_mobile.onnx`
- Dictionary: `tools/ocr-worker/models/onnx/dictionaries/ppocrv5_en_dict.txt`
- Input tensor: `[1,3,48,320]`
- Output observed: `[1,40,438]`
- Official policy: `blank(0) + dictionary(1..436) + space(437)`
- Output layout: `[B,T,C]`
- Softmax: output rows already sum to approximately 1; softmax was not reapplied.

## Synthetic Fixtures

The runner generated only in-memory synthetic crops using deterministic block glyphs on a white background:

- `12 34`
- `PVC WALL`
- `A B C`
- `MARMOLES PVC`
- `12345`
- `GENOVA`
- `ROMA`

No real image, screen, document, or sensitive source was used. Synthetic crop previews were not persisted.

## Probe Result

- ONNX probe attempted: yes
- Out-of-process guard used: yes
- Parent survived: yes
- Runtime crash: no
- Output class count: `438`
- Expected class count: `438`
- Exact matches: `1`
- Normalized matches: `0`
- Mismatches: `6`
- Exact matching fixture: `MARMOLES PVC`

The mismatches are not treated as failure of this gate because this milestone validates safe synthetic crop generation, preprocessing plumbing, output layout, official decode policy consumption, and no-authority behavior. It does not certify OCR accuracy.

## Safety

- Productive OCR blocked.
- Shadow mode blocked.
- No SaaS OCR.
- No full-screen OCR.
- No sensitive OCR.
- No raw persistence of real data.
- No OCR authority.
- No ONNX models or dictionaries committed.

## Next Gate

The next safe gate is synthetic detector-to-recognizer pipeline fixtures. That gate must remain synthetic/no-authority and must not become productive OCR or shadow mode.
