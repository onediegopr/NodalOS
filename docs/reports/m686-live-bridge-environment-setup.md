# M686 Live Bridge Environment Setup

Milestone: M686

Decision: `LIVE_BRIDGE_ENVIRONMENT_SETUP_READY_CONDITIONAL_CHROME_QA`

## Summary

The live bridge environment was checked from the required worktree on branch `chrome-lab-001-extension-local-ai-bridge`.

The local bridge configuration exists at `config/chrome-lab.local.json`. The `ExtensionToken` is present and was treated as a secret. Only its presence and observed length were recorded; the value was not printed, logged, or stored.

## Bridge Liveness

Initial TCP check for `127.0.0.1:8787` failed because no listener was present.

The bridge was then started with the documented runbook command:

```powershell
dotnet run --project src/OneBrain.ChromeLab.Bridge/OneBrain.ChromeLab.Bridge.csproj
```

The read-only liveness script returned PASS for:

- TCP `127.0.0.1:8787`
- listener process `OneBrain.ChromeLab.Bridge`
- `/health`
- `/runtime`
- `/config/public`
- `/debug`
- `/ws/extension` upgrade

The temporary process opened for the check was stopped after evidence capture.

## Public Variant Readiness

`browser-extension/onebrain-chrome-lab/manifest.public.json` remains the public candidate manifest. It is valid JSON, has localhost/127.0.0.1 host permissions, does not include `http://*/*` or `https://*/*`, and does not define automatic external content scripts.

## Boundary

No product files were modified. Bridge and CSP were not modified.

Chrome public variant load, Runtime tab evidence, and Service Worker DevTools evidence remain pending because live Chrome inspection was not available in this environment.
