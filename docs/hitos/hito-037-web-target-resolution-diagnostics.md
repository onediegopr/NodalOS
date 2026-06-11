# HITO-037 — Web Target Resolution Diagnostics

## 1. Objetivo
Mejorar la capacidad de encontrar targets clickeables dentro de páginas web en Edge, con diagnostics claros.

## 2. WebTargetResolver
Itera TODOS los HWNDs de un proceso browser (no solo el main window), busca descendientes UIA con límite 500, recoge diagnostics:
- candidateCount
- selectedName / selectedControlType
- hasInvoke / hasClickablePoint
- windowsSearched

## 3. Resultados reales en example.com
- Windows searched: 2
- Candidate count: 0
- Target "More information..." not found in Edge UIA tree
- Edge no expone el link in-page incluso con `--force-renderer-accessibility`

## 4. No hacks
- No JS injection
- No DevTools / CDP click
- No OCR click
- No coordenadas arbitrarias
- No Selenium
- No bypass de seguridad

## 5. Controlled safe.click en Calc sigue PASS (11/11)
- safeClick.executed=true
- Before: 0 → After: 7

## 6. HITO-038 deferred
Non-commercial web safe click queda diferido por limitación de UIA accessibility tree en Edge.

Próximo bloque: HITO-038+039 — Web Accessibility Fallback / Non-Commercial Click Strategy.
