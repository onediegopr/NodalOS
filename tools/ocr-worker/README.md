# NODAL OS — Local OCR Worker (Out-of-Process Synthetic)

## Status

**Phase:** Synthetic-only scaffold (M185-M187). No real OCR runtime installed.

## Contract

### Version

`nodal-local-ocr-worker.v1.synthetic`

### JSON envelope

```json
{
  "requestId": "string",
  "workerContractVersion": "nodal-local-ocr-worker.v1.synthetic",
  "redactionResultId": "string (required)",
  "redactionDecision": "RedactedSafeForOcr | RedactedWithWarnings | RedactionFailed | BlockedSensitive",
  "safeForOcr": true,
  "originalRawPersisted": false,
  "cropRef": "string (redacted)",
  "fullScreen": false,
  "sensitivity": "Low | Medium | SensitiveSurface",
  "externalDataTransfer": false,
  "noAuthority": true
}
```

### Response envelope

```json
{
  "responseId": "string",
  "requestId": "string",
  "status": "AvailableForSynthetic | SyntheticOnly | FailedHealthCheck | ...",
  "textBlocks": [],
  "boundingBoxes": [],
  "confidence": 0.0,
  "warnings": [],
  "evidenceRefs": [],
  "processingTimeMs": 0,
  "requiresHumanReview": false,
  "noAuthority": true,
  "rawPersisted": false,
  "callsRealOcr": false,
  "callsExternalProcess": false,
  "callsRealSaas": false,
  "redacted": true
}
```

## Rules

- NO real OCR — synthetic-only
- NO raw persistence — all inputs must pass redaction
- NO network — loopback/in-process only
- NO external process — no subprocess, no CLI invocation of real binary
- NO authority — worker has no authority to decide or act
- NO full-screen OCR allowed
- All evidence redacted before audit

## Future slots

When real OCR is approved:

1. **PaddleOCR slot:** `tools/ocr-worker/paddle-ocr-worker.{py|cs}` — Python subprocess or .NET binding
2. **Tesseract slot:** `tools/ocr-worker/tesseract-ocr-worker.{py|cs}` — Tesseract CLI wrapper
3. **Container slot:** `tools/ocr-worker/Dockerfile` — isolated container worker

## How to plug in real worker

1. Install PaddleOCR or Tesseract locally (future)
2. Update `NodalOsLocalOcrWorkerManifest.RealOcrSupported = true` (with approval)
3. Add transport kind `FuturePythonWorker` or `FutureContainerWorker`
4. Update `NodalOsLocalOcrWorkerProcessPolicy` to allow external process (with approval)
5. Pass redacted crop ONLY (never raw image)
6. Re-run isolation dry run before first real invocation
