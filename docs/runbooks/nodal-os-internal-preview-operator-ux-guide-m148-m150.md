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

## Reading OCR/Vision Routing

- `NoOcrNeeded` means DOM/CDP/UIA or screenshot hash/diff already gives enough safe context. Do not force OCR.
- `LocalOnly` or a local provider decision means only model-only/local OCR stubs are in scope; no real OCR is executed in this phase.
- `CloudDisabled` means a SaaS OCR/VLM provider might be a future candidate, but it is disabled-by-default and no API call/key is used.
- `AskHuman` means confidence, sensitivity, quality or provider policy requires human review.
- `RedactionFailed` means OCR is blocked. Do not process or persist screenshots/crops.
- OCR routing decisions may appear as timeline evidence/status cards in future UI, but OCR does not authorize actions.
- OCR does not authorize actions, clicks, submit/pay/sign/delete, credential entry, login, CAPTCHA/2FA bypass, sensitive sites or production flows.
- Prefer redacted crops over full-screen screenshots whenever OCR is considered in a future phase.

## Reading OCR/Vision Provider Settings

- `Disabled` means the provider cannot run.
- `Testing` means model-only local stub state; no real OCR runtime is installed or invoked.
- `ShadowOnly` means future comparison state only; no execution.
- `Fallback` means fallback decision metadata, commonly human review.
- `Paused` means do not route to that provider, even model-only.
- `RequiresApiKey` only shows API key state. No real API key is stored.
- `Missing`, `PlaceholderConfigured`, `SecretVaultRequired` and `Disabled` are configuration states, not secrets.
- `ExternalDataTransfer=true` means future SaaS transfer risk. Current phase keeps those providers disabled-by-default.
- Priority/fallback changes are model-only and do not make a provider executable.
- If a provider shows `allowedForFullScreen=false`, full-screen OCR remains blocked; use redacted crops only.

## Reading OCR/Vision Evaluation Reports

- Evaluation reports use synthetic fixtures only.
- `selectedProvider` is a routing expectation, not a real OCR execution.
- `cloud candidate disabled` means SaaS might fit the future case but is blocked now.
- `budget exceeded`, `redaction failed`, `full-screen blocked` and `sensitive blocked` require stop/human review.
- Reports must say no real OCR and no SaaS OCR were executed.
- OCR/Vision evaluation is no-authority and cannot approve actions.

## Reading OCR/Vision Activation States

- `ModelOnly` means contracts, stubs, routing and evaluation only. No real OCR runs.
- `ShadowEvaluation` means future comparison-only evaluation. It is not execution authority.
- `LocalWorkerAvailable` means a future worker may be installed, but Core policy still decides whether it can be used.
- `LocalWorkerEnabledForSynthetic` means synthetic-only future activation after opt-in/audit. It does not permit customer data, sensitive screenshots or production.
- `LocalWorkerEnabledForRedactedCrops` means future redacted-crop shadow mode only after audit. Full-screen OCR remains blocked by default.
- `SaasProviderConfigured` and `SaasProviderShadowOnly` are future states requiring opt-in, secret vault, privacy, budget and audit.
- `BlockedByPolicy`, `BlockedByPrivacy`, `BlockedByBudget` and `BlockedByMissingAudit` mean stop and do not run OCR.
- `ReadyForSyntheticOnly` means a future local worker could run synthetic fixtures after requirements pass; it is not production readiness.
- `ReadyForRedactedCropShadow` means a future worker could shadow redacted crops; it is not action authority.
- SaaS OCR remains disabled-by-default and requires opt-in, secret vault, budget, privacy and activation audit.
- OCR/Vision remains no-authority: it cannot approve actions, click, submit, sign, pay, delete, enter credentials or bypass captcha/2FA.

## Reading Local OCR Synthetic Worker Status

- `ReadyForSyntheticOnly` means the worker skeleton can run synthetic fixtures only. It does not enable OCR real.
- `WorkerUnavailable` means the skeleton or future worker is missing/disabled; pause provider and stop with evidence.
- `RedactionMissing` means no verified redaction result exists; do not route to OCR.
- `RedactionFailed` means the crop is unsafe or uncertain; ask human or stop with evidence.
- `SensitiveBlocked` means policy blocked the crop; do not attempt OCR or workaround.
- `TimedOut` means stop with evidence; do not keep retrying blindly.
- `VersionMismatch` means the worker contract version is incompatible; block before running.
- `RawPersistenceAttempted` or `ExternalProcessAttempted` means stop immediately and file an issue.
- Synthetic worker results are fixture outputs only and may require human review on low confidence.
- OCR real remains disabled until a separate audited activation gate changes that in a future phase.

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
