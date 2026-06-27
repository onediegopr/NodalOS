# M3 Reliable Recipe Lab Read-Only Quality Surface Report

## Decision

`NODAL_OS_M3_RELIABLE_RECIPE_LAB_READ_ONLY_QUALITY_SURFACE`

Status at report creation: implemented pending full validation.

## Guard Summary

- Repo: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- M2 dependency commit: `e183e40250e5842396e869ba38f51444ea4b1f78`
- M2 commit in ancestry: yes
- Initial worktree: clean
- Initial origin sync: `0/0`
- Repo identity: NODAL OS
- NODRIX: no

## Product Surface Decision

Existing Recipe Product Surface patterns are Core DTO/viewmodel contracts with fixture-safe tests. M3 follows that convention and adds read-only view models, mapper and fixture catalog only.

No UI component or endpoint was added because a DTO/viewmodel surface is the safest established convention for this layer.

## Added Surface

- `ReliableRecipeLabViewModel`
- `ReliableRecipeLabCategoryCard`
- `ReliableRecipeLabFindingViewModel`
- `ReliableRecipeLabPanelViewModel`
- `ReliableRecipeLabTimelinePreviewItem`
- `ReliableRecipeLabNoLiveRuntimeNotice`
- `ReliableRecipeLabViewModelMapper`
- `ReliableRecipeLabFixtureCatalog`

## Product Boundary

The view model presents:

- readiness label
- requested/allowed mode
- quality score
- category cards
- blocking findings and warnings
- recommended fixes
- evidence completeness panel
- validation completeness panel
- target confidence panel
- risk posture panel
- sandbox readiness panel
- human handoff panel
- perception/OCR panel
- recorder draft panel
- timeline preview
- no-live runtime notice

It exposes no runtime action, no execution callback, no browser/desktop runtime flag, no provider/network access and no secret-reading capability.

## OCR Protected Status

- OCR files touched: no
- OCR behavior changed: no
- OCR gates changed: no
- OCR runtime/live activation changed: no
- OCR displayed only as read-only signal/reference: yes

OCR is displayed as supporting evidence only. OCR-only sensitive target states show: "OCR signal is supporting evidence only and cannot authorize sensitive actions."

## Fixture Catalog

M3 fixture catalog includes:

- `safe_invoice_download_quality_pass`
- `submit_without_validation_quality_blocked`
- `ocr_only_sensitive_submit_blocked`
- `recorder_draft_quality_draft_only`
- `captcha_handoff_quality_review`
- `desktop_live_sandbox_blocked`
- `external_side_effect_needs_approval`
- `ambiguous_target_needs_review`
- `high_quality_high_risk_blocked`

All fixtures are static, deterministic and reference-only.

## Validation Snapshot

At report creation:

- Build: PASS
- Focused M3 tests: PASS, 18/18

Full validation is recorded in the final operator report.

## Safety Boundary

M3 does not add:

- browser execution
- CDP runtime path
- Playwright/Selenium/Puppeteer runtime
- Cloak state mutation
- desktop hooks
- recorder runtime
- sandbox/VM runtime
- provider/LLM calls
- external side effects
- secrets/tokens/cookies/password capture
- screenshot capture activation
- challenge circumvention
- payment/publish/send/delete/write action
- OCR internals changes

## Percentages

| Area | Before M3 | After M3 Target |
| --- | ---: | ---: |
| Audit/placement | 100% | 100% |
| Reliable Recipe contracts | 63% | 69% |
| Validation/policy gates | 58% | 62% |
| Evidence/timeline recipe linkage | 43% | 50% |
| Recorder draft readiness | 33% | 38% |
| Eval harness readiness | 33% | 38% |
| Sandbox readiness | 38% | 44% |
| Perception stack formalization | 53% | 58% |
| Product surface readiness for Recipe Lab | 0% | 42% |
| Runtime real autonomy | 0% | 0% intentionally |
| Overall new upgrade | 40% | 48% |

## Next Recommended Block

M4 should expand recorder-to-recipe fixture drafts and map their quality reports into this read-only lab surface.
