# M629 - Extension Legacy Naming Inventory + NODAL OS Minimum Naming Cleanup

Decision target: `EXTENSION_LEGACY_NAMING_MINIMUM_CLEANUP_READY`

## Scope

M629 audited legacy extension naming and applied the smallest safe visible naming patch for the installed Chrome extension shell.

The patch is intentionally narrow:

- Chrome extension visible metadata now says `NODAL OS`.
- Sidepanel shell already used `NODAL OS`.
- Two visible consent microcopy mojibake strings were corrected.
- Compatibility identifiers remain unchanged.
- No runtime behavior, protocol, storage, port, alarm, permission, or sidepanel logic was changed.

## User Evidence

The user reported that the extension loaded after selecting:

`C:\DESARROLLO\NodalOS\Codigo-m12-audit\browser-extension\onebrain-chrome-lab`

The reported installed extension still showed legacy `NEXA` naming. The user also supplied compatibility identifiers observed in the extension code:

- `nexaRecipes`
- `nexaLearningDraft`
- `nexaRuntimeState`
- `nexa.keepalive`
- `onebrain-sidepanel`

## Inventory Summary

### `manifestMetadata`

Renamed now:

- Manifest visible name: `NEXA` to `NODAL OS`.
- Manifest short name: added `NODAL OS`.
- Manifest visible description: `NEXA` to `NODAL OS`.
- Manifest action title: `NEXA` to `NODAL OS`.

### `storageCompatibilityKey`

Kept unchanged:

- `nexaRecipes`
- `nexaLearningDraft`
- `nexaRuntimeState`

Reason: these are persisted compatibility keys. Renaming them without a migration would risk data loss or state ambiguity.

### `runtimeProtocolIdentifier`

Kept unchanged:

- `nexa.keepalive`
- recipe schema constants using `NEXA`
- generated DOM id prefix using `nexa`

Reason: these identifiers are behavior-adjacent and require a dedicated compatibility audit.

### `portOrMessageIdentifier`

Kept unchanged:

- `onebrain-sidepanel`
- `nexa.content.ping`

Reason: these link extension surfaces internally and renaming would be functional, not visible metadata cleanup.

### `userVisibleNaming`

Deferred:

- Runtime-adjacent messages in the background service file still include `NEXA`.

Reason: those strings live in behavior code. M629 is not a JS functional or runtime-adjacent microcopy patch.

### `testFixtureOrHistoricalDoc`

Deferred:

- Extension developer documentation still contains `ONE BRAIN`.

Reason: documentation history is outside the installed extension visible shell cleanup.

## Product Changes

Modified:

- `browser-extension/onebrain-chrome-lab/manifest.json`
- `browser-extension/onebrain-chrome-lab/sidepanel.html`

Not modified:

- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/content_script.js`
- `browser-extension/onebrain-chrome-lab/recipe_core.js`
- Background service code

## Manifest Before / After

Before:

- `name`: `NEXA`
- `short_name`: absent
- `description`: `Local-first Chrome lab client for NEXA.`
- `action.default_title`: `NEXA`

After:

- `name`: `NODAL OS`
- `short_name`: `NODAL OS`
- `description`: `Local-first Chrome lab client for NODAL OS.`
- `action.default_title`: `NODAL OS`

## Protected Manifest Fields

Confirmed unchanged:

- `manifest_version`
- `version`
- `permissions`
- `host_permissions`
- `background`
- `content_scripts`
- `side_panel`
- `content_security_policy`

No permission, host permission, background entry, sidepanel path, content script, or policy change was made.

## HTML Microcopy

Corrected visible mojibake only:

- `autorizaciÃ³n` to `autorización`
- `InstrucciÃ³n` to `Instrucción`

No HTML structure changed. No ids, classes, scripts, handlers, or data attributes changed.

## Compatibility Boundaries

Confirmed unchanged:

- Storage keys.
- Port names.
- Alarm names.
- Protocol version.
- Existing socket URL construction.
- Runtime message types.
- Sidepanel connection logic.
- Recipe schema identifiers.

## Manual Reload QA

Created:

- `artifacts/agent-operations/m629/manual-qa-reload-checklist-after-naming.json`

Manual reload must confirm:

- The installed extension visible name is `NODAL OS`.
- The sidepanel heading remains `NODAL OS`.
- Main shell no longer shows `NEXA`.
- Console has no critical errors after reload.

## Go / No-Go

HTML minimum patch:

- NO-GO until manual reload QA verifies installed extension naming and no critical console errors.

Manifest/naming:

- GO only for the completed minimum metadata cleanup in this milestone.
- Further manifest work remains NO-GO until a separate extension review milestone.

JS changes:

- NO-GO.

Runtime/provider/cloud/filesystem/productive consent/capability/source-of-truth:

- NO-GO.

## Decision

M629 is ready to close as:

`M629 CERRADO / EXTENSION_LEGACY_NAMING_MINIMUM_CLEANUP_READY`
