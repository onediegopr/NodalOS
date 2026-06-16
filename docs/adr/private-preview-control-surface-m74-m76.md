# M74-M76 Private Preview Control Surface

## Status

Accepted for local private preview only.

## Context

M-WT established `Codigo-m12-audit` as the canonical NEXA worktree and isolated the dirty legacy `Codigo` tree. M51/M65 external proof remains deferred because no test-owned external target is configured.

## Decision

M74 adds a local canonical workspace guard. It reports expected path, expected remote branch, HEAD, remote head, dirty state, detached HEAD acceptance, and known legacy path detection. It never cleans, resets, deletes, or modifies files.

M75 adds a local readiness dashboard model that separates local private preview readiness from external/live readiness. Percentages are explicitly estimated. Local preview can remain allowed when skipped tests only block external/live. Public SaaS, real billing, real email, real credentials, sensitive real pilot, and submit/pay/sign/delete remain denied.

M76 adds operator-facing blocker explanations. The UI, Companion, and Admin surfaces may display these explanations, but authority remains with Core. Each explanation includes cause, expected user action, safe options, blocked options, evidence refs, and a recommended next step.

## Non-goals

- No public SaaS.
- No public API listener.
- No real billing or email delivery.
- No real credentials.
- No external/live target validation.
- No M51/M65 closure.
- No legacy worktree cleanup.

## Consequences

Future preview operations can block early when run from a wrong path, dirty tree, wrong branch, or wrong HEAD. Operators get specific remediation instead of generic blocked messages.
