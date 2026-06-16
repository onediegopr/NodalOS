# ADR - Safe Action Expansion Local Fixtures - M139-M141

## Status

Accepted.

## Context

HITO-162 was rewritten as a NODAL OS roadmap sequence. M133-M135 implemented Identity/Fingerprint v2 as fixture-first evidence. M136-M138 implemented robust perception stabilization as liveness, overlay, empty-surface, and semantic fallback signals.

M139-M141 defines a safe action expansion boundary without enabling production or sensitive actions.

## Decision

NODAL OS adds a local fixture-first safe action taxonomy and evaluator.

Allowed local fixture categories can include:

- ObserveOnly
- LocalPanelOpen
- LocalDiagnosticsOpen
- LocalEvidenceReview when redacted
- LocalIssueTriage
- LocalCopyToClipboardIfRedacted
- LocalDraftOnly

ExternalReadOnlyTargetOwned remains limited to prior target-owned proof scope and does not imply external general-ready.

## Always Blocked

The following remain always blocked:

- credentials
- submit
- payment
- delete
- sign
- sensitive surfaces
- external general
- production
- productive recorder/replay

## Core Boundary

Identity/Fingerprint v2 and robust perception are signals only. High identity or perception confidence does not approve an action. UI/Admin/Companion also cannot approve actions.

Core approval is required for local read-only/draft actions beyond observe-only. Dangerous categories are AlwaysBlocked even if Core signals are otherwise healthy.

## Non-Scope

This hito does not open:

- SaaS public
- public API real
- billing/email real
- real credentials
- sensitive sites
- submit/pay/sign/delete
- external CDP general-ready
- embedded runtime
- Chromium fork

## Next Steps

M142-M144 should move toward process memory/workflow learning using only local, redacted, non-sensitive run records and without production recorder/replay.

