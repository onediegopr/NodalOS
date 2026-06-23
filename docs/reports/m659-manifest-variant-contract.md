# M659 Manifest Variant Contract

Decision: `M659 CERRADO / MANIFEST_VARIANT_CONTRACT_READY`

## Scope

M659 defines the future internal/public manifest variant contract. It does not create a new manifest file and does not modify `manifest.json`, host permissions, content script matches, permissions, CSP, service worker, content script, sidepanel, recipe core, or bridge source.

## Internal Manifest Preservation

The internal manifest remains the current installed-extension evidence baseline. Its broad HTTP/HTTPS host permission posture is preserved only for controlled internal candidate use.

## Public Manifest Target Shape

The future public manifest should reduce store risk by using one of these strategies:

- Narrow host permissions to local bridge surfaces where viable.
- Move broad site access behind `optional_host_permissions`.
- Use `activeTab` or user-granted origins where feature requirements allow.
- Split content script matches by build channel.
- Keep broad wildcard access only if a future final public-store justification explicitly approves it.

## Target Recommendation

Recommended target: `split_internal_public_build_with_public_optional_permissions_and_narrow_default_matches`.

This keeps the public default posture narrower while allowing a future user-granted origin model if browser-side features need it.

## Required QA

Any future manifest patch requires:

- Extension reload QA.
- Runtime tab verification.
- Service worker DevTools verification.
- Content script regression QA on allowed origins.
- Negative QA on disallowed origins.
- Store disclosure review.
- Rollback verification.

## Release State

Internal Candidate: GO.

Public Release: NO-GO.

Public Manifest Patch: CONDITIONAL-GO for M661-M663 after M660 readiness gate.
