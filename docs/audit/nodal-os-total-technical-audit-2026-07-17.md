# NODAL OS — Total Technical Audit and Targeted Cleanup

Date: 2026-07-17  
Baseline: `d4bbac23eabb4efadecff71762029fe6428e9193`  
Decision: `GO_WITH_FINDINGS_TOTAL_TECHNICAL_AUDIT_AND_TARGETED_CLEANUP_READY`

## Executive summary

The repository has strong local-first safety and runtime foundations, but its historical growth produced a much larger test/document/evidence surface than the current private-beta product. The highest-value defects were not missing abstractions; they were product-boundary leaks:

1. the canonical Mission Control root executed and displayed a synthetic fixture whenever no real mission existed;
2. the packaged MSIX still registered and served legacy/demo/internal routes;
3. a lab-only CloakBrowser binary blocker appeared in the clean product experience;
4. four test projects remained on a deprecated preview MSTest adapter;
5. README and roadmap understated the validated MSIX while overstating the applicability of Node/Rust/Tauri.

All five were corrected without adding a domain contract, policy engine, approval gate, ledger, timeline, database or product authority.

## System map

| Surface | Current role | Audit result |
| --- | --- | --- |
| `OneBrain.Pilot` | Mission Control, workspace/mission/action/BYOK routes and private-beta host | Keep; clean product state and package boundary corrected |
| Agent Operations Core | mission runtime, workspace binding, controlled action, evidence and BYOK | Keep; no duplicate runtime introduced |
| Runtime/Recipes/Safety/ChromeLab tests | behavior, integration and guardrail coverage | Keep; package references modernized, warning removed |
| ChromeLab and extension | lab/transition browser surface | Keep outside packaged product |
| CloakBrowser/CDP | future canonical browser runtime | Keep blocked until verified binary exists; do not expose blocker in clean MVP UI |
| MSIX pipeline | current Windows private-beta distribution | Keep; test signing only, no public-release claim |
| Vercel/Neon | optional future cloud complement | No NODAL/NODRIX project or database found; no mutation performed |

## Repository inventory

| Metric | Result |
| --- | ---: |
| Tracked files | 5,445 |
| Text lines | 648,830 |
| Product C# lines | 198,897 |
| Test C# lines | 180,121 |
| Markdown lines | 160,262 |
| Test/source ratio | 0.906 |
| Docs/source ratio | 0.806 |
| .NET projects / solution entries | 24 / 24 |
| MSTest methods | 8,319 |
| HTTP route mappings | 56 |
| Empty tracked files | 0 |
| Generated-directory paths tracked | 0 |
| Exact duplicate C# groups | 0 |
| TODO/FIXME/HACK markers | 33 |

Largest implementation hotspots:

- `RecipeRunner.cs` — 6,399 lines;
- `PilotHomePageRenderer.cs` — 2,490 lines;
- `RecipeProductSurfaceContracts.cs` — 2,255 lines;
- OCR compatibility/vision services — 2,166 / 2,006 lines;
- Product Ledger local/dev route mapper — 1,857 lines;
- workspace handoff execution — 1,661 lines.

These are refactor seams, not a mandate for a broad rewrite. Split only when a product feature touches the seam and the extraction removes an actual responsibility.

## Findings by severity

### Critical

None confirmed.

No public listener, unrestricted filesystem authority, secret exposure, production deployment, billing authority or bypass of verification/mission-scope approval was found.

### High

| Finding | Action | Risk | Effort | Gain | Status |
| --- | --- | --- | --- | --- | --- |
| Product root synthesized a completed fixture mission/model/fallback/evidence | Separate explicit fixture mode from mapped product services | High | Medium | High trust and truthful onboarding | Fixed |
| Packaged MSIX exposed legacy/demo/harness routes | Exact packaged route allowlist with process smoke | High | Low | Smaller attack/product surface | Fixed |
| GitHub default branch and protection do not reflect canonical `main` | Change repository setting and require real CI checks | High governance | Low admin | High integrity | External owner action, issue #27 |
| No declared source/product distribution terms | Select terms before customer/public distribution | High legal/release | Owner decision | Unblocks release | External owner action, issue #28 |

### Medium

