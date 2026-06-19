# NODAL OS Naming Cleanup M464-LITE

## Motive

M464-LITE corrects naming contamination introduced while absorbing an external RPA plan that used the NODRIX name. In this repository, branch, and worktree the operational project name is NODAL OS.

This cleanup does not implement features, change architecture, or touch runtime behavior.

## Search Performed

The cleanup searched `docs`, `artifacts`, `tests`, and `src` for:

- `NODRIX`
- `Nodrix`
- `nodrix`

The search also checked recent references to the old ADR filename:

- `nodrix-automation-layer-decision-record.md`

## Files With NODRIX Found

Operational NODRIX references were found in:

- `docs/architecture/nodrix-automation-layer-decision-record.md`
- `docs/architecture/recipe-dsl-decision-record.md`
- `docs/backlog/runtime-gated-recipe-risk-classifier-hardening.md`
- M446-M463 reports.
- M451/M454/M457 artifacts.
- M449/M451, M446/M448, M452/M454, and M455/M457 documentation tests.
- Contract-only fixture/provenance strings in `OneBrain.AgentOperations.Core`.
- `docs/roadmap/nodal-os-roadmap-vnext.md`.

## Changes Applied

- Renamed `docs/architecture/nodrix-automation-layer-decision-record.md` to `docs/architecture/nodal-os-automation-layer-decision-record.md`.
- Replaced operational NODRIX references with NODAL OS in recent ADRs, reports, artifacts, roadmap entries, and documentation tests.
- Updated recent tests to assert the NODAL OS operational name instead of accepting NODRIX.
- Updated contract-only fixture/provenance strings from NODRIX to NODAL OS.
- Created M464-LITE artifact and naming guard tests.

## Changes Not Applied

- No broad rename of `OneBrain`.
- No broad rename of historical `Nexa*` symbols.
- Historical NEXA mentions in roadmap remain as legacy/compatibility notes, not as the operational project name.
- No product class, namespace, or API rename was performed.

## Operational Name Confirmation

The operational project name for this repo and branch is NODAL OS.

Preferred terms:

- NODAL OS Automation Layer.
- NODAL OS Mission Control.
- NODAL OS core.

## NODRIX / NEXA Mixing Confirmation

NODRIX is no longer used as the operational project/product name in `docs`, `artifacts`, `tests`, or `src`.

NEXA remains only as historical/compatibility debt where already documented.

## External Input Handling

If a future external input mentions NODRIX, it must be treated as source-input wording only and normalized to NODAL OS in repo documentation unless explicitly quoted as historical source context.

## Runtime / Architecture Confirmation

- Runtime behavior changed: false.
- Architecture changed: false.
- Feature implementation added: false.
- UI changed: false.
- Execution added: false.

## Final State

M464-LITE restores NODAL OS as the sole operational naming in recent docs, artifacts, tests, and contract-only metadata.
