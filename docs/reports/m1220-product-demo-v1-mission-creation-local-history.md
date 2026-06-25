# Product Demo v1 - Mission Creation + Local History

## 1. Decision

PRODUCT_DEMO_V1_MISSION_CREATION_LOCAL_HISTORY_READY

## 2. What Changed

NODAL OS Mission Control now supports creating local demo missions, running a no-op demo per mission, saving run history locally, reopening previous runs, and copying a run-specific summary.

## 3. What Is Visible In Product

- Nueva misión form with title and short description.
- Crear misión flow from the Mission Control UI.
- Mission list / switcher.
- Run demo action scoped to the active mission.
- Historial panel with previous runs.
- Timeline and evidence/log panel update when a previous run is selected.
- Copiar resumen uses the selected mission/run.
- Limpiar historial demo resets local demo missions after a simple confirmation.

## 4. How To Try It

1. Open the NODAL OS sidepanel.
2. In Mission Control, create a mission under Nueva misión.
3. Select the mission if needed.
4. Click Run demo.
5. Verify the timeline shows started, accepted, evidence, and completed.
6. Verify Logs / evidence and Historial update.
7. Select an older run from Historial.
8. Click Copiar resumen.

## 5. Local Persistence

Persistence uses browser `localStorage` under `nodal-os.demoMissions.v1`.

Stored records are local demo metadata:

- mission id, title, description, createdAt, status
- run id, missionId, startedAt, completedAt, status
- timeline events
- evidence/log summary

No provider/cloud call is added. No runtime real or PC Commander path is added.

## 6. Validations

Executed:

- PASS - `dotnet build .\OneBrain.slnx --no-restore`
- PASS - `node --check browser-extension/onebrain-chrome-lab/sidepanel.js`
- PASS - `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=M1161M1172"`: 9 passed, 0 failed
- PASS - `git diff --check`
- PASS - simple secret scan
- PASS - Mission Control bad UX wording scan for heavy caveat/governance wording
- NOTE - legacy advanced/runtime code still contains technical status vocabulary outside the main demo surface

## 7. Modified Files

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `tests/OneBrain.Safety.Tests/NodalOsProductVisibleLocalDemoM1161M1172Tests.cs`
- `docs/reports/m1220-product-demo-v1-mission-creation-local-history.md`

## 8. Pending

- Improve mission cards with inline edit/delete.
- Add persistent run filters/search if history grows.
- Prepare a grabable demo script.
- Decide whether localStorage should later migrate to `chrome.storage.local`.

## 9. Next Milestone

M1221-M1232 - Product Demo v2: Mission Editing + Demo Recording Flow
