# M795 Collector Enforcement Tests

M795 adds collector enforcement tests for the three allowed fake executors.

Each executor must emit an `EvidenceEnvelope`, `LedgerEvent[]`, `RedactionProof`, and `NoExecutionProof` through the in-memory collector path. Each executor must keep side-effect sink invocations at `0` and all real executor/provider/filesystem/browser/capability/release/store/ZIP flags false.

The filesystem read executor simulates metadata only. The ledger append executor appends only to the in-memory evidence ledger.
