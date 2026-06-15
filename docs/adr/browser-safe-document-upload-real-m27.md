# ADR: Browser Safe Document Upload Real M27

## Status

Accepted for M27.

## Context

M16 modeled upload safety using fixture-first behavior. M27 introduces real upload execution only against controlled low-risk endpoints with explicit approval, policy, gate, evidence, and audit. It does not allow sensitive uploads or wildcard file handling.

## Decision

Safe upload uses `BrowserSafeUploadPolicy`, `BrowserSafeUploadRequest`, `BrowserSafeUploadApproval`, `BrowserSafeUploadResult`, and `BrowserSafeUploadArtifact`.

Uploads require:

- Allowlisted host and endpoint path.
- File inside controlled upload root.
- Explicit approval.
- Scoped consent.
- Passing phase gate.
- Allowed MIME and extension.
- Size limit.
- SHA256 hash.
- No wildcard upload.
- No directory upload.
- No hidden/system file.
- No executable/script/macro/archive/secret-bearing file type.

Allowed initial file types:

- `.txt`
- `.csv`
- `.json`
- Synthetic non-sensitive `.pdf`

Blocked file types include executables, scripts, archives, macro documents, `.env`, `.pfx`, `.key`, and `.pem`.

## Path and Content Minimization

Public DTOs expose only normalized filename and `[CONTROLLED_UPLOAD_ROOT]` path markers. File contents are never recorded in audit, evidence, logs, UI, protocol, or export.

## Approval

Upload requires explicit authoritative approval. Consent is not sufficient by itself; Core policy and gate must still approve the operation.

## Local Fixture

M27 validates real upload against a localhost fixture endpoint. The fixture receives synthetic file content but audit records only metadata, hash, and evidence refs.

## Out of Scope

- Upload to sensitive external sites.
- AFIP, banks, ERP, fiscal portals.
- Wildcard or directory upload.
- Sensitive documents.
- Request/response body capture.
- Companion authority.
- Replay execution.

## Consequences

M27 enables controlled upload mechanics while preserving fail-closed policy. Broader end-to-end external workflows require M25B live target validation and a separate M28 milestone.

