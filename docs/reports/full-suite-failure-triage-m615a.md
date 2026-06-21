# Full Suite Failure Triage - M615A

Decision target: FULL_SUITE_TRIAGE_READY

This diagnostic block investigates 26 full-suite test failures observed after M615 (Sidepanel Token Patch 1).

## Executive Summary

**All 26 failures are pre-existing baseline failures or flaky tests. None are caused by M615.**
M616 (Sidepanel Token Patch 2) has a GO recommendation with documented baseline.

## Evidence

### 1. M615 Commit Scope
M615 changed only 5 files:
- browser-extension/onebrain-chrome-lab/sidepanel.css (CSS :root only)
- docs/reports/sidepanel-token-patch-1-m615.md
- docs/roadmap/nodal-os-roadmap-vnext.md
- docs/roadmap/nodal-os-unified-roadmap-post-pause.md
- tests/OneBrain.Safety.Tests/NodalOsSidepanelTokenPatch1M615Tests.cs

**Zero changes** to any failing test file or source dependency.

### 2. Git Diff
git diff 19517ab..HEAD -- <all 6 failing test files>
Result: empty diff -- byte-identical between parent and M615.

### 3. Full Suite Runs
Run 1: 29 failed, 3714 passed, 37 skipped (incl. 3 flaky BrowserRuntimeSmokeTests)
Run 2: 26 failed, 3717 passed, 37 skipped (3 flaky passed on rerun)

### 4. M615 Filter
20/20 pass -- all M615 tests green.

### 5. Isolated Reruns
| Test Class | Failures | Total | Classification |
|------------|----------|-------|----------------|
| BrowserSafeUploadM27Tests | 9 | 25 | Reproducible baseline |
| BrowserDocumentWorkflowM28ATests | 3 | 17 | Reproducible baseline |
| BrowserSensitiveSimulationM33AM34ATests | 3 | 32 | Reproducible baseline |
| NodalOsPaddleOcrOnnxModelVerificationM197Tests | 4 | 12 | Reproducible baseline (env) |
| ProductEvidenceDemoSamplesTests | 1 | 9 | Reproducible baseline |
| BrowserRuntimeSmokeTests | 1 | 17 | Flaky |

### 6. Failure Root Causes
All failures are in browser automation (Chrome CDP), ONNX model files, or demo path validation.
None involve CSS, sidepanel UI, selectors, HTML/JS/manifest, runtime, providers, filesystem, or capabilities.

## Conclusion
- M615 implicated: NO
- Sidepanel patch implicated: NO  
- Pre-existing baseline: YES (zero diff on failing test files)
- M616 GO: YES with documented baseline
