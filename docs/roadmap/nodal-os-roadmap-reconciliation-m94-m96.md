# NODAL OS Roadmap Reconciliation M94-M96

## Scope

This document reconciles the legacy HITO roadmap line with the newer Browser Runtime, Chrome/CDP, product/admin, external proof, and private preview line.

The official product name is now NODAL OS. The codebase still contains historical NEXA and ONE BRAIN names. The global rename from NEXA to NODAL OS is pending and must not be mixed with security-critical proof work.

## Legacy Line Detected

The canonical worktree contains explicit roadmap evidence through:

- `docs/audits/roadmap-reconciliation-hito-129.md`
- `docs/architecture/one-brain-engine-master.md`
- `docs/hitos/hito-161-approved-input-binding-unification.md`

The reliable legacy sequence visible in this worktree reaches:

- HITO-129 reconciliation/reset.
- HITO-147 through HITO-161 in the master architecture roadmap.
- HITO-161 as a standalone implemented hito for Approved-Input Binding Unification.

No standalone canonical document for HITO-162, HITO-163, or post-162 was found in this worktree. Therefore HITO-162 is marked as paused/not forgotten, but not validated as the next correct implementation target.

## Last Reliable Point Before Pause

The last reliable legacy milestone in the current worktree is HITO-161.

HITO-161 established that `safe.type` requires manifest-bound approved input binding and that diagnostic runtime variables are not authority.

The next legacy phase listed after HITO-161 is perception robustness:

- WindowLivenessMonitor
- SystemOverlayDetector
- UIA empty/block detection
- SemanticAccessFallback
- OCR regional read-only
- Vision region verification

## Why The Legacy Line Paused

The program shifted into a Browser Runtime and external proof line because product safety required stronger evidence before expanding automation:

- Chrome extension authority had to be reduced.
- Core-governed Browser Runtime needed local/sandbox proof.
- Vault, audit ledger, leak hardening, local private preview, and API local boundaries needed hardening.
- External test-owned proof was missing and blocked broad external claims.

## New Line Opened

The newer line includes:

- Browser Runtime local/sandbox hardening.
- Chrome/CDP contracts, local CDP proof, and companion non-authority.
- Product/Admin private preview local.
- Private Local API with auth/tenant/rate-limit.
- Audit integrity key custody.
- External test-owned target preparation.
- External HTTP read-only proof against `https://lab.nodalos.com.ar`.

M51 is closed with strict scope:

- external HTTP read-only proof only;
- target test-owned: `https://lab.nodalos.com.ar`;
- `ProbeKind=RealHttpClient`;
- `Tooling=HttpReadOnlyExternal`;
- redacted evidence persisted to `BrowserPersistentAuditLedger`;
- no Chrome/CDP external navigation;
- no DOM read-only external proof.

M65 remains deferred:

- `DeferredNeedsDedicatedEvidence`;
- not closed by M51 HTTP proof;
- requires dedicated external low-risk/auth evidence.

## Relationship Between Lines

The Browser Runtime line did not invalidate HITO-161. It absorbed or superseded some old execution-safety concerns by adding stronger Core authority, audit, redaction, vault, and private-preview gates.

However, it did not replace all legacy roadmap items. Perception robustness and later safe action expansion remain relevant, but they must be rewritten against the NODAL OS evidence and Browser Runtime constraints.

## Risks Of Returning Blindly To HITO-162

Returning directly to HITO-162 would be risky because:

- HITO-162 is not present as a canonical standalone document in this worktree.
- The product now has new blockers and gates that did not exist in the legacy roadmap.
- M51 closure is HTTP-only and does not prove external Chrome/CDP/DOM.
- M65 is still deferred.
- Rename NEXA to NODAL OS is pending and should not be mixed with proof/security work.
- SaaS public, public API, billing real, email real, real credentials, sensitive sites, and submit/pay/sign/delete remain blocked.

## Reconciliation Recommendation

Do not resume HITO-162 blindly.

Treat HITO-162 as paused/not forgotten and require a rewrite or explicit mapping into the new roadmap.

Use the absorption matrix in `docs/roadmap/nodal-os-legacy-hito-absorption-matrix.md` as the authority for whether legacy items are absorbed, still valid, superseded, deferred, deprecated, or unknown.

Use `docs/roadmap/nodal-os-roadmap-vnext.md` for the next execution sequence.

