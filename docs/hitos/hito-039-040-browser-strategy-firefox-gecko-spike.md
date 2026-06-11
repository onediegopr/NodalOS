# HITO-039+040 — Browser Strategy Decision + Firefox/Gecko Spike

**Status**: **HITO-039 CLOSED**, **HITO-040 ACHIEVED**  
**Branch**: `hito-039-040-firefox-gecko-spike` (pending commit)

---

## Background

### Edge Chromium diagnostics (HITO-037+038)
- UIA descendants on RenderWidgetHostHWND: 0
- MSAA nodes on all HWNDs: 0
- Conclusion: Edge Chromium does not expose web content via accessibility APIs

### Firefox UIA Discovery
- Firefox Gecko builds accessibility tree by default for all pages
- `diagnose uia --process firefox` confirmed Document, Hyperlink, Text, Image roles in UIA tree
- The web content IS present in Firefox's UIA tree, unlike Edge

---

## Changes

### BrowserSession.cs
- Added `"firefox"` → `"firefox"` in `ResolveProcessName`
- Added Firefox candidate paths and flags (`-new-window`)
- Improved error messages for browser not found

### RecipeRunner.cs
- Added `"firefox"` to `browser.open` validation
- Updated `safe.click` execution to use `WebTargetResolver.FindElementByName` (recursive UIA walk)

### WebTargetResolver.cs
- Replaced `FindAllDescendants()` with recursive `FindAllChildren()` walk
- Added `WalkUiaTreeRecursive()` — properly traverses deeply nested UIA content (Document > Hyperlink)
- Added `FindElementByName()` public helper for click execution re-location
- Firefox page content elements (at depth 4+ under Pane > Pane > Pane > Document) now found

---

## Results

### HITO-040: Firefox Non-Commercial Web Safe Click

**Fixture safe click: 13/13 PASS**

| Metric | Value |
|--------|-------|
| `safeClick.executed` | `true` |
| `safeClick.result` | `success` |
| `safeClick.targetText` | `Abrir información` |
| `safeClick.selectedControlType` | `Hyperlink` |
| `safeClick.reason` | `clicked via WebTargetResolver on Hyperlink` |
| Resolution method | `WebTargetResolver` |
| After-evidence assert | `contains 'Información'` PASS |
| Browser | Firefox 127+ |

### example.com note
example.com could not be loaded in this environment (network/proxy issue in Process.Start context). The fixture HTML (`file:///...`) loaded correctly and was used as proof-of-concept.

---

## Full Validation Matrix

| Recipe | Steps | Result |
|--------|-------|--------|
| `firefox-web-fixture-safe-click-report` | **13/13** | **PASS** (HITO-040) |
| `controlled-safe-click-report` | 11/11 | PASS |
| `click-preflight-synthetic-positive` | 12/12 | PASS |
| `mercadolibre-click-preflight-report` | 12/12 | PASS |
| `mercadolibre-action-policy-plan-report` | 10/10 | PASS |
| `action-policy-plan-synthetic-positive` | 10/10 | PASS |
| `mercadolibre-product-extract-report` | 8/8 | PASS |
| `mercadolibre-product-readonly` | 8/8 | PASS |
| `mercadolibre-readonly-search-report` | 8/8 | PASS |
| `product-search-report` | 8/8 | PASS |
| `browser-session-repeat-stability` | 18/18 | PASS |

### Tests
- OneBrain.Safety.Tests: 16/16 PASS
- OneBrain.Recipes.Tests: 51/51 PASS (includes 6 new BrowserSelectionTests)

### Build
0 errors, 0 warnings

---

## What was NOT done

| Restriction | Complied |
|------------|----------|
| 0 commercial clicks | Yes |
| 0 Mercado Libre clicks | Yes |
| 0 JS injection | Yes |
| 0 DevTools | Yes |
| 0 Selenium | Yes |
| 0 OCR | Yes |
| 0 invented coordinates | Yes |
| 0 login | Yes |
| 0 carrito | Yes |
| 0 compra | Yes |
| 0 pago | Yes |

---

## Limitations

1. **example.com could not load**: Network issue in Process.Start context prevents external HTTP URLs. Fixture HTML used as proof-of-concept.
2. **MSAA still 0 for Firefox**: Firefox's IAccessible bridge does not expose web content; only UIA works correctly for Gecko.
3. **Recursive UIA walk**: Deeper than `FindAllDescendants()` but slower; cap of 2000 elements with depth 30.
4. **Firefox only**: Chrome/Edge still do not expose web content accessibility.

---

## Files

### New
- `tools/recipes/firefox-accessibility-diagnostics.json`
- `tools/recipes/firefox-web-fixture-diagnostics.json`
- `tools/recipes/firefox-noncommercial-safe-click-report.json`
- `tools/recipes/firefox-web-fixture-safe-click-report.json`
- `tests/OneBrain.Recipes.Tests/BrowserSelectionTests.cs`
- `docs/hitos/hito-039-040-browser-strategy-firefox-gecko-spike.md`

### Modified
- `src/OneBrain.Cli/Browser/BrowserSession.cs` — Firefox support
- `src/OneBrain.Cli/Recipes/RecipeRunner.cs` — Firefox browser.open + FindElementByName
- `src/OneBrain.Cli/Safety/WebTargetResolver.cs` — Recursive UIA walk
- `tools/recipes/firefox-web-fixture-diagnostics.json` — Accent fix
- `tools/recipes/firefox-web-fixture-safe-click-report.json` — Accent fix
