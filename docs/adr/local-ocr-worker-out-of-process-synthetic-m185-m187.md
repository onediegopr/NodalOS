# ADR — Local OCR Worker Out-of-Process Synthetic (M185-M187)

**Status:** Accepted  
**Date:** 2026-06  
**Milestones:** M185, M186, M187

## Context

M182-M184 established the synthetic worker skeleton, request/response envelopes, synthetic activation pipeline, and health checker. However, the worker was fully in-process and synthetic-only. To prepare for a potential future where a real local OCR runtime (PaddleOCR, Tesseract) is installed and run out-of-process, we need:

1. A scaffold for the out-of-process worker (M185)
2. A synthetic IPC/loopback contract executor to validate the contract without a real process (M186)
3. A runtime isolation dry run to audit readiness before any real OCR runtime is activated (M187)

## Decision

### M185 — Worker Scaffold

- Created `tools/ocr-worker/` directory with README, contracts, and fixtures
- Created `NodalOsLocalOcrWorkerSyntheticManifestService` with:
  - `NodalOsLocalOcrWorkerManifest` — declares supported transports, blocked features, no real OCR
  - `NodalOsLocalOcrWorkerContractManifest` — declares contract version, input limits, blocked features
  - `NodalOsLocalOcrWorkerTransport` — transport kind (InProcessSynthetic only), IPC mode, capability flags
  - `NodalOsLocalOcrWorkerCommandSpec` — synthetic command template
- All supported transports are synthetic-only; `FuturePythonWorker` and `FutureContainerWorker` are explicitly excluded

### M186 — IPC Synthetic Contract Execution

- `NodalOsLocalOcrWorkerSyntheticTransport` enforces transport capability gates: real OCR, external process, network, raw persistence all blocked
- `NodalOsLocalOcrWorkerIpcContractExecutor` runs synthetic contract against multiple request fixtures, produces `NodalOsLocalOcrWorkerIpcContractSummary`
- `NodalOsLocalOcrWorkerLoopbackSimulator` simulates a full IPC loopback with valid/invalid/full-screen requests
- All responses are no-authority, redacted, synthetic-only

### M187 — Runtime Isolation Dry Run

- `NodalOsLocalOcrWorkerRuntimeIsolationDryRun` evaluates skeleton, manifest, contract, isolation audit, and activation gate
- Produces `NodalOsLocalOcrWorkerRuntimeIsolationReport` with `ReadyForSyntheticOutOfProcessOnly` decision
- Confirms real OCR remains not-ready

### Why no real OCR yet

- PaddleOCR requires Python + model files (not installed)
- Tesseract requires native binaries (not installed)
- Activation gate (`NodalOsOcrRealActivationGate`) keeps `RealOcrEnabled=false`
- Redaction pipeline must be proven before any real OCR access

### Isolation model

- Transport gates: can't call real OCR, can't invoke external process, can't open network, can't persist raw
- Redaction precondition: all inputs must have valid `RedactionResultId`
- No network: loopback/in-process only
- No authority: worker has zero authority to decide or act
- Rollback/pause: skeleton supports SyntheticReady ↔ Paused transitions

## Consequences

- Synthetic-only out-of-process contract validated
- All future real OCR slots documented but blocked
- Claude audit prompt prepared for pre-real-OCR review
- No runtime change to existing services

## Next phase requirements

Before installing PaddleOCR/Tesseract:

1. External security review (Claude audit in `docs/audits/claude-local-ocr-worker-pre-real-audit-prompt-m187.md`)
2. Redaction pipeline proven with real-world crop samples
3. Isolation dry run must pass with transport set to `FuturePythonWorker`
4. Activation gate must approve real OCR with explicit opt-in
5. Sandbox/container boundary must be proven before first real OCR invocation
