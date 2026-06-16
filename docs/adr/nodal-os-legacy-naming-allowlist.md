# NODAL OS Legacy Naming Allowlist

## Purpose

This allowlist documents where historical NEXA naming may remain after M97-M99.

The allowlist prevents old branding from leaking back into visible operator-facing surfaces while preserving technical compatibility and audit history.

## Allowed Legacy Categories

| Category | Allowed examples | Reason | Duration |
|---|---|---|---|
| Technical symbols | `Nexa*` classes, records, enums, tests, methods, filters | Avoid breaking internal APIs, test filters, and historical references during the rename block | Temporary; remove in later technical cleanup |
| Legacy env var aliases | `NEXA_EXTERNAL_LIVE_PROOF_OPT_IN`, `NEXA_CHROME_BRIDGE_TOKEN` | Compatibility for existing live proof and Chrome bridge operator scripts | Temporary; deprecate after migration window |
| Historical docs | `docs/adr`, `docs/hitos`, `docs/audits`, `docs/architecture` | Preserve original decisions and audit trail | Permanent historical record |
| Historical roadmap reconciliation | Docs that explicitly state NEXA was the former name | Explain migration and legacy context | Permanent historical record |
| Vercel source folder | `apps/nexa-test-owned-target` | Existing local folder and historical deployment source name; Vercel real project is not changed by this block | Temporary; rename only in a separate deploy-safe block |
| Historical artifacts | `artifacts/m91-m93-live-proof-ledger` with old proof ids if present | Ledger and evidence must not be rewritten | Permanent audit record |
| Test data and fixtures | synthetic ids such as `nexa-test-owned.example.invalid` where not user-facing | Compatibility test fixtures | Temporary |

## Disallowed Outside Allowlist

NEXA must not appear as current product branding in:

- operator-facing messages;
- readiness dashboard copy;
- blocker explanations;
- Vercel target visible HTML metadata;
- new roadmap documents except migration context;
- new runbook user instructions except legacy context;
- evidence display names.

## Removal Plan

After M97-M99, a future cleanup may:

- rename `Nexa*` technical symbols to `NodalOs*`;
- rename the test-owned target folder;
- remove the legacy env var alias;
- update historical tests that still use NEXA as synthetic text.

That cleanup must be separate from security/proof work.
