# Next Prompt: WCU Read-Only Live Design Gate Audit

Continue NODAL OS on branch `chrome-lab-001-extension-local-ai-bridge`.

## Block

`WCU-031-036 -- READ-ONLY LIVE DESIGN GATE + AUDIT PACK`

## Goal

Design the audit gate for any future UIA/Win32 read-only live adapter. Do not implement action execution.

## Hard Rules

- No real Windows actions.
- No mouse or keyboard.
- No UIA/FlaUI Invoke, Click, SetValue.
- No default live UIA event subscription.
- No real PC read by default in tests.
- No SetForegroundWindow, SendInput, PostMessage, SendMessage.
- No clipboard.
- No raw screenshots.
- No OCR duplication.
- No browser live, CDP live, WebSocket live, or Safe Injection.
- No login/captcha/2FA/UAC/admin automation.
- No production-ready or live-ready claim.

## Required Focus

- Read-only live adapter threat model.
- Explicit opt-in gate design.
- Audit pack schema.
- Redaction and evidence invariants.
- Fixture-to-live boundary proof.
- Failure modes and rollback.
- Negative tests proving no action authority.

## Expected Output

- Architecture doc.
- QA report and JSON.
- Handoff doc.
- Next prompt that still does not proceed directly to controlled actions.
