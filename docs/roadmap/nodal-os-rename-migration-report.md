# NODAL OS Rename Migration Report M97-M99

## Names

- Official name: NODAL OS
- Former name: NEXA
- Slug: `nodal-os`

## Scope

M97-M99 updates visible product naming while preserving technical compatibility.

Renamed:

- operator-facing handoff and consent text;
- private preview readiness text;
- bridge console text;
- Vercel test-owned target visible metadata;
- current runbook language;
- roadmap/migration docs.

Preserved as legacy:

- `Nexa*` technical symbols and tests;
- historical ADRs, hitos, audits, and architecture docs;
- `apps/nexa-test-owned-target` folder name;
- legacy env var aliases `NEXA_EXTERNAL_LIVE_PROOF_OPT_IN` and `NEXA_CHROME_BRIDGE_TOKEN`;
- historical proof IDs and ledger refs.

## Compatibility

Preferred live proof opt-in:

- `NODAL_OS_EXTERNAL_LIVE_PROOF_OPT_IN=true`
- `NODAL_OS_CHROME_BRIDGE_TOKEN=<token>`

Temporary legacy alias:

- `NEXA_EXTERNAL_LIVE_PROOF_OPT_IN=true`
- `NEXA_CHROME_BRIDGE_TOKEN=<token>`

No opt-in means no live proof execution.

## Current Roadmap Status

- M51: closed with HTTP read-only external proof scope only.
- M65: deferred, `DeferredNeedsDedicatedEvidence`.
- HITO-162: paused/not forgotten, `UnknownNeedsAudit`.
- External Chrome/CDP/DOM proof: pending.

## Next Steps

1. Keep the visible product name as NODAL OS.
2. Use the naming audit to prevent new visible NEXA branding.
3. Plan a later technical symbol cleanup for `Nexa*` identifiers.
4. Do not mix technical symbol cleanup with security proof or external live proof work.
