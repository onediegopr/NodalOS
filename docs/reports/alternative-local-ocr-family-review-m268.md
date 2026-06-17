# NODAL OS M268 - Alternative Local OCR Family Review Pack

## Purpose

Prepare a Claude-first audit package for selecting the next local OCR engine strategy. This report does not approve implementation, decode, shadow mode, or productive OCR.

## Evidence Included

- M200-M267 OCR history summary.
- ONNX Runtime final version: `Microsoft.ML.OnnxRuntime 1.22.1`.
- Detector status: current detector runs.
- PP-OCRv4 mismatch: `95 + blank = 96`, output `97`.
- PP-OCRv5 mismatch: `436 + blank = 437`, output `438`.
- PP-OCRv5 extra class probability: max `0.2835`, average/max `0.1183`.
- M265-M267 candidate matrix.
- Security constraints.
- Explicit question: what local OCR family/pair should NODAL OS use now?

## Sources For Claude Review

- RapidOCR official model list/default models.
- PaddleOCR official recognition model documentation.
- Tesseract official local OCR documentation.
- NODAL OS reports/artifacts M200-M267.

## Safety Status

- Productive OCR: blocked.
- SaaS OCR: not used.
- Raw persistence: no.
- Full-screen OCR: no.
- Sensitive OCR: no.
- OCR authority: prohibited.
- Decode attempted: no.
- Model/dictionary download: no.
