# Pre-Live CDP Readiness Checklist v1

Date: 2026-06-26

Project: NODAL OS

Global result: `LIVE_CDP_IMPLEMENTATION_READY: NO`

This checklist is a readiness model only. It does not authorize live implementation.

## Status Values

- `READY`: completed for fixture-safe or governance scope.
- `PARTIAL`: partially defined but not live-ready.
- `BLOCKED`: blocked until a new decision or design exists.
- `NOT_STARTED`: not designed or not implemented.
- `NO_GO_FOR_LIVE`: live implementation must not start.

## Architecture Required Before Live

| Item | Status | Notes |
| --- | --- | --- |
| Fixture-safe CBPR line closed | READY | CBPR-001/010 closed and re-audited |
| Live read-only collector ADR | PARTIAL | Design gate exists, implementation not allowed |
| Separate live action gateway design | NOT_STARTED | Must be separate from fixture executor |
| Live-disabled default architecture | PARTIAL | Required, not implemented |
| Runtime proof with pinned CloakBrowser | READY | From prior CloakBrowser migration |
| No-extension default proof | READY | Extension legacy/no-default |
| No-system-browser proof | READY | Existing gates, must be rerun |

## Human Permissions Required

| Item | Status | Notes |
| --- | --- | --- |
| New human decision for live design | BLOCKED | Required before live design implementation |
| New prompt for live collector | BLOCKED | Required |
| New prompt for live actions | NO_GO_FOR_LIVE | Not allowed now |
| Explicit action approval model | NOT_STARTED | Required before any live action |
| Scoped target approval | NOT_STARTED | Required before live |

## Approval UX

| Item | Status | Notes |
| --- | --- | --- |
| User-visible disabled state | NOT_STARTED | Future product work |
| Action preview before approval | NOT_STARTED | Required |
| Per-action confirmation | NOT_STARTED | Required |
| Human handoff copy | PARTIAL | Defined in docs, not productized |
| Risk copy for credentials/challenges | NOT_STARTED | Required |

## Sandboxing

| Item | Status | Notes |
| --- | --- | --- |
| Fixture-only executor | READY | Implemented |
| Live action sandbox | NOT_STARTED | Required before live |
| Kill switch | NOT_STARTED | Required |
| No product file mutation proof | PARTIAL | Fixture proof exists; live proof needed |
| Isolated live collector boundary | NOT_STARTED | Required |

## Navigation Policy

| Item | Status | Notes |
| --- | --- | --- |
| External navigation disabled now | READY | No live navigation |
| Future domain allowlist | NOT_STARTED | Required |
| Internal controlled fixture target policy | PARTIAL | Fixture scope exists |
| Redirect handling policy | NOT_STARTED | Required |
| Cross-origin frame policy | NOT_STARTED | Required |

## Future Domain Allowlist

| Item | Status | Notes |
| --- | --- | --- |
| Allowlist storage | NOT_STARTED | Required |
| Allowlist approval | NOT_STARTED | Required |
| Deny-by-default behavior | NOT_STARTED | Required |
| Evidence of target URL redaction | PARTIAL | Redaction exists for metadata |

## Future Action Allowlist

| Item | Status | Notes |
| --- | --- | --- |
| Fixture action kinds | READY | Scroll, focus, click, type, select, wait in memory |
| Live click | NO_GO_FOR_LIVE | Blocked |
| Live type | NO_GO_FOR_LIVE | Blocked |
| Live select | NO_GO_FOR_LIVE | Blocked |
| Live scroll/wait | NO_GO_FOR_LIVE | Blocked |
| Credential entry | NO_GO_FOR_LIVE | Human handoff only |

## Rollback And Verification

| Item | Status | Notes |
| --- | --- | --- |
| Precondition contracts | READY | Snapshot-based |
| Postcondition contracts | READY | Snapshot-based |
| Live rollback design | NOT_STARTED | Required |
| Action failure evidence | READY | Fixture-safe evidence pack |
| Live failure recovery | NOT_STARTED | Required |

## Evidence Capture

| Item | Status | Notes |
| --- | --- | --- |
| Fixture evidence pack | READY | CBPR-010 |
| Plan-only evidence | READY | CBPR-010 |
| Fixture success/failure evidence | READY | CBPR-010 |
| Live evidence policy | NOT_STARTED | Required |
| Screenshot policy | PARTIAL | Metadata-only currently |

