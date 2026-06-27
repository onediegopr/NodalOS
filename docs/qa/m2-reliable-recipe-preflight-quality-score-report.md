# M2 Reliable Recipe Preflight Quality Score Report

## Decision

`NODAL_OS_M2_RELIABLE_RECIPE_PREFLIGHT_QUALITY_SCORE`

Status at report creation: implemented pending full validation.

## Guard Summary

- Repo: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- M1 dependency commit: `634cfc0b797532fb3becca78476a5a8a9372c0f1`
- M1 commit in ancestry: yes
- Worktree before M2: clean
- Origin sync before M2: `0/0`
- Repo identity: NODAL OS
- NODRIX: no

## Existing Surfaces Composed

- M1 Reliable Recipe contracts and validators.
- Existing Recipe Runtime `RecipePolicyPreflightResult` as an optional stricter policy input.
- Existing recipe risk/readiness concepts by adapter only.
- M1 perception signal contracts, including OCR as a signal only.
- M1 sandbox readiness profile/report contracts.
- M1 human intervention request contracts.

## Added M2 Contracts

- `ReliableRecipeQualityReport`
- `ReliableRecipeQualityCategory`
- `ReliableRecipeQualityDecision`
- `ReliableRecipeQualityFinding`
- `ReliableRecipePreflightReport`
- `ReliableRecipePreflightComposer`
- `EvidenceCompletenessScore`
- `ValidationCompletenessScore`
- `TargetResolutionQualityScore`
- `RecipeRiskPostureReport`
- `RecipeSandboxReadinessScore`
- `HumanInterventionPlanQualityScore`

## Deterministic Scoring

The quality score is deterministic and uses only supplied fixture/design-time data:

- limits and M1 preflight result
- evidence expectation labels
- validation requirement labels
- target descriptor confidence
- perception signal agreement
- risk profile flags
- sandbox profile policy strings
- human intervention request quality
- optional existing Recipe Runtime preflight result

No AI, provider, live browser, live desktop, OCR runtime, screenshot capture, connector, vault, scheduler or recorder is invoked.

## OCR Protected Status

- OCR files touched: no
- OCR behavior changed: no
- OCR gates changed: no
- OCR runtime/live activation changed: no
- OCR consumed only as signal/reference: yes

M2 treats OCR as one perception signal among DOM, accessibility, visual and Set-of-Marks signals. OCR-only sensitive targets block. OCR-only read extraction can remain reviewable/fixture-safe with lower confidence.

## Safety Boundary

This block does not add:

- browser execution
- CDP runtime calls
- Playwright/Selenium/Puppeteer runtime
- Cloak state mutation
- desktop hooks
- recorder runtime
- screenshot capture activation
- sandbox/VM runtime
- provider/LLM calls
- external side effects
- credentials/secrets/tokens/cookies capture
- CAPTCHA/2FA circumvention
- payment/publish/send/delete/write actions

## Validation Snapshot

At report creation:

- Build: PASS
- Focused M2 tests: PASS, 26/26

Full validation results are recorded in the final operator report.

## Percentages

| Area | Before M2 | After M2 Target |
| --- | ---: | ---: |
| Audit/placement | 100% | 100% |
| Reliable Recipe contracts | 50% | 63% |
| Validation/policy gates | 40% | 58% |
| Evidence/timeline recipe linkage | 30% | 43% |
| Recorder draft readiness | 25% | 33% |
| Eval harness readiness | 25% | 33% |
| Sandbox readiness | 25% | 38% |
| Perception stack formalization | 40% | 53% |
| Runtime real autonomy | 0% | 0% intentionally |
| Overall new upgrade | 28% | 40% |

## Next Recommended Block

M3 should add read-only Recipe Lab/Product Surface presentation for reliable recipe quality reports, without enabling runtime.
