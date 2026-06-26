# NODAL OS Windows Computer Use Fixture-Safe Foundation Handoff

Date: 2026-06-26

Decision target: `GO_WCU_FIXTURE_SAFE_FOUNDATION_READY`

## Summary

The WCU line creates a fixture-safe/design-first Windows Computer Use control plane. It is separate from CBPR and from existing UIA execution modules. It does not execute real Windows actions.

## Created

- `src/OneBrain.WindowsComputerUse/`
- `tests/OneBrain.Safety.Tests/WindowsComputerUseFixtureSafeFoundationTests.cs`
- `docs/architecture/computer-use/`
- `docs/qa/computer-use/wcu-000-foundation/`
- `docs/prompts/computer-use/next-wcu-uia-read-only-adapter-design-prompt.md`

## NO-GO

- Real mouse.
- Real keyboard.
- Live UIA/FlaUI action.
- Clipboard capture.
- Raw screenshots.
- Credential/UAC/admin/destructive automation.
- Productive Windows automation.
- Browser live/CDP/WebSocket/Safe Injection.

## Next Step

Only a design-only UIA read-only adapter block may be considered next. Live actions remain blocked.
