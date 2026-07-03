# Durable Audit Trail Enablement Gate Design-Only

## Status

Accepted as design-only. Enablement is not authorized.

## Baseline

- Baseline commit: `2c6b6f59cdc45217f3b426c7d2f539e45d23c922`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Existing capability: `DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL`
- Current state: implemented-not-enabled local/test-safe append/write candidate.

The previous no-write/no-persistence preview canon is historical. The current canon permits only local/test-safe append/write through the isolated durable audit trail candidate.

## Current Canon

Allowed only inside the local/test-safe scope:

- JSONL ledger.
- `Directory.CreateDirectory`.
- `File.AppendAllText`.
- `AppendWriteCount=1`.
- `PersistedEventCount=1`.
- Tests that write to controlled temporary locations.

Still prohibited:

- Product/runtime enablement.
- Service registration.
- Command handlers.
- UI product actions.
- Product ledger paths.
- DB or migration backing.
- Provider, cloud, network, browser/CDP, WCU/OCR, or recipes live integration.
- Release, commercial, production, WORM, compliance-grade, or product-ready claims.

This ADR does not enable runtime or product behavior.

## Gate Matrix V0

| Gate ID | Gate name | Required condition | Evidence required | Current status | Failure mode | Required owner/reviewer | Allowed next action |
|---|---|---|---|---|---|---|---|
| G0 | Baseline integrity | Repo clean, origin sync `0 0`, baseline recorded | Git guard output | PASS | Pause | Maintainer | Plan only |
| G1 | Local/test-safe scope proof | Writes limited to local/test-safe storage | Source and tests | PASS | Block enablement | Maintainer + safety reviewer | Keep isolated |
| G2 | No product runtime registration | No runtime wiring or DI registration | Static scan | PASS | Block enablement | Safety reviewer | Scan again |
| G3 | No command handlers | No command bus or handler exposure | Static scan | PASS | Block enablement | Safety reviewer | Scan again |
| G4 | No UI product actions | No buttons, menu actions, or product action entrypoints | Static scan | PASS | Block enablement | Product safety reviewer | Keep hidden |
| G5 | Path boundary / local storage root | Storage root remains inside allowed local/test-safe boundary | Tests and source review | PASS | Reject append | Safety reviewer | Harden only |
| G6 | Append-only invariant | No overwrite/delete and sequence/hash continuity | Tests and future property tests | PARTIAL | Fail closed | Safety reviewer | Add property tests before enablement |
| G7 | Malformed ledger handling | Invalid JSON fails closed | Tests | PASS | Reject append | Safety reviewer | Maintain |
| G8 | Tamper handling | Hash/sequence/shape tamper fails closed | Tests | PASS | Reject append | Safety reviewer | Maintain |
| G9 | Secret-like content rejection | Raw payload and secret-like fields rejected | Tests | PASS | Reject append | Privacy reviewer | Expand corpus |
| G10 | Concurrent append/local lock behavior | Local in-process lock protects append sequence | Tests and source review | PASS/PARTIAL | Reject or block enablement | Safety reviewer | Add stress/property tests |
| G11 | Redaction-before-persistence requirement | Future product scope must prove redaction before persistence | Future design/test evidence | MISSING | Block enablement | Privacy reviewer | Design gate |
| G12 | Evidence schema compatibility | Event envelope schema is versioned and compatible | Future schema report | PARTIAL | Block enablement | Architecture reviewer | Version schema |
| G13 | Audit trail replay/read model plan | Replay/read model cannot mutate or enable product actions | Future read-only design | PARTIAL | Block enablement | Architecture reviewer | Design read model |
| G14 | Failure/rollback/non-rollback policy | Fail-closed and explicit non-rollback semantics documented | QA/risk register | PARTIAL | Pause | Safety reviewer | Document policy |
| G15 | Test isolation/temp-only proof | Tests leave worktree clean and write only temp | Test output + git status | PASS | Block merge | Maintainer | Repeat per block |
| G16 | Static scan no enablement | Scans show no enablement, runtime, handlers, services, UI actions | Scan report | PASS | Block merge | Safety reviewer | Repeat per block |
| G17 | External audit requirement | External audit before enablement | Audit report | REQUIRED | Block enablement | External auditor | Prepare audit |
| G18 | Manual GO requirement | Explicit human GO before enablement | Decision log | REQUIRED | Block enablement | Human operator | Wait |
| G19 | Runtime feature flag plan | Future flag must be fail-closed and disabled by default | Future ADR/tests | MISSING | Block enablement | Architecture reviewer | Design only |
| G20 | Release/commercial NO-GO lock | Release/commercial remains NO-GO | Decision log and QA | PASS | Block release | Maintainer | Maintain NO-GO |