## Redaction

| Item | Status | Notes |
| --- | --- | --- |
| Sensitive field redaction | READY | Implemented |
| Secret-like pattern redaction | READY | Implemented |
| Live summary redaction | NOT_STARTED | Required |
| Storage value redaction | NO_GO_FOR_LIVE | Values must not be collected |
| Evidence serialization proof | READY | Tested fixture-safe |

## Session And Credential Policy

| Item | Status | Notes |
| --- | --- | --- |
| Credential automation blocked | READY | Human handoff |
| Session/cookie value capture blocked | READY | Metadata-only policy |
| Future session scope policy | NOT_STARTED | Required |
| Login without explicit user action | NO_GO_FOR_LIVE | Prohibited |
| Credential entry without user action | NO_GO_FOR_LIVE | Prohibited |

## Challenge Handoff

| Item | Status | Notes |
| --- | --- | --- |
| CAPTCHA handoff | READY | Fixture-safe logic |
| 2FA handoff | READY | Fixture-safe logic |
| Anti-bot handoff | READY | Fixture-safe logic |
| Paywall bypass | NO_GO_FOR_LIVE | Prohibited |
| Challenge solving | NO_GO_FOR_LIVE | Prohibited |

## Replay Prevention

| Item | Status | Notes |
| --- | --- | --- |
| Action correlation IDs | PARTIAL | Fixture executor has correlation metadata |
| Live approval replay prevention | NOT_STARTED | Required |
| Stale approval invalidation | NOT_STARTED | Required |
| One-shot action tokens | NOT_STARTED | Required |

## Dry Run

| Item | Status | Notes |
| --- | --- | --- |
| Fixture dry-run | READY | Existing fixture flow |
| Live dry-run no-action collector | NOT_STARTED | Required |
| Live action simulation | NOT_STARTED | Required before any action |

## Kill Switch

| Item | Status | Notes |
| --- | --- | --- |
| Fixture live disabled default | READY | Fixture executor rejects live modes |
| Product kill switch | NOT_STARTED | Required |
| Runtime kill switch | NOT_STARTED | Required |
| Audit kill switch proof | NOT_STARTED | Required |

## Audit Log

| Item | Status | Notes |
| --- | --- | --- |
| Fixture evidence records | READY | Evidence pack |
| Live audit log schema | NOT_STARTED | Required |
| Human approval audit event | NOT_STARTED | Required |
| Redacted log display | NOT_STARTED | Required |

## Test Strategy

| Item | Status | Notes |
| --- | --- | --- |
| Fixture CBPR tests | READY | 83/83 in re-audit |
| Live disabled tests | PARTIAL | Fixture live rejection exists |
| Live collector read-only tests | NOT_STARTED | Required |
| No-bypass live tests | NOT_STARTED | Required |
| External audit | BLOCKED | Must happen before live |

## Staged Rollout

| Item | Status | Notes |
| --- | --- | --- |
| Fixture-safe stage | READY | Closed |
| Design-only live stage | PARTIAL | ADR exists |
| Read-only live collector stage | NOT_STARTED | Future only |
| Live action stage | NO_GO_FOR_LIVE | Not authorized |
| Productive automation stage | NO_GO_FOR_LIVE | Not authorized |

## Rollback Plan

| Item | Status | Notes |
| --- | --- | --- |
| Fixture rollback | READY | Code can be reverted by commit |
| Live feature flag rollback | NOT_STARTED | Required |
| Runtime shutdown rollback | NOT_STARTED | Required |
| Product UI disable rollback | NOT_STARTED | Required |

## Mandatory External Audit

| Item | Status | Notes |
| --- | --- | --- |
| CBPR fixture-safe re-audit | READY | GPT/Kimi GO |
| Pre-audit pack review | READY | This pack prepares it |
| Live design external audit | BLOCKED | Required before live |
| Live implementation external audit | NO_GO_FOR_LIVE | Not authorized |

## Final Readiness Decision

- `LIVE_CDP_IMPLEMENTATION_READY: NO`
- `SAFE_INJECTION_LIVE_READY: NO`
- `PRODUCTIVE_BROWSER_AUTOMATION_READY: NO`
- `PRE_LIVE_DESIGN_REVIEW_READY: YES`
