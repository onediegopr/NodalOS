# M886 - BrowserRuntimeSmoke Cleanup Guard Decision

Project: NODAL OS.

Decision: `BROWSER_RUNTIME_SMOKE_CLEANUP_EXTERNAL_QUARANTINED`.

Strategy: keep the test visible, keep the internal cleanup gate assertion, and classify residual temp CDP profile cleanup as `Assert.Inconclusive` when the smoke cleanup gate itself passes but environment cleanup remains delayed.

This does not hide failures, does not disable security tests, and does not mark full suite clean without evidence.
