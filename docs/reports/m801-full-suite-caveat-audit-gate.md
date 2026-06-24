# M801 Full Suite Caveat Audit Gate

M801 carries the inherited M797-M799 full-suite caveat explicitly.

Inherited caveat: `BrowserRuntimeSmokeRunnerExecutesAllGatesOnFixture` failed in full suite with WebSocket aborted in Gate 9 idempotency/replay. `BrowserRuntimeSmokeTests` passed isolated 17/17. The caveat blocks declaring full suite clean unless full suite passes in this block.

The gate allows `AUDIT_GATE_CLEAN`, `AUDIT_GATE_CONDITIONAL_GO_FLAKY_EXTERNAL`, or `AUDIT_GATE_NO_GO`. Routing/security failures are NO-GO. A repeated same browser smoke flake remains conditional and must not be hidden.
