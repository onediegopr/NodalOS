# Recorder-to-Recipe Fixture Draft v1

Status: M4 contracts and read-only Lab integration.

## Purpose

M4 models how a human demonstration could become a reliable recipe draft without introducing a real recorder. The output is a reviewable draft with quality findings, missing validation, missing evidence, handoff requirements and a read-only Recipe Lab summary.

This is a fixture-only design layer. It does not observe a real screen, browser, desktop, keyboard, mouse, clipboard or operating system.

## Dependencies

M4 builds on:

- M1 reliable recipe foundation contracts.
- M2 reliable recipe quality/preflight composition.
- M3 read-only Recipe Lab quality surface.

M4 extends M1 recorder contracts rather than replacing them.

## Clean Boundary

Allowed:

- fixture trajectories
- fake observation references
- redacted text tokens
- deterministic draft conversion
- review checklist generation
- M2 quality/preflight reports
- M3 read-only Lab mapping

Blocked:

- real recorder
- mouse and keyboard capture
- clipboard capture
- active-window capture
- screen or screenshot capture
- browser/CDP/Playwright/Cloak runtime
- desktop hooks, UIA live or Win32 live
- provider/LLM calls
- sandbox/VM runtime
- external side effects
- secret capture
- CAPTCHA/2FA bypass
- runtime execution

## Trajectory-to-Draft Flow

1. A fixture `ReliableRecorderTrajectory` describes observed interactions by reference.
2. `RecorderToRecipeDraftConverter` maps interactions into `ReliableRecipeBlock` instances.
3. Sensitive fixture input becomes replacement-token variables.
4. Missing validation and evidence become checklist items.
5. Challenge, credential, ambiguous target, OCR-only sensitive target and desktop future cases become blocked/review states.
6. The converted recipe is sent through M2 `ReliableRecipePreflightComposer`.
7. The M2 report is mapped through M3 `ReliableRecipeLabViewModelMapper`.
8. The Lab view model receives a recorder draft review panel.

## Review States

- `DraftOnly`
- `NeedsReview`
- `NeedsValidationDesign`
- `NeedsEvidenceDesign`
- `BlockedSensitiveInput`
- `BlockedChallenge`
- `BlockedAmbiguousTarget`
- `DryRunCandidate`
- `Rejected`

`DryRunCandidate` means the fixture draft has enough modeled validation/evidence for review. It does not mean live runtime authorization.

## Sensitive Input

Sensitive input is never stored as raw data. M4 uses replacement tokens such as `{PASSWORD}` and marks variables that need confirmation. Credential trajectories require human review and remain blocked from dry-run authorization.

## CAPTCHA, 2FA and Login

CAPTCHA, two-factor and login-sensitive paths become explicit human handoff requirements. The draft explains that the user must handle the sensitive boundary manually or keep the draft blocked. There is no bypass, solver or retry automation.

## OCR as Supporting Signal

OCR is treated only as an existing protected signal reference. M4 does not change OCR files, engines, activation gates or privacy gates. OCR-only sensitive targets are blocked or require review; OCR cannot authorize sensitive actions.

## Recipe Lab Integration

The M3 read-only Recipe Lab now includes:

- source trajectory label
- draft review state label
- checklist rows
- detected variable rows
- sensitive input summary
- fixture-only conversion notice

The panel exposes no start, playback, capture, browser, desktop or runtime actions.

## Future M5 Options

M5 should expand deterministic eval harness fixture scenarios:

- scenario outcome reports
- flakiness taxonomy
- evidence completeness across fixture attempts
- policy-blocked scenario reporting
- handoff-rate metrics

M5 must remain fixture-only and no-runtime.
