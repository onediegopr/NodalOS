# Reliable Recipe Eval Harness Fixture Scenarios v1

Status: M5 fixture-only eval harness contracts and reports.

## Purpose

M5 gives Reliable Recipes and Recorder-to-Recipe drafts a deterministic evaluation harness before any runtime exists. The harness evaluates predefined fixture scenarios against expected outcomes, quality/preflight reports, evidence completeness, validation completeness, target confidence, sandbox readiness, human handoff quality and flakiness.

The eval harness does not execute recipes. It evaluates fixture data and policy reports only.

## Dependencies

M5 builds on:

- M1 Reliable Recipe foundation contracts.
- M2 quality/preflight composition.
- M3 read-only Recipe Lab surface.
- M4 recorder-to-recipe fixture drafts.

M1 had a minimal eval harness. M5 adds a richer `ReliableRecipeFixtureEval*` layer rather than replacing M1.

## No-Live Boundary

Blocked in M5:

- browser execution
- CDP or browser driver runtime
- no Playwright/Selenium/Puppeteer/Cloak mutation
- desktop hooks or live desktop control
- real recorder
- no mouse, keyboard, clipboard, screen or screenshot capture
- OCR live activation
- sandbox/VM runtime
- provider or LLM call
- network/API call
- filesystem/productive side effects
- CAPTCHA/2FA solver
- credential automation
- payment, publish, send, delete or write action

Allowed in M5:

- deterministic fixture scenario definitions
- repeated evaluation over predefined fixture variants
- policy/preflight scoring
- evidence and validation completeness scoring
- target/sandbox/human intervention score aggregation
- failure taxonomy
- flakiness report
- read-only Lab eval panel

## Eval Harness Model

Core M5 types:

- `ReliableRecipeFixtureEvalScenario`
- `ReliableRecipeFixtureEvalRun`
- `ReliableRecipeFixtureEvalIterationResult`
- `ReliableRecipeFixtureEvalMetrics`
- `ReliableRecipeFlakinessReport`
- `ReliableRecipeFixtureEvalReport`
- `ReliableRecipeFixtureEvalRunner`
- `ReliableRecipeEvalScenarioCatalog`

The runner resolves a scenario to an M3 or M4 fixture, reuses M2 preflight and quality reports, computes deterministic iteration results and produces a read-only report.

## Expected Blocks

An expected block is a pass for the scenario when the scenario explicitly expects a blocked, handoff, validation-failure or evidence-failure outcome.

Examples:

- OCR-only sensitive submit is expected to block.
- CAPTCHA/2FA is expected to create human handoff.
- Desktop future surface is expected to fail sandbox readiness.
- Government submit is expected to block by risk/policy.

## Unexpected Pass

An unexpected pass is a regression when a scenario expects a block but fixture policy reports a dry-run candidate/pass. M5 includes `unexpected_pass_regression_fixture` to prove the detection path.

## Failure Taxonomy

M5 taxonomy includes:

- target not found
- target ambiguous
- OCR-only sensitive target
- validation missing/failed
- evidence missing
- policy blocked
- risk blocked
- secret exposure blocked
- loop limit reached
- human handoff required
- sandbox not ready
- recorder draft not reviewed
- fixture mismatch
- expected block did not occur
- unexpected pass

## Flakiness Scoring

M5 does not use randomness. Flakiness is calculated from predefined fixture variants. If two deterministic variants produce different failure taxonomies, the flakiness score becomes non-zero and the report recommends reviewing the fixture variants before any future runtime evaluation.

## Evidence and Validation Scoring

The eval harness aggregates M2 evidence and validation completeness scores across iterations. Missing evidence or validation can be an expected outcome, a warning or a blocking condition depending on the scenario.

## Recipe Lab Integration

M5 adds `ReliableRecipeLabEvalPanel` as a read-only DTO surface:

- scenario id
- final decision
- success-rate label
- expected-outcome match label
- flakiness label
- evidence completeness label
- validation completeness label
- top failure kinds
- fixture-only notice
- read-only action labels

The panel says: "Fixture-only evaluation. Runtime not enabled."

## OCR Protected Status

OCR is only a fixture signal/reference. M5 does not modify OCR files, OCR engines, OCR activation gates, OCR privacy/redaction gates or OCR/WCU behavior. OCR-only sensitive target scenarios are blocked as expected.

## Recorder Protected Status

M5 evaluates M4 fixture recorder drafts. It does not add recorder runtime, event hooks, capture, background listeners or replay.

## Future M6 Options

M6 should expand computer-use sandbox readiness reporting:

- isolation policy matrix
- rollback requirements
- network/filesystem/credential/evidence policy scoring
- allowed vs blocked surfaces
- fixture-only product report

M6 must remain design-only and no-runtime.
