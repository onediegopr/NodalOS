# ADR: Browser Document Workflow End-to-End Sandbox M28A

## Status

Accepted for M28A.

## Context

M25B remains blocked because no external test-owned low-risk target is configured. M28A therefore proves a complete document workflow only in a local sandbox. It is not external production workflow validation.

## Decision

M28A chains the existing controlled runtime capabilities:

- Consent.
- Phase gate.
- Controlled profile.
- Minimal sandbox vault reference.
- Authenticated sandbox login proof.
- Safe document download to quarantine with hash.
- Local no-op transformation.
- Safe document upload from controlled root with approval.
- Final status verification with semantic proof.
- Audit/evidence summary and cleanup.

Completion requires every workflow step to be verified and to produce evidence refs. HTTP 200, file existence, cookie presence, user completion, or upload request sent are not sufficient.

## Fixture

The workflow fixture is localhost-only and synthetic. It exposes login, dashboard, document/download, upload-page/upload, status, and logout semantics without external network, real cookies, real credentials, or personal data.

## Redaction and Audit

Audit and result objects must not contain cookies, secrets, bodies, sensitive headers, full local paths, or document content. Audit uses existing HMAC/head-seal assumptions and safe metadata.

## Out of Scope

- M28 external real.
- AFIP, banks, ERP, fiscal/financial sites.
- External non-test-owned targets.
- Sensitive documents.
- Request/response body capture.
- Replay execution.

## Consequences

M28A validates local end-to-end composition. It does not unblock external real workflow while M25B remains blocked.

