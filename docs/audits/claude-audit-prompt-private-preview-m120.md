# Claude Audit Prompt - NODAL OS Private Preview M120

Audit NODAL OS critically before internal local private preview.

Do not provide generic approval. Look for scope inflation, missing evidence, leaks, false readiness claims, and release blockers.

## Context

- Product: NODAL OS.
- Commit: `2811615c00f342e5b030b4515aaa3815263a98ef`
- Worktree: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo-m12-audit`
- Branch/remoto: `origin/chrome-lab-001-extension-local-ai-bridge`
- Current release gate: `ReadyWithRestrictions`

## Evidence

- M51 closed with limited HTTP read-only target-owned proof and persisted ledger.
- M65 closed with limited target-owned Chrome/CDP/DOM read-only proof and persisted ledger.
- M65 ledger ref: `audit-ledger-edb3e2fbb0a0446788dae17a269c0058`
- M65 ledger hash: `61f52af1eebf08d59a24e5fbb72e70acf0038e7a329bff6599a0ac00c757f03e`
- External CDP general-ready: false.

## Active Blocks

- No SaaS public.
- No public API real.
- No billing/email real.
- No real credentials.
- No sensitive sites.
- No submit/pay/sign/delete.
- No productive recorder/replay.
- No external CDP general-ready.
- No new external targets without dedicated evidence.
- UI/Admin/Companion have no authority.

## Files To Review

- `docs/audits/nodal-os-core-private-preview-audit-pack-m118-m120.md`
- `artifacts/private-preview-audit-pack-m118-m120/audit-summary.json`
- `docs/adr/local-private-preview-final-review-m118-m120.md`
- `docs/adr/local-private-preview-release-gate-m115-m117.md`
- `docs/runbooks/nodal-os-private-preview-operator-runbook.md`
- `src/OneBrain.BrowserExecutor.Cdp/NodalOsPrivatePreviewReleaseServices.cs`
- `src/OneBrain.BrowserExecutor.Contracts/NodalOsPrivatePreviewReleaseContracts.cs`
- `src/OneBrain.BrowserExecutor.Cdp/M65DedicatedEvidenceServices.cs`
- `src/OneBrain.BrowserExecutor.Contracts/M65DedicatedEvidenceContracts.cs`

## Questions

1. Is the `ReadyWithRestrictions` scope valid for internal local private preview?
2. Is there any scope inflation from M51 or M65?
3. Can Product/Admin start private preview local without opening production surfaces?
4. Are there leaks or obvious risks in audit pack, summary JSON, runbook, evidence refs, or readiness outputs?
5. What would you block before internal tests?
6. What must remain closed?
7. Should the next block rewrite/map HITO-162, or should more hardening happen first?
8. What confidence level would you assign from 1 to 10?

## Required Response Format

Return:

1. Executive verdict.
2. Critical blockers.
3. High risks.
4. Medium risks.
5. Low risks.
6. Scope inflation findings.
7. Evidence integrity findings.
8. Product/Admin readiness findings.
9. Operator UX findings.
10. Release gate findings.
11. What must not be opened.
12. Recommended next block.
13. Confidence score 1-10.
