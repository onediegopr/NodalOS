# ADR: Implementation Planning Gate Design-Only

## Status

Accepted as design-only planning if validation passes.

## Context

NODAL OS is paused in `PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_PHYSICAL_EXPORT_NO_REDACTION_RUNTIME`.
The read-only reentry decision packet is available, but no real capability has implementation approval.

## Decision

Add a deterministic in-memory planning gate packet that evaluates future real capability candidates without opening any of them.
The recommended future candidate is `DURABLE_AUDIT_TRAIL_APPEND_ONLY_IMPLEMENTATION_PLANNING_DESIGN_ONLY`, but it remains `FUTURE_CANDIDATE_BLOCKED_BY_AUDIT`.

## Candidate Matrix

- Durable audit trail append-only minimal path: future candidate blocked by external audit.
- Approval execution minimal path: blocked.
- Mutation store minimal path: blocked.
- Redaction runtime minimal path: blocked.
- Physical export controlled minimal path: blocked.
- Retention/deletion runtime minimal path: blocked.
- Recipes execution safe runtime path: blocked.
- WCU/OCR safe runtime path: blocked.
- Browser/CDP safe runtime path: blocked.

## Mandatory Gates

- Explicit user GO for the exact real capability.
- Repo guard clean.
- Scope isolation.
- External audit before implementation.
- Negative tests before code.
- No secrets or PII exposure.
- No broad filesystem access.
- No service registration until separately approved.
- No command handler until separately approved.
- No product IO until separately approved.
- Rollback and no-side-effect plan.
- Evidence and audit trail plan.
- Failure mode must fail closed.
- Overclaim scan.
- Final external audit before enablement.
- Release/commercial remains `NO-GO`.

## Non-Goals

This ADR does not implement runtime/live, execution, mutation, physical export, redaction runtime, secret/PII scan, retention/deletion runtime, durable audit trail real, mutation store real, writer/policy productive integration, service registration, command handlers, product actions, filesystem product IO, DB/migration, provider/cloud/network, LLM/browser/CDP/WCU/OCR live, recipes execution real or release/commercial readiness.

## Consequences

- Implementation planning can be audited.
- No candidate receives implementation approval.
- All enabled counts remain `0`.
- All real capability readiness remains `0%`.
- Release/commercial remains `NO-GO`.

## Hardening Addendum: Browser/CDP, WCU/OCR and Recipes

The pre-runtime external audit found a P2 non-blocking gap: Browser/CDP live, WCU/OCR live and recipes real execution were blocked in the candidate matrix, but did not have dedicated negative requirement rows.

This addendum hardens those future candidates as design-only requirements:

- Browser/CDP remains `FUTURE_CANDIDATE_BLOCKED_BY_EXTERNAL_AUDIT_AND_USER_GO`; no system browser, user Chrome/Edge, real navigation, credential entry, login automation, challenge bypass, stealth/proxy evasion, cookie/session reuse, CDP live connection, WebSocket live connection, DOM mutation, click/type/submit, download/export, filesystem output, service registration or command handler is allowed.
- WCU/OCR remains `FUTURE_CANDIDATE_BLOCKED_BY_EXTERNAL_AUDIT_AND_USER_GO`; no real screen capture, OCR over real data, UIA live access, keyboard/mouse action, click/type/hotkey, window focus manipulation, clipboard access, filesystem output, screenshot/OCR retention, secret/PII scan disguised as OCR, external app automation, service registration or command handler is allowed.
- Recipes remain `FUTURE_CANDIDATE_BLOCKED_BY_EXTERNAL_AUDIT_AND_USER_GO`; no recipe action runner, scheduler, background execution, retry loop, detector/trigger, browser action, desktop action, filesystem output, network call, credential use, data extraction, export, mutation, service registration or command handler is allowed.

Each hardened candidate requires:

- explicit user GO for that exact scope;
- a scope-specific external audit before implementation;
- fail-closed behavior;
- no-side-effect proof;
- negative tests before implementation.

The hardening does not approve implementation. Runtime/live, execution, mutation, physical export, redaction runtime, retention/deletion runtime, Browser/CDP live, WCU/OCR live, recipes execution real and release/commercial readiness remain unavailable.
