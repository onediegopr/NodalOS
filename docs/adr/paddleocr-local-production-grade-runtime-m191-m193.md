# ADR — PaddleOCR Local Production-Grade Runtime (M191-M193)

## Status

Accepted for controlled local experimental use. **Not accepted for public production OCR.**

## Context

NODAL OS needs a production-grade local OCR capability. After M188-M190 closed pixel redaction V2 and honest pre-PaddleOCR readiness (`ReadyForPaddleOcrDesignOnly`), the next step is to install and control a real PaddleOCR runtime without enabling public production OCR.

## Decision

1. **Runtime:** PaddleOCR + PaddlePaddle CPU, installed in isolated venv `tools/ocr-worker/.venv`.
2. **Scope:** crop-only, redacted-only, local-only, no SaaS, no full-screen, no sensitive data.
3. **Authority:** OCR remains a read-only input sensor; it cannot approve, click, submit, pay, sign or delete.
4. **Public production:** remains disabled until explicit gates pass.
5. **If install fails:** close as `BLOCKED_BY_ENVIRONMENT_WITH_PRODUCTION_PLAN` with scripts, runbook and rollback ready.

## Architecture

- `NodalOsPaddleOcrRuntimeInspector` probes Python/pip/venv/PaddleOCR state.
- `setup-paddleocr.ps1` creates isolated venv and installs wheels.
- `check-paddleocr.ps1` reports JSON status.
- `run-paddleocr-worker.ps1` invokes `paddleocr_worker.py` with base64 JSON request.
- `rollback-paddleocr.ps1` removes venv and temp files.
- `NodalOsPaddleOcrLocalWorkerAdapter` integrates with existing OCR contracts.
- `NodalOsPaddleOcrWorkerInvocationPolicy` fail-closed gates.

## Security

- No secrets or API keys.
- No network calls from worker (CPU install, no cloud inference).
- Temp redacted crop files only; deleted immediately.
- Raw original never written.
- No full-screen, no sensitive, no production enable.

## Rollback

Run `tools/ocr-worker/rollback-paddleocr.ps1` to remove `.venv` and temp files.

## Remaining gates before public production

1. OS-level hard sandbox for worker process.
2. Red-world crop redaction evidence pack.
3. Human opt-in for real OCR activation.
4. Commercial/legal review.
5. Telemetry and abuse monitoring.
