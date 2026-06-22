# M631 - Installed Extension Visual QA Closeout / Claude Audit Prep

Decision target: `INSTALLED_EXTENSION_VISUAL_QA_CLOSEOUT_READY`

## Timeline

- M615-M618: CSS token patches for the sidepanel.
- M619-M620: visual QA fixes and cleanup.
- M621-M623: screenshot QA, dead-style cleanup, and responsive contrast microfixes.
- M624: installed-extension interactive QA readiness was prepared.
- M625: manual QA runbook and enablement were documented.
- M626: manual evidence contract was formalized.
- M627: HTML/manifest readiness gate was established.
- M628: initial installed-extension QA evidence capture was blocked by missing real evidence.
- M629: visible naming cleanup corrected `NEXA` to `NODAL OS` in the manifest and fixed visible mojibake in the sidepanel.
- M630: user-reported manual reload QA closed the line with the statement `probé la extensión y está perfecta`.

## What Was Tested

- Installed extension visual state after the CSS-only Research OS migration.
- Visible naming cleanup for `NODAL OS`.
- Manual reload QA after the naming fix.
- Boundary checks that product files stayed unchanged after the closeout evidence was recorded.

## Real Evidence

- The user reported that the extension was loaded, reloaded, and visually correct.
- The user reported the visible naming state as correct.
- The closeout package records that no screenshots were provided.
- The closeout package records that no console logs were provided.

## User-Reported Evidence

- M630 remains user-reported manual QA, not screenshot-based QA.
- M630 is the positive manual evidence point that closes the installed-extension visual line.

## What Was Not Verified

- No screenshots were provided.
- No console log transcript was provided.
- No additional browser automation evidence was captured in this closeout milestone.

## Final State

### Naming

- `NODAL OS` visible naming is treated as validated by the user-reported QA closeout.
- The legacy `NEXA` naming is not treated as a product release signal.

### CSS Research OS

- The CSS-only Research OS migration is treated as closed on the evidence timeline.
- No further CSS changes are authorized by this milestone.

### HTML

- HTML minimum patch remains a future candidate only.
- This milestone does not authorize automatic HTML changes.

### JS

- JS remains NO-GO.
- No JS functional changes are authorized by this milestone.

### Manifest

- Manifest additional changes remain NO-GO unless a dedicated milestone is created.

## Open Risks

- The closeout is based on user-reported evidence rather than screenshots and console logs.
- Compatibility naming keys may still exist internally and should remain under a dedicated review boundary.
- Further manifest or HTML work needs separate audit scope.

## Final Go / No-Go

- Go for closeout and audit prep: yes.
- Go for HTML minimum patch: candidate only, not now.
- Go for JS: no.
- Go for runtime: no.
- Go for additional manifest changes: no.

## Claude Audit Recommendation

Prepare Claude review against the installed-extension visual/naming line before any product changes after M631.
The audit should verify that the closeout evidence is sufficient, identify remaining visible legacy naming risks, and confirm that JS/runtime/manifest scope stays blocked until a separate milestone is approved.
