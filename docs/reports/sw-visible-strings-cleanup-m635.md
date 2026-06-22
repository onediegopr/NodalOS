# M635 - Service Worker Visible Strings Cleanup

## Decision

M635 CERRADO / SW_VISIBLE_STRINGS_CLEANUP_READY

## Scope

This milestone performs a narrow cleanup of user-visible service worker strings that still used NEXA naming.

Modified product file:

- `browser-extension/onebrain-chrome-lab/service_worker.js`

No other product files were modified.

## Strings Changed

- `NEXA` was changed to `NODAL OS` only in visible messages, log-adjacent notices, error messages, and human-handoff copy.
- Visible Spanish accent fixes were applied only inside the same edited string literals.
- The English restricted-tab copy now says `NODAL OS can operate`.

## Protected Compatibility Keys

The following compatibility identifiers were intentionally preserved:

- `nexaRecipes`
- `nexaLearningDraft`
- `nexaRuntimeState`
- `nexa.keepalive`
- `nexa.content.ping`
- `NEXA_RECIPE_*`
- `onebrain-sidepanel`
- `PROTOCOL_VERSION`
- `chrome-lab-v1`

## Explicit Non-Changes

- No storage keys changed.
- No protocol changed.
- No message types changed.
- No port names changed.
- No alarm names changed.
- No WebSocket URL or connection logic changed.
- No retry logic changed.
- No runtime state machine changed.
- No tool validation changed.
- No capabilities changed.
- No permissions, host permissions, CSP, content script behavior, or recipe logic changed.

## Product Boundary

Unchanged product files:

- `browser-extension/onebrain-chrome-lab/manifest.json`
- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `browser-extension/onebrain-chrome-lab/content_script.js`
- `browser-extension/onebrain-chrome-lab/recipe_core.js`

## Go / No-Go

- CSP tightening candidate: GO for a future dedicated milestone.
- Public release: NO-GO.
- JS changes: NO-GO.
- Runtime changes: NO-GO.
- Manual reload QA after service worker visible strings cleanup: required.

Recommended next milestone: M635A Manual Reload QA After SW Visible Strings Cleanup.
