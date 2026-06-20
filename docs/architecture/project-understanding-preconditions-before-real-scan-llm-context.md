# ADR — Governed Project Understanding Preconditions Before Real Scan / LLM Context

**Project:** NODAL OS
**ADR ID:** adr-governed-project-understanding-preconditions-m536
**Milestone:** M536
**Status:** Accepted
**Date:** 2026-06-20

---

## Context

NODAL OS has completed the Assignment/Planner Preview governance phase (M519-M533). That phase established:

- Assignment Engine v1 contracts (non-executable, draft-only).
- TaskGraph draft (no execution authority; planner runtime not implemented).
- Mission Plan Preview (static/redacted HTML; evidence refs only; claims unverified).
- Review Cards, UI Preview, No-Op Interactions (all visual/mock; no operative wiring).
- Review Persistence Mock, Handoff Contract, Safety Audit Pack (mock persistence only).
- History Mock, Handoff Compare Preview, Planner Governance Closeout (ref-only; closeout is not runtime permission).

The project now prepares for its next governed phase. Project Understanding (real filesystem scan, indexing, embeddings, LLM context build) is the natural next capability, but it introduces high-risk surfaces:

- **Real filesystem access** exposes secrets, binary content, and sensitive paths.
- **LLM context building** requires BYOK configuration, provider policy, prompt governance, and budget enforcement.
- **Indexing and embeddings** require size and scope controls, secret redaction, and structured evidence plans.
- **Cloud sync** violates the no-cloud-by-default invariant unless explicitly gated.

None of these capabilities can be enabled without extensive preconditions that maintain the local-first, no-execution, no-cloud-by-default, policy-first invariants of NODAL OS.

---

## Decision

**NODAL OS will NOT advance directly to real Project Understanding.**

A Governed Project Understanding Preconditions phase (M534-M536) defines all required gates explicitly. Each gate must be implemented in a dedicated future milestone with its own audit, consent mechanism, and evidence plan. Until then:

1. **NODAL OS does not proceed directly to Project Understanding real.** All current capabilities are precondition definitions only.

2. **Real scan/file read/index/embedding/LLM context build blocked.** These remain blocked until future governed milestones define and implement each required capability with its associated policy, consent, and audit.

3. **Assignment/Planner outputs are refs and governance context only.** All M519-M533 outputs can feed Project Understanding as governance context refs but cannot serve as execution authority, LLM prompt source, or filesystem authority.

4. **Mock history/handoff is not source of truth.** Mock Review History, Handoff Compare, and Governance Closeout must never be promoted to productive data sources without an explicit migration audit.

5. **BYOK/secret/provider/prompt/budget required before LLM real.** Any LLM context build, prompt generation, or provider call requires: BYOK configuration, provider policy definition, prompt governance framework, and budget enforcement.

6. **Path jail/consent/scan preview/cancellation/audit required before filesystem real.** Any real filesystem scan requires: path jail implementation, explicit user consent, scan scope preview, cancellation semantics, secret detection, exclusion policy, and a dedicated implementation audit.

7. **Positive execution gate required before runtime real.** Any real execution requires the positive execution gate ADR (defined in AUDIT-A-FIX-3) plus a separate architectural and security audit.

---

## Consequences

### Positive

- All capabilities remain blocked by structural preconditions, not just documentation.
- Assignment/Planner outputs cannot accidentally become operational without explicit promotion gates.
- Each future real capability requires its own milestone, audit, consent gate, and evidence plan.
- The local-first, no-cloud-by-default, policy-first invariants are preserved.
- No accidental LLM/filesystem/runtime wiring can occur through the current contract surface.

### Negative

- Project Understanding real capability is deferred; timeline depends on future milestones.
- Multiple sequential gates (path jail → consent → preview → secret detection → audit → scan) extend the pre-scan timeline.
- BYOK and provider policy definition require additional design work before any LLM context build.

---

## Accepted Alternatives

- Define preconditions fully before any real capability (accepted — this ADR).
- Maintain Assignment/Planner outputs as governance context only (accepted).
- Require a dedicated audit before any real scan, LLM, or runtime (accepted).

## Rejected Alternatives

- Proceed directly to real filesystem scan without preconditions (rejected — violates policy-first invariant).
- Use Assignment/Planner mock history as LLM prompt source (rejected — mock is not authoritative).
- Reuse governance closeout as runtime permission (rejected — closeout is not execution authorization).
- Enable cloud sync by default for Project Understanding (rejected — violates no-cloud-by-default invariant).
- Build LLM context without BYOK and provider policy (rejected — BYOK required before provider calls).

---

## Guardrails

| ID | Description | Enforced Structurally | Requires Separate Milestone |
|---|---|---|---|
| `guardrail-no-real-scan-until-preconditions-met` | Real filesystem scan blocked until path jail, consent, scope preview, secret detection, exclusion policy, cancellation semantics, and audit are all implemented and validated. | yes | yes |
| `guardrail-no-llm-until-byok-policy` | LLM context build, prompt generation, and provider calls blocked until BYOK configuration, provider policy, prompt governance, and budget enforcement are implemented. | yes | yes |
| `guardrail-no-filesystem-mutation` | Any scan or read operation must be structurally read-only. No file writes, no git commits, no state changes to the scanned workspace. | yes | no |
| `guardrail-no-cloud-by-default` | No cloud upload by default. Cloud sync requires a separate explicit policy, consent, and milestone. | yes | yes |
| `guardrail-assignment-outputs-are-refs-only` | All Assignment/Planner M519-M533 outputs can only be used as governance context refs. Cannot serve as execution authority, LLM prompt source, or filesystem authority. | yes | no |
| `guardrail-separate-audit-before-runtime` | A dedicated architectural and security audit of any real scan, LLM, or runtime implementation must be completed before enabling the capability. | no | yes |

---

## Required Next Milestones

| Milestone | Purpose | Blocks Real Scan | Blocks LLM Context | Blocks Runtime |
|---|---|---|---|---|
| M537+ Path Jail Implementation | Real path jail validation with defined boundaries, symlink protection, and escape prevention. | yes | no | no |
| M537+ Consent UI and Scope Preview | Explicit user consent mechanism and scan scope preview (file count, size estimate, excluded patterns). | yes | no | no |
| M537+ Secret Detection Implementation | Real secret and credential detection before any path, filename, or content is included in context. | yes | yes | no |
| M537+ BYOK and Provider Policy | BYOK configuration, provider policy, prompt governance, and budget enforcement before any LLM context build or provider call. | no | yes | no |
| M537+ Real Scan Implementation Audit | Dedicated architectural and security audit of real scan implementation before enabling it. | yes | yes | yes |
| M537+ Positive Execution Gate | Define and implement the positive execution gate (policy + approval + verification) required before any runtime execution. | no | no | yes |

---

## Explicit Non-Goals

This ADR does NOT implement:

- Real filesystem scan
- Real directory listing
- File read or file hashing
- Git commands
- Embeddings
- Index creation
- LLM context building
- Prompt generation
- Provider calls
- BYOK
- Cloud sync
- Runtime execution
- A real planner
- Promotion of Assignment/Planner mock outputs to operational authority

---

## Evidence and Timeline Refs

- `evidence-adr-m536-ref-only`
- `evidence-m531-m533-governance-closeout-ref`
- `evidence-m534-preconditions-ref-only`
- `timeline-adr-created-m536`
- `timeline-next-phase-decision-recorded`
