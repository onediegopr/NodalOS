# M793 Fake Executors Boundary Plan Next Decision

M793 defines the future fake executor boundary.

Future fake executors may be introduced only as test-only in-memory adapters. They must not call provider/cloud, write filesystem, control browser, unlock capabilities, touch product files, touch Bridge/CSP, publish release, submit to Store, or create signed public ZIPs.

Recommended next milestone: `M794-M796 — Capability-Specific Fake Executors In-Memory Plan + Collector Enforcement Tests`.

Final decision: `IN_MEMORY_EVIDENCE_LEDGER_RUNTIME_RESULT_OBJECTS_READY`.
