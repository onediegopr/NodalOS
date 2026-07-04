# Global Safe Runtime Pre-Enablement Readiness And Next Scope

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_GLOBAL_SAFE_RUNTIME_PRE_ENABLEMENT_READINESS_READY`

## Scope

Docs-only/read-only global readiness, consolidation, audit and backlog prioritization before any runtime/product enablement.

This ADR does not authorize runtime product enablement, productive service registration, command handlers, UI product actions, product ledger paths, DB/migration, provider/cloud/network, Browser/CDP/WCU/OCR/Recipes live execution, KMS/WORM/cloud/external trust provider implementation, release readiness or commercial readiness.

## Operational Canon

Current operational canon is the latest `docs/decision-log.md` plus the most recent QA/handoff chain. `docs/ROADMAP.md` and older roadmap/report files remain legacy traceability unless explicitly revalidated by the latest decision-log and QA artifacts.

Latest Durable safe-chain state:

- Durable Stage 2 remains test-only/local-temp.
- Redaction-before-persistence remains isolated Core/test-only.
- Runtime feature flag accepts exact test-only value only.
- Checkpoint evidence remains local-temp/caller-held.
- Checkpoint trust boundary is local-only/no-provider/test-only.
- Runtime/product/release/commercial remains `0% / NO-GO`.
- External provider/cloud/KMS/WORM trust remains `0% / NO-GO`.

## Global State Matrix

| Area | State | Current authority | Evidence | Blocker / next action |
| --- | --- | --- | --- | --- |
| Durable Audit Trail Stage 1 | implemented test-only/local-test | `LOCAL_TEST_SAFE_ONLY / IMPLEMENTED_NOT_ENABLED` | Append-only minimal ledger, Safety/Recipes tests, decision-log Stage 1 entries | Product ledger threat model and runtime enablement design remain required. |
| Durable Audit Trail Stage 2 | implemented test-only/local-temp | `TEST_ONLY / NO_PRODUCT` | `AppendStage2TestOnly`, redaction result gate, exact feature flag, recent QA | Product path, runtime wiring and external audit/manual GO remain blockers. |
| Redaction-before-persistence service | implemented test-only | `CORE_TEST_ONLY / NOT_PRODUCT_SERVICE` | `RedactionBeforePersistenceService`, closeout QA, corpus tests | Product wiring design, logging/error policy and manual GO required. |
| Runtime feature flag | implemented test-only | `FAIL_CLOSED_TEST_ONLY` | `DurableAuditTrailStage2RuntimeFeatureFlag` | Product flag service and rollout policy not designed or authorized. |
| Replay/read-model/checkpoint evidence | implemented local-temp only | `LOCAL_TEMP_CALLER_HELD_ONLY` | `DurableAuditTrailLocalTempCheckpointEvidence` | Cannot provide independent external trust. |
| External checkpoint trust boundary | local-only/no-provider/test-only | `NO_PROVIDER / NO_WORM / NO_KMS` | Local-only trust boundary ADR/audit | Any external trust provider requires product/security decision. |
| Browser/CDP/ChromeLab | lab/separate boundary | `LAB_RUNTIME_ONLY / NOT_PRODUCT_AUTHORITY` | claim-freeze ADR, Browser/CDP boundary docs | Dedicated product authority audit required before upgrade. |
| WCU/OCR | fixture-safe/read-only/design-only | `PRODUCT_AUTHORITY_0` | WCU/OCR/Pilot/Nexa mega audit | Live action authority remains prohibited. |
| OneBrain.Pilot | separate/historical local runtime footprint | `NOT_CURRENT_PRODUCT_AUTHORITY` | Pilot/Nexa/OCR authority handoff | Dedicated audit required before relying on it. |
| Nexa admin handlers | separate/historical admin boundary | `NOT_CURRENT_PRODUCT_COMMAND_AUTHORITY` | Pilot/Nexa/OCR authority handoff | Dedicated audit and command authority design required. |
| Recipes | design/test/readiness only | `LIVE_AUTHORITY_0` | claim-freeze ADR and recipe handoffs | Live runner/scheduler/trigger execution blocked. |
| Runtime/service registration/command handlers | frozen | `NO_GLOBAL_PRODUCT_AUTHORITY` | claim-freeze ADR | Inventory plus authority audit required before any registration. |
| Product ledger path | absent/prohibited | `0% / NO-GO` | Stage 2 final reconciliation | Threat model/design-only block is next highest value. |
| DB/cloud/provider/network | absent/prohibited | `0% / NO-GO` | Stage 2 final reconciliation and claim freeze | No provider/cloud/network path may be added without new GO. |
| Release/commercial | blocked | `0% / NO-GO` | decision-log and QA closeouts | Requires full product authority audits and manual release decision. |

## Runtime/Product Blockers

| Blocker | Severity | Why it blocks product | Required future macro-block | Human GO |
| --- | --- | --- | --- | --- |
| Product ledger path absent/prohibited | P3 | Runtime audit trail has no authorized product storage boundary. | Product ledger path threat model design-only. | Required before implementation. |
| Product DI/service registration absent/prohibited | P3 | No productive lifecycle, ownership or failure policy exists. | Runtime service registration authority design-only. | Required before implementation. |
| Command handlers absent/prohibited | P3 | No command authority, idempotency or rollback contract. | Command-handler authority design-only. | Required before implementation. |
| UI product actions absent/prohibited | P3 | No user-facing affordance, consent or audit path. | UI product action authority design-only. | Required before implementation. |
| Runtime feature flag product service absent/prohibited | P3 | Only exact test-only value is allowed. | Runtime feature flag product-readiness design-only. | Required before implementation. |
| Redaction product wiring absent/prohibited | P3 | Redaction is isolated test-only, not product service. | Redaction product-wiring design-only. | Required before implementation. |
| External trust provider absent/prohibited | P3 | Current checkpoints are local-temp/caller-held only. | External trust provider decision pack expansion. | Required for any provider/KMS/WORM. |
| Product checkpoint writer absent/prohibited | P3 | No product checkpoint persistence or custody policy. | Checkpoint writer design-only after ledger threat model. | Required before implementation. |
| DB/cloud/provider absent/prohibited | P3 | No data governance, network, custody or migration policy. | DB/provider/cloud authority design-only. | Required before implementation. |
| Browser/CDP/WCU/OCR/Recipes live absent/prohibited | P3 | Live authority is frozen at 0%. | Release blocker/authority audit. | Required before live use. |
| Release/commercial NO-GO | P4 | Readiness percentages are internal, not release claims. | Global MVP readiness audit no-enablement. | Required before release planning. |

## External Trust Provider Decision Pack

| Option | Benefit | Risk / complexity | Data/key custody | Product implication | Recommendation |
| --- | --- | --- | --- | --- | --- |
| Stay local-only/no-provider | Lowest exposure; aligns with current manual decision. | No independent external trust. | No external custody. | Safe for test-only and local evidence. | Recommended now. |
| Local WORM-like file discipline design-only | Adds operational discipline without provider. | Easy to overclaim WORM/compliance. | Local operator custody. | Must remain design-only until audited. | Useful as future design-only exploration. |
| Local OS key store / DPAPI-style design-only | Better local key protection. | Platform-specific and recovery risk. | Local OS/user custody. | Requires key rotation/recovery design. | Candidate after product ledger threat model. |
| BYOK local signing design-only | Clear custody model for advanced users. | UX/key-loss risk. | User-held key. | Needs threat model and support policy. | Later design option. |
| KMS/cloud provider future option | Strongest external custody/compliance path. | Network/provider/data exposure and policy complexity. | Provider custody or BYOK/KMS custody. | Requires product/security decision and external audit. | Not default; blocked. |
| No external trust provider until MVP-safe runtime | Avoids premature provider decisions. | External trust remains 0%. | None. | Keeps MVP scope smaller. | Recommended pairing with local-only now. |

## Next Safe Roadmap Options

| Rank | Option | Allowed scope | Prohibited scope | Risk | Continue automatically |
| ---: | --- | --- | --- | --- | --- |
| 1 | `NODAL_OS_PRODUCT_LEDGER_PATH_THREAT_MODEL_DESIGN_ONLY` | Docs-only threat model for product ledger path, data boundaries, redaction, custody, failure modes and negative tests. | No product ledger implementation or runtime wiring. | Low if docs-only. | Yes. |
| 2 | `NODAL_OS_RUNTIME_FEATURE_FLAG_PRODUCT_READINESS_DESIGN_ONLY` | Product flag policy design without runtime registration. | No enablement or rollout code. | Low. | Yes after Rank 1. |
| 3 | `NODAL_OS_REDACTION_PRODUCT_WIRING_DESIGN_ONLY` | Product service wiring plan and safety contracts. | No DI/product service registration. | Medium due claim drift. | Yes if strictly design-only. |
| 4 | `NODAL_OS_DURABLE_RUNTIME_ENABLEMENT_DESIGN_ONLY_NO_CODE` | End-to-end enablement plan, no code. | No product enablement. | Medium. | Yes if no implementation. |
| 5 | `NODAL_OS_EXTERNAL_TRUST_PROVIDER_DECISION_PACK_EXPANSION` | Deeper comparison of local-only, DPAPI/BYOK/KMS/provider options. | No provider/KMS/cloud integration. | Medium because product/security decision likely follows. | Only if design-only. |
| 6 | `NODAL_OS_BROWSER_CDP_WCU_RECIPES_RELEASE_BLOCKER_AUDIT_READ_ONLY` | Read-only release blocker audit. | No live Browser/CDP/WCU/OCR/Recipes. | Low. | Yes. |
| 7 | `NODAL_OS_GLOBAL_MVP_READINESS_AUDIT_NO_ENABLEMENT` | Global readiness scorecard. | No runtime/release action. | Low. | Yes. |

Selected next macro-block:

`NODAL_OS_PRODUCT_LEDGER_PATH_THREAT_MODEL_DESIGN_ONLY`

## Overclaim Scan Result

Overclaim scan categories:

- Negative assertion: PASS.
- Prohibited boundary: PASS.
- Design-only mention: PASS.
- Historical reference: PASS.
- Accepted test-only wording: PASS.
- Accepted local-temp wording: PASS.
- Lab boundary wording: PASS.
- TRUE_RISK: none found in the current changed scope.

Known noisy hits include ChromeLab `AddSingleton`/`MapGet`/`MapPost`/WebSocket terms in lab-boundary docs, and guard literals in Safety tests. They remain classified as historical, lab boundary or negative assertions.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No runtime/product/live authority or release/commercial overclaim found. |
| P1 | 0 | No productive registration, handler, UI action, product ledger path, DB/provider/cloud/network or KMS/WORM implementation. |
| P2 | 0 | No blocker requiring immediate stop. |
| P3 | 5 | Product ledger path, productive runtime registration, command/UI authority, redaction product wiring and external trust provider remain blockers. |
| P4 | 2 | Historical roadmap/docs are noisy; full solution warnings are inherited/pre-existing. |

## Percentages

| Track | Conservative status |
| --- | --- |
| Durable Stage 1 local/test-safe | 92-95% |
| Durable Stage 2 test-only chain | 91-95% |
| Redaction-before-persistence test-only | 91-95% |
| Runtime feature flag test-only boundary | 92-95% |
| Local-temp checkpoint evidence | 90-93% |
| Local-only checkpoint trust boundary | 84-89% |
| Browser/CDP/ChromeLab boundary hardening | 85-90% |
| Runtime/Browser/WCU/Pilot/Nexa/OCR claim freeze | 85-90% |
| Product ledger path | 0% / NO-GO |
| Runtime/product enablement | 0% / NO-GO |
| Provider/cloud/KMS/WORM trust | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |

## Decision

`GO_WITH_FINDINGS_GLOBAL_SAFE_RUNTIME_PRE_ENABLEMENT_READINESS_READY`

Automatic continuation is allowed to the selected next docs-only/design-only macro-block.
