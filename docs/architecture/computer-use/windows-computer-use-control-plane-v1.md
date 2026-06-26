# Windows Computer Use Control Plane V1

Date: 2026-06-26

Project: NODAL OS

Line: `Windows Computer Use Control Plane`

Decision target: `GO_WCU_FIXTURE_SAFE_FOUNDATION_READY`

## Objective

Create a fixture-safe/design-first control plane for future Windows Computer Use. This line models observation, classification, locator strategy, blockage detection, safe planning, verification expectations, evidence, redaction, and handoff without controlling real Windows apps.

## Scope

This block is fixture-safe only:

- No real mouse movement.
- No real keyboard input.
- No live UIA action.
- No FlaUI action execution.
- No clipboard capture.
- No persisted real screenshots.
- No shell/subprocess automation.
- No browser live/CDP/WebSocket/Safe Injection.
- No product UI action enablement.

## CBPR Analogy

WCU follows the same separation pattern as CBPR:

```text
Windows app/window fixture
  -> ComputerUseSnapshot
  -> Capability Classifier
  -> Blockage/Sensitive Surface Detectors
  -> Locator Engine
  -> Safe Action Planner (dry-run only)
  -> Evidence Pack + Redaction
  -> Human Handoff
```

The analogy is architectural only. WCU does not reuse CBPR browser live gates to enable Windows live automation.

## Layered Architecture

1. Snapshot contracts: metadata-only window and element representation.
2. Fixture collector: in-memory snapshot builder.
3. Capability classifier: UIA-rich, UIA-poor, visual-only, sensitive, UAC/admin, modal.
4. Blockage detector: credential, UAC/admin, destructive, visual-only, modal, non-allowlisted.
5. Sensitive surface detector: password, credential, token, OTP, card, SSN, clipboard markers.
6. Locator engine: candidate-only selector scoring.
7. Safe action planner: dry-run action plans with human handoff.
8. Evidence pack: metadata-only records with redaction.

## Future Providers, Not Implemented

- UIA/FlaUI provider: future read-only provider only; not implemented here.
- Win32 context collector: future read-only window context provider only.
- Visual/OCR/OmniParser-style fallback: future metadata provider only; no visual click action.

## Evidence And Redaction

All evidence must be metadata-only and redacted. Raw credentials, raw clipboard, raw screenshots, unredacted OCR, raw filesystem paths, and raw app content are not allowed.

## Human Handoff

Human handoff is mandatory for:

- Credential fields.
- UAC/admin prompts.
- Destructive actions.
- Low-confidence selectors.
- Visual-only targets.
- Non-allowlisted apps.
- Remote desktop/Citrix-like surfaces.

## No-Live Gates

The WCU line is governed by:

- `NO_REAL_MOUSE_GATE`
- `NO_REAL_KEYBOARD_GATE`
- `NO_LIVE_UIA_ACTION_GATE`
- `NO_CLIPBOARD_CAPTURE_GATE`
- `NO_SCREENSHOT_LEAKAGE_GATE`
- `NO_CREDENTIAL_FIELD_ACTION_GATE`
- `NO_UAC_ADMIN_AUTOMATION_GATE`
- `NO_DESTRUCTIVE_OS_ACTION_GATE`
- `NO_LOW_CONFIDENCE_VISUAL_CLICK_GATE`
- `NO_AUDIT_LOG_BYPASS_GATE`
- `NO_EVIDENCE_TAMPERING_GATE`

## Decision

This block establishes fixture-safe foundation only.

- `WINDOWS_COMPUTER_USE_LIVE_READY: NO`
- `WINDOWS_COMPUTER_USE_PRODUCT_AUTOMATION_READY: NO`
- `WCU_FIXTURE_SAFE_FOUNDATION_ALLOWED: YES`
