# M622 - Sidepanel Dead-Style Cleanup CSS-Only

Decision target: `SIDEPANEL_DEAD_STYLE_CLEANUP_READY`.

## Scope

M622 is a CSS-only cleanup. The only product file modified is `sidepanel.css`.

No HTML, JavaScript, or manifest file was modified.

## Verification Before Removal

The following legacy root variables were checked for exact `var(...)` references in `sidepanel.css`:

- `--bg`
- `--panel`
- `--ink`
- `--muted`
- `--line`
- `--green`
- `--red`
- `--amber`
- `--blue`
- `--black`

Each candidate had zero references before removal and one root declaration.

## Removed

Removed from `:root`:

- `--bg`
- `--panel`
- `--ink`
- `--muted`
- `--line`
- `--green`
- `--red`
- `--amber`
- `--blue`
- `--black`

No `--nos-*` token was removed.

## Expected Visual Impact

No visual change is expected because all removed variables had zero `var(...)` references in `sidepanel.css`.

## Not Touched

- No selectors.
- No visual remaps.
- No focus ring.
- No tabs.
- No badges.
- No timeline styles.
- No governance surfaces.
- No log/pre styles.
- No action button behavior or layout.

## Product Boundary

Unchanged:

- `sidepanel.html`
- `sidepanel.js`
- `manifest.json`

No runtime, provider/cloud coupling, operational access, productive consent, capability enablement, or product source-of-truth promotion was introduced.

## Remaining Risks

- `.tab.active` contrast remains a future visual fix candidate.
- STOP button narrow viewport clipping remains a future responsive QA candidate.
- HTML, JS, and manifest remain blocked pending separate approval and Claude review.
