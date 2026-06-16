# NODAL OS Core Private Preview Audit Pack M118-M120

## Canonical State

- Commit: `2811615c00f342e5b030b4515aaa3815263a98ef`
- Worktree: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo-m12-audit`
- Branch/remoto canónico: `origin/chrome-lab-001-extension-local-ai-bridge`
- Product name: NODAL OS
- Legacy compatibility names: NEXA only where allowlisted

## Evidence Status

- M51: closed with HTTP read-only target-owned proof and persisted ledger.
- M65: closed with limited target-owned Chrome/CDP/DOM read-only proof and persisted ledger.
- M65 ledger ref: `audit-ledger-edb3e2fbb0a0446788dae17a269c0058`
- M65 ledger hash: `61f52af1eebf08d59a24e5fbb72e70acf0038e7a329bff6599a0ac00c757f03e`

## Product/Admin Private Preview

- Product/Admin private preview status: ReadyWithRestrictions.
- Operator runbook status: available.
- Release gate status: ReadyWithRestrictions.
- External CDP general-ready: false.

## Suite Counts

- OneBrain.Safety.Tests: 1305 passed, 29 skipped, 0 failed.
- OneBrain.Recipes.Tests: 635 passed, 0 skipped, 0 failed.
- Skipped tests current: 29.
- Skipped audit declared: 29.

## Active Blockers

- SaaS public blocked.
- Public API real blocked.
- Real billing/email blocked.
- Real credentials blocked.
- Sensitive sites blocked.
- Submit/pay/sign/delete blocked.
- Productive recorder/replay blocked.
- External CDP general-ready blocked.
- New external targets require dedicated evidence.
- UI/Admin/Companion have no authority.

## Roadmap State

- HITO-162: paused/not forgotten/UnknownNeedsAudit.
- Embedded runtime/WebView2/CEF: future, not now.
- Chromium fork: not planned unless a hard limitation appears.

## Allowed Preview Scope

- Internal local private preview only.
- Local Product/Admin shell.
- Private local API in-process.
- Local diagnostics/evidence review.
- Local issue triage.

## Denied Scope

- Production.
- SaaS public.
- External general CDP.
- Third-party/sensitive sites.
- Real credentials.
- Billing/email real.
- Submit/pay/sign/delete.
- Recorder/replay productive mode.

## Known Risks

- M65 proof is target-owned only and must not be interpreted as general external CDP readiness.
- HITO-162 has not been rewritten yet.
- Technical `Nexa*` symbols remain as compatibility/internal names.

## Recommended Next Blocks

- External audit review using `docs/audits/claude-audit-prompt-private-preview-m120.md`.
- Post-audit remediation if Claude finds blockers.
- HITO-162 rewrite/map only after audit.
