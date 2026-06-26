# Next Prompt: WCU Read-Only Live Prototype Gated

Use this prompt only if `WCU-031-036` receives external audit GO.

## Block

`WCU-037-044 — READ-ONLY LIVE PROTOTYPE GATED`

## Non-Negotiable Scope

- Disabled by default.
- Explicit dev flag required.
- Human operator confirmation required.
- Allowlisted test apps only: Notepad, Calculator, Explorer in a disposable test folder, Settings read-only.
- No actions.
- No UIA Invoke/Click/SetValue.
- No mouse or keyboard.
- No window manipulation.
- No clipboard.
- No raw screenshots.
- No credential value capture.
- No UAC/admin automation.
- No product UI.
- No browser live/CDP/WebSocket/Safe Injection.
- No OCR engine duplication.

## Required First Steps

1. Re-run protected scope scan.
2. Re-run no-action scan.
3. Verify all `WCU-031-036` audit findings are GO.
4. Add a live provider only as disabled-by-default experimental prototype.
5. Keep tests fixture-safe unless a human explicitly runs a local allowlisted manual verification.

## Expected Outcome

A gated, disabled-by-default read-only prototype may collect redacted metadata from allowlisted test apps only. It still cannot authorize or execute actions.
