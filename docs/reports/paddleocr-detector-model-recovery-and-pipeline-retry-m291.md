# M289-M291 PaddleOCR Detector Model Recovery And Pipeline Retry

## Decision

`M289+M290+M291 CERRADO / BLOCKED_BY_SYNTHETIC_PIPELINE_DECODE_EVIDENCE`

The detector model was recovered and verified. The synthetic detector-to-recognizer pipeline now runs through detector boxes, crop extraction, PP-OCRv5 recognizer runtime, and official-space decode policy. The pipeline is blocked because every synthetic detector-derived crop decoded as a mismatch.

## Detector Recovery

- Expected path: `tools/ocr-worker/models/onnx/ch_PP-OCRv4_det.onnx`
- Source: RapidOCR/ModelScope v3.8.0 PP-OCRv4 detector
- URL: `https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.8.0/onnx/PP-OCRv4/det/ch_PP-OCRv4_det_mobile.onnx`
- Expected SHA-256: `d2a7720d45a54257208b1e13e36a8479894cb74155a5efe29462512d42f49da9`
- Observed SHA-256: `d2a7720d45a54257208b1e13e36a8479894cb74155a5efe29462512d42f49da9`
- Expected/observed size: `4745517`
- Verification: passed with `verify-models.ps1`
- Model committed: no

## Pipeline Retry

- Detector input metadata: `x=[-1,3,-1,-1]`
- Detector output metadata: `sigmoid_0.tmp_0=[-1,1,-1,-1]`
- Detector input shape: `[1,3,640,640]`
- Detector output shape: `[1,1,640,640]`
- Threshold used: `0.3`
- Synthetic full-image fixtures: 5
- Detected boxes: 5
- Recognized crops: 5
- Recognizer output shape: `[1,40,438]`
- Recognizer class count: `438`
- Softmax: output already sums to approximately 1; softmax was not reapplied.
- Decode policy: `OfficialSpaceToken`, blank `0`, dictionary `1..436`, space `437`.

## Fixture Results

- `MARMOLES PVC` -> `ARMOL FS PO`, mismatch.
- `PVC WALL` -> `LCNHL`, mismatch.
- `GENOVA` -> `EAD`, mismatch.
- `ROMA` -> empty, mismatch.
- `12 34` -> empty, mismatch.

## Interpretation

Detector recovery and detector box evidence are successful. Runtime, crop extraction, recognizer session/run, output layout, class count, softmax evidence, and official decode policy are all exercised. The remaining blocker is synthetic pipeline decode evidence: detector-derived crops do not yet decode accurately enough to advance to controlled real image fixtures.

Likely next tuning targets:

- detector box expansion/padding,
- recognizer crop aspect handling,
- PP-OCRv5 preprocessing calibration,
- synthetic font/glyph compatibility,
- crop normalization alignment.

## Safety

- Internal development only.
- No SaaS OCR.
- No API keys.
- No real screens.
- No real documents.
- No sensitive data.
- No raw persistence of real data.
- No OCR authority.
- No public-product readiness claim.
