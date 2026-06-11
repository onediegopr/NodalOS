# HITO-034+035 — Safe Click Preflight + Approval Policy v2

## 1. Objetivo
Evaluar clicks antes de ejecutarlos. Sin acciones reales. Solo preflight y manifiesto.

## 2. Step kinds

### `preflight.click`
Evalúa si un clic sobre targetText sería seguro. Output: decision (blocked/requiresApproval/allowedForFuture/requiresReview), riskCategory, riskLevel, evidence.

### `approval.manifest`
Genera manifiesto de aprobación auditable desde evidencia preflight. Siempre `executionAllowedInThisHito=false`.

## 3. Reglas
- payment/dangerous/auth → blocked
- navigation → requiresApproval
- safe-readonly → allowedForFuture (pero no executable)
- unknown → requiresReview

## 4. No se ejecutan clicks reales en este hito.
