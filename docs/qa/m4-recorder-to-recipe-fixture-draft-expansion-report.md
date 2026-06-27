# M4 Reliable Recipe Recorder-to-Recipe Fixture Draft Expansion

Decision target: `NODAL_OS_M4_RECORDER_TO_RECIPE_FIXTURE_DRAFT_EXPANSION`

Decision: `GO_M4_RECORDER_TO_RECIPE_FIXTURE_DRAFT_EXPANSION_READY`

## Guard

- Repo path: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Initial HEAD: `7690fb3e4fb845b3680cb2e059a202d7468b2eeb`
- Required M3 commit in ancestry: yes
- Origin sync at start: `0/0`
- Worktree at start: clean
- Repo confirmed as NODAL OS, not NODRIX.

## Scope Decision

M4 extends the M1 recorder contracts instead of duplicating them. M1 already defined `ReliableRecorderTrajectory`, `ReliableRecordedInteraction`, `ReliableRecordedObservation` and `ReliableRecipeDraftFromRecording`. M4 adds a fixture-only review/conversion layer around those contracts:

- `RecorderToRecipeDraft`
- `RecorderDraftReviewState`
- `RecorderDraftReviewChecklistItem`
- `RecorderDraftDetectedVariable`
- `RecorderSensitiveInputSummary`
- `ReliableRecipeRecorderFixtureCatalog`
- `RecorderToRecipeDraftConverter`

M4 also extends the M3 read-only Lab view model with `ReliableRecipeLabRecorderDraftReviewPanel`.

## Implemented

- Fixture trajectory catalog with eight demonstrations:
  - invoice download
  - login credential redaction
  - CAPTCHA/2FA handoff
  - ambiguous Continue target
  - OCR-only canvas target
  - government form submit
  - desktop future surface
  - corrected user click
- Deterministic trajectory-to-draft conversion.
- Sensitive input summary and replacement-token variable rows.
- Review checklist generation for missing validation, missing evidence, sensitive input, challenges, ambiguous target, OCR-only target and correction markers.
- M2 quality/preflight integration for converted drafts.
- M3 read-only Recipe Lab integration for recorder draft review state.
- Focused tests: `RecorderToRecipeFixtureDraft`, 28 tests.

## Safety Boundary

M4 remains fixture-only and read-only.

Explicitly not implemented:

- real recorder
- mouse capture
- keyboard capture
- clipboard capture
- screen or screenshot capture
- OCR live activation
- browser/CDP/Playwright/Cloak runtime
- desktop hooks, UIA live or Win32 live
- provider/LLM calls
- sandbox/VM runtime
- external side effects
- secret capture
- CAPTCHA/2FA bypass
- runtime execution

## OCR Protected Statement

- OCR files touched: no
- OCR behavior changed: no
- OCR gates changed: no
- OCR live activation changed: no
- OCR used/displayed only as fixture signal/reference: yes

## Recorder Protected Statement

- Recorder runtime added: no
- Mouse/keyboard capture added: no
- Screenshot/screen capture added: no
- Desktop/browser hooks added: no
- Fixture-only trajectory conversion: yes

## Validation Summary

Required validations were run after implementation and are recorded in the final operator report. The focused M4 test passed with 28/28 before full validation.

## Percentages

Before M4:

- Audit/placement: 100%
- Reliable Recipe contracts: 69%
- Validation/policy gates: 62%
- Evidence/timeline recipe linkage: 50%
- Recorder draft readiness: 38%
- Eval harness readiness: 38%
- Sandbox readiness: 44%
- Perception stack formalization: 58%
- Product surface readiness for Recipe Lab: 42%
- Runtime real autonomy: 0%
- Overall new upgrade: 48%

After M4:

- Audit/placement: 100%
- Reliable Recipe contracts: 74%
- Validation/policy gates: 67%
- Evidence/timeline recipe linkage: 57%
- Recorder draft readiness: 63%
- Eval harness readiness: 42%
- Sandbox readiness: 48%
- Perception stack formalization: 64%
- Product surface readiness for Recipe Lab: 54%
- Runtime real autonomy: 0%
- Overall new upgrade: 58%

## Next Recommended Block

`NODAL_OS_M5_RELIABLE_RECIPE_EVAL_HARNESS_FIXTURE_SCENARIOS`

Focus M5 on deterministic eval scenario expansion, flakiness taxonomy, evidence completeness scoring across fixture runs and report surfaces. Keep it fixture-only and no-runtime.
