# HITO-053+054 - Retail Category/Product Read-Only Pack

## Scope

Public retail category read-only validation for:

- HITO-053: Suministros Roca Uruguay category `https://suministrosroca.uy/categoria-producto/pisos/`
- HITO-054: Sodimac Uruguay category `https://www.sodimac.com.uy/sodimac-uy/category/cat20668/pisos-y-revestimientos/`

Both URLs were discovered from public home HTML links and verified with direct `GET 200` before creating the recipes.

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

- `tools/profiles/web/suministrosroca-uy-category.json`
- `tools/profiles/web/sodimac-category.json`
- `tools/recipes/suministrosroca-category-readonly-report.json`
- `tools/recipes/sodimac-category-readonly-report.json`

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

Suministros Roca category is approved if the public category page loads, visible text is readable, retail/category signals are reported, and the owned browser session closes.

Sodimac category is approved if the public category page loads and readable text is obtained. If the site blocks by cookies, geolocation, antibot or network policy, the hito is diagnostic only: no interaction is allowed and the owned browser session must still close.

## Pending after this hito

- Optional product-level read-only recipes for stable direct product URLs.
- CI/tooling enforcement that retail read-only recipes contain no click/type/invoke/safe.click actions.
