# M18 - CDP Live Read-Only Proof

## Context

M16 closed the fixture-first Browser Runtime phase and M17 hardened audit, redaction, network metadata-only contracts, and audit ledger integrity. The remaining audit gap was that several runtime capabilities were still proven primarily through fixtures and contract-level tests.

M18 proves the CDP wiring against a real Chrome/Edge process while keeping the environment controlled and read-only.

## Decision

M18 uses a local HTTP fixture server bound to `127.0.0.1` and opt-in tests marked `BrowserCdpLive`.

No external sites, login, real profile, real vault, real cookies, credentials, request bodies, response bodies, CAPTCHA, 2FA, AFIP, banks, ERP systems, or executable replay are introduced.

## Local Fixture Server

The test fixture serves only synthetic local endpoints:

- `/` for read-only DOM proof;
- `/network` for controlled local requests;
- `/headers` for synthetic sensitive header redaction checks;
- `/frames` and `/frame-child` for frame attach/navigate/detach behavior;
- `/download-page` and `/download` for synthetic download event observation.

The fixture is local-only and does not require internet access.

## CDP Live Capabilities Proven

M18 proves:

- launching a disposable-profile browser controlled by CDP;
- navigating to a localhost fixture;
- observing title, URL, ready state, and DOM text;
- verifying read-only success only with semantic proof;
- capturing `Network.*` metadata without bodies;
- capturing sensitive header presence only, never values;
- consuming live `Page.frame*` events into the target/frame manager;
- blocking stale frame contexts for read/execution decisions;
- observing `Page.downloadWillBegin` for a synthetic local download;
- closing the browser process and deleting the disposable profile after tests.

## Network Capture

Network capture remains metadata-only. M18 does not add body capture support.

Sensitive headers such as `authorization`, `cookie`, `set-cookie`, `x-api-key`, `x-csrf-token`, `x-xsrf-token`, and `proxy-authorization` are persisted as presence-only metadata:

- header name;
- `Present=true`;
- `ValueCaptured=false`;
- `Value=[NOT_CAPTURED]`;
- `RedactionReason=SensitiveHeaderValueNotCaptured`.

Opaque synthetic values used in tests must not appear in network events, audit exports, logs, or evidence.

## Audit and Evidence

M18 reuses the M17 HMAC audit ledger. Live network observations can be appended to the persistent audit ledger and verified with the head seal. Evidence for read-only page verification still requires `Verified` status and semantic proof refs.

## Test Execution

Normal test runs skip live tests unless explicitly opted in:

```powershell
$env:ONEBRAIN_RUN_CDP_LIVE_TESTS="1"
dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --filter BrowserCdpLive
```

M18 is not considered closed unless the live test filter runs successfully against a real local Chrome/Edge process.

## Out of Scope

M18 does not implement:

- vault real;
- profile real;
- login real;
- cookies real;
- external sensitive sites;
- CAPTCHA or 2FA automation;
- upload to real sites;
- request or response body capture;
- executable replay;
- companion authority.

## Next Step

If M18 passes live validation, the next recommended hito is `M19 - Phase Gate Enforcement Real`, focused on enforcing the runtime gates around live capabilities before any authenticated or sensitive-site work.
