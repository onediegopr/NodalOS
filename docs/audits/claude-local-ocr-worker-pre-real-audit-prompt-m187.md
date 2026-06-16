# Claude Audit Prompt — Local OCR Worker Pre-Real-Audit (M187)

## Request

Please audit the NODAL OS local OCR worker scaffold (M185-M187) before any real OCR runtime (PaddleOCR, Tesseract) is installed.

## Scope

### 1. Scaffold worker
- `tools/ocr-worker/README.md` — worker design document
- `tools/ocr-worker/contracts/contract-manifest.json` — contract manifest
- `NodalOsLocalOcrWorkerSyntheticManifestService` — manifest/transport/command spec builder
- `NodalOsLocalOcrWorkerManifest` — supported transports, blocked features

### 2. IPC / loopback contract
- `NodalOsLocalOcrWorkerSyntheticTransport` — transport capability gates
- `NodalOsLocalOcrWorkerIpcContractExecutor` — multi-request synthetic executor
- `NodalOsLocalOcrWorkerLoopbackSimulator` — full loopback simulation
- `NodalOsLocalOcrWorkerIpcContractSummary` — contract execution summary

### 3. Isolation
- `NodalOsLocalOcrWorkerRuntimeIsolationDryRun` — dry run evaluator
- `NodalOsLocalOcrWorkerRuntimeIsolationReport` — isolation decision
- `NodalOsLocalOcrWorkerIsolationAudit` — audit from M184 health checker

### 4. Redaction dependency
- `NodalOsImageCropRedactor` (M181) — redaction pipeline
- `NodalOsImageCropRedactionResult.SafeForOcr` — gate for OCR
- `RedactionResultId` must be present on all worker requests

### 5. Raw persistence
- `NodalOsLocalOcrWorkerProcessPolicy.AllowsRawPersistence = false`
- `NodalOsLocalOcrWorkerTransport.CanPersistRaw = false`
- `OriginalRawPersisted = false` on all request envelopes

### 6. External process risk
- `NodalOsLocalOcrWorkerProcessPolicy.AllowsExternalProcess = false`
- `NodalOsLocalOcrWorkerTransport.CanInvokeExternalProcess = false`
- `FuturePythonWorker` and `FutureContainerWorker` transport kinds are **disabled** and not in `SupportedTransports`

### 7. No-authority
- `NoAuthority = true` on all manifests, transports, workers, audits
- Worker cannot decide, cannot act, cannot authorize

### 8. Activation gate
- `NodalOsOcrRealActivationGate.Evaluate()` — must return `RealOcrEnabled=false`
- Synthetic activation (`NodalOsOcrSyntheticWorkerActivationService`) — must return `ReadyForSyntheticOnly`

## Questions for Claude

1. Can we safely pass to **PaddleOCR local real synthetic/crop-only** at this stage?
2. What is missing before installing PaddleOCR or Tesseract?
3. Are there any isolation gaps in the transport/contract/manifest design?
4. Is the redaction pipeline dependency sufficient to protect against raw image access?
5. Does the no-authority design hold if a future Python worker has file system access?
6. Should there be an explicit sandbox boundary (container, AppContainer, or job object) before first real OCR invocation?
7. Are there any information leakage risks in the audit records or evidence refs?

## Context

- All OCR is synthetic-only at this stage
- No PaddleOCR, Tesseract, or any real OCR runtime is installed
- No SaaS OCR (Azure, Google, AWS) is enabled
- Activation gate keeps `RealOcrEnabled = false`
- All evidence is redacted
- Worker has no network, no raw persistence, no external process capability
