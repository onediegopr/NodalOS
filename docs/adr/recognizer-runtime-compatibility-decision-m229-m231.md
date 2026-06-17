# ADR - M229-M231 Recognizer Runtime Compatibility Decision

## Status

Accepted.

## Context

M226-M228 isolated the OCR crash downstream of detection. The recognizer model creates a session and exposes metadata, but `session.Run` crashes with `0xC0000094` even for zero/ones/gradient tensors.

Dictionary mismatch is known:

- recognizer output classes: `97`
- ASCII+blank: `87`

Dictionary completion is not the primary next step while recognizer runtime cannot complete a run.

## Decision

Add a recognizer-only runtime experiment matrix:

- tensor variants
- NCHW shape variants
- unsupported NHWC skip
- invalid shape pre-runtime block
- session options matrix

All risky recognizer execution remains out-of-process.

## Consequences

The next technical route is:

`READY_FOR_ONNX_RUNTIME_VERSION_EXPERIMENT`

This is preferred before recognizer model replacement because zero/ones/gradient tensors crash in `session.Run`, which points first to runtime/model execution compatibility rather than crop preprocessing.

## Non-Goals

- no productive OCR
- no shadow mode
- no real documents or real screens
- no SaaS OCR
- no dictionary download
- no OCR-as-authority

## Final Decision

`M229+M230+M231 CERRADO / READY_FOR_ONNX_RUNTIME_VERSION_EXPERIMENT`
