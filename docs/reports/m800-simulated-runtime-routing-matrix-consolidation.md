# M800 Simulated Runtime Routing Matrix Consolidation

M800 consolidates the simulated runtime routing matrix into a canonical test-only source.

Allowed capabilities route only to fake in-memory executors: `local_provider_model`, `filesystem_read_metadata`, and `ledger_append`. Denylisted capabilities are evaluated first and always return `DENY` with `selectedExecutor=null`. Unknown capabilities also return `DENY`.

Every allowed and denied result must create an evidence envelope, ledger events, redaction proof, no-execution proof, and keep side-effect sink invocations at `0`.
