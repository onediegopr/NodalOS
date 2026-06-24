# M890 - Path Drift Report

Project: NODAL OS.

F3 status: `F3_REAL_PATH_DRIFT_SCAN_READY`.

Scanner method: test-only real path prefix scanner. It evaluates changed file paths against allowed prefixes and prohibited prefixes, then injects negative cases for browser-extension, product src, Bridge/CSP, release/store, provider/cloud and filesystem/browser unlock paths.

Allowed paths: `tests/OneBrain.Safety.Tests`, `docs/reports`, `artifacts/agent-operations`.

Product files modified: false.

Bridge/CSP modified: false.
