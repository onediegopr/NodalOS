# ADR: ONNX Model Availability Reconciliation M215-M217

## Status

Accepted.

## Context

M212-M214 closed as `READY_FOR_MODEL_COMPATIBILITY_FIX` because the current checkout had only the classifier ONNX file and lacked the detector and recognizer files required for real OCR.

M200-M202 previously selected RapidOCR/ModelScope v3.8.0 and recorded verified hashes, sizes, scripts, and successful session smoke in another environment state.

## Decision

Keep RapidOCR/ModelScope v3.8.0 as the selected source for now. Do not change hashes, URLs, filenames, or model source without new evidence.

Use only the controlled scripts for acquisition and verification:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/download-models.ps1 -Confirm
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/ocr-worker/models/onnx/verify-models.ps1
```

Do not commit `.onnx` files. `.onnx` files remain gitignored.

## Current Decision

`M215+M216+M217 CERRADO / READY_FOR_MODEL_DOWNLOAD`

## Rationale

The manifest, scripts, and expected paths are internally consistent and match M200-M202 evidence. Detector and recognizer are simply absent from disk in this checkout. The local environment also lacks `pwsh`, so controlled verification/download cannot be executed here.

## Consequences

- Guarded synthetic OCR retry remains blocked until detector and recognizer are present and verified.
- No shadow mode.
- No productive OCR.
- No SaaS OCR.
- No raw image persistence.
- No OCR over full-screen or sensitive content.
- No OCR-as-authority.
