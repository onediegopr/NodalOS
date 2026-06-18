# PaddleOCR QA Window Host Real Region Capture M315

Milestone: M313-M315
Base commit: 78fe513
Decision: M313+M314+M315 CERRADO / READY_FOR_QA_WINDOW_CAPTURE_HARDENING

## Scope

This block replaces the previous `blocked-before-real-capture` state with a real controlled Windows QA window host and bounded real region capture.

Allowed in this block:

- QA-owned Windows host process only.
- Explicit bounded region: x=80, y=64, width=640, height=160.
- Dummy non-sensitive text only.
- Local ONNX detector and recognizer only.
- Out-of-process ONNX guard.

Still blocked:

- Full-screen OCR.
- Real documents.
- Sensitive windows or data.
- CDP pipeline.
- OCR-driven actions.
- Product/public readiness.

## Implementation

Created `tools/qa-window-host`, a minimal Windows Forms host:

- Fixed title: `NODAL OS OCR QA Window`.
- Traceable process: `OneBrain.Tools.QaWindowHost`.
- Known client size: 800x320.
- Renders dummy QA text.
- Captures only the configured client-relative region.
- Writes a temporary raw RGBA fixture for the runner.
- Cleans up the host process and temporary files.

Updated `tools/onnx-ocr-probe-runner`:

- `--real-qa-window-region-probe` now starts the QA host.
- Validates title, process/source, handle/source id, bounds, visibility, liveness, no-full-screen, no-sensitive, no-document.
- Runs the existing detector-to-recognizer child under `NodalOsOnnxOutOfProcessGuard`.
- The child consumes the captured RGBA as `InternalQaWindowRegion`.
- The OCR pipeline remains no-authority and non-productive.

## Evidence

Models available:

- Detector: `tools/ocr-worker/models/onnx/ch_PP-OCRv4_det.onnx`
- Recognizer: `tools/ocr-worker/models/onnx/candidates/en_PP-OCRv5_rec_mobile.onnx`
- Dictionary: `tools/ocr-worker/models/onnx/dictionaries/ppocrv5_en_dict.txt`

Runtime:

- ONNX Runtime: Microsoft.ML.OnnxRuntime 1.22.1
- Recognizer output: `[1,40,438]`
- Class policy: blank `0`, dictionary `1..436`, space `437`
- Softmax: output already softmax; `SoftmaxReapplied=false`
- Resize mode: `RatioPreservingRightPad`

Results:

| Fixture | Expected | Decoded preview | Match | Edit distance |
| --- | --- | --- | --- | --- |
| qa-real-window-pvc-wall | PVC WALL | PVC WALI | Mismatch | 1 |
| qa-real-window-roma | ROMA | ROMA | Exact | 0 |
| qa-real-window-12-34 | 12 34 | \|2 34 | Mismatch | 1 |

Summary:

- Fixtures accepted: 3/3
- Exact matches: 1
- Normalized matches: 0
- Mismatches: 2
- Total edit distance: 2
- Success criteria met: false
- Parent survived: true
- Host process cleaned up: true

## Decision

The real QA window host and bounded region capture are working, but OCR evidence does not meet the required gate of at least 2/3 exact or normalized matches.

Close as `READY_FOR_QA_WINDOW_CAPTURE_HARDENING`.

Recommended next work:

1. Harden QA host rendering and capture timing.
2. Add optional QA region preview artifact for visual inspection, still non-sensitive.
3. Tune detector crop expansion / unclip for real captured window text.
4. Re-run the same bounded region gate before internal low-risk screen OCR observation.
