# ADR: OCR/Vision Admin, SaaS Stubs and Evaluation M175-M177

## Decision

NODAL OS adds model-only OCR/Vision admin settings, SaaS connector stubs and an evaluation harness. This does not activate OCR real, SaaS OCR, API keys, Python workers, PaddleOCR/Tesseract runtimes, browser scope expansion or action authority.

## Admin Settings Design

The admin settings surface is a service/view model, not a production provider manager. It lists:

- provider id, display name and kind
- enabled/disabled/paused/testing/shadow/fallback status
- capabilities
- cost, performance and privacy profiles
- API key placeholder state
- external transfer flag
- sensitivity/full-screen/crop policy
- priority and fallback order
- budgets and confidence/latency thresholds
- blocked reasons
- last evaluation summary

Enable/disable, pause/resume, priority update and fallback update are model-only. They do not make providers executable.

## API Key Handling

No API keys are stored. Admin settings only model:

- `Missing`
- `PlaceholderConfigured`
- `SecretVaultRequired`
- `Disabled`

Any real API key or executable provider attempt is blocked.

## SaaS Disabled-By-Default

SaaS disabled-by-default remains mandatory. Azure Document Intelligence, Google Document AI, Google Vision OCR, OpenAI Vision OCR/VLM, Mistral OCR and Amazon Textract are stubs only.

Every SaaS provider stub:

- requires opt-in
- requires secret vault state
- requires budget
- blocks sensitive data by default
- blocks redaction failed
- blocks current production phase
- makes no HTTP call
- stores no secret
- grants no authority

## Evaluation-First Strategy

NODAL OS uses its own synthetic benchmark fixtures before relying on vendor claims. Evaluation compares routing behavior by case type, expected provider, fallback, estimated cost, latency/confidence band, privacy risk, redaction status, human escalation and no-authority.

Fixtures cover:

- simple UI crop
- simple document text
- low-quality/blurred/skewed crops
- table/invoice/receipt layouts
- handwriting and mixed handwriting synthetic
- ambiguous screenshot UI
- sensitive redaction failed
- full-screen blocked
- cloud candidate disabled
- budget exceeded

## Tradeoffs

Local OCR stubs are preferred for simple redacted crops. SaaS providers are future candidates for complex layout/document extraction but remain disabled until dedicated opt-in, secret management, budget controls and privacy gates exist.

## Crops Over Full-Screen

Full-screen OCR remains blocked by default. OCR should prefer small redacted crops tied to a grounding snapshot.

## No-Authority

OCR/Vision remains no-authority. It cannot approve, click, submit, pay, sign, delete, enter credentials, bypass login/CAPTCHA/2FA, unblock sensitive surfaces or enable production.

## Future Activation Gates

Future activation requires dedicated evidence for:

- real local OCR worker isolation
- secret vault integration
- budget enforcement
- provider-specific privacy review
- redaction proof
- human approval flows
- audit ledger persistence
- no-authority UI clarity
