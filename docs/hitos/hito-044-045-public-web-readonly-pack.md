# HITO-044+045 — Public Web Read-Only Pack

**Status**: In Progress  
**Branch**: `hito-044-045-public-web-readonly` (pending commit)

---

## Objective

Validate ONE BRAIN on public informational websites in read-only mode — no clicks, no interactions.

---

## HITO-044: Wikipedia

- URL: `https://en.wikipedia.org/wiki/Artificial_intelligence`
- Browser: Edge
- Mode: read-only

### Results

| Metric | Value |
|--------|-------|
| Title | |
| Text sample | |
| Actions count | |
| Navigation candidates | |
| Auth related | |
| Payment related | |
| NavPlan hasExecutableActions | |

---

## HITO-045: elEconomista.es

- URL: `https://www.eleconomista.es/`
- Browser: Edge
- Mode: read-only, cookie/popup tolerant

### Results

| Metric | Value |
|--------|-------|
| Title | |
| Text sample | |
| Cookie signals | |
| Login signals | |
| Subscription signals | |
| Actions count | |
| NavPlan hasExecutableActions | |

---

## Compliance

| Restriction | Complied |
|------------|----------|
| 0 clicks | |
| 0 cookies accepted | |
| 0 popups closed | |
| 0 login | |
| 0 forms | |
| 0 carrito/compra/pago | |
| 0 JS/DevTools/Selenium/OCR | |

---

## Files

### New
- `tools/profiles/web/wikipedia-ai.json`
- `tools/profiles/web/eleconomista-home.json`
- `tools/recipes/wikipedia-public-readonly-report.json`
- `tools/recipes/eleconomista-readonly-report.json`
- `docs/hitos/hito-044-045-public-web-readonly-pack.md`
