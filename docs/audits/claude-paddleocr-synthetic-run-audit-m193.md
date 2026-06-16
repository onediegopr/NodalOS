# Claude Audit Prompt — PaddleOCR Synthetic Run (M193)

## Request

Please audit the NODAL OS M193 PaddleOCR synthetic redacted crop run.

## Scope

1. **Installation control**
   - `setup-paddleocr.ps1` isolates install to `tools/ocr-worker/.venv`.
   - `check-paddleocr.ps1` reports honest status.
   - `rollback-paddleocr.ps1` removes venv and temp files.

2. **Worker adapter**
   - `NodalOsPaddleOcrLocalWorkerAdapter` is disabled-by-default.
   - Invocation policy requires pixel redaction V2, crop-only, no full-screen, no sensitive, no production.

3. **Synthetic run**
   - `NodalOsPaddleOcrSyntheticRunService` generates a synthetic crop.
   - Runs pixel redaction V2.
   - Checks runtime availability.
   - Invokes adapter only if runtime available.

4. **Constraints verified**
   - No SaaS.
   - No API keys.
   - No raw original persistence.
   - No full-screen.
   - No sensitive data.
   - No authority.
   - Production public OCR remains disabled.

## Questions for Claude

1. Is the PaddleOCR module production-grade enough to continue, given the current Python 3.13 blocker?
2. Does the installation isolation satisfy security requirements?
3. Are the invocation policy gates fail-closed?
4. Is the no-authority design preserved?
5. What remains before any commercial use of local OCR?
6. Should we require OS-level sandbox (job object/AppContainer) before first real OCR invocation?

## Context

- Python 3.13 is installed; PaddleOCR/PaddlePaddle wheels do not support it.
- No real OCR was executed in this run.
- Decision: `BlockedByEnvironment` with production plan.
