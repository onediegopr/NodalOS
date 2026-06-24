# M897 - BrowserRuntimeSmoke Cleanup Root Cause

Project: NODAL OS.

The observed cleanup caveat is tied to `%TEMP%/onebrain-cdp-*` profile directories. The managed smoke cleanup gate reports cleanup complete, but Windows temp IO or Chrome/CDP profile locking can delay physical directory deletion beyond the immediate test assertion window.

Classification: external temp/CDP profile cleanup race or IO lock with test-infra assertion sensitivity.

Product files modified: false.

Bridge/CSP modified: false.

Runtime productive execution: DISABLED.
