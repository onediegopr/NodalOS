# M301-M303 PaddleOCR Internal Controlled Real Image Fixtures

## Decision

`M301+M302+M303 CERRADO / READY_FOR_INTERNAL_CONTROLLED_SCREEN_REGION_FIXTURES`

The first internal controlled real image fixture gate passed with local ONNX detector-to-recognizer evidence. The fixtures are internal QA controlled, non-sensitive, not screen captures, and not documents.

## Fixture Policy

Fixture provenance categories:

- `SyntheticGenerated`
- `InternalControlledRealImage`
- `InternalNonSensitiveFixture`
- `RejectedSensitive`
- `RejectedUnknownProvenance`

Each fixture must record:

- id,
- filename,
- source category,
- internal QA creation flag,
- real person/customer/financial/document/screen/sensitive flags,
- expected text,
- OCR pipeline allowance,
- reason.

Storage selected:

`tests/fixtures/ocr/internal-controlled-real-images/internal-controlled-real-image-fixtures.json`

The repository stores tracked metadata only. Raw RGBA fixture images are generated in memory for the probe and are not persisted.

## Fixtures

- `qa-real-pvc-wall`: `PVC WALL`
- `qa-real-roma`: `ROMA`
- `qa-real-12-34`: `12 34`

Accepted: `3`

Rejected: `0`

Unknown provenance and sensitive fixtures are rejected by policy tests.

## Pipeline Evidence

- Detector model available: yes
- Detector verified: yes
- Recognizer model available: yes
- Dictionary available: yes
- Out-of-process guard: yes
- Parent survived: yes
- Recognizer resize mode: `RatioPreservingRightPad`
- Recognizer tensor shape: `[1,3,48,320]`
- Recognizer output shape: `[1,40,438]`
- Class count: `438`
- Official space policy: blank `0`, dictionary `1..436`, space `437`
- Softmax reapplied: `false`

Results:

- `PVC WALL` -> `PVC WALL`, exact, edit distance `0`
- `ROMA` -> `ROMA`, exact, edit distance `0`
- `12 34` -> `12 34`, exact, edit distance `0`

Exact matches: `3`

Normalized matches: `0`

Mismatches: `0`

Total edit distance: `0`

## Safety

- No SaaS OCR.
- No API keys.
- No real screen used.
- No real document used.
- No sensitive fixtures.
- No raw persistence of sensitive data.
- No OCR authority.
- No public product readiness claim.
- No ONNX models committed.
- No gitignored dictionaries committed.

## Validation

- Restore: passed.
- Build: passed.
- OCR/provenance filter: `164 passed`, `1 skipped`, `0 failed`.
- Full suite: not clean due to two unrelated browser smoke flakes.
- Browser flake isolated rerun: `2 passed`, `0 failed`.
