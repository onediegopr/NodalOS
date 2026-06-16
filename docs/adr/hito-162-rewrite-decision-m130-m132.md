# ADR - HITO-162 Rewrite Decision - M130-M132

## Status

Accepted.

## Context

HITO-162 was documented as paused/not forgotten/UnknownNeedsAudit during roadmap reconciliation. The project has since advanced as NODAL OS with private preview local stable, M51 closed for external HTTP read-only target-owned proof, and M65 closed for limited target-owned Chrome/CDP/DOM read-only proof.

The available legacy evidence does not include a standalone HITO-162 implementation or closure document.

## Evidence

- HITO-161 is the last reliable standalone legacy milestone found.
- The HITO-161 document names `H162 - Identity/Fingerprint v2` as the next step.
- The master engine roadmap points to robust perception work after HITO-161.
- Current NODAL OS gates block production, SaaS public, public API, billing/email real, real credentials, sensitive sites, submit/pay/sign/delete, productive recorder/replay, and external CDP general-ready.

## Decision

HITO-162 will not be resumed blindly.

HITO-162 is rewritten as a NODAL OS replacement sequence:

- M133-M135: Identity/Fingerprint v2 local fixture-first
- M136-M138: robust perception stabilization
- M139-M141: safe action expansion design and local fixtures
- M142-M144: process memory and workflow learning, local-only

## Scope Lock

This decision does not open:

- production
- SaaS public
- public API real
- billing/email real
- real credentials
- sensitive sites
- submit/pay/sign/delete
- external CDP general-ready
- embedded runtime
- Chromium fork

## Consequences

The legacy intent is preserved, but implementation must follow current NODAL OS safety, evidence, and Core authority rules. HITO-162 becomes rewritten roadmap intent, not a completed feature.

