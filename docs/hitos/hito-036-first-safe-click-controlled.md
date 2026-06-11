# HITO-036 — First Safe Click Controlled

## 1. Objetivo
Ejecutar el primer click real de ONE BRAIN en un contexto seguro y controlado.

## 2. Logro
Calculator "Siete" button clicked via `safe.click` pipeline:
- Preflight: requiresReview (unknown target, allowed in controlled mode)
- Approval: executionAllowedInThisHito=true (controlled mode)
- safe.click: executed=true
- Before: "La pantalla muestra 0" → After: "La pantalla muestra 7"

## 3. Step kinds implementados
- `preflight.click` - evaluates target safety
- `approval.manifest` - generates auditable manifest (mode-based execution allowance)
- `safe.click` - executes click only after preflight+approval checks

## 4. HITO-037 deferred
Web content target finding limited in Edge UIA for in-page elements. Deferred to HITO-037+038 — Web Target Resolution + Non-Commercial Safe Click.

## 5. Safety
- No clicks on commercial/dangerous/auth targets
- executionAllowedInThisHito=false for commercialWeb
- All safety guards active (MinimalSafetyGuard, approval, foreground check)
