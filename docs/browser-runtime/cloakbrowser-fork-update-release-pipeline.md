# CloakBrowser Fork Update Release Pipeline

This is the minimal repeatable process for updating the external CloakBrowser runtime consumed by NODAL OS.

NODAL OS does not vendor the runtime repository or runtime artifacts. It consumes a pinned external artifact through:

* `browser-runtime.lock.json`
* `.local/browser-runtime.local.json` or `NODAL_CLOAKBROWSER_RUNTIME_PATH`
* redacted local verification evidence

## Repositories

External runtime fork:

* local path: sibling `nodal-cloakbrowser-runtime`
* origin: `https://github.com/onediegopr/nodal-cloakbrowser-runtime`
* upstream: `https://github.com/CloakHQ/cloakbrowser`
* runtime branch: `nodal/runtime`

## Update Flow

1. Inspect upstream and origin metadata in the external runtime fork.
2. Sync the runtime fork only from the approved CloakBrowser upstream.
3. Build or download the runtime artifact using the CloakBrowser-supported flow in the runtime fork.
4. Keep generated runtime files outside the NODAL OS repo.
5. Calculate the artifact SHA256.
6. Update `browser-runtime.lock.json` with safe metadata:
   * `runtime_version`
   * `runtime_commit`
   * `upstream_commit`
   * `binary_sha256`
   * `runtime_source=fork`
   * `runtime_path_policy=env-or-local-config`
7. Point `.local/browser-runtime.local.json` to the local artifact path.
8. Run the readiness script:
   * `powershell -File scripts/verify-cloakbrowser-cdp-fork-update-release-pipeline.ps1`
9. Run CDP/no-extension validation:
   * `powershell -File scripts/verify-cloakbrowser-cdp-runtime.ps1`
   * `powershell -File scripts/verify-cloakbrowser-cdp-no-extension-default.ps1`
   * `powershell -File scripts/verify-cloakbrowser-cdp-minimal-product-surface.ps1`
   * `powershell -File scripts/verify-cloakbrowser-cdp-extension-deprecation-hardening.ps1`

## Acceptance Rules

The runtime update is acceptable only when:

* the artifact lives in the external runtime fork managed cache;
* the artifact hash matches `browser-runtime.lock.json`;
* the runtime fork branch and HEAD match the lockfile;
* origin and upstream match the approved remotes;
* `.local/browser-runtime.local.json` is present locally and remains unversioned;
* no fallback to the extension is used;
* no fallback to a system browser is used;
* no Bridge/WebSocket path is required;
* evidence remains redacted and metadata-only.

## Never Commit

Do not commit:

* `.local/`
* `.cloakbrowser/`
* binary artifacts
* screenshots
* temp profiles
* generated local verification artifacts

## Current Pinned Runtime

Current lockfile metadata:

* runtime version: `146.0.7680.177.5`
* runtime commit: `8432254124667a3d2742b1727132d8a045e115da`
* upstream commit: `0bb3737a29d9133f6207793eb0eeeefe36c9d910`
* SHA256: `03f53661a5c47e7b0a661bee2bce8a0d302b7a60834c328df417561fa0636d80`
