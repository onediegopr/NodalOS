# NODAL OS Internal Preview Operator UX Guide - M148-M150

## Purpose

This guide explains how to read the Product/Admin dashboard and operator summaries during internal local private preview.

## ReadyWithRestrictions Is Not Production

ReadyWithRestrictions means local internal preview can continue with blockers active. It does not mean production, SaaS public, public API real, billing/email real, real credentials, sensitive sites, submit/pay/sign/delete, or productive recorder/replay.

## Reading M51 and M65

- M51: closed with HTTP read-only target-owned ledger scope.
- M65: closed with limited target-owned Chrome/CDP/DOM read-only ledger scope.
- M65 does not mean external general CDP.

## Reading HITO-162 Replacement

HITO-162 replacement is stable local fixture-first:

- Identity/Fingerprint v2 is a signal, not action authority.
- Robust perception is a signal, not action authority.
- Safe action expansion is local fixture-only and Core-boundary controlled.
- Process memory/workflow learning is local-only and redacted, not productive recorder/replay.

## Allowed Examples

- Review Product/Admin local summary.
- Review readiness dashboard.
- Review redacted evidence/log refs.
- Review local issue triage.
- Review operator blocker explanations.

## Blocked Examples

- production/SaaS public
- public API real
- billing/email real
- real credentials
- sensitive sites
- submit/pay/sign/delete
- productive recorder/replay
- external general CDP
- new external targets without dedicated evidence

## Stop Conditions

Stop and file an issue if:

- credential/login prompt appears
- submit/pay/sign/delete is requested
- sensitive site or real customer data appears
- external general CDP is requested
- unredacted evidence or token/cookie appears
- scope inflation warning appears

## Issue Reporting Path

Create a local private preview issue report with category, severity, decision, redacted summary, evidence refs, and next action. Do not paste secrets, cookies, tokens, raw DOM, raw UIA trees, screenshots with secrets, or unredacted logs.

