# M8 Protected Dry-Run Adapter Readiness Design

Decision target: `NODAL_OS_M8_PROTECTED_DRY_RUN_ADAPTER_READINESS_DESIGN_AUDIT`

## Decision

NODAL OS adds a protected dry-run adapter readiness design layer. This layer is declarative only: it defines readiness reports, gate requirements, blocked capabilities, future adapter boundaries and protected-scope references for a possible future Reliable Recipe dry-run adapter.

M8 does not add an executable adapter. M8 does not add a runtime command. M8 does not launch browsers or desktops. M8 does not connect CDP. M8 does not change Cloak, Playwright, Selenium, Puppeteer, OCR, WCU, recorder, sandbox, provider or network behavior.

## Dependency Chain

Future adapter consideration depends on M1-M7:

- M1 Reliable Recipe contracts and no-runtime preflight.
- M2 deterministic quality/preflight scoring.
- M3 read-only Recipe Lab view models.
- M4 fixture-only recorder draft review.
- M5 fixture-only eval harness.
- M6 design-only sandbox readiness.
- M7 fixture-only perception integration reports.

## Future Adapter Boundary

Allowed future inputs:

- Reliable recipe definitions.
- Preflight and quality reports.
- Fixture eval reports.
- Sandbox readiness reports.
- Perception integration reports.
- Human handoff and approval policy metadata.

Allowed future outputs:

- Read-only readiness reports.
- Evidence expectation plans.
- Validation expectation plans.
- Operator handoff checklists.
- Audit summaries.

Forbidden inputs:

- live browser state,
- live desktop state,
- raw credentials,
- screenshots,
- network responses,
- provider output.

Forbidden outputs:

- executed actions,
- live artifacts,
- filesystem writes,
- network calls,
- runtime commands.

## Required Gates

Every future adapter candidate must satisfy:

- Reliable recipe preflight passes.
- Quality score above threshold.
- Validation expectations are structured.
- Evidence expectations are structured.
- Recorder draft reviewed.
- Fixture eval harness passes.
- Sandbox readiness remains fixture-only.
- Perception signals are sufficient.
- Human handoff policy is present when needed.
- Approval policy is present when needed.
- Secret redaction policy is present.
- No live runtime capability is exposed.
- Protected-scope audit passes.
- External audit is required before runtime.

## Blocked Capabilities

M8 blocks:

- browser live,
- CDP live,
- Cloak live,
- Playwright live,
- desktop live,
- recorder live,
- OCR live,
- screenshot capture,
- sandbox runtime,
- network access,
- shell execution,
- filesystem write,
- provider call,
- credential automation,
- CAPTCHA or 2FA bypass,
- payment, publish, send or delete actions.

## Protected Scopes

Protected scopes remain untouched:

- post-M1345 browser/live execution,
- OCR/WCU interop,
- recorder/live capture,
- sandbox/runtime.

Any future change to those areas requires a separate explicit prompt, protected-scope audit, external audit and operator approval.

## Why M8 Is Design-Only

M1-M7 establish deterministic contracts, quality gates, lab surfaces, recorder draft review, eval fixtures, sandbox readiness and perception reporting. They do not create runtime authority. M8 intentionally keeps runtime real autonomy at 0% and records the prerequisite matrix before any adapter implementation can be considered.

## Next Recommended Block

M9 should not implement runtime directly. The safer next block is structured evidence/validation prerequisite hardening for adapter candidates, still no-runtime, so M8 missing-gate results become more actionable before any adapter code exists.
