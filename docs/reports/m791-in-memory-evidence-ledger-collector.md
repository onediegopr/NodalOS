# M791 In-Memory Evidence Ledger Collector

M791 closes the audit finding that evidence/ledger was only flag-based by adding test-only in-memory objects.

`InMemoryEvidenceLedger`, `EvidenceEnvelope`, `LedgerEvent`, `RedactionProof`, and `NoExecutionProof` live under `tests/OneBrain.Safety.Tests` only. They are not product runtime, not Bridge/CSP code, and not wired to providers, filesystem, browser automation, capability unlock, release, or Store.

Status: READY.
