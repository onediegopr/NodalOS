# M97-M99: NODAL OS Global Rename

## Status

Accepted for M97-M99.

## Decision

NODAL OS is the official product name.

NEXA is the historical product name and must no longer appear in operator-facing copy, readiness text, blocker explanations, live target visible metadata, or new roadmap documents except where explicitly marked as legacy or compatibility.

## Renamed Surfaces

The following visible surfaces were moved to NODAL OS language:

- Chrome/bridge console text.
- Human handoff messages.
- Consent and vault authorization titles.
- Private preview readiness copy.
- Admin placeholder organization display names.
- Email default notification subject.
- Vercel test-owned target HTML metadata and titles.
- Vercel target runbook.
- Roadmap vNext language.

## Legacy Compatibility

Some technical identifiers intentionally remain temporarily:

- `Nexa*` classes, records, tests, filters, and file names.
- `apps/nexa-test-owned-target` source folder.
- Historical docs under `docs/adr`, `docs/hitos`, `docs/audits`, and `docs/architecture`.
- Historical ledger/proof ids and artifact content.
- Legacy env var aliases `NEXA_EXTERNAL_LIVE_PROOF_OPT_IN` and `NEXA_CHROME_BRIDGE_TOKEN`.

These are compatibility identifiers, not current product branding.

## Env Vars

New preferred env var:

- `NODAL_OS_EXTERNAL_LIVE_PROOF_OPT_IN=true`
- `NODAL_OS_CHROME_BRIDGE_TOKEN=<token>`

Temporary compatibility alias:

- `NEXA_EXTERNAL_LIVE_PROOF_OPT_IN=true`
- `NEXA_CHROME_BRIDGE_TOKEN=<token>`

If both live proof opt-in variables are absent, live proof remains disabled.

## What Does Not Change

- Domain remains `https://lab.nodalos.com.ar`.
- Branch remains `origin/chrome-lab-001-extension-local-ai-bridge`.
- Historical commit messages are not rewritten.
- Historical ledger refs are not rewritten.
- M51 scope remains HTTP read-only only.
- M65 remains deferred.
- Chrome/CDP external proof is not declared.
