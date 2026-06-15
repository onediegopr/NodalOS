# M17 - Audit and Redaction Hardening

## Context

The post M1-M16 audit concluded that the Browser Runtime Layer should not advance to real vault, real profile, login, AFIP, ERP, or banking flows yet. The blocking issues were narrow but security-relevant:

- sensitive headers could still rely on pattern redaction;
- network metadata-only mode was implicit;
- the audit ledger used plain SHA-256 chain integrity;
- JWT-like redaction was too broad and could destroy useful evidence;
- test-only vault helpers still live in runtime assemblies.

## Decision

M17 hardens audit and redaction without enabling new product capabilities.

No real vault, profile, login, CAPTCHA, 2FA, AFIP, ERP, banking, cookies, request bodies, response bodies, or executable replay are introduced.

## Sensitive Headers

Sensitive header values are never persisted, even if a policy asks for sensitive header capture.

Sensitive headers include:

- `authorization`;
- `proxy-authorization`;
- `cookie`;
- `set-cookie`;
- `x-api-key`;
- `x-csrf-token`;
- `x-xsrf-token`.

For these headers, M17 stores presence only:

- header name;
- `Present=true`;
- `ValueCaptured=false`;
- `Value=[NOT_CAPTURED]`;
- `RedactionReason=SensitiveHeaderValueNotCaptured`.

This avoids relying on pattern redaction for opaque tokens.

## Metadata-Only Network Capture

Network capture is now explicitly modeled with `BrowserNetworkCaptureMode.MetadataOnly`.

Request and response body capture support is hard-coded as unsupported by contract:

- `BodiesCaptureSupported=false`;
- `RequestBodyCaptureSupported=false`;
- `ResponseBodyCaptureSupported=false`.

Raw events requesting body capture are invalid, and the runtime normalizes captured events back to metadata-only.

## Audit Ledger Integrity

M17 introduces `IBrowserAuditLedgerIntegrityProvider` and a HMAC-backed implementation:

- `BrowserAuditLedgerHmacIntegrityProvider`;
- HMACSHA256 event integrity;
- canonical event serialization;
- head seal containing ledger id, event count, last sequence, last event hash, timestamps, and head HMAC.

The HMAC key is synthetic/dev fixture material only. M17 does not implement real key storage, DPAPI, Windows Credential Manager, or vault-backed signing keys.

The head seal detects:

- tampered events;
- SHA-only recomputation without the HMAC key;
- tail truncation;
- wrong event count;
- wrong last sequence.

## Canonical Serialization

Integrity material uses stable property order, invariant timestamps, normalized strings, and sorted metadata. It avoids relying on default JSON serialization as the signing material.

## Redaction Precision

JWT-like values are no longer identified by `a.b.c` alone. A value must have plausible segment lengths and a decodable JWT header containing typical fields such as `alg` or `typ`.

This prevents redacting useful evidence such as:

- hostnames;
- common filenames;
- innocent dotted identifiers.

DNI/CUIT redaction is now contextual:

- formatted CUIT remains sensitive;
- DNI-like numbers require nearby identity context;
- plain 7-8 digit order ids or counts are not redacted.

## Test Vault Location

The in-memory test vault remains in the runtime assembly for M17. Moving it to a test-only assembly is deferred because it is not required to close the high-priority audit/redaction issues and would be a broader refactor.

The existing restrictions still apply:

- test-only naming;
- synthetic references only;
- no secret value exposure;
- default deny in productive providers.

## Out Of Scope

M17 does not implement:

- productive vault;
- real HMAC key vaulting;
- real profile launch;
- login;
- 2FA/CAPTCHA;
- AFIP, ERP, banks, or account-based sites;
- request or response body capture;
- CDP-live changes;
- executable replay.

## Next

After M17, the next step is M18 CDP-Live Read-Only Proof, not a vault/profile/login production rollout.

## Percentages

- CDP read-only core-governed: 93%.
- Browser Runtime productiva real: 68-72%.
- ONE BRAIN global: 77-80%.
