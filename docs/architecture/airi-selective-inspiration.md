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

No AIRI source file was copied verbatim. The behavior was independently implemented in C# for NODAL OS. AIRI remains credited here because its public contracts and tests informed the design comparison.
