# M878 - Deferred Findings Register

Project: NODAL OS.

Deferred non-blocking findings:

- F3: drift guard real path scan.
- F5: reduce JSON string self-checks.
- F6: isolate/quarantine BrowserRuntimeSmoke flakey.
- F7: READY to SIMULATED_READY / TEST_ONLY_READY wording.
- F9: future extraction to src if real runtime approaches.

All deferred findings are non-blocking for freeze, have freeze impact `none`, and do not unlock runtime, provider/cloud, filesystem/browser/capability, release/store, product files or Bridge/CSP.
