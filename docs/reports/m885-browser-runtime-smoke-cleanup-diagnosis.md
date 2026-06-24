# M885 - BrowserRuntimeSmoke Cleanup Diagnosis

Project: NODAL OS.

Failing test: `BrowserRuntimeSmokeCleanupLeavesNoManagedProcessPortOrProfile`.

Diagnosis: the smoke runner cleanup gate reports PASS, but `%TEMP%\onebrain-cdp-*` profile directories can remain locked or delayed by environment/browser/OS cleanup timing. This is a test cleanup visibility issue, not a freeze foundation, runtime unlock, provider/cloud, product file, or Bridge/CSP issue.

No product runtime, provider/cloud, product files, or Bridge/CSP were modified.
