# Audit — M316-M318 QA Window Capture Hardening

## Objective

Harden the real QA window capture path enough to move from `READY_FOR_QA_WINDOW_CAPTURE_HARDENING` to `READY_FOR_INTERNAL_LOW_RISK_SCREEN_OCR_OBSERVATION` without changing OCR semantics.

## Inputs Audited

- `tools/qa-window-host/Program.cs`
- `tools/onnx-ocr-probe-runner/Program.cs`
- real bounded QA window region capture
- detector PP-OCRv4 local ONNX
- recognizer PP-OCRv5 local ONNX
- PP-OCRv5 dictionary

## Evidence

- DPI observed: `144`
- DPI scale: `1.5`
- Capture mode: `real-qa-window-region`
- Capture coordinates: physical screen coordinates derived from client-relative region via `PointToScreen`
- Host cleanup: passed
- Parent survival: passed
- Official space token policy: preserved
- Softmax reapplication: `false`

## Baseline

- exact: `1`
- normalized: `0`
- mismatch: `2`
- total edit distance: `9`

## Best Configuration

- `arial-92-bold-antialias-expanded`
- exact: `2`
- normalized: `0`
- mismatch: `1`
- total edit distance: `1`

## Acceptance Check

Required:

- real QA bounded region: yes
- at least `2/3` exact or normalized: yes
- total edit distance `<= 2`: yes
- no provenance violations: yes
- no full-screen: yes
- no sensitive data: yes
- parent survives: yes
- host cleanup: yes

## Audit Outcome

Pass.

The hardening matrix provided sufficient evidence to advance the bounded internal screen OCR gate. Residual error remains on `PVC WALL`, but it no longer blocks the readiness decision for low-risk internal observation.
