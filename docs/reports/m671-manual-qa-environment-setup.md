# M671 Manual QA Environment Setup

Milestone: M671

Decision: HUMAN_ASSISTED_MANUAL_QA_HANDOFF_READY

M668-M670 could not execute real Chrome extension QA because browser automation was blocked from opening `chrome://extensions`. This is an environment and tooling policy limitation, not product evidence.

No bypass is attempted. The safe path is a human-assisted Chrome load using Developer Mode and a staging folder that uses `manifest.public.json` as the package manifest.

Required human setup:

1. Prepare a local QA staging folder outside the release path.
2. Copy extension files needed for unpacked loading.
3. Copy `manifest.public.json` into the staging folder as `manifest.json`.
4. Verify the staged manifest has no `http://*/*` or `https://*/*` host permissions.
5. Open Chrome manually.
6. Navigate manually to `chrome://extensions`.
7. Enable Developer Mode.
8. Use Load unpacked and select the staging folder.
9. Confirm visible name `NODAL OS Public Candidate`.
10. Record any load errors without secrets or long raw logs.

Public release remains NO-GO. Chrome Web Store remains NO-GO.

Product files, bridge, CSP, runtime, provider, filesystem, browser automation, and capability unlock were not modified.
