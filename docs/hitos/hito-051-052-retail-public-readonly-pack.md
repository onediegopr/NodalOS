# HITO-051+052 - Retail Public Read-Only Pack

## Scope

Public retail read-only validation for:

- HITO-051: Suministros Roca Uruguay, `https://suministrosroca.uy`
- HITO-052: Sodimac public home, primary `https://www.sodimac.com.uy`, fallback documented as `https://www.sodimac.com.ar`

## Safety contract

- No clicks.
- No `safe.click`.
- No invoke.
- No type.
- No submit.
- No cookies accepted.
- No popups closed.
- No geolocation/store selection.
- No login or registration.
- No cart, checkout, purchase or payment.
- No WhatsApp open.
- No forms sent.
- Browser session must close before assertions/reporting.

## Files

- `tools/profiles/web/suministrosroca-uy-home.json`
- `tools/profiles/web/sodimac-public-home.json`
- `tools/recipes/suministrosroca-readonly-report.json`
- `tools/recipes/sodimac-readonly-report.json`

## Recipe pattern

The recipes use only:

- `profile.load`
- `browser.open`
- `delay`
- `browser.read`
- `browser.close`
- `extract.visiblefields`
- `discover.actionableelements`
- `plan.safenavigation`
- supported assertion equivalents
- `note`

`assert.exists` is not a supported RecipeRunner action in this codebase, so the recipes use the existing `if` condition with `operator: exists` and an `assert.equals` branch.

## Expected outcomes

Suministros Roca is approved if the public site loads, visible text is readable, retail signals are reported, and the owned browser session closes.

Sodimac is approved as read-only if the public site loads and readable text is obtained. If the site blocks by cookies, geolocation, antibot or network policy, the hito is diagnostic only: no interaction is allowed and the owned browser session must still close.

## Pending after this hito

- Optional fallback recipe/profile for `https://www.sodimac.com.ar` only if Uruguay fails.
- CI/tooling enforcement that retail read-only recipes contain no click/type/invoke/safe.click actions.
