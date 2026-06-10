# HITO-026 — Failure Artifacts Hardening + Static Template Validation

## 1. Objetivo
Hardening post-failure: metadata estructurada, screenshot best-effort, cleanup owned sessions, static template validation en dry-run.

## 2. Failure artifacts

- Directorio por falla: `artifacts/failures/{ts}-{recipe}-{step}/`
- `failure.json`: timestamp, step index/kind, error, sessions, screenshot path
- `screenshot.png`: fullscreen fallback si session HWND no disponible
- `snapshot.txt`: dump de variables `.text`/`.title`
- `{ts}-cleanup.json`: resultado de cleanup de sesiones owned

## 3. Static template validation

`RecipeRunner.ValidateTemplates(recipe)` escanea `{{...}}` patterns, compara contra built-in + recipe variables + inferred step outputs (profile.load, browser.open, snapshot.read). Warnings en dry-run, no bloqueante en v1.

## 4. Limitaciones

- Screenshot best-effort post-mortem; sin session owned HWND usa fullscreen
- Template validation no es order-aware estricto (prefijo-basado)
- Warnings no bloquean ejecucion
- Artifacts locales gitignored
