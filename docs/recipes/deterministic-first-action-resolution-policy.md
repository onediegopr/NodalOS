# Deterministic-First Action Resolution Policy

Phase: 2/9 - Limits / Validation / Risk / Deterministic Policy.

Action resolution is contract-only in this phase. It describes allowed target resolution strategies for a future runtime but does not resolve or execute targets.

## Strategy Order

Strategies are ordered from strongest to weakest:

1. `KnownTarget`
2. `StableSelector`
3. `DomOrAccessibility`
4. `VisibleText`
5. `SemanticTarget`
6. `VisualAnchor`
7. `RelativeCoordinate`
8. `AIFallback`
9. `HumanHandoff`
10. `Abort`

Deterministic strategies must be declared before AI fallback.

## Blocking Rules

Readiness blocks when:

- `AIFallback` appears before deterministic strategies,
- `AIFallback` is used without explicit policy allowance,
- `AIFallback` lacks an evidence expectation,
- a sensitive action starts with `AIFallback`,
- a sensitive or external action has no deterministic strategy,
- a desktop draft is treated as live desktop action.

`RelativeCoordinate` is allowed only as a warning-level last-resort metadata strategy before AI/human/abort. It does not grant action authority.

## Authority Boundary

Action resolution confidence is not authorization. This phase grants no live runtime and no action authority.
