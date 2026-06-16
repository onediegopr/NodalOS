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

## Reading the Vertical Timeline / Stepper

NODAL OS uses a vertical timeline in the side panel for task structuring, recipes, execution state, evidence, blockers and operator summaries.

- Circular nodes show the ordered step sequence.
- Indented subtasks show decomposition, not authority.
- Status badges show planned/running/done/blocked/needs-human/evidence states.
- Evidence refs are redacted references only.
- Blocker cards describe why an option is stopped and what the operator should do.
- "Core authority required" means UI/Admin/Companion cannot approve the action.
- ReadyWithRestrictions in the timeline does not mean production.
- Timeline output never authorizes submit/pay/sign/delete, credentials, sensitive sites, public SaaS, external general CDP or productive recorder/replay.

## Reading Plan Preview

Plan preview is emitted by Core before execution and rendered in the existing timeline.

- `PlanDrafted` means the plan is still a draft.
- `PlanPreviewReady` means it can be reviewed by the operator.
- `PlanAwaitingApproval` means human/Core approval is still required.
- `PlanRejected` and `ExecutionBlockedByPolicy` do not execute.
- Sensitive actions such as credentials, login, submit, payment, sign, delete, captcha or 2FA appear as blockers.
- The sidepanel cannot approve or execute the plan by itself.

## Reading Recovery / Stagnation

If NODAL OS detects repeated URL, repeated DOM/screenshot hash, repeated action, selector failure, click with no visual change, page not loaded, modal issues or captcha/login/2FA, the timeline shows a recovery card.

- Read the cause first.
- Review redacted evidence refs.
- Choose only safe options: retry, replan, ask human, partial evidence, finish, copy LOG, view evidence or report issue.
- Do not use recovery to bypass credentials, captcha, 2FA, submit/pay/sign/delete or sensitive sites.
- If human input is required, complete only the local/safe action described and do not paste secrets, cookies, tokens or credentials into reports.

## Reading Grounding Snapshot

- Grounding snapshot is a DOM + screenshot metadata debug card inside the existing timeline.
- The screenshot thumbnail is shown only when the snapshot is redacted/safe. It is a reference for operator review, not an action authority.
- Page health explains whether the page is `Ready`, `Loading`, `Blocked`, `Error` or `NotLoaded`.
- Focused element and visible interactables are redacted metadata. Do not expose credentials, tokens, cookies, raw DOM/body, or sensitive screenshots.
- If `redaction failed` or `BlockedSensitive` appears, stop persistence, do not use the screenshot, and ask Core/human review.
- If page health is `Loading` or `NotLoaded`, treat it as insufficient grounding and use recovery/retry only if Core permits.
- If page health is `Blocked`, keep blockers visible and do not try a sensitive workaround.
- Grounding can help explain stagnation, but screenshot is not a source of truth by itself. DOM/CDP/Core policy remain preferred.

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
