# NODAL OS M477-M479 - AUDIT-A Pre-UI Boundary & Naming Hardening

## Executive Summary

M477-M479 closes the minimum pre-UI hardening items from AUDIT-A by adding architecture fitness tests, ADRs and a legacy quarantine plan.

The block does not implement UI, runtime execution, browser automation, cloud, LLM calls, scheduler/worker, recorder/replay, queue or DSL parser runtime.

Decision: `AUDIT_A_PRE_UI_BOUNDARY_NAMING_HARDENING_READY`.

## Initial Git State

- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`.
- Branch: `chrome-lab-001-extension-local-ai-bridge`.
- Base commit: `07ed349c1f927e27894fa9d9795d4815191deda9`.
- Remote: `https://github.com/onediegopr/NodalOS.git`.
- Forbidden path not used: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo`.

## Objective

Harden the boundary before UI real / Mission Control Shell so future UI cannot accidentally rely on safety by disconnection / disconnection-only safety.

## AUDIT-A Findings Covered

- HIGH A-H1: BrowserExecutor.Cdp runtime exists; added boundary tests and ADR guardrails so AgentOperations/future UI do not reference it directly.
- HIGH A-H2: legacy `Nexa*` sensitive subsystem exists; documented quarantine plan and cloud/licensing/BYOK blocker.
- MEDIUM A-M1: dual evidence-ref model; created evidence consolidation ADR.
- MEDIUM A-M2: `Nexa*` in AgentOperations.Contracts; documented as compatibility debt, no broad rename.
- MEDIUM A-M3: missing positive execution authorization gate; created execution authorization ADR.
- LOW A-L2 / A-R2: naming/serialization guards added for exportable new surfaces.

## M477 - Dependency Boundary Guard

Added architecture tests for:

- no AgentOperations project reference to `OneBrain.BrowserExecutor.Cdp`;
- no `using OneBrain.BrowserExecutor.Cdp` in AgentOperations;
- no prohibited BrowserExecutor.Cdp namespace in AgentOperations source;
- future UI project detection must not reference BrowserExecutor.Cdp;
- runtime primitive detection in AgentOperations for `HttpClient`, `ClientWebSocket`, `Process.Start`, `System.Diagnostics.Process`, `BackgroundService`, `Thread`/`Timer` runtime wiring.

`BrowserExecutor.Cdp` remains present as a temporary runtime host but outside the new core foundation.

## M478 - Evidence + Execution Gate ADRs

Created:

- `docs/architecture/nodal-os-evidence-model-consolidation-decision-record.md`
- `docs/architecture/nodal-os-execution-authorization-gate-decision-record.md`

The evidence ADR establishes `NodalOsEvidenceBridgeRef` as canonical for future UI/export surfaces and marks `NexaEvidenceRef` legacy/compatibility only.

The execution ADR establishes that no real execution can occur without a future positive gate bridge composed of registry, policy, approval, evidence, verification, rollback/redaction/jail and risk-classifier hardening where applicable.

## M479 - Naming/Serialization Guard + Legacy Quarantine

Created:

- `docs/backlog/nodal-os-legacy-nexa-subsystem-quarantine-plan.md`

Added tests ensuring new exportable surfaces do not serialize:

- `Nexa`;
- `NEXA`;
- `NODRIX`;
- `HOTEP`.

The quarantine plan blocks cloud/licensing/BYOK until sensitive legacy `Nexa*` subsystems are removed, archived, isolated or migrated with tests.

## Files Created

- `tests/OneBrain.Safety.Tests/NodalOsAuditAPreUiBoundaryNamingM477M479Tests.cs`
- `docs/architecture/nodal-os-evidence-model-consolidation-decision-record.md`
- `docs/architecture/nodal-os-execution-authorization-gate-decision-record.md`
- `docs/backlog/nodal-os-legacy-nexa-subsystem-quarantine-plan.md`
- `docs/reports/audit-a-pre-ui-boundary-naming-m477-m479.md`
- `artifacts/agent-operations/m479/audit-a-pre-ui-boundary-naming-summary.json`

## Files Modified

- `docs/roadmap/nodal-os-roadmap-vnext.md`
- `docs/roadmap/nodal-os-unified-roadmap-post-pause.md`

## Tests Added

Tests cover:

- dependency direction;
- browser runtime disconnection guard;
- ADR coverage;
- naming serialization guard;
- legacy quarantine plan;
- no-runtime/no-UI guardrails.

## Validations Executed

Final validation results are recorded in the artifact and final report.

## Guardrails Confirmed

- No UI real.
- No cloud.
- No LLM provider calls.
- No browser automation.
- No scheduler/worker.
- No recorder/replay/queue.
- No DSL parser runtime.
- No shell/subprocess.
- No execution wiring.
- No broad rename.
- No deletion of legacy runtime.

## What Was Not Implemented

- No positive execution authorization gate implementation.
- No UI.
- No BrowserExecutor.Cdp deletion.
- No legacy `Nexa*` deletion.
- No cloud/licensing/BYOK.
- No runtime bridge.

## Risks / Pending

- `BrowserExecutor.Cdp` still exists and remains a temporary runtime host.
- Legacy `Nexa*` contracts remain compatibility debt.
- Cloud/licensing/BYOK remain blocked until legacy sensitive subsystems are quarantined or removed.
- Positive execution authorization gate remains future work before runtime.

## Updated Percentages

- NODAL OS global: 96.5%.
- Agent Operations / Automation Layer: 97.5%.
- Core Runtime: 75%.
- Evidence/Timeline foundation: 76%.
- Approval foundation: 68%.
- Redaction/Safety foundation: 79%.
- Productization foundation: 40%.
- Mission Control UX: 20%.

## Next Recommended Block

`M480+M481+M482 - Mission Control Shell V1 Read-Only + Approval Display + Timeline/Evidence Views`.

## Final Decision

`M477+M478+M479 CERRADO / AUDIT_A_PRE_UI_BOUNDARY_NAMING_HARDENING_READY`
