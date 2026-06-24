# M860 - Flake Regression Watch + Full Validation Gate

Project: NODAL OS.

Status: AUDIT_GATE_CLEAN.

BrowserRuntimeSmoke Gate 9 history remains visible. M845-M862 recorded two transient browser smoke events and closed the gate with rerun/full-suite evidence:

- BrowserRuntimeSmoke isolated first run failed Gate 1 launcher/CDP readiness. Isolated rerun passed 20 tests.
- Full safety first run failed same-family Gate 9 WebSocket aborted. Full safety rerun passed 5245 tests with 37 skipped.
- Full suite passed: Recipes 635 passed; Safety 5245 passed, 37 skipped.

Caveat status: CLOSED_BY_RERUN_AND_FULL_SUITE_PASS_WITH_TRANSIENT_GATE1_GATE9_RECORDED.