| Finding | Action | Status |
| --- | --- | --- |
| Clean Mission Control showed a lab-only CloakBrowser binary blocker | Browser context now appears only in explicit fixture/lab mode | Fixed |
| Four test projects used deprecated `MSTest.TestAdapter 4.0.0-preview.25358.7` | Replace with supported stable MSTest package and run regression suites | Fixed |
| One MSTest analyzer warning used boolean equality assertion | Replace with `Assert.AreEqual` | Fixed |
| 1,469 tracked historical artifacts are all referenced outside `artifacts/` | Do not delete blindly; archive by severing references in deliberate batches | Maintained |
| Documentation and tests are nearly the size of product source | Freeze micro-hito/evidence growth; write only canonical docs and behavior tests | Active rule |
| Several source/test files are monolithic | Extract by responsibility only when touched by MVP work | Planned incremental |
| Runtime targets a pinned .NET 11 preview SDK | Keep self-contained private beta; move to supported release SDK before public distribution | Release blocker |

### Low

- historical naming across NODAL OS, ONE BRAIN, NODRIX and HOTEP;
- explicit development-only legacy routes remain available outside the package;
- old roadmaps and milestone records remain large but are retained for traceability;
- disabled future navigation placeholders remain non-authoritative.

## Corrections executed

### Product truth

- Mission Control runs the selective-runtime fixture only when called explicitly without product services.
- A fresh product state is `NotStarted`, 0%, with no synthetic timeline, evidence, model or fallback.
- Fixture-backed behavior remains testable through an explicit test path.
- The clean product surface no longer shows the CloakBrowser external-binary blocker.

### Packaged product boundary

- The packaged runtime accepts only canonical Mission Control, workspace, mission, execution and model routes.
- Pilot legacy, demos, executor harness, run history, recipes, guide and other internal surfaces return 404 in MSIX mode.
- Development retains the explicit lab routes for engineering use.
- The Mission Control footer and navigation no longer link into legacy/internal product paths.

### Test and dependency hygiene

- Runtime, Recipes, ChromeLab and Safety test projects moved off the deprecated preview adapter.
- The remaining analyzer equality warning was removed.
- Vulnerable-package audit found no vulnerable package in the solution.

### Documentation truth

- README now describes the validated test-signed MSIX and its actual public-release blockers.
- Node, Cargo and Tauri are recorded as intentionally not applicable to the native .NET/MSIX product path.
- The compact roadmap was shortened and reconciled with the clean private-beta product state.

## Validation

Executed evidence includes:

- deterministic repository inventory;
- solution restore and vulnerable/deprecated package audit;
- reference-aware artifact cleanup dry-run;
- canonical Pilot, Runtime Tests and Recipes Tests Release builds;
- full Runtime regression after MSTest migration;
- focused Recipes/Mission Control/desktop-launch regression;
- ChromeLab regression after MSTest migration;
- clean Mission Control process smoke;
- Windows package build/install/launch/health/lab-route-block/uninstall workflow;
- Tier 1 secret and safety checks.

Temporary one-shot audit/patch workflows and encoded patches were removed before merge.

## Guardrails preserved

- loopback-only local runtime;
- no raw secrets or absolute workspace paths in product surfaces/evidence;
- mission/scope approval rather than per-step approval churn;
- exact target and SHA-256 precondition before mutation;
- atomic create/replace and deterministic post-write verification;
- guarded rollback disabled after later user changes;
- trusted instructions separated from observed UI content;
- provider fallback limited by prior privacy/capability/budget authorization;
- no Chrome/Edge/Playwright fallback for the canonical browser runtime;
- no production, public distribution, billing or commercial authority.

## Roadmap adjustment

Resume product development only through private-beta outcomes:

1. polish human-readable handoff/report export;
2. add opt-in redacted startup/error/crash diagnostics;
3. measure startup, first-value and mission completion time locally;
4. run the real loop with five to ten design partners;
5. correct observed usability/reliability failures;
6. resolve default branch/protection, license, production signing and release channel.

Do not resume broad browser automation, global capture, cloud sync, teams, marketplace or managed AI before those gates close.

Next exact macro:

`NODAL_OS_PRODUCTIZATION_PRIVATE_BETA_EXPORT_DIAGNOSTICS_AND_DESIGN_PARTNER_READINESS`
