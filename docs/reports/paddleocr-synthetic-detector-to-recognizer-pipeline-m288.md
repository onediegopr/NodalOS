# M286-M288 PaddleOCR Synthetic Detector-To-Recognizer Pipeline

## Decision

`M286+M287+M288 CERRADO / BLOCKED_BY_MODEL_OR_DICTIONARY_AVAILABILITY`

The PP-OCRv5 recognizer candidate and dictionary are present locally, but the detector model required for this synthetic full-image pipeline is not present at:

`tools/ocr-worker/models/onnx/ch_PP-OCRv4_det.onnx`

The runner now contains a guarded synthetic detector-to-recognizer pipeline harness, but the ONNX detector stage was not launched because the detector model was unavailable. This avoids a misleading detector or decode claim.

## Implemented Harness

The new runner mode is:

`--synthetic-detector-to-recognizer-pipeline-probe`

It is designed to execute:

1. generated synthetic full image,
2. detector preprocessing,
3. detector ONNX session/run,
4. detector postprocessing and box ordering,
5. crop extraction,
6. PP-OCRv5 recognizer preprocessing to `[1,3,48,320]`,
7. recognizer ONNX session/run,
8. official PaddleOCR space-token decode preview.

The child mode is guarded:

`--synthetic-detector-to-recognizer-pipeline-child`

## Synthetic Fixtures Planned

- `MARMOLES PVC`
- `PVC WALL`
- `GENOVA`
- `ROMA`
- `12 34`

Variants include centered line, upper-left line, no-space text, wide padding, and numeric text with space.

## Availability Evidence

- Detector model available: no.
- Recognizer model available: yes.
- PP-OCRv5 dictionary available: yes.
- Detector probe attempted: no, blocked before runtime.
- Recognizer probe attempted: no, because detector boxes were unavailable.

## Safety

- Internal development only.
- No public product readiness.
- No SaaS OCR.
- No API keys.
- No real screens.
- No real documents.
- No sensitive data.
- No raw persistence of real data.
- No ONNX models or gitignored dictionaries committed.
- No OCR authority.

## Next Step

Restore or verify the detector model through the approved local model acquisition path, then rerun the guarded synthetic detector-to-recognizer pipeline.

## Validation

- Restore: passed.
- Build: passed with 0 warnings and 0 errors.
- OCR/pipeline filter: 145 passed, 2 skipped, 0 failed.
- Full suite: not clean because of unrelated/pre-existing browser runtime smoke flakes.
- Browser smoke triage: the first full run failed cleanup-related browser smoke tests; isolated rerun of those tests passed. A second full run failed `BrowserRuntimeSmokeRunnerExecutesAllGatesOnFixture` with a WebSocket `Aborted` state during idempotency/replay. This is unrelated to OCR/ONNX and no full-clean claim is made.
