# OCR/Vision Controlled Activation Readiness M180

Date: 2026-06-16

Product: NODAL OS

Decision: real OCR remains disabled. SaaS OCR remains disabled. Current phase is controlled activation design only.

## Current Status

- OCR/Vision registry: model-only.
- PaddleOCR: future primary local worker candidate, stub only.
- Tesseract: future lightweight fallback, stub only.
- SaaS OCR: disabled-by-default.
- VLM providers: disabled-by-default.
- Local worker: not installed.
- Python worker: not created.
- Real OCR executed: no.
- SaaS OCR executed: no.
- API keys stored: no.
- OCR/Vision no-authority: confirmed.

## Why Real OCR Remains Disabled

- No audited local worker exists.
- No PaddleOCR/Tesseract runtime exists.
- No process/container/CLI/loopback worker is installed.
- No activation opt-in exists.
- No activation audit evidence exists.
- Redacted-crop shadow mode has not been run.
- Privacy, budget and rollback gates are not complete for real OCR.

## What Is Needed To Enable Local PaddleOCR Real

- Create a local worker outside Core .NET.
- Keep communication JSON-only.
- Install/version PaddleOCR in that worker under an audited local setup.
- Run synthetic-only fixtures first.
- Pass redaction and crop-only policy.
- Configure budget, privacy, no-authority, rollback/pause and human escalation.
- Produce activation audit evidence.
- Keep full-screen OCR blocked by default.

## What Is Needed To Enable Tesseract Real

- Same local worker boundary requirements.
- Use as fallback for simple printed text only.
- Keep low-confidence results human-reviewed.
- Keep no-authority.

## What Is Needed To Enable SaaS OCR Real

- Explicit opt-in.
- Secret vault integration, no raw keys in config/docs/logs.
- Budget and cost controls.
- Privacy/data-transfer review.
- Redaction gate.
- Provider-specific audit evidence.
- Human escalation policy.
- Current phase does not allow SaaS real activation.

## Risks

- Redaction failure can expose sensitive screenshots/crops.
- Full-screen OCR can capture unrelated content.
- SaaS OCR creates external transfer risk.
- OCR confidence can be over-trusted by operators.
- Worker runtime can add operational and dependency risk.

## Next Recommended Phase

Prepare a local worker proof plan for synthetic-only fixtures. Do not install or run OCR until the next phase explicitly opts in.

## Final M180 Result

- Activation gate exists.
- Default decision: blocked.
- Current phase decision: blocked by default / missing worker and opt-in.
- Synthetic readiness can be modeled as `ReadyForSyntheticOnly`, but it does not enable real OCR in this phase.
- SaaS remains blocked.
- OCR/Vision remains no-authority.
