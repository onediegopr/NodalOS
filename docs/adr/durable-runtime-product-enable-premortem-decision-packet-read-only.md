# Durable Runtime Product Enablement Premortem And Decision Packet Read-Only

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_DURABLE_RUNTIME_PRODUCT_ENABLEMENT_PREMORTEM_AND_DECISION_PACKET_READ_ONLY_READY`

## Scope

Read-only/design-only decision packet for possible future Durable Runtime product enablement.

This packet does not enable runtime/product behavior, create an active product ledger path, register services, add command handlers, add UI product actions, add DB/migration/provider/cloud/network integration, implement KMS/WORM/external trust, enable Browser/CDP/WCU/OCR/Recipes live behavior or claim release/commercial readiness.

## Premortem

The first product enablement attempt can fail in these ways:

- Product ledger path points to the wrong root or an operator-supplied path with ambiguous ownership.
- A path appears inside the local boundary but resolves outside through symlink, junction or reparse-point behavior.
- Path canonicalization differs between validation, write, replay and cleanup paths.
- Redaction is incomplete, stale, bypassed by nested metadata or missing before persistence.
- Raw payload, secret-like, PII-like or path-like values are persisted, logged or surfaced in error text.
- Replay evidence is missing, stale, duplicated, malformed or not tied to a current read-model snapshot.
- Read-model snapshot is stale, partial or calculated from an untrusted checkpoint.
- Rollback is misclassified as safe when it is not, or non-rollback cases are hidden behind generic failure language.
- Product authority is ambiguous, unauthenticated or confused with the current test-only human GO evidence flag.
- Test-only evidence is treated as product authority.
- DI/service registration happens before all dependency gates are green.
- Command handlers or UI product actions accidentally expose product writes.
- Provider/cloud/network terms are overclaimed as implemented trust.
- WORM/KMS/external trust is claimed from local-only or caller-held evidence.
- Release/commercial readiness is inferred from test-only scaffold success.
- Tail-deletion and checkpoint limitations are ignored.
- Browser/CDP/WCU/OCR/Recipes live authority is inferred from unrelated historical roadmap work.

## Decision Options

| Option | Recommendation | Benefit | Risk | Required validation | Rollback plan | Files/layers likely touched | Prohibited |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A - NO-GO / keep test-only | Viable fallback | Maximum safety; preserves current guarantees | No product value unlocked | Existing scaffold tests and docs remain current | No code rollback needed | Docs/QA only | Any runtime/product wiring |
| B - GO to another safety-hardening block | Recommended | Closes the weakest product blockers before product implementation | Delays enablement | Test-plan/static guards for product ledger canonicalization, reparse handling, authority and replay | Revert docs/tests only; no product state | Docs, tests, disabled guard catalogs only | Product ledger activation, DI, handlers, UI actions |
| C - GO to product implementation scaffold but disabled | Not yet recommended | Can shape product interfaces while default-off | Risk of accidental registration or overclaim | Static no-enable guards, disabled-by-default tests, no registration tests, build/tests | Revert disabled scaffold commit; no migration/state | Future Core interfaces/services, tests, docs | Runtime enablement, active path, DB/cloud |
| D - GO to limited product enablement behind explicit local-only operator gate | NO-GO now | Could unlock constrained local product value | Highest risk; crosses real stop boundary | All P3 blockers closed, manual GO, security owner, rollback proof, product authority, external audit | Product rollback playbook and feature kill switch required | Core, host registration, command/UI paths, storage, QA | Not allowed in this block |

Default recommendation: choose Option B before any product implementation scaffold or limited enablement.

## Product Readiness Matrix

| Area | Current state | Test-only state | Product gap | Risk | Required evidence | Required tests | Readiness | GO/NO-GO |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Product ledger path | Not active | Path blockers and boundary preview | Real root policy, canonicalization, containment | Writes outside boundary | Root policy, resolved path trace | traversal, symlink, junction, reparse, mixed separator | 10-15% | NO-GO |
| Redaction product wiring | Isolated Core/test-only | Hash-bound safe request evidence | Product policy ownership, no raw logs/errors | Sensitive persistence | policy id, corpus version, redacted evidence schema | nested metadata, references, raw error leakage | 22-32% | NO-GO |
| Runtime feature flag | Exact test-only only | Fail-closed gate | Product flag service, kill switch, rollout policy | Accidental enablement | owner, default-off proof, dependency gates | missing/malformed/env variants, disabled default | 18-28% | NO-GO |
| Authority wiring | Test-only evidence flag | Blocks product authority overclaims | Real authn/authz/operator identity | Unauthorized enablement | signed approval model, audit owner | missing identity, stale approval, scope mismatch | 18-28% | NO-GO |
| Read-model/replay | Scaffold evidence flags | Read-model snapshot and consistency blockers | Product read model and replay service | False recovery confidence | snapshot schema, replay trace, checkpoint limits | stale snapshot, mismatch, tamper, missing refs | 32-42% | NO-GO |
| Failure evidence | Failure evidence required | Failure catalog blocker | Product failure taxonomy and observability | Unknown failure modes | catalog, classification, audit pack | known failure modes, redacted diagnostics | 32-42% | NO-GO |
| Rollback policy | Not productized | Rollback/non-rollback classification blocker | Executable rollback policy and non-rollback playbook | Unsafe rollback | rollback matrix, non-rollback signoff | rollback simulation, no-side-effect proof | 15-25% | NO-GO |
| Local filesystem boundary | Test-only checks | Local boundary preview | Product jail and canonical realpath enforcement | boundary escape | resolved path evidence, TOCTOU notes | realpath, parent traversal, race threat model | 15-25% | NO-GO |
| Symlink/junction/reparse | Evidence required only | Blocks missing evidence | Real platform-specific enforcement | resolved-outside write | reparse policy, OS behavior proof | symlink, junction, mount/reparse variants | 15-25% | NO-GO |
| DI/service registration | None productive | Static no-enable scan | Disabled scaffold registration plan | accidental activation | registration map, default-off proof | no AddSingleton/AddHostedService until GO | 0% | NO-GO |
| Command handlers | None productive | Static no-enable scan | Product command contract | accidental write path | command authority model | no handler until product gate | 0% | NO-GO |
| UI product actions | None productive | Static no-enable scan | Product UX and disabled states | user-triggered mutation | UI authority and confirmation model | disabled action, no hidden submit | 0% | NO-GO |
| Static scans | Present for touched files | No TRUE_RISK in safe chain | Product scan suite | missed wiring | scan catalog and CI criteria | DI/handler/UI/DB/cloud/runtime scans | 45-55% | GO for hardening only |
| QA reports | Present | MD/JSON reports | Product evidence pack standard | incomplete audit trail | signed QA template | JSON schema, claim scan | 45-55% | GO for docs/tests |
| External trust/provider | Not implemented | Explicit no-overclaim | Security/provider decision | false trust claim | product/security decision | provider absent until GO | 0% | NO-GO |
| Release/commercial | Not ready | Explicit no-go | release owner, support, compliance | premature release | release decision packet | release-blocking scan | 0% | NO-GO |

## Future Implementation Map, No Code

Possible future classes and interfaces:

- `IDurableRuntimeProductLedgerPathPolicy`
- `IDurableRuntimeProductRedactionPolicy`
- `IDurableRuntimeAuthorityService`
- `IDurableRuntimeReadModel`
- `IDurableRuntimeReplayService`
- `IDurableRuntimeRollbackPolicy`
- `DurableRuntimeProductEnablementGate`
- `DurableRuntimeProductEnablementOptions`

Where not to register yet:

- no host DI/service collection registration;
- no hosted service;
- no command handler;
- no command bus wiring;
- no UI action;
- no DB migration or provider-backed storage;
- no cloud/KMS/WORM/external trust adapter.

Tests before any product code:

- static no-enable guards;
- product ledger path canonicalization and reparse test-plan;
- redaction no-raw evidence and error-leak tests;
- authority scope/identity/stale approval tests;
- read-model/replay mismatch tests;
- rollback/non-rollback classification tests;
- feature flag default-off/kill-switch tests.

Migration from scaffold to product policy would need to preserve fail-closed behavior: every missing dependency must reject enablement, every overclaim must remain a blocker and every product path must be disabled until the explicit product gate succeeds.

## Audit Questions Bank

Security:

- What exact data can be persisted, and what must never be persisted?
- How are raw payloads prevented from appearing in logs, errors, reports and telemetry?
- Who owns redaction corpus updates and regression review?

Architecture:

- Which layer owns product enablement decisions?
- What contracts are stable enough for product code?
- How does the product gate fail closed if a dependency is absent?

Claims and roadmap:

- Which claims remain test-only?
- Which documents are canonical if historical roadmap entries disagree?
- How is release/commercial wording blocked?

Filesystem/path:

- What is the product ledger root?
- How is canonical realpath computed on Windows?
- How are symlink, junction, mount point and reparse-point cases rejected?
- How is TOCTOU between validation and write handled?

Authority:

- What is a real product approval?
- How is operator identity verified?
- What expires an approval?
- What prevents human GO text from becoming product authority?

Replay/failure/rollback:

- What read model is canonical?
- What proves replay is read-only unless explicitly authorized?
- Which failures are rollback-safe, and which are non-rollback?
- How are stale snapshots rejected?

Release/commercial:

- Who can approve release/commercial readiness?
- What support, retention, compliance and incident procedures are required?
- What evidence blocks release until product authority is complete?

## Stop Conditions

Codex must stop and ask for explicit human GO before:

- enabling runtime/product;
- creating an active product ledger path;
- adding productive DI/service registration;
- adding command handlers or command bus wiring;
- adding UI product actions;
- adding DB/migration/provider/cloud/network;
- adding KMS/WORM/external trust;
- enabling Browser/CDP/WCU/OCR/Recipes live behavior;
- claiming release/commercial readiness.

Before asking for GO, Codex should bring:

- this decision packet;
- latest QA JSON/MD;
- static scan results;
- proposed file/layer touch list;
- rollback/no-side-effect plan;
- explicit stop conditions.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No runtime/product enablement added. |
| P1 | 0 | No scope leak found. |
| P2 | 0 | No blocking inconsistency found in this packet. |
| P3 | 6 | Product ledger path, redaction product wiring, runtime flag ownership, authority wiring, read-model/replay/rollback and provider/external trust remain blockers. |
| P4 | 2 | Percentages remain conservative; historical docs still contain no-go vocabulary by design. |

## Recommendation

Recommendation: Option B, another safety-hardening block before any product implementation scaffold.

Recommended next safe macro-block:

`NODAL_OS_PRODUCT_LEDGER_PATH_CANONICALIZATION_REPARSE_AND_AUTHORITY_TEST_PLAN_ONLY`

Do not proceed to Option C or D without a new explicit manual GO.
