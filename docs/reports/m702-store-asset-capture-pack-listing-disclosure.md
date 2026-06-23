# M702 Store Asset Capture Pack + Listing / Disclosure Finalization

Milestone: M702

Decision: `STORE_ASSET_CAPTURE_PACK_LISTING_DISCLOSURE_READY`

## Store Listing Final Draft

Name: `NODAL OS`

Short description: Local-first Chrome companion for NODAL OS and its local bridge.

Long description:

NODAL OS is a local-first Chrome companion that connects the browser side panel to a local NODAL OS bridge. The public build does not auto-access websites and does not request broad website host permissions. The public build communicates only with localhost or 127.0.0.1 for local bridge workflows. The public build does not auto-inject content scripts into external websites. Runtime productive capabilities, provider/cloud features, filesystem access, browser automation, and capability unlock remain disabled until future gates and approvals.

## Permission Disclosure Final

- `storage`: local extension state and UI preferences.
- `sidePanel`: NODAL OS companion interface.
- `alarms`: local scheduling and health checks.
- `tabs`: extension UI coordination and future user-approved flows.
- `scripting`: reserved for future user-approved flows; no automatic external injection in public build.
- host permissions: limited to `http://127.0.0.1/*` and `http://localhost/*` for local bridge communication.

## Asset Pack

Required assets remain pending:

- extension loaded screenshot;
- sidepanel connected screenshot;
- runtime tab evidence;
- service worker DevTools evidence;
- permission warning if shown;
- final icon set;
- final support/docs destination.

Chrome Web Store remains NO-GO.
