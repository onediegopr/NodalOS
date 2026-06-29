# NODAL OS Phase D Context Selection Lock Exclusion Guards Handoff

Decision target: `GO_PHASE_D_CONTEXT_SELECTION_LOCK_EXCLUSION_GUARDS_READY`

## Implementation

Added read-only, in-memory selection/lock/exclusion guard contracts and evaluator in `WorkspaceContextReadOnlyFoundation.cs`.

Added fixture-safe tests in:

- `tests/OneBrain.Recipes.Tests/WorkspaceContextReadOnlyFoundationTests.cs`
- `tests/OneBrain.Safety.Tests/WorkspaceContextReadOnlyFoundationSafetyTests.cs`

The guard evaluates 22 deterministic fixtures and enforces excluded-over-selected, locked-over-auto-use, and human-review-over-decision-use precedence.

## What Remains Disabled

- real workspace reads;
- workspace indexing;
- filesystem writes;
- DB/dependency;
- durable memory;
- durable persistence;
- provider/cloud/network;
- semantic/vector backend;
- LLM live/provider;
- migration runner/execution;
- runtime/live;
- browser/CDP, WCU, OCR live;
- product action commands;
- product service registration.

## Validation Summary

Completed so far:

- build: PASS;
- Workspace/Context/Memory Recipes tests: PASS;
- Workspace/Context/Memory Safety tests: PASS;
- Evidence Safety filter: PASS;
- EvidenceIntelligence Safety tests: PASS;
- EvidenceIntelligence Recipes tests: PASS;
- Recipe Safety filter: PASS;
- full Recipes: PASS;
- full Safety: PASS;
- Stealth audit-safe: PASS;
- CloakBrowser/CDP gates: PASS.

Final changed/new scans and git checks are tracked in the final hito report.

## Risks

Remaining non-blocking risk is future UI or memory work accidentally treating selected fixture context as trusted durable state. Current guards reduce that risk by blocking or requiring review for excluded, locked, stale, unknown, contradictory, unsafe, provider-derived, semantic-derived, legacy, duplicate, or dependency-conflicted states.

## Next Recommended Block

Recommended: `PHASE_D_MEMORY_CANDIDATE_CONTRADICTION_RISK_READ_ONLY`

Reason: authority/freshness and selection/lock/exclusion are now fail-closed, so memory candidates can be hardened next around contradiction/risk without increasing runtime or persistence capability.
