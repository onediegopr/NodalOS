# M6 Computer-Use Sandbox Readiness Reports v1

## Decision

NODAL OS adds fixture-only, read-only Computer-Use Sandbox Readiness Reports for Reliable Recipes, Recorder Drafts and Eval Scenarios.

This M6 layer does not create or run a sandbox. It only explains whether a recipe subject is fixture-ready, design-only, blocked by missing requirements, blocked by policy, or blocked because live runtime is not allowed.

## Dependency Chain

- M1: Reliable Recipe foundation, recorder draft, eval and sandbox contracts.
- M2: deterministic quality/preflight scoring.
- M3: read-only Recipe Lab quality surface.
- M4: fixture-only recorder-to-recipe drafts.
- M5: deterministic fixture eval harness and eval report surface.
- M6: sandbox readiness reports and read-only Lab/Eval summaries.

## No-Live Boundary

M6 excludes:

- no real sandbox
- no VM/container/Docker creation
- no browser execution
- no desktop execution
- no CDP/browser driver runtime
- no Playwright/Selenium/Puppeteer/Cloak mutation
- no UIA/Win32 live control
- no mouse, keyboard, screen, screenshot or clipboard capture
- no OCR live activation
- no provider/LLM call
- no network call
- no shell/process runner
- no filesystem productive write

The only allowed output is deterministic report/viewmodel data.

## Readiness Model

`ComputerUseSandboxReadinessReport` records:

- subject id and kind
- overall decision
- readiness score
- allowed assessment mode
- required future isolation mode
- surface readiness
- requirement reports
- blocked capabilities
- missing requirements
- risk reasons
- future unlock conditions
- fixture-only/no-runtime notice

The report carries explicit negative guards:

- `SandboxRuntimeEnabled = false`
- `VmOrContainerCreated = false`
- `DockerEnabled = false`
- `BrowserLiveLaunched = false`
- `DesktopLiveLaunched = false`
- `NetworkCallEnabled = false`
- `ShellOrProcessRunnerEnabled = false`
- `FilesystemWriteEnabled = false`
- `OcrLiveActivationEnabled = false`
- `RecorderRuntimeEnabled = false`

## Subject Kinds

- `ReliableRecipe`
- `RecorderDraft`
- `EvalScenario`
- `ImportedWorkflow`
- `PolicyRegressionFixture`

## Required Isolation Modes

- `NoneForFixture`
- `BrowserProfileFuture`
- `DesktopProfileFuture`
- `VmFuture`
- `ContainerFuture`
- `RemoteSandboxFuture`
- `NotAllowed`

Future isolation modes are requirements, not enabled runtimes.

## Surface Readiness

M6 evaluates future surfaces as report data:

- Browser
- Desktop
- FileDialog
- RemoteSession
- MobileFuture
- FilesystemFuture
- NetworkFuture

Browser live and desktop live remain blocked in every report. A browser-related subject can be `FutureSandboxCandidate`, but only as a future browser profile requirement. It is not sandbox live.

## Missing Requirements

M6 tracks:

- rollback policy
- network policy
- filesystem policy
- credential policy
- evidence policy
- validation policy
- runtime limit policy
- surface isolation
- redaction policy
- human handoff policy
- approval policy
- audit log policy
- secret handling policy
- challenge handling policy

Credential, challenge, sensitive, high-risk, eval-regression and live-surface gaps are blocking or future-required.

## Blocked Capabilities

Every M6 report explicitly blocks:

- browser live
- desktop live
- no CDP live
- no Playwright live
- Cloak live
- recorder live
- no OCR live capture
- no screenshot capture
- network access
- filesystem write
- credential automation
- CAPTCHA bypass
- 2FA bypass
- payment or publish
- shell execution

## Eval Integration

`ReliableRecipeFixtureEvalReport` now includes `ReliableRecipeEvalSandboxReadinessSummary`.

Expected blocks remain valid fixture outcomes when they prove policy is holding. Unexpected pass regressions become blocked sandbox readiness and require audit review.

## Recipe Lab Integration

`ReliableRecipeLabViewModel` now includes `ReliableRecipeLabSandboxReadinessPanel`.

The panel is read-only and fixture-only. It exposes safe labels only:

- review sandbox readiness
- open report
- copy summary

It does not expose run, launch, execute, browser, desktop or sandbox actions.

## OCR Protected Status

M6 treats OCR as an existing protected signal. It does not touch OCR files, behavior, gates or live activation. OCR-only sensitive targets are marked blocked for action authority.

## Recorder Protected Status

M6 evaluates recorder-derived fixture drafts only. It does not add recorder runtime, mouse capture, keyboard capture, screen capture, clipboard capture, hooks, listeners or replay.

## Runtime Protected Status

M6 does not add runtime adapters, browser launch, desktop launch, sandbox launch, process runners, shell execution, network calls or productive file writes.

## Future M7

Recommended M7: Perception Stack formal integration reports that connect DOM/accessibility/OCR/vision/SoM signal quality to target confidence and sandbox readiness, still fixture-only.
