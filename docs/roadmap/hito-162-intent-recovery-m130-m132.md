# HITO-162 Intent Recovery - M130-M132

## Status

HITO-162 remains a legacy milestone: paused/not forgotten/UnknownNeedsAudit.

This document does not resume or close HITO-162. It reconstructs its likely intent from available evidence and maps that intent to the current NODAL OS roadmap.

## Evidence Found

- `docs/hitos/hito-161-approved-input-binding-unification.md` is the last standalone reliable milestone document found before the pause.
- HITO-161 was completed as approved input binding unification.
- The HITO-161 next-step note identifies `H162 - Identity/Fingerprint v2`.
- The same note identifies `H163 - UIA truncation detection`.
- `docs/architecture/one-brain-engine-master.md` shows the next legacy roadmap phase after HITO-161 as robust perception.
- The robust perception line included WindowLivenessMonitor, SystemOverlayDetector, UIA empty/block detection, SemanticAccessFallback, OCR regional read-only, and vision region verification.
- `docs/roadmap/nodal-os-roadmap-reconciliation-m94-m96.md` keeps HITO-162 visible as paused/not forgotten.
- `docs/roadmap/nodal-os-legacy-hito-absorption-matrix.md` classifies HITO-162 as UnknownNeedsAudit.
- `docs/roadmap/nodal-os-roadmap-vnext.md` preserves HITO-162 as a legacy roadmap debt.

## Evidence Missing

- No standalone HITO-162 implementation document was found.
- No standalone HITO-163 implementation document was found.
- No acceptance criteria were found for Identity/Fingerprint v2.
- No proof was found that HITO-162 was implemented, reviewed, or closed.
- No evidence was found that HITO-162 intended to open SaaS public, public API, billing/email real, real credentials, sensitive sites, or submit/pay/sign/delete.

## Last Reliable Point

HITO-161 is the last reliable point before the legacy pause. Its documented next step makes HITO-162 a probable Identity/Fingerprint v2 milestone, not a broad product launch milestone.

## Probable Intent

The likely HITO-162 intent was to improve robust target identity after input binding unification:

- stable element/window identity across UI changes
- fingerprinting for approved input targets
- stronger detection of stale, hidden, blocked, or shifted surfaces
- continuity between perception evidence and safe action gating
- preparation for UIA truncation detection and other perception hardening in later milestones

Because the legacy roadmap also references robust perception immediately after HITO-161, HITO-162 should be treated as part of a perception/identity hardening sequence rather than resumed as a single isolated feature.

## Original Dependencies

- HITO-161 approved input binding unification
- Core authority over action decisions
- evidence-led action binding
- local/sandbox browser runtime readiness
- safe action policy gates
- UIA and window liveness signals

## Absorbed by Current NODAL OS Work

The Browser Runtime and private preview line absorbed several enabling concerns that HITO-162 would have depended on:

- M51 closed external HTTP read-only proof with target-owned ledger evidence
- M65 closed limited target-owned Chrome/CDP/DOM read-only proof with ledger evidence
- Browser Runtime local/sandbox remains controlled by Core
- Product/Admin private preview local is stable under ReadyWithRestrictions
- release gate, evidence freeze, ledger verification, and skipped category audit now protect private preview scope
- operator-facing blocker explanations now make blocked surfaces explicit

## Still Valid

The following legacy intent remains valid:

- Identity/Fingerprint v2 for local approved surfaces
- WindowLivenessMonitor
- SystemOverlayDetector
- UIA empty/block detection
- SemanticAccessFallback
- OCR regional read-only as auxiliary evidence only
- vision region verification as auxiliary evidence only
- safe action identity checks before any action

## Obsolete or Unsafe to Resume As-Is

The following assumptions are invalid in the current roadmap:

- treating target-owned external proof as external general-ready
- enabling SaaS public or public API as part of HITO-162
- using real credentials or sensitive sites
- enabling submit/pay/sign/delete
- treating UI/Admin/Companion as authoritative
- using external Chrome/CDP proof beyond the approved target-owned scope
- adding embedded runtime or Chromium fork work inside this sequence

## Risks of Blind Resumption

- scope inflation from Identity/Fingerprint v2 into broad external automation
- bypassing the current Core authority model
- confusing M51/M65 target-owned evidence with general external readiness
- reintroducing legacy assumptions without ledger-backed evidence
- mixing perception hardening with unsafe actions

## Preliminary Recommendation

Do not resume HITO-162 as written. Rewrite it into a NODAL OS sequence:

- M133-M135: Identity/Fingerprint v2 local fixture-first, with Core-governed evidence gates
- M136-M138: robust perception stabilization for liveness, overlays, UIA empty/block detection, and auxiliary OCR/vision
- M139-M141: safe action expansion design and local fixture validation only

HITO-162 is now rewritten as roadmap intent, not implemented functionality.

