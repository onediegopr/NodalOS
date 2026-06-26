# Windows Computer Use Win32 Context v1

Status: fixture-safe read-only design.

## Purpose

Win32 context gives WCU read-only visibility into the active desktop window around a UIA snapshot. It is observability metadata, not an action surface.

Win32 context can help answer:

- which top-level window is active in a fixture;
- which process/class/title is associated with that window;
- whether a modal owner relationship is present;
- whether placement, monitor, or DPI conditions reduce confidence;
- whether the Win32 active window matches the UIA fixture snapshot.

## UIA Semantic Tree vs Win32 Context

UIA semantic tree describes controls: names, roles, automation ids, runtime ids, patterns, ancestry, and bounds.

Win32 context describes windows: opaque HWND identity, title, process, class, placement, monitor/DPI, foreground state, and modal/top-level relationship.

WCU priority remains:

1. UIA semantic tree.
2. Win32 window context.
3. UIA event metadata.
4. Existing Robust Perception/OCR fallback.
5. Human handoff.

## Allowed Fields

- Foreground window metadata.
- HWND as opaque identifier only.
- Process id/name.
- Process path redacted where needed.
- Window title redacted where needed.
- Placement: normal, minimized, maximized, hidden.
- Monitor bounds and primary flag.
- DPI scale and mismatch flag.
- Top-level/modal owner relationship.
- Z-order index metadata when fixture-safe and non-invasive.
- Evidence refs and source hashes.

## Prohibited Fields and Operations

- Window manipulation.
- Focus stealing or `SetForegroundWindow`.
- Input injection, `SendInput`, keyboard, mouse, `PostMessage`, or `SendMessage` actions.
- Clipboard reads/writes.
- Raw screenshots or screen capture.
- Credentials, raw sensitive titles, raw profile paths, raw OCR text.
- Any authority to execute clicks, typing, submit, login, payment, delete, overwrite, UAC/admin, or browser actions.

## WCU Snapshot Integration

Win32 context is provided to `ComputerUsePerceptionFusionRequest` as optional metadata. The fusion classifier:

- checks whether active Win32 process/title/class matches the UIA fixture snapshot;
- adds `win32.context.redacted` capability when fixture context is available;
- adds handoff/blockage when active window mismatches, modal context appears, app is not allowlisted, or DPI mismatch is present;
- never sets `ActionAuthorityGranted=true`.

## Evidence and Redaction

Win32 context evidence uses `ComputerUseReadOnlyContextEvidenceBuilder`.

Evidence stores:

- redacted title;
- redacted process path;
- process name/id;
- placement and modal metadata;
- monitor/DPI metadata;
- evidence refs.

Evidence does not store screenshots, clipboard values, raw credentials, or raw sensitive titles.

## OCR/Robust Perception Interop

Win32 context does not replace OCR/Robust Perception. It helps decide whether OCR/visual fallback observations are attached to the expected active window. OCR remains auxiliary evidence and cannot authorize actions.
