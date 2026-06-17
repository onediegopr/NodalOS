# PaddleOCR Official Space Token Policy Integration - M276

## Decision

`M274+M275+M276 CERRADO / READY_FOR_SYNTHETIC_OFFICIAL_SPACE_DECODE_FIXTURES`

This block integrates the official PaddleOCR `use_space_char=true` class policy into NODAL OS readiness without enabling productive OCR, shadow mode, or any OCR authority path.

## Official Policy

PaddleOCR CTC charset layout is:

```text
blank(0) + dictionary(1..N) + space(N+1)
```

The recognizer output layout is `[B,T,C]`. The output node is already a softmax output, so NODAL OS must not apply softmax a second time.

## Model Formulas

PP-OCRv4 English:

```text
95 dictionary tokens + blank + space = 97 classes
```

PP-OCRv5 English:

```text
436 dictionary tokens + blank + space = 438 classes
```

`IgnoreExtraClass` is explicitly unsafe for PaddleOCR English because the extra class is a real space token.

## Synthetic Probability Fixtures

Only deterministic synthetic probability matrices were used:

- `12 34`
- `PVC WALL`
- `A B C`
- blank-dominant with space as top-2
- space wins argmax in a timestep

No real document, real screen, full-screen crop, sensitive crop, or raw image was used or persisted.

## Readiness

The readiness gate marks:

- class semantics resolved
- mapping policy approved
- productive OCR blocked
- shadow mode blocked
- no-authority preserved
- decode success not claimed for production

## Legacy Test Debt

M276 normalizes three missing legacy test artifacts as tracked, non-sensitive JSON fixtures:

- `artifacts/ocr-vision-onnx/m199/onnx-ocr-offline-readiness-summary.json`
- `artifacts/ocr-vision-onnx/m202/onnx-model-session-smoke-summary.json`
- `artifacts/ocr-vision-onnx/m205/onnx-synthetic-ocr-run-summary.json`

The M199 tests were changed to use a temporary repository root for dummy ONNX files so they cannot overwrite or delete downloaded models in the real worktree.

The browser runtime cleanup smoke now removes stale `onebrain-cdp-*` temp directories before and after the cleanup-specific test, while still failing if a live process keeps a profile locked.

## Safety

- Productive OCR: blocked
- SaaS OCR: not used
- Raw persistence: blocked
- Full-screen OCR: blocked
- Sensitive OCR: blocked
- OCR authority: blocked
- Shadow mode: blocked
- CDP production pipeline: not enabled
- ONNX/dictionary binaries: not committed
