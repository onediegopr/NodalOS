# M16 - Browser Runtime Phase Close: Download, Upload, Network Metadata, Diagnostic Replay

## Context

M1-M15 established a CDP-first Browser Runtime governed by Core/FSM/Safety/Evidence, with Chrome Companion as UI/relay only, formal credential boundaries, human handoff, vault consent contracts, and a persistent redacted audit ledger.

M16 closes the initial Browser Runtime phase by adding the remaining runtime capabilities needed before a broader production audit:

- secure Download Manager;
- secure Upload Manager;
- metadata-only Network Capture;
- safe Session Export / Diagnostic Replay;
- explicit phase close gate.

## Decision

M16 is implemented as contract-first and fixture-first. It does not enable real sensitive downloads, real uploads, real network body capture, real login, real profile usage, real vault storage, or browser automation replay.

Core remains the authority. Browser Runtime exposes controlled capabilities. Chrome Companion remains non-authoritative.

## Download Manager

Downloads are allowed only through policy:

- controlled directory;
- allowed extensions;
- max size;
- required hash;
- required evidence;
- no auto-open.

The manager records redacted metadata, normalized filename, size, MIME type, SHA-256, quarantine flag, evidence refs, and audit event.

M16 does not execute or open downloaded files.

## Upload Manager

Uploads require:

- sandbox path;
- allowed extension;
- max size;
- explicit approval when policy requires it;
- no external targets in M16.

The manager records file metadata, hash, redacted path, evidence refs, and audit event. It blocks path traversal, files outside sandbox, secret-like paths, unsupported extensions, and missing approval.

M16 does not upload to real sites.

## Network Capture

Network capture is metadata-only:

- method;
- redacted URL;
- status code;
- resource type;
- timing;
- request/correlation id;
- redacted response headers;
- API candidate flag.

It does not capture request bodies, response bodies, cookies, authorization headers, bearer tokens, API keys, JWT values, or payloads.

API candidate detection is diagnostic only. It does not authorize replacing browser actions with direct HTTP calls.

## Session Export / Diagnostic Replay

Export packages include:

- session/run/correlation summary;
- steps;
- decisions;
- evidence references;
- audit events;
- download/upload/network summaries;
- runtime kind;
- manifest hash.

Replay is diagnostic-only. It can replay timeline and decisions, but it cannot execute actions, clicks, inputs, downloads, uploads, login, or sensitive operations.

## Phase Close Gate

The phase close gate validates:

- audit ledger export;
- download manager;
- upload manager;
- network metadata-only capture;
- diagnostic replay export;
- companion non-authoritative;
- service worker not brain;
- no real profile;
- no real vault;
- no real login.

The gate returns `Passed`, `Failed`, `Blocked`, or `RequiresAudit`. M16 uses `Passed` only when all checks are safe.

## Out Of Scope

M16 does not implement:

- real credential storage;
- real secret retrieval;
- DPAPI activation;
- Windows Credential Manager activation;
- real profile launch;
- login automation;
- CAPTCHA solving;
- 2FA automation;
- AFIP, banks, MercadoLibre account flows, ERP flows, or new real sites;
- WebView2;
- CEF;
- production recipe recorder;
- production network capture bodies;
- production download/upload flows;
- automation replay that executes actions.

## Redaction

All M16 artifacts rely on the shared browser credential redactor and safe identifier validator. Covered patterns include bearer tokens, password/pass/secret, cookie/set-cookie, access/refresh/id tokens, API keys, JWT-like standalone values, and sensitive identity-like values.

## Phase Close

M16 prepares the Browser Runtime Layer for an external architecture audit across M1-M16. It is not a production-hardening completion for all future runtimes.

## Next

Run the full Claude architecture audit post M1-M16 before starting the next product phase.

## Percentages

- Browser Executor CDP read-only core-governed: 97%.
- Browser Runtime Layer productiva completa: 96%.
- ONE BRAIN global: 87%.
