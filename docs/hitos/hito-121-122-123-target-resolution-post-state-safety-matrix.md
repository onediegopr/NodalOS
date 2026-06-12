# HITO-121+122+123 - Target Resolution, Post-State Verification, Safety Matrix

## Scope

This hito hardens the existing executor harness without expanding product automation:

- HITO-121: real target resolution hardening for the benign local harness target.
- HITO-122: semantic post-action verification instead of relying only on string signals.
- HITO-123: executor safety matrix before any harness click can be attempted.

## What Changed

- Added `ExecutorHarnessTargetResolver`.
- Added `ExecutorHarnessTargetResolution`.
- Added `ExecutorHarnessPostActionState`.
- Added `ExecutorHarnessSafetyMatrix`.
- Added `ExecutorHarnessSafetyMatrixEvaluation`.
- `ExecutorHarnessService` now evaluates target resolution and safety matrix before the executor runs.
- `PilotUiaHarnessClickExecutor` resolves the command before calling `UiaActionExecutor`.
- Post-action success now requires:
  - allowlisted target resolution,
  - window found,
  - target visible,
  - target name matches the expected benign harness target,
  - exactly one observed click,
  - executor-reported target found.

## Safety Contract

The only real click path remains the supervised benign harness:

- harness id must be `onebrain-pilot-benign-click-harness-v1`,
- app profile must be `onebrain-pilot-local`,
- window title must contain `ONE BRAIN Pilot`,
- target must be `name:Objetivo benigno del harness`,
- action kind must be `benign_harness_click`,
- target must be controlled and benign,
- safe executor must exist,
- human approval decision must be approved,
- `ExecutionAllowed=true` must come from the scoped approval decision.

If any condition fails, ONE BRAIN fails closed before the executor can click.

## What This Does Not Add

- No free playback.
- No external website clicks.
- No MercadoLibre click.
- No login.
- No cookies accepted.
- No cart.
- No purchase.
- No payment.
- No OpenAI real call.
- No API keys.
- No CDP.
- No WGC.
- No SemanticScene.

## Testing Strategy

Tests use fake executors only. They verify:

- target resolver rejects external/non-local targets,
- safety matrix allows only approved benign local targets,
- safety matrix fails closed without scoped approval,
- missing approval blocks before executor,
- `ExecutionAllowed=false` blocks,
- one benign fake click can succeed with semantic post-state,
- post-action verification fails without independent state,
- post-action verification fails when target name does not match,
- evidence remains under `artifacts/executor-harness/`,
- Pilot exposes harness route without auto-running the click.

## Validation

Required validation remains:

- build,
- tests,
- `NEGATIVE_EXIT_CODE=1`,
- demo script,
- demo Markdown dry-run/run,
- demo HTML dry-run/run,
- ML readonly/diagnostic,
- Pilot HTTP smoke including `/executor-harness`,
- artifacts ignored and untracked,
- no real click unless explicitly supervised by the user.
