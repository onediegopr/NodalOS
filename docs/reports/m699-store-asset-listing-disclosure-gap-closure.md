# M699 Store Asset / Listing / Disclosure Gap Closure

Milestone: M699

Decision: `STORE_ASSET_LISTING_DISCLOSURE_GAP_CLOSURE_READY`

## Final Naming

Recommended final Store name: `NODAL OS`.

`NODAL OS Public Candidate` remains valid for QA only and should not be used as the final Store listing name.

## Store Listing Draft

NODAL OS is a local-first Chrome companion for connecting a browser side panel to a local NODAL OS bridge.

The public build does not auto-access websites and does not include broad website host permissions.

The extension works with a local bridge on localhost or 127.0.0.1.

The public build does not auto-inject content scripts into external websites.

Runtime productive capabilities, provider/cloud features, filesystem access, browser automation, and capability unlock remain disabled until future gates.

## Permission Disclosure Draft

- `storage`: local extension state.
- `sidePanel`: Chrome side panel UI.
- `alarms`: local extension scheduling and health checks.
- `tabs`: UI coordination and future user-approved flows; no broad website auto-access by default.
- `scripting`: future user-approved flows; no automatic external injection in public build.
- host permissions: limited to `http://127.0.0.1/*` and `http://localhost/*` for local bridge communication.

## Open Store Gaps

- privacy URL pending;
- support URL pending;
- docs URL pending;
- screenshots/assets pending;
- final permission disclosure review pending.

## Store Status

Chrome Web Store remains NO-GO.
