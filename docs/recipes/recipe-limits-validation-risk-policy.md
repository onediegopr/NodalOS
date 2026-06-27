# Recipe Limits / Validation / Risk Policy

Phase: 2/9 - Limits / Validation / Risk / Deterministic Policy.

Phase 1 closed at commit `2079a04efe66e6187f7fe018c772ec3f6b51f9d8` with decision `GO_RECIPE_RUNTIME_FOUNDATION_WORKITEMS_READY`.

## Scope

This phase adds contract-only safety policy for Recipe Runtime definitions:

- run limits,
- complete criteria,
- terminate criteria,
- validation requirements,
- risk profiles,
- sensitive action gates,
- deterministic-first action resolution,
- readiness/preflight evaluation.

The evaluator inspects recipe definitions and fixture metadata only. It does not execute recipe blocks.

## Run Limits

`RecipeRunLimits` bounds recipe definitions before any future runtime is considered:

- maximum steps,
- maximum runtime seconds,
- maximum retries,
- maximum loop iterations,
- maximum nested loops,
- maximum workitems per run,
- maximum downloaded files,
- maximum captured artifacts,
- maximum external system calls as contract metadata,
- allowed domains, apps, file scopes, and action categories,
- blocked action categories,
- approval required for sensitive actions,
- validation required after side effects,
- evidence required after downloads/captures.

Live runtime remains disabled. `LiveRuntimeAllowed` is a blocked future-gated field and must not enable execution.

## Required Blocking Rules

Readiness blocks when:

- a loop block has no loop limits,
- an action block has no max-step limit,
- a loop has no loop termination criterion,
- live-like mode is requested,
- output-producing recipes lack complete criteria,
- external or sensitive recipes lack terminate criteria,
- side-effect blocks lack post-validation,
- sensitive actions lack approval or human intervention gates.

No real file, UI, browser, desktop, scheduler, network, or secret vault is evaluated.

## Safety Boundary

- OpenRPA/OpenCore is inspiration only.
- No OpenRPA dependency was added.
- No code was copied.
- No XAML was imported.
- No browser extension or native messaging was added.
- No live browser automation was added.
- No live desktop/computer-use automation was added.
- No scheduler, recorder, replay, file watcher, OS hook, or network/API call was added.
- Secrets are references only.
- Live runtime remains blocked.
