# HITO-050 - Full Recipe Cleanup Audit to Zero

## Objetivo

Llevar el riesgo de cleanup del pack automatico de recipes a cero sin agregar features nuevas ni cerrar ventanas de usuario por heuristicas inseguras.

Regla aplicada:

- Toda recipe automatica/regression que abre app o browser debe cerrar su sesion antes de assertions finales.
- Toda recipe que no puede garantizar cleanup seguro queda clasificada como `manual`, `legacy`, `diagnostic`, `negative`, `dry-run-only` o `external-fragile`.
- Explorer no se cierra sin ownership real.
- Notepad con buffer sucio no se cierra automaticamente porque puede disparar dialogos de guardado.
- No se usan `taskkill`, kill global ni cierres por titulo/proceso generico.

## Cambios aplicados

- `edge-example-smoke.json`: convertido a regression con sesion owned, lectura por session y `browser.close` antes de assert.
- `browser-session-example.json`: mueve `browser.close` antes de `assert-title`.
- `browser-session-no-stale-window.json`: mueve `browser.close` antes de `assert-example`.
- `calculator-to-notepad-variables.json`: mueve `app.close calculator` antes de assert final; Notepad sigue abriendose y cerrandose en blanco.
- `web-fixture-safe-click-report.json`: corrige acentos reales, agrega metadata regression, fuerza cleanup antes de asserts finales.
- `firefox-web-fixture-safe-click-report.json`: corrige acentos reales, agrega metadata regression, fuerza cleanup antes de asserts finales.
- Recipes con Notepad sucio, Explorer sin ownership, samples negativos o dry-run quedaron marcadas fuera de regresion automatica.
- Recipes externas de safe-click sobre `example.com` quedaron marcadas como `external-fragile`.

## Resultado de auditoria

- Total recipes auditadas: 59.
- Riesgo automatico pendiente: 0.
- Recipes con cleanup incompleto pero fuera de automatico: clasificadas como `manual`, `legacy`, `negative` o `dry-run-only`.
- Recipes externas/read-only: clasificadas como `external-fragile` en este documento aunque puedan seguir siendo utiles como smoke manual/controlado.

## Validacion ejecutada

- `dotnet build OneBrain.slnx`: 0 errores, 0 warnings.
- `dotnet test`: 82/82 pass.
- `controlled-safe-click-report.json` dry-run: pass, `safe.click` sensible.
- `controlled-safe-click-report.json` run: pass, Calculator cerrado.
- `edge-example-smoke.json`: pass, browser session owned cerrada antes del assert.
- `browser-session-example.json`: pass, browser session owned cerrada antes del assert.
- `browser-session-no-stale-window.json`: pass, browser session owned cerrada antes del assert.
- `browser-session-repeat-stability.json`: pass.
- `calculator-to-notepad-variables.json`: pass, Calculator y Notepad blanco cerrados.
- `web-fixture-safe-click-report.json`: pass, browser session owned cerrada antes de asserts finales.
- `firefox-web-fixture-safe-click-report.json`: pass, browser session owned cerrada antes de asserts finales.

Observacion post-smoke: se detectaron procesos no atribuibles a estas recipes (`Notepad` con titulo `*ss56: Bloc de notas`, `msedge` sin titulo y `ApplicationFrameHost` con `Peliculas y TV`). No fueron cerrados porque no hay ownership de ONE BRAIN.

## Tabla de clasificacion

