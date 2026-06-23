# M655 Public Build Manifest Strategy

Decision: `M655 CERRADO / PUBLIC_BUILD_MANIFEST_STRATEGY_READY`

## Scope

M655 is planning and manifest strategy only. It does not modify `manifest.json`, host permissions, `content_scripts.matches`, permissions, JavaScript, bridge source, CSP, runtime, provider/cloud, filesystem, packaging, or public release state.

## Current Manifest State

Current internal candidate manifest posture:

- `host_permissions`: `http://*/*`, `https://*/*`
- `content_scripts.matches`: `http://*/*`, `https://*/*`

This remains valid for the controlled internal/local-first candidate only.

## Internal Build vs Public Build

Internal build:

- can retain broad permissions for evidence review,
- remains local-first and controlled,
- is not a public Chrome Web Store candidate.

Public build:

- should use a narrower permission posture or explicit store-approved justification,
- requires its own manifest strategy,
- requires separate QA and store evidence.

## Narrowing Options

Options reviewed:

- keep wildcard permissions only for internal build,
- limit public build to localhost and `127.0.0.1` if content script behavior allows it,
- use `optional_host_permissions` for user-granted origins,
- separate content script matches by build,
- use future user-granted origins,
- retain wildcard with strong Chrome Web Store justification.

## Recommendation

Recommended strategy: `split_internal_public_build`.

Recommended next milestone: `M658 Public/Internal Build Split Implementation Plan`.

Alternative if the team chooses to patch immediately: `M658 Host Permissions Narrowing Patch Plan`.

## Go / No-Go

Internal candidate: GO.

Public release: NO-GO.
