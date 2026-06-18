# PaddleOCR QA Window Host Real Region Capture Audit M315

## Audit Question

Did M313-M315 create a real QA window host and capture a bounded region safely enough to advance to internal low-risk screen OCR observation?

## Evidence Reviewed

- `tools/qa-window-host`
- `tools/onnx-ocr-probe-runner --real-qa-window-region-probe`
- `artifacts/ocr-vision-onnx/m315/paddleocr-qa-window-host-real-region-capture-summary.json`
- OfficialSpaceToken decode policy from M271-M276
- RatioPreservingRightPad preprocessing from M295-M300

## Safety Findings

- SaaS OCR: not used.
- API keys: not used.
- Full-screen: rejected and not used.
- Documents: not used.
- Sensitive data: not used.
- Raw sensitive persistence: not used.
- Simulated window falsely claimed as real: no.
- Productive OCR: not enabled.
- Shadow mode: not enabled.
- OCR authority/actions: not enabled.
- ONNX models/dictionaries: not committed.

## Technical Findings

- Real QA host created: yes.
- Capture mode: `real-qa-window-region`.
- Window title: `NODAL OS OCR QA Window`.
- Process/source: `OneBrain.Tools.QaWindowHost`.
- Bounds/liveness validated: yes.
- Host cleanup: yes.
- ONNX guard used: yes.
- Parent survived: yes.
- Recognizer output layout: `[B,T,C]`.
- Class count: `438`.
- Official space token policy preserved.
- Softmax was not reapplied.

## OCR Evidence

The pipeline ran, but did not meet the success criteria:

- `PVC WALL -> PVC WALI`, edit distance 1.
- `ROMA -> ROMA`, exact.
- `12 34 -> |2 34`, edit distance 1.

Exact/normalized matches: 1/3.

Total edit distance: 2.

## Audit Conclusion

The capture infrastructure is materially improved and real QA window capture is demonstrated. The OCR evidence is not strong enough to open `READY_FOR_INTERNAL_LOW_RISK_SCREEN_OCR_OBSERVATION`.

Recommended decision: `READY_FOR_QA_WINDOW_CAPTURE_HARDENING`.
