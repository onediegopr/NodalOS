# M663 Public Manifest Regression QA

Decision: `M663 CERRADO / PUBLIC_MANIFEST_REGRESSION_QA_READY`

## Static Validation

Internal manifest static validation remains required and passed in this milestone through JSON parsing and unchanged baseline hash checks in safety tests.

Public manifest static validation is required and passed through JSON parsing and safety tests that reject wildcard public host permissions and wildcard public content script matches.

## Internal Candidate Regression

The internal candidate remains GO. `manifest.json` was preserved as the installed/internal baseline.

## Public Variant QA Plan

Manual QA is still required before any public release decision:

- Extension reload.
- Runtime tab.
- Service Worker DevTools.
- CSP violation check.
- Allowed origin check.
- Disallowed origin check.
- Bridge liveness.
- `invalid_token` absence.
- Reconnect storm absence.
- Critical console error absence.

## Public Release Status

Public Release: NO-GO.

Chrome Web Store: NO-GO.

The public variant is a candidate artifact only, not a public release package.

## Rollback

Rollback target is the existing internal candidate manifest baseline. If public variant QA fails, public candidate evidence is invalidated and distribution remains blocked.
