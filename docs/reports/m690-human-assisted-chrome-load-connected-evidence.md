# M690 Human-Assisted Chrome Load + Connected Evidence

Milestone: M690

Decision: `HUMAN_CHROME_LOAD_INPUT_REQUIRED`

## Human Runbook

### A. Prepare Bridge

1. From the repo root, start the bridge:

   ```powershell
   dotnet run --project src/OneBrain.ChromeLab.Bridge/OneBrain.ChromeLab.Bridge.csproj
   ```

2. In another terminal, verify liveness:

   ```powershell
   pwsh -File tools/scripts/bridge-liveness-check.ps1
   ```

3. Use `config/chrome-lab.local.json` only locally to copy the ExtensionToken. Do not paste the token into reports, screenshots, logs, or chat.

### B. Load Public Variant

1. Open Chrome manually.
2. Go to `chrome://extensions`.
3. Enable Developer Mode.
4. Click `Load unpacked`.
5. Select:

   ```text
   C:\DESARROLLO\NodalOS\Codigo-m12-audit\artifacts\manual-qa\public-variant-staging
   ```

6. Confirm visible extension name: `NODAL OS Public Candidate`.

### C. Sidepanel Checks

1. Open the sidepanel.
2. Paste the ExtensionToken locally.
3. Save/connect.
4. Confirm:

- token present in UI by boolean or equivalent state;
- WebSocket connected;
- diagnostic state connected or equivalent;
- no reconnect loop;
- no invalid token;
- no close 1008.

## Evidence Template

```text
Public variant loaded: true/false
Visible extension name:
Manifest selection verified human: true/false/unknown
Loaded staging path:
Token present UI: true/false/unknown
WebSocket connected: true/false/unknown
Runtime tab: PASS/FAIL/UNKNOWN
Service Worker DevTools: PASS/FAIL/UNKNOWN
CSP violations: yes/no/unknown
invalid token: yes/no/unknown
close 1008: yes/no/unknown
reconnect storm: yes/no/unknown
critical console errors: yes/no/unknown
Permission warnings:
Bridge liveness: PASS/FAIL/UNKNOWN
Evidence redacted: true/false
Secrets included: no/yes
```

## Current Evidence State

No human Chrome evidence was provided in this milestone. No PASS was invented.
