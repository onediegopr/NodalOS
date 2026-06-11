# HITO-046+047 — App Close v1 + Critical Recipe Cleanup Fixes

**Status**: In Progress  
**Branch**: `hito-046-047-cleanup-enforcement` (pending commit)

---

## Before Audit

| Metric | Before |
|--------|--------|
| Recipes with opens | 37 |
| Missing close | 19 |
| HIGH risk | 12 |
| `app.close` step | ❌ Not implemented |

---

## HITO-046: app.close v1

Implemented `app.close` step in RecipeRunner. Supports: `calculator`, `notepad`, `explorer`.

### Behavior
- Finds window by process name + title via WindowFinder
- `candidateCount == 1`: closes with WM_CLOSE
- `candidateCount == 0`: reports "not found (already closed)"
- `candidateCount > 1`: blocks with "ambiguous target"
- Dry-run: no action

### Limitations
- No ownership tracking (WindowFinder matches any visible window)
- Single-window only (blocks if multiple Calculator windows)
- Notepad may trigger save dialog (recipe-level risk)

---

## HITO-047: Fixed Recipes

| Recipe | Fix |
|--------|-----|
| `controlled-safe-click-report.json` | +`app.close calculator` |
| `browser-session-repeat-stability.json` | Close before assert per cycle |
| `browser-read-example.json` | +`browser.close` before asserts |
| `browser-read-wikipedia.json` | +`browser.close` before asserts |
| `visual-browser-capture.json` | +`browser.close` before asserts |
| `calculator-to-notepad-variables.json` | +`app.close calculator` + `app.close notepad` |
| `real-smoke-windows-pack.json` | +`app.close calculator` + `browser.close edge` |
| `noncommercial-web-safe-click-report.json` | Close before note |
| `firefox-external-noncommercial-safe-click-report.json` | Close before note |

## Remaining Debt (not fixed in this hito)

| Recipe | Reason |
|--------|--------|
| `browser-smoke.json` | No asserts, low priority |
| `edge-example-smoke.json` | No asserts, low priority |
| `browser-session-example.json` | Close after assert (minor) |
| `browser-session-no-stale-window.json` | Close after assert (minor) |
| `explorer-temp-smoke.json` | Explorer close ambiguous |
| `notepad-report-smoke.json` | Notepad close may trigger save dialog |
| `recorded-notepad-sample.json` | Notepad close may trigger save dialog |
| `dry-run-safety-sample.json` | Dry-run test |

---

## Files

### Modified
- `src/OneBrain.Cli/Recipes/RecipeRunner.cs` — +ExecuteAppClose
- `tools/recipes/controlled-safe-click-report.json`
- `tools/recipes/browser-session-repeat-stability.json`
- `tools/recipes/browser-read-example.json`
- `tools/recipes/browser-read-wikipedia.json`
- `tools/recipes/visual-browser-capture.json`
- `tools/recipes/calculator-to-notepad-variables.json`
- `tools/recipes/real-smoke-windows-pack.json`
- `tools/recipes/noncommercial-web-safe-click-report.json`
- `tools/recipes/firefox-external-noncommercial-safe-click-report.json`

### New
- `docs/hitos/hito-046-047-app-close-critical-cleanup.md`
