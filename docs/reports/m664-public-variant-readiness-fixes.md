# M664 Public Variant Readiness Fixes

Decision: `M664 CERRADO / PUBLIC_VARIANT_READINESS_FIXES_READY`

## Scope

M664 accepts the M664A `AUDIT_CONDITIONAL_GO` and closes the audit follow-up as documentation, artifacts, and safety evidence only. It does not modify `manifest.json`, `manifest.public.json`, sidepanel HTML/CSS/JS, service worker, content script, recipe core, bridge source, CSP, runtime, provider/cloud, filesystem, browser automation, or capability unlock.

## Known Limitations

The public variant intentionally omits automatic `content_scripts`. This prevents broad website injection by default and keeps the public candidate permission posture narrow.

Expected limitations:

- Public build does not auto-access websites.
- Site-specific page features are unavailable by default.
- Site-specific features require a future user-granted origin flow.
- Local bridge and Runtime tab features remain local-first.
- Unavailable page features are expected in the public candidate.

## Microcopy Contract

No UI was changed in this milestone. Required future copy:

- "Public build does not auto-access websites."
- "Site-specific features require future user-granted access."
- "Local bridge features remain local-first."
- "Unavailable page features are expected in this public candidate."

## Permission Justification

The public variant currently declares `activeTab`, `scripting`, `storage`, `tabs`, `sidePanel`, `alarms`, and localhost/127.0.0.1 host permissions. These remain permitted for public candidate prep only and require final store disclosure before release.

## Naming Review

`NODAL OS Public Candidate` is valid for QA. It is not approved as final Chrome Web Store naming.

## Release State

Internal Candidate: GO.

Public Build Candidate: GO for prep.

Public Release: NO-GO.

Chrome Web Store: NO-GO.
