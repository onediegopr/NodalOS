# Selective AIRI inspiration for NODAL OS

NODAL OS does not fork or import Project AIRI. The implementation keeps the existing C#/.NET architecture and selectively reimplements a small set of behaviors after reviewing `moeru-ai/airi` at commit `ed664e8107115fe5ff05735a976b4e110a303a38` (MIT).

The useful deltas are intentionally narrow:

- a plan guides execution but cannot prove completion;
- compact current-run memory is bounded, deduplicated, resettable and regenerable;
- compatible model fallback is automatic inside an already authorized privacy/cost/capability scope;
- caller cancellation stops fallback immediately;
- capability discovery reports health but does not replace the existing safety policy;
- transient runtime events feed the existing evidence/timeline boundary and are not a second ledger;
- browser reliability uses bounded reconnect, correlation, timeout, one-response semantics, frame-aware coordinates and cancellable polling;
- telemetry is local-first, sanitized, best-effort and non-blocking.

## Integrated NODAL OS path

The selective runtime is now connected to existing product boundaries rather than becoming a parallel architecture:

- `AIModelRuntimeBridge` keeps `AIModelProfile` and `AIModelRouter` as the product-facing configuration/policy source, then delegates attempts and fallback to the resilient model runtime;
- environment and composite secret stores resolve opaque `SecretReference` values without rendering key prefixes or suffixes;
- `NodalOsMissionPlanProjector` converts the existing TaskGraph draft into a transient mission plan and excludes future executable placeholders;
- mission events are projected into `NodalOsCoreEventBus` and its existing timeline projection, not a new ledger or timeline;
- the fixture-safe vertical slice completes only after evidence and verification, while an authorized simulated `429` fallback continues without an extra approval prompt;
- Pilot exposes a loopback-only, local/dev, read-only Runtime Inspector in JSON and dark Mission Control HTML surfaces;
- CloakBrowser remains the canonical browser runtime and is reported as externally blocked until the pinned binary is provisioned.

## Executed validation

GitHub Actions Release validation passed:

- `OneBrain.Runtime.Tests`: `40/40`;
- focused Recipes/Pilot integration tests: `11/11`;
- Pilot Runtime Inspector smoke: JSON `200`, HTML `200`, `no-store`, read-only/local-dev boundaries, completed fixture mission, no scripts/forms/external resources;
- existing Tier 1 jobs: ChromeLab security, selective runtime foundation and secret scan all passed.

No AIRI source file was copied verbatim. The behavior was independently implemented in C# for NODAL OS. AIRI remains credited here because its public contracts and tests informed the design comparison.
