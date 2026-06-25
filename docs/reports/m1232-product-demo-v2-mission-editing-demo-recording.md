# Product Demo v2 - Mission Editing + Demo Recording Flow

## 1. Decision

PRODUCT_DEMO_V2_MISSION_EDITING_DEMO_RECORDING_READY

## 2. What Changed

Mission Control now supports editing the active mission, deleting a mission with a simple confirmation, adding a note to the selected run, richer run history cards, and a visible Demo script for recording a product walkthrough.

## 3. What Is Visible In Product

- Editar misión button and inline edit fields.
- Guardar / Cancelar / Borrar misión actions.
- Nota del run input for the selected run.
- Better Historial cards with note/title, date, duration, event count, status, and Ver label.
- Demo script panel with recording steps.
- Copiar script button.
- Improved Copiar resumen output with mission description, run note, timeline, evidence/log summary, date, duration, and local/no-op mode.

## 4. How To Try It

1. Open the NODAL OS sidepanel.
2. Create a mission or select an existing one.
3. Click Editar, change the title/description, and save.
4. Click Run demo.
5. Add a short Nota del run, then save it.
6. Select the run from Historial.
7. Copy the Demo script.
8. Copy the run summary.
9. Optionally delete a demo mission with Borrar misión.

## 5. Local Persistence

Persistence remains local under `nodal-os.demoMissions.v1`.

The store is backward compatible with the v1 shape and now writes `schemaVersion: 2`.

The stored demo records include missions, runs, run notes, duration metadata, timeline events, and evidence/log summaries.

## 6. Validations

Executed:

- PASS - `dotnet build .\OneBrain.slnx --no-restore`
- PASS - `node --check browser-extension/onebrain-chrome-lab/sidepanel.js`
- PASS - `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=M1161M1172"`: 11 passed, 0 failed
- PASS - `git diff --check`
- PASS - simple secret scan
- PASS - bad UX wording scan in Mission Control

Build emitted existing warnings from historical OCR/visual QA tests, with 0 errors.

## 7. Modified Files

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `tests/OneBrain.Safety.Tests/NodalOsProductVisibleLocalDemoM1161M1172Tests.cs`
- `docs/reports/m1232-product-demo-v2-mission-editing-demo-recording.md`

## 8. Risks

- Demo persistence still uses `localStorage`; this is appropriate for a local sidepanel demo but not a synced multi-device history.
- Visual browser QA was not executed in this block.
- Legacy advanced/runtime sections still contain technical vocabulary outside the main Mission Control demo.

## 9. Next Milestone

M1233-M1244 - Product Demo v3: Visual QA Pass + Demo Recording Checklist
