# Claude Audit Prompt - ONNX Model Availability Reconciliation M217

Audit M215-M217 for honest model availability and retry readiness.

Files to inspect:

- `tools/ocr-worker/models/onnx/paddleocr-onnx-model-manifest.json`
- `tools/ocr-worker/models/onnx/download-models.ps1`
- `tools/ocr-worker/models/onnx/verify-models.ps1`
- `tools/ocr-worker/models/onnx/rollback-models.ps1`
- `src/OneBrain.BrowserExecutor.Contracts/NodalOsOnnxModelAvailabilityContracts.cs`
- `src/OneBrain.BrowserExecutor.Cdp/NodalOsOnnxModelAvailabilityServices.cs`
- `tests/OneBrain.Safety.Tests/NodalOsOnnxModelAvailabilityM215M217Tests.cs`
- `docs/reports/onnx-model-availability-reconciliation-m217.md`
- `artifacts/ocr-vision-onnx/m217/onnx-model-availability-reconciliation-summary.json`
- `docs/adr/onnx-model-availability-reconciliation-m215-m217.md`

Audit questions:

- Does manifest vs disk inventory honestly report detector and recognizer missing?
- Does classifier verification match expected SHA-256 and size?
- Do model paths resolve under the repository root without discovery mismatch?
- Are `.onnx` models gitignored and not committed?
- Are download/verify/rollback scripts controlled and pinned to approved URLs/hashes?
- Is the `pwsh` runtime prerequisite documented accurately?
- Is the reason M212 saw det/rec missing explained by disk absence rather than path mismatch?
- Is guarded synthetic retry blocked until det+rec are verified?
- Did implementation avoid SaaS OCR, raw persistence, full-screen OCR, sensitive OCR, and OCR-as-authority?
- Should NODAL OS continue with RapidOCR/ModelScope acquisition or revisit model source after verification?

Expected audit conclusion:

The honest next step is to run the controlled `download-models.ps1 -Confirm` and `verify-models.ps1` commands under PowerShell 7.2+, then retry guarded synthetic OCR only out-of-process.
