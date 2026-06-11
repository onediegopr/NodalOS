# HITO-041+042 — Firefox Safe Click Hardening + External Non-Commercial Click

**Status**: **HITO-041 CLOSED**, **HITO-042 DEFERRED**  
**Branch**: `hito-041-042-firefox-hardening` (pending commit)

---

## HITO-041 — Hardening (Achieved)

### Changes

#### WebTargetResult
- Added `SelectedBoundingRect` property
- Exposed via `SetResolutionVars` in recipe variables

#### RecipeRunner safe.click
- Ambiguity (candidateCount > 1) now returns `result=blocked` not `failed`
- Reason: `ambiguous: N candidates`
- `selectedBoundingRect`, `hasInvoke`, `hasClickablePoint` in resolution vars

#### SafeClickPolicyTests (4 new tests)
- `CommercialWeb_Blocks_Even_Nav_Target` — commercial mode never executable for nav targets
- `CommercialWeb_Blocks_Even_SafeReadonly_Target` — commercial mode blocks safe-readonly
- `NonCommercialWeb_Blocks_Dangerous_Target` — carrito blocked even in nonCommercialWeb
- `Target_Login_Siempre_Blocked_All_Modes` — login blocked in all 3 modes

---

## HITO-042 — External Non-Commercial Click (Deferred)

### Attempts

| URL | Result |
|-----|--------|
| `https://example.com` | "Problema al cargar la página" — connection error |
| `https://www.iana.org/help/example-domains` | Firefox internal page, URL not loaded |
| `https://en.wikipedia.org/wiki/Example.com` | Firefox internal page, URL not loaded |
| `http://httpbin.org/` | HTTP 503 error (connected but server error) |

### Root Cause
Firefox launched via `Process.Start` with `UseShellExecute=false` cannot load external HTTPS/HTTP URLs in this environment. Internal `file://` URLs work correctly (HITO-040 fixture). This is likely a firewall/proxy/security context issue.

### Deferred Reason
No external non-commercial URL could be loaded via Firefox Process.Start. HITO-041 hardening is complete. HITO-042 requires environment-level resolution (proxy config, Firefox profile, or alternative launch method) before retry.

---

## Validation Matrix

| Recipe | Steps | Result |
|--------|-------|--------|
| `firefox-web-fixture-safe-click-report` | 13/13 | **PASS** |
| `controlled-safe-click-report` | 11/11 | PASS |
| `click-preflight-synthetic-positive` | 12/12 | PASS |
| `mercadolibre-click-preflight-report` | 12/12 | PASS |
| `mercadolibre-action-policy-plan-report` | 10/10 | PASS |
| `browser-session-repeat-stability` | 18/18 | PASS |

### Tests
- Safety: 16/16 PASS
- Recipes: 55/55 PASS (includes 4 new SafeClickPolicy)

### Build
0 errors, 0 warnings

---

## Files

### Modified
- `src/OneBrain.Cli/Safety/WebTargetResolver.cs` — +SelectedBoundingRect
- `src/OneBrain.Cli/Recipes/RecipeRunner.cs` — boundingRect + ambiguity→blocked
- `tests/OneBrain.Recipes.Tests/SafeClickPolicyTests.cs` — +4 hardening tests

### New
- `tools/recipes/firefox-external-noncommercial-safe-click-report.json`
- `docs/hitos/hito-041-042-firefox-safe-click-hardening-external-noncommercial.md`
