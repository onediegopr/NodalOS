# Recipe Handoff Export Preview Surface

Block: `NODAL_RECIPE_RUNTIME_PRODUCT_SURFACE_003_OPERATOR_PREVIEW_FLOW_HANDOFF_EXPORT_SURFACE`

Product-surface phase: 3/4.

## Purpose

The handoff export preview surface describes what a handoff package would contain as product metadata. It is a read-only preview model only.

No real export is generated. No file is written. No PDF or DOCX is produced. No save dialog is opened. No network, connector, vault, browser, desktop, recorder, playback, or capture path is used.

## Preview Fields

The preview model includes:

- Handoff title.
- Template summary.
- Readiness snapshot.
- Blocking reasons.
- Missing requirements.
- Approval path.
- Tool trust summary.
- Secret references summary without secret values.
- Evidence requirements.
- Validation requirements.
- Locator and capture implications.
- Trigger observe-only summary.
- Operator notes.
- Not included and not automated list.
- Export availability state.
- Product-safe copy for the operator.

## Export Availability

The only valid availability for this phase is preview-only or disabled. A preview may say that a handoff package is not generated as a real file.

## Safety Boundary

The preview model cannot:

- Start recipe execution.
- Process workitems.
- Open connector/API/network paths.
- Read secrets or credentials.
- Write runtime files.
- Generate real artifacts.
- Enable browser or desktop automation.
- Enable recorder/playback/capture.
- Enable live runtime.
