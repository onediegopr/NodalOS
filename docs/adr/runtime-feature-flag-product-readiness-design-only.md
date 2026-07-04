# Runtime Feature Flag Product Readiness Design-Only

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_RUNTIME_FEATURE_FLAG_PRODUCT_READINESS_DESIGN_ONLY_READY`

## Scope

Docs-only design for a future product-ready runtime feature flag policy for Durable Audit Trail Stage 2.

This ADR does not change `DurableAuditTrailStage2RuntimeFeatureFlag`, add a product flag service, register DI, add command handlers, add UI actions, enable runtime/product behavior, create a product ledger path, call DB/provider/cloud/network, enable Browser/CDP/WCU/OCR/Recipes live behavior, or claim release/commercial readiness.

## Current State

The current implementation is intentionally test-only:

- exact allowed value: `enabled:test-only`;
- `ExplicitTestFixture` must be true;
- values containing product/runtime/live/release/commercial are rejected;
- result flags for runtime product, service registration, command handlers, UI product actions and release/commercial remain false.

This closes test-only fail-closed behavior but is not a product feature flag system.

## Product-Readiness Requirements

A future product runtime feature flag policy must define:

- owner and approving authority;
- environment scope and default-off behavior;
- explicit deny-by-default unknown value handling;
- separation between test fixture, local dev, internal preview and product runtime;
- kill switch and rollback behavior;
- redaction-before-persistence precondition;
- product ledger path precondition;
- service registration precondition;
- command/UI authority precondition;
- checkpoint/trust boundary precondition;
- observability without raw sensitive values;
- audit event for flag decision without allowing the flag to create product authority by itself.

## Forbidden Shortcuts

- A string value alone must never enable product runtime.
- Environment variables alone must never bypass manual GO.
- Product flag evaluation must not register services, handlers or UI actions.
- A product flag must not choose a ledger path.
- A product flag must not call provider/cloud/network.
- A product flag must not imply release/commercial readiness.

## Future Negative Tests

Before implementation, tests must prove rejection for:

- missing owner or authority;
- unknown environment;
- missing manual GO evidence;
- product flag without product ledger policy;
- product flag without redaction product wiring;
- product flag without service/handler/UI authority;
- mixed casing and whitespace values;
- product/runtime/live/release/commercial aliases;
- stale flag evaluation;
- rollback/kill-switch disabled;
- provider/cloud/network side effects;
- Browser/CDP/WCU/OCR/Recipes live metadata.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No runtime/product enablement added. |
| P1 | 0 | No productive flag service, DI registration, command handler, UI action, product ledger path or provider/network call added. |
| P2 | 0 | No blocker in this design-only block. |
| P3 | 4 | Product flag ownership, environment taxonomy, kill-switch behavior and dependency gates remain future work. |
| P4 | 1 | Current exact test-only flag remains valid and intentionally narrow. |

## Percentages

| Track | Conservative status |
| --- | --- |
| Runtime feature flag test-only boundary | 92-95% |
| Runtime feature flag product-readiness design | 55-65% |
| Runtime feature flag product implementation | 0% / NO-GO |
| Runtime/product enablement | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |

## Next Safe Option

`NODAL_OS_REDACTION_PRODUCT_WIRING_DESIGN_ONLY`
