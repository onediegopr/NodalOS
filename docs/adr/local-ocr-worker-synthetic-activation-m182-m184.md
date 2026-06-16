# Local OCR Worker Synthetic Activation M182-M184

Date: 2026-06-16

Product: NODAL OS

Decision: add a local OCR worker skeleton and synthetic-only activation path, without real OCR, without Python, without PaddleOCR/Tesseract runtime, without external process invocation and without raw image persistence.

## Worker Skeleton Design

The worker skeleton is an in-process model-only service that exercises the future IPC contract shape:

- request envelope
- response envelope
- worker contract version
- provider id
- engine hint
- grounding snapshot id
- redaction result id
- valid redaction decision
- synthetic/redacted crop ref
- evidence refs
- bounded synthetic processing time

It does not call OCR runtimes, Python, PaddleOCR, Tesseract, binaries, HTTP or SaaS providers.

## Synthetic-Only Activation

Synthetic activation can reach only `ReadyForSyntheticOnly`. It requires:

- worker skeleton available
- local stub provider
- evaluation harness passed
- redactor passed
- no raw persistence
- no external process
- no real OCR
- no SaaS
- synthetic opt-in
- rollback/pause modeled
- audit record generated
- no-authority confirmed

`RealOcrEnabled` remains false. `RealSaasEnabled` remains false.

## Isolation Policy

- No external network.
- No process invocation.
- No Python coupling.
- No raw image persistence.
- No full-screen OCR.
- No sensitive crop execution.
- No external data transfer.
- No action authority.
- Redaction result is mandatory.

## Health Checks

The health checker verifies:

- contract version compatible
- worker disabled by default
- synthetic-only allowed
- resource limits configured
- timeout configured
- no external network
- no raw persistence
- no real OCR
- no process invocation
- no authority
- rollback/pause available

## Failure Modes

- WorkerUnavailable
- VersionMismatch
- Timeout
- ResourceLimitExceeded
- RedactionMissing
- RedactionFailed
- SensitiveBlocked
- FullScreenBlocked
- ExternalProcessAttempted
- RawPersistenceAttempted
- AuthorityViolation
- UnexpectedOutput
- LowConfidence
- UnknownError

Failure decisions include block, ask human, retry synthetic, stop with evidence, degraded, and pause provider.

## No Real OCR

M182-M184 does not activate OCR. The worker skeleton output is synthetic/redacted fixture text only.

## Next Phase Requirements For Real Worker

Before any real worker:

- keep redaction-result precondition mandatory
- build versioned out-of-process JSON IPC
- add loopback/process/container/CLI isolation proof
- prove no network
- prove no raw persistence
- prove timeouts/resource limits
- audit PaddleOCR/Tesseract installation separately
- keep SaaS disabled-by-default
- keep no-authority
