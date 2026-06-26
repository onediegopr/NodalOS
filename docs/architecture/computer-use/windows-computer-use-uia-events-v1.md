# Windows Computer Use UIA Events v1

Status: fixture-safe read-only design.

## Purpose

UIA events provide passive timeline evidence about a fixture UIA surface. Events are observations, not triggers.

Events are useful for:

- focus changes;
- structure changes;
- property changes;
- window opened/closed;
- text/value changed metadata, redacted;
- modal/blocker appearance;
- stale or unresponsive state.

## Supported Event Kinds

- `FocusChanged`
- `StructureChanged`
- `PropertyChanged`
- `WindowOpened`
- `WindowClosed`
- `TextValueChanged`
- `SensitiveValueChanged`
- `ModalAppeared`
- `AppBecameUnresponsive`
- `BlockedStateDetected`

## Rules

- Event observation is not action authorization.
- Event stream cannot trigger execution directly.
- Event callbacks cannot click, type, submit, set values, invoke patterns, use keyboard/mouse, or access clipboard.
- Events can create redacted evidence.
- Events can update confidence, richness, blockage, modal/overlay, stale/unresponsive, and handoff state.
- Events must be throttled/debounced.
- Events must be redacted before evidence.
- Live subscription is disabled by default and not run in fixture-safe tests.

## Contract Surface

WCU exposes:

- `WindowsUiAutomationEvent`
- `WindowsUiAutomationEventKind`
- `WindowsUiAutomationEventSource`
- `WindowsUiAutomationEventPayload`
- `WindowsUiAutomationEventStreamOptions`
- `WindowsUiAutomationEventStreamState`
- `WindowsUiAutomationEventRedactionResult`
- `IWindowsUiAutomationEventStream`
- `FixtureWindowsUiAutomationEventStream`
- `DisabledWindowsUiAutomationEventStream`

The disabled stream returns `Disabled`, no live subscription, no action callback, and `ActionAuthority=false`.

## Fusion Behavior

The perception fusion classifier maps events as follows:

- focus/property/structure events can slightly improve read-only richness/confidence;
- modal/window-open events can create modal or UAC/admin blockers;
- text/value/sensitive events can create sensitive-surface blockers and handoff;
- stale/unresponsive/blocked events create blockage and handoff;
- no event can make a target executable.

## Evidence Behavior

`ComputerUseReadOnlyContextEvidenceBuilder` creates UIA event evidence packs with:

- event kind;
- redacted element name;
- redacted property/value metadata;
- evidence refs;
- sensitive field categories;
- `RawScreenshotStored=false`;
- `ClipboardCaptured=false`;
- `ActionAuthority=false`;
- `EventTriggeredExecution=false`.
