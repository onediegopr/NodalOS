# M797 Simulated Capability Executor Routing

M797 adds a test-only in-memory router for simulated capability execution.

Allowed routes are fixed: `local_provider_model` to `FakeLocalModelExecutor`, `filesystem_read_metadata` to `FakeFilesystemReadMetadataExecutor`, and `ledger_append` to `FakeLedgerAppendExecutor`.

The router emits evidence envelope, ledger events, redaction proof, and no-execution proof. It does not wire or invoke real executors, provider/cloud clients, filesystem writers, browser automation, capability unlock, release, Store, product files, or Bridge/CSP.
