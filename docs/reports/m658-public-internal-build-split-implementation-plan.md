# M658 Public/Internal Build Split Implementation Plan

Decision: `M658 CERRADO / PUBLIC_INTERNAL_BUILD_SPLIT_IMPLEMENTATION_PLAN_READY`

## Scope

M658 defines how NODAL OS should split the current internal candidate line from a future public build line. It is planning-only and does not modify `manifest.json`, host permissions, content script matches, JavaScript, bridge source, CSP, runtime, provider, filesystem, browser automation, or release packaging.

## Internal Build

The internal build is the existing local-first evidence candidate. It preserves the current broad HTTP/HTTPS host permission posture for controlled internal QA and evidence capture.

Allowed internal-build traits:

- Uses current installed-extension evidence line.
- Keeps broad host permissions only under controlled internal candidate scope.
- Requires bridge liveness and runtime/DevTools evidence.
- Remains blocked from public Chrome Web Store release.
- Keeps runtime/provider/cloud/filesystem/browser automation disabled.

## Public Build

The public build is a future separate candidate with a narrowed manifest strategy. It must not inherit broad wildcard host permissions without a separate explicit release approval.

Expected public-build traits:

- Separate build channel identity.
- Public manifest variant defined before implementation.
- Host permissions narrowed or moved behind optional/user-granted origin flow.
- Content script matches narrowed or isolated per build.
- Store disclosure, QA, rollback, and regression evidence required.

## Split Strategy

Recommended strategy: `manifest variant contract + build channel boundary`, followed by a future implementation patch.

Implementation path:

1. Preserve current internal candidate as the evidence baseline.
2. Define internal/public channel taxonomy.
3. Define manifest variant contract without creating the public manifest yet.
4. Define public patch readiness gate.
5. Implement the public variant only in M661-M663 after explicit approval.

## Prohibited Differences

The public/internal split must not change runtime behavior, provider/cloud availability, filesystem access, bridge source, CSP, or product JavaScript without a dedicated future milestone.

## Minimum QA

Each future build variant must pass build, safety tests, extension reload QA, Runtime tab evidence, service worker DevTools evidence, product boundary checks, and rollback verification.

## Decision

Internal Candidate: GO.

Public Release: NO-GO.

Public Manifest Patch: CONDITIONAL-GO for M661-M663 only after readiness gate acceptance.
