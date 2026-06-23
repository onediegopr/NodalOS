# M649 Release Scope Decision

Decision: `M649 CERRADO / RELEASE_SCOPE_DECISION_READY`

## Scope

M649 decides the release scope that is possible now without changing product files or publishing.

## Decision Matrix Summary

Internal Evidence Candidate: GO.

Local-first Internal Candidate: GO.

Private Beta Candidate: CONDITIONAL-GO only if audience, support path, and distribution controls are explicitly approved.

Public Chrome Web Store Candidate: NO-GO.

Public Release: NO-GO.

## Recommended Decision

`INTERNAL_CANDIDATE_GO / PUBLIC_RELEASE_NO_GO`

This decision matches the evidence state: installed extension evidence and clean Runtime/DevTools checks are ready, but public release blockers remain open.

## Required Conditions For Public Candidate

- Host permissions final decision.
- Packaging candidate artifact.
- Signing path evidence.
- Store listing, privacy disclosure, support URL, screenshots, and assets.
- Provider/runtime disabled-state caveats in release copy.
- Final public release audit.
