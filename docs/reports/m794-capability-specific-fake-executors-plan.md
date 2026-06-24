# M794 Capability-Specific Fake Executors Plan

M794 defines three allowed fake executors under `tests/OneBrain.Safety.Tests` only: `FakeLocalModelExecutor`, `FakeFilesystemReadMetadataExecutor`, and `FakeLedgerAppendExecutor`.

Each executor is `TEST_ONLY_IN_MEMORY_FAKE`, runs with `SIMULATED_FAKE_ONLY_IN_MEMORY`, and is prohibited from provider/cloud calls, real filesystem writes, browser automation, capability unlock, public release, Store submission, product file changes, and Bridge/CSP changes.

Blocked executor classes remain boundary-only: provider/cloud, filesystem write, browser automation, capability unlock, public release, and Chrome Web Store.