| Recipe | Tipo | Abre | Cierra | Riesgo | Accion |
|---|---|---|---|---|---|
| action-policy-plan-synthetic-positive.json | regression | - | - | low | keep automatic |
| approval-deny-negative.json | negative | app:notepad | - | classified | not automatic |
| approval-required-sample.json | manual | app:notepad | - | classified | not automatic |
| assert-contains-fail.json | regression | - | - | low | keep automatic |
| assert-contains-pass.json | regression | - | - | low | keep automatic |
| assert-fail.json | regression | - | - | low | keep automatic |
| browser-close-not-owned-negative.json | negative | - | browser | low | not automatic |
| browser-read-example.json | regression | browser:edge | browser | low | keep automatic |
| browser-read-wikipedia.json | external-fragile | browser:edge | browser | low | not automatic |
| browser-session-example.json | regression | browser:edge | browser | low | keep automatic |
| browser-session-no-stale-window.json | regression | browser:edge | browser | low | keep automatic |
| browser-session-repeat-stability.json | regression | browser:edge, browser:edge, browser:edge | browser, browser, browser | low | keep automatic |
| browser-session-timeout-negative.json | negative | browser:__NONEXISTENT_BROWSER__ | - | classified | not automatic |
| browser-smoke.json | legacy | browser:edge | - | classified | not automatic |
| browser-timeout-hard-negative.json | negative | - | - | low | not automatic |
| browser-timeout-negative.json | negative | - | - | low | not automatic |
| calculator-to-notepad.json | legacy | app:calculator, app:notepad | - | classified | not automatic |
| calculator-to-notepad-variables.json | regression | app:calculator, app:notepad | app:calculator, app:notepad | low | keep automatic |
| click-preflight-synthetic-positive.json | regression | - | - | low | keep automatic |
| conditional-contains.json | regression | - | - | low | keep automatic |
| conditional-equals-else.json | regression | - | - | low | keep automatic |
| conditional-equals-then.json | regression | - | - | low | keep automatic |
| conditional-exists.json | regression | - | - | low | keep automatic |
| conditional-missing-variable.json | regression | - | - | low | keep automatic |
| controlled-safe-click-report.json | regression | app:calculator | app:calculator, app:calculator | low | keep automatic |
| debug-hang-timeout-negative.json | negative | - | - | low | not automatic |
| dry-run-safety-sample.json | dry-run-only | app:calculator, browser:edge | - | classified | not automatic |
| edge-example-smoke.json | regression | browser:edge | browser | low | keep automatic |
| eleconomista-readonly-report.json | external-fragile | browser:edge | browser | low | not automatic |
| explorer-temp-smoke.json | manual | app:explorer | - | classified | not automatic |
| firefox-accessibility-diagnostics.json | diagnostic | browser:firefox | browser | low | not automatic |
| firefox-external-noncommercial-safe-click-report.json | external-fragile | browser:firefox | browser | classified | not automatic |
| firefox-launch-diagnostics.json | diagnostic | browser:firefox, browser:firefox, browser:firefox | browser, browser, browser | low | not automatic |
| firefox-noncommercial-safe-click-report.json | external-fragile | browser:firefox | browser | classified | not automatic |
| firefox-web-fixture-diagnostics.json | diagnostic | browser:firefox | browser | low | not automatic |
| firefox-web-fixture-safe-click-report.json | regression | browser:firefox | browser | low | keep automatic |
| invalid-kind.json | regression | - | - | low | keep automatic |
| invalid-kind-stop-negative.json | negative | - | - | low | not automatic |
| mercadolibre-action-policy-plan-report.json | external-fragile | browser:edge | browser | low | not automatic |
| mercadolibre-click-preflight-report.json | external-fragile | browser:edge | browser | low | not automatic |
| mercadolibre-product-extract-report.json | external-fragile | browser:edge | browser | low | not automatic |
| mercadolibre-product-readonly.json | external-fragile | browser:edge | browser | low | not automatic |
| mercadolibre-readonly-search-report.json | external-fragile | browser:edge | browser | low | not automatic |
| msaa-example-diagnostics.json | diagnostic | browser:edge | browser | low | not automatic |
| msaa-web-fixture-diagnostics.json | diagnostic | browser:edge | browser | low | not automatic |
| noncommercial-web-safe-click-report.json | external-fragile | browser:edge | browser | classified | not automatic |
| notepad-report-smoke.json | manual | app:notepad | - | classified | not automatic |
| product-search-report.json | external-fragile | browser:edge | browser | low | not automatic |
| real-smoke-windows-pack.json | legacy | app:calculator, app:notepad, app:explorer, browser:edge | app:calculator, browser | classified | not automatic |
| recorded-notepad-sample.json | legacy | app:notepad | - | classified | not automatic |
| template-missing-variable.json | regression | - | - | low | keep automatic |
| template-validation-negative.json | negative | - | - | low | not automatic |
| unsupported-action-kind-negative.json | negative | - | - | low | not automatic |
| visual-browser-capture.json | regression | browser:edge | browser | low | keep automatic |
| web-example-profile-report.json | legacy | browser:edge, app:notepad | browser | classified | not automatic |
| web-fixture-safe-click-report.json | regression | browser:edge | browser | low | keep automatic |
| web-real-task-pack.json | legacy | browser:edge, browser:edge, app:notepad | browser, browser | classified | not automatic |
| web-wikipedia-profile-report.json | external-fragile | browser:edge | browser | low | not automatic |
| wikipedia-public-readonly-report.json | external-fragile | browser:edge | browser | low | not automatic |

## Pendiente

- AppSessionTracker real para poder cerrar Explorer solo si fue abierto por ONE BRAIN.
- Politica explicita para Notepad con buffer sucio: guardar en archivo controlado, descartar con aprobacion explicita o no cerrar.
- Enforced metadata en tooling/CI para que `autoRegression=false` no dependa solo de documentacion.
- Separar carpetas fisicas `regression`, `manual`, `legacy`, `diagnostic`, `negative` si el pack crece.
- Mantener external-web como smoke controlado, no como regresion deterministica.
