# ADR: Browser Safe Document Download Real M26

## Status

Accepted for M26.

Canonical status hardening note (2026-07-03): this ADR is historical and does not authorize current physical export, browser download, file output, runtime/live execution, product action, external download flow or release/commercial readiness. Current NODAL OS status remains `PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_PHYSICAL_EXPORT_NO_REDACTION_RUNTIME`; physical export implementation readiness remains 0%.

## Context

M16 modeled download policy and M18 proved CDP live read-only wiring. M26 enables safe document download under strict policy, quarantine, hash verification, and audit. It does not enable arbitrary downloads or sensitive document handling.

## Decision

Safe downloads are represented by `BrowserSafeDownloadPolicy`, `BrowserSafeDownloadRequest`, `BrowserSafeDownloadResult`, `BrowserSafeDownloadArtifact`, and `BrowserSafeDownloadValidation`.

Allowed file types are initially limited to:

- `.pdf`
- `.txt`
- `.csv`
- `.json`

Blocked types include executables, scripts, archives, macro documents, and libraries:

- `.exe`, `.msi`, `.bat`, `.cmd`, `.ps1`, `.vbs`, `.js`, `.scr`, `.com`, `.dll`
- `.zip`, `.rar`, `.7z`
- `.docm`, `.xlsm`

## Required Controls

Every safe download must validate:

- Allowlisted host.
- Allowed extension.
- Allowed MIME type when available.
- Maximum size.
- Normalized filename.
- Controlled root.
- Quarantine path.
- SHA256 hash.
- No path traversal.
- No auto-open.
- No execution.
- Evidence refs.
- Audit event without secrets.

The quarantine layout is:

```text
ControlledDownloadRoot / quarantine / runId / file
```

## Verification

A download is verified only when the runtime observes or materializes the file in a controlled path and validates extension, MIME, size, hash, quarantine, no auto-open, and evidence. Anything incomplete remains blocked or failed, never `Done`.

## Redaction

The runtime records source host and redacted/minimized path only. It does not record cookies, authorization, sensitive headers, request bodies, response bodies, or secret values.

## Out of Scope

- Sensitive real documents.
- Auto-open.
- Executing downloads.
- Archive handling.
- Browser prompt bypass.
- External uncontrolled download targets.
- Upload real sensitive documents.

## Consequences

M26 enables controlled real download validation with fixture/local CDP proof while keeping unsafe download classes blocked. Future support for archives or sensitive documents requires a separate milestone and stronger inspection policy.
