# NODAL OS HITO-162 Replacement Sequence - M130-M132

## Decision

Do not resume HITO-162 as-is.

HITO-162 is rewritten as a NODAL OS roadmap sequence. The recovered intent points to Identity/Fingerprint v2 and adjacent robust perception work, not production launch, SaaS public, public API, real credentials, sensitive sites, or mutating actions.

## Current Baseline

- Product name: NODAL OS
- HITO-162 state: rewritten legacy intent, not blindly resumed
- M51: closed with HTTP read-only target-owned ledger scope
- M65: closed with limited target-owned Chrome/CDP/DOM read-only ledger scope
- External CDP general-ready: false
- Private preview local: stable internal local preview
- Core remains authoritative
- UI/Admin/Companion observe and transport only

## Replacement Sequence

### M133-M135 - Identity/Fingerprint v2 Local Fixture-First

Goal: rebuild HITO-162 as local, deterministic identity hardening.

Status after M133-M135: implemented as fixture-first contracts, evaluator, harness, and redacted evidence model. It is an informational Core input only and does not authorize actions.

Scope:

- approved target identity model
- fingerprint evidence for local fixture surfaces
- stale/shifted target detection
- Core-governed identity checks before action
- evidence refs for identity decisions
- no external general CDP
- no real credentials
- no mutating actions

### M136-M138 - Robust Perception Stabilization

Goal: implement the adjacent robust perception line safely.

Scope:

- WindowLivenessMonitor
- SystemOverlayDetector
- UIA empty/block detection
- SemanticAccessFallback
- OCR regional read-only as auxiliary evidence
- vision region verification as auxiliary evidence
- no OCR/vision authority over Core

### M139-M141 - Safe Action Expansion Design and Local Fixtures

Goal: prepare safe action categories without opening dangerous actions.

Allowed design-only/local fixture candidates:

- safe.select
- safe.download metadata-only
- safe.upload synthetic-only
- safe.form.fill synthetic-only without submit
- safe.modal.confirm for non-destructive local fixtures only

Blocked:

- submit/pay/sign/delete
- production recorder/replay
- sensitive sites
- real credentials

### M142-M144 - Process Memory and Workflow Learning, Local-Only

Goal: revive the process memory line under current evidence rules.

Scope:

- flow ledger
- process memory
- repeated workflow learning
- recipe suggestions
- no sensitive raw storage
- no public SaaS
- no external general automation

### M145+ - Embedded Runtime Evaluation Only If Needed

Embedded runtime/WebView2/CEF remains future-only.

Chromium fork is not planned unless a hard limitation is proven and documented.

## Active Blockers

- No SaaS public
- No public API real
- No billing real
- No email real
- No real customer credentials
- No sensitive sites
- No submit/pay/sign/delete
- No productive recorder/replay
- No external CDP general-ready claim
- No new external targets without dedicated evidence
- No embedded runtime now
- No Chromium fork planned now

## Updated Percentages

- NODAL OS general: 98%
- Private preview local: 98%
- Product/Admin: 98%
- Security/evidence integrity: 95-97%
- Roadmap legacy reconciliation: 95-97%
- HITO-162 replacement readiness: 70-80%

The HITO-162 replacement readiness is an estimated roadmap readiness value. It means the sequence is documented and ready to plan; it does not mean HITO-162 functionality is implemented.

## Roadmap Rule

Future work must continue using the current Core authority model, evidence ledger discipline, local-first fixtures, and explicit scope locks. Target-owned proof must not be generalized into production, SaaS public, sensitive-site access, or external CDP general-ready.
