# HITO-042 — External Network Launch Diagnostics

**Status**: **CLOSED**  
**HITO-043**: DEFERRED

---

## Diagnostics Results

| # | URL | Title | Diagnosis |
|---|-----|-------|-----------|
| 1 | `file://` fixture | "Mozilla Firefox"¹ | Default tab, URL not navigated |
| 2 | `http://httpbin.org/` | "Mozilla Firefox" | Default tab, URL not navigated |
| 3 | `https://example.com` | "Mozilla Firefox" | Default tab, URL not navigated |

¹ Earlier manual tests showed "ONE BRAIN Web Safe Click Fixture" for `file://` and "Problema al cargar la página" for `https://example.com`. In this automated test, all 3 showed "Mozilla Firefox" — Firefox opened but stayed on default page without navigating to the URL.

## Root Cause

Firefox via Process.Start **does not consistently navigate** to the URL argument. File was consistent in earlier tests but external URLs never load. Hypothesis:

1. **Process.Start context differs** from manual launch (TLS/proxy/certificate context)
2. Firefox may cache session state across launches, ignoring the URL argument
3. `-new-window` flag behavior is inconsistent across Firefox sessions

No system configuration was modified.

## What was NOT done

| Restriction | Complied |
|------------|----------|
| 0 system network config changes | Yes |
| 0 proxy changes | Yes |
| 0 certificate installation | Yes |
| 0 external clicks | Yes |
| 0 JS/DevTools/Selenium/OCR | Yes |

## Next Steps

- HITO-043 external safe click deferred
- Revisit Firefox launch with fresh profile (`-no-remote -CreateProfile`)
- Consider WebDriver/Marionette for reliable navigation
- Edge remains the supported browser for read-only/preflight

## Files

- `tools/recipes/firefox-launch-diagnostics.json` (new)
- `docs/hitos/hito-042-firefox-external-network-launch-diagnostics.md` (new)
