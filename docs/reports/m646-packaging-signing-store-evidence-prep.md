# M646 Packaging / Signing / Store Evidence Prep

Decision: `M646 CERRADO / PACKAGING_SIGNING_STORE_EVIDENCE_PREP_READY`

## Scope

M646 prepares packaging, signing, store, privacy, support, rollback, and release note evidence.

It does not package a final release, sign with production credentials, upload to a store, publish, enable provider/cloud, or modify product files.

## Packaging Checklist

- Confirm release candidate commit.
- Confirm extension manifest version candidate.
- Confirm package contents inventory.
- Confirm no development-only artifacts in package.
- Confirm no secrets or API keys in package.
- Confirm product boundary hashes.
- Confirm release evidence artifacts are available.

Current state: prepared, not final.

## Version Candidate Checklist

- Candidate commit must be selected.
- Candidate version must be tied to a release evidence pack.
- Candidate cannot ship public release while host permissions and store review remain open.

Current state: not finalized.

## Signing Requirements

- Chrome Web Store signing or internal distribution signing path must be selected.
- Credentials must not be exposed in repo, artifacts, logs, prompts, or screenshots.
- Any signing action requires a separate release milestone.

Current state: requirements documented only.

## Chrome Web Store Readiness

Required before public release:

- Store listing copy.
- Permission justification.
- Privacy disclosure.
- Support URL.
- Screenshots.
- Release notes.
- Store policy review.
- Rollback path.

Current state: not ready for public release.

## Internal Distribution Readiness

Internal distribution can continue only as an evidence/local-first candidate if:

- public release remains disabled,
- provider/runtime remain disabled,
- host permissions are clearly documented,
- users understand local bridge dependency.

## Privacy / Support Disclosure Draft

Privacy disclosure must state:

- broad host permissions are present,
- provider/cloud is disabled for release,
- runtime productive execution is disabled,
- local bridge is required for the installed extension evidence path,
- no public provider/BYOK or filesystem execution is enabled.

Support URL remains required before public release.

## Rollback Plan

Rollback must include:

- prior known-good commit,
- removal/unload instructions for installed extension,
- bridge stop procedure,
- release notes correction procedure,
- store delisting or unpublishing path if applicable.

## Release Notes Draft

Draft release note:

NODAL OS installed extension evidence candidate validates local bridge connectivity, runtime tab visibility, and clean service worker DevTools evidence. Public release remains blocked pending host permissions, store packaging/signing, provider/runtime disabled-state proof, and final closure audit.

## Store Listing Risks

Open risks:

- host permissions breadth,
- local bridge dependency explanation,
- provider/runtime disabled-state messaging,
- privacy disclosure completeness,
- support URL readiness,
- packaging/signing process not executed.

## Go / No-Go

- Evidence prep: GO.
- Public release: NO-GO.
- Packaging finalization: NO-GO until separate release closure milestone.
