# M623 - Sidepanel Contrast / Responsive Microfix CSS-Only

Decision target: `SIDEPANEL_CONTRAST_RESPONSIVE_MICROFIX_READY`.

## Scope

M623 is a small CSS-only patch. It modifies only `sidepanel.css`.

Touched selectors:

- `.tab.active`
- `.stop-button`
- `@media (max-width: 430px) .stop-button`
- `@media (max-width: 430px) .app-header`

## Contrast Fix

Before:

- `.tab.active` used `#171717` text on `#5B6CFF`.
- Estimated contrast: 4.3:1.

After:

- `.tab.active` uses white text on a token-mixed darker accent surface.
- Estimated surface: `#4B58C7`.
- Estimated contrast: 5.97:1.

This keeps the active tab visually navigational because the shape, height, grid placement, and tab context remain unchanged.

## STOP Responsive Fix

Before:

- M621 screenshot QA showed STOP clipping at 420px.

After:

- `.stop-button` keeps its text and behavior.
- Width and padding are reduced.
- A small right margin prevents the button text from touching the viewport edge.
- A narrow viewport media query further reduces button width, height, padding, and type size and tightens header spacing.
- Static 420px screenshot after the patch shows the STOP text no longer clipped.

No event handlers, IDs, data attributes, enabled state, or command behavior changed.

## Not Touched

- No HTML.
- No JavaScript.
- No manifest.
- No runtime behavior.
- No consent behavior.
- No provider/model behavior.
- No source-of-truth behavior.

The responsive header adjustment is local to the existing header and does not alter DOM structure, event behavior, control state, or command semantics.

## Remaining Work

- Interactive installed-extension QA is still required before any HTML, JS, or manifest change.
- Future work must remain separately approved if it touches layout structure or behavior.