## Staged Enablement Plan

| Stage | State | Allowed capabilities | Prohibited capabilities | Required tests | Required docs | Exit criteria | Rollback/pause condition | Audit requirement |
|---|---|---|---|---|---|---|---|---|
| 0 | Current state | local/test-safe append/write candidate | runtime, product actions, service registration | current focused tests | current QA/handoff | baseline clean | any scope drift | none beyond recorded audits |
| 1 | Test-only internal enablement | explicit test fixture and temp ledger only | product paths, UI, command bus | temp-only and clean-worktree tests | QA addendum | no repo side effects | dirty worktree or broad path | internal review |
| 2 | Dev-only local sandbox | fail-closed flag disabled by default | product data, DB, network | flag and path-boundary tests | ADR update | flag cannot enable by accident | flag ambiguity | external audit before moving on |
| 3 | Internal runtime dry-run | runtime may observe but durable write disabled by default | write-by-default, service registration without approval | no-side-effect runtime tests | dry-run report | dry-run cannot persist | any write leak | external audit |
| 4 | Controlled local durable write sandbox | non-product local durable write under explicit scope | product users/data, DB/cloud, UI product action | append-only, redaction, schema, rollback tests | risk register | sandbox-only proof | path or data leak | external audit |
| 5 | Product candidate | only after explicit GO and external audit | release/commercial, broad runtime, provider/cloud | full negative and positive suite | enablement pack | separate enablement decision | any P0/P1/P2 | final external audit |

## Required Future Tests

- Append-only invariant property tests.
- Malformed ledger tests.
- Tamper, replay, rollback, truncation, and checkpoint mismatch tests.
- Concurrent append and stress tests.
- Path boundary tests.
- Secret-like rejection corpus tests.
- Redaction-before-persistence tests.
- Evidence schema compatibility tests.
- Read model no-mutation tests.
- Runtime feature flag fail-closed tests.
- No service registration scan tests.
- No command handler scan tests.
- No product action scan tests.
- Docs overclaim scan.
- Clean worktree and origin sync gate.
- External audit artifact requirement.

## Required Future Docs

- QA report.
- Risk register.
- Decision log update.
- Handoff.
- Roadmap percentage update.
- External audit pack.
- Rollback/pause protocol.
- User-facing non-claim wording.
- Runtime feature flag design.
- Redaction-before-persistence design.

## Anti-Capabilities

This gate does not authorize:

- product audit trail enablement;
- service registration;
- command handler activation;
- UI action buttons;
- product ledger paths;
- DB-backed audit trail;
- cloud/network persistence;
- provider or LLM calls;
- browser/CDP, WCU/OCR, or recipes live writes;
- production, WORM, compliance-grade, release-ready, or commercial-ready claims.

## Risks

- Redaction-before-persistence is not implemented.
- Append-only property tests are partial.
- Evidence schema compatibility is partial.
- Replay/read model plan is partial.
- Failure/rollback policy is partial.
- Runtime feature flag plan is missing.
- Local/test-safe append/write must not be confused with product enablement.
- Head checkpoint/truncation evidence remains design-only.

## Decision

The enablement gate is documented for future planning only. Product/runtime enablement remains blocked until a later explicit user GO, external audit, complete negative tests, and a separate implementation block.
