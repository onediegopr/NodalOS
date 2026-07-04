# Product Ledger Public Surface Readiness And Launch Blocker Map Design-Only

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PUBLIC_SURFACE_READINESS_AND_LAUNCH_BLOCKER_MAP_DESIGN_ONLY_READY`

## Context

Product Ledger now has local-only/internal-only building blocks: active writer, runtime gate, operator diagnostics, internal UI preview, internal command router, internal command handler and bounded local diagnostic report export. None of those blocks authorizes public UI, public/product command handler exposure, destructive action execution, external/cloud export, provider/cloud/network, DB/migration, KMS/WORM/external trust, live Browser/CDP/WCU/OCR/Recipes or release/commercial readiness.

This ADR is design-only/read-only. It prepares the decision packet before any public surface.

## Public Surface Readiness Matrix

| Area | Current state | Readiness | Blockers | Required tests | Required evidence | Allowed next safe step | GO/NO-GO |
| --- | --- | ---: | --- | --- | --- | --- | --- |
| Public UI readiness | Not implemented. Internal preview only. | 0% | No public UI contract, no auth/operator identity model, no public action gating. | read-only public mock fixtures, disabled action tests, stale state tests. | operator identity, fresh readiness evidence, no raw/secret exposure. | public UI read-only mock/preview with all actions disabled. | NO-GO |
| Public/product command handler readiness | Not exposed. Internal handler only. | 0% | No product command bus exposure, no public handler id/callback contract. | unknown command fail-closed, stale command fail-closed, handler/callback absence tests. | router and handler evidence, command allowlist evidence. | command exposure test-plan-only. | NO-GO |
| Destructive action readiness | Not implemented. | 0% | No destructive action approval model, no rollback semantics, no user-facing confirmation model. | destructive command blocked, accidental click blocked, replay/rollback tests. | explicit separate GO, rollback/non-rollback classification. | destructive-action threat model docs-only. | NO-GO |
| Bounded local export readiness | Implemented local-only/internal-only. | 100% internal / 0% public | Same-machine evidence only, no public export surface, no cloud/export authority. | boundary, overwrite, raw/secret, hash verification, no external export tests. | canonical boundary evidence, redaction evidence, post-write hash. | export corpus/static scan hardening. | GO internal only |
| Product Ledger local-only writer readiness | Implemented local-only bounded writer. | 82% | Same-boundary local trust, no external/WORM custody, no public action. | append/read/checkpoint/corruption/failure tests. | authority, redaction, retention, replay/rollback. | read model/checkpoint hardening. | GO internal only |
| Runtime local-only gate | Implemented default-off internal gate. | 78% | No public runtime enablement, no public DI/service registration. | default-off, forged flag, unsupported command tests. | runtime flag evidence, diagnostics/readiness evidence. | runtime static scan hardening. | GO internal only |
| Operator diagnostics readiness | Implemented Core-only read-only surface. | 100% internal | Same-boundary evidence; not public UI. | stale/malformed evidence and unsafe runtime tests. | fresh evidence references. | public read-only mock input design. | GO internal only |
| Internal UI preview readiness | Implemented read-only/internal-only preview. | 100% internal | Not public, actions disabled. | disabled actions, unsafe preview rejection. | diagnostics and router preview evidence. | public UI read-only mock/preview with all actions disabled. | GO internal only |
| Internal command router readiness | Implemented no-op/read-only preview router. | 100% internal | Preview-only, disabled, no execution authority. | unknown/corrupt command and blocked command tests. | action disabled evidence. | command exposure test-plan-only. | GO internal only |
| Internal command handler readiness | Implemented internal-only/non-destructive, plus bounded export path. | 100% internal | Not public/product exposed. | blocked public/product/destructive/external cases. | router preview and bounded export evidence. | public command contract design-only. | GO internal only |
| Redaction/retention/authority evidence | Present for internal gates. | 82% | Must be freshness-checked before any public surface. | stale/malformed evidence tests, raw/secret negative corpus. | redaction-before-export/persistence, retention, authority. | evidence freshness test-plan-only. | NO-GO public |
| Failure/replay/rollback evidence | Present for local/internal gates. | 78% | No user-facing rollback semantics. | corrupted ledger/checkpoint, rollback/non-rollback tests. | failure/replay/rollback classification. | rollback semantics design-only. | NO-GO public |
| Static no-external/no-release scans | Present per block. | 85% | Needs public surface specific scan pack. | no public/product exposure, no release, no provider/cloud scans. | changed-file scan logs and audit pack. | static scan hardening. | NO-GO public |
| Provider/cloud/network | Not implemented/authorized. | 0% | No provider/cloud/network policy. | provider/cloud/network block tests. | explicit future GO and policy. | design-only only. | NO-GO |
| DB/migration | Not implemented/authorized. | 0% | No DB authority, schema or migration plan. | DB/migration block tests. | explicit future GO and migration review. | design-only only. | NO-GO |
| KMS/WORM/external trust | Not implemented/authorized. | 0% | Local evidence is not WORM/compliance-grade custody. | external trust overclaim block tests. | external custody/key/trust design and audit. | design-only only. | NO-GO |
| Browser/CDP/WCU/OCR/Recipes live | Not authorized. | 0% | No live automation authority. | live path block tests. | explicit future GO and live authority audit. | design-only only. | NO-GO |
| Release/commercial | Not authorized. | 0% | No release/commercial policy or readiness approval. | release/commercial claim block tests. | business/release decision. | launch blocker docs-only. | NO-GO |

## Public UI Threat Model

| Threat | Severity | Required mitigation | Required test | Blocker before public exposure |
| --- | --- | --- | --- | --- |
| User clicks destructive action accidentally. | P1 | No destructive public action until separate approval and confirmation model. | destructive action stays disabled and blocked. | destructive action model missing. |
| Disabled action becomes executable. | P1 | Disabled action invariant and static scan. | executable flag fails closed. | disabled-action guard missing. |
| Handler id/callback accidentally exposed. | P1 | No public handler/callback in view model. | handler/callback presence rejects render. | public command contract missing. |
| Public UI bypasses internal router. | P1 | Every future action must route through router then handler. | direct command path blocked. | route-through-router evidence missing. |
| Stale readiness state is shown as safe. | P2 | Freshness timestamps and stale evidence blockers. | stale readiness rejects public render. | freshness policy missing. |
| Stale authority evidence is accepted. | P2 | Evidence freshness and consistency gates. | stale/malformed authority evidence blocks. | public authority evidence model missing. |
| Physical export is exposed as public export. | P1 | Public UI cannot expose export action; export remains local/internal bounded. | public export action blocked. | public export policy missing. |
| Local-only writer is mistaken for cloud/durable/WORM. | P2 | Wording and status model must state local-only/non-WORM. | overclaim scan for cloud/WORM/release wording. | external trust design missing. |
| Release/commercial overclaim. | P1 | Release/commercial flags remain false and claims block. | release/commercial claim fails closed. | business/release GO missing. |
| External trust overclaim. | P1 | No KMS/WORM/external trust wording or authority. | external trust claim blocks. | external custody design missing. |
| Public UI exposes raw payload/secret. | P1 | Redaction-before-display/export and negative corpus. | raw/secret corpus blocks render/export. | public redaction display policy missing. |
| Public UI triggers provider/cloud/network. | P1 | No provider/cloud/network path in public surface. | provider/cloud command blocks. | provider policy missing. |
| UI routes live Browser/CDP/WCU/OCR/Recipes. | P1 | Live automation absent unless future explicit GO. | live command blocks. | live authority audit missing. |
| Corrupted local ledger/checkpoint shown as safe. | P2 | Read/verify before display, fail closed on corruption. | corrupted ledger/checkpoint display blocks. | public read model policy missing. |

## Safe Exposure Contract Design-Only

Future public UI can only render read-only state first. Every future action must pass through the command preview router and command handler. Destructive actions require explicit separate approval. No public action may execute until evidence gates pass, including redaction, retention, authority, failure/replay, rollback/non-rollback and stale evidence checks.

Disabled actions remain disabled unless separately authorized. No future public surface may add provider/cloud/network, DB/migration, KMS/WORM/external trust, live Browser/CDP/WCU/OCR/Recipes, external/cloud export or release/commercial readiness without a new explicit GO.

## Public Command Handler Test Plan

- Unknown command fails closed.
- Stale UI command fails closed.
- Disabled action cannot execute.
- Missing handler/callback blocks.
- Any handler/callback in public read-only model blocks.
- Destructive action blocks.
- Provider/cloud blocks.
- DB/migration blocks.
- KMS/WORM/external trust blocks.
- Browser/CDP/WCU/OCR/Recipes live blocks.
- Release/commercial blocks.
- Raw payload/secret blocks.
- Physical export outside boundary blocks.
- Public export request blocks.
- Corrupted ledger/checkpoint blocks.
- Public UI bypass of router blocks.

## Launch Blocker Map

Hard blockers before public UI:

- No public UI action implementation exists.
- No product command handler exposure exists.
- No public auth/operator identity model exists.
- No final destructive action approval model exists.
- No provider/cloud/network policy exists.
- No release/commercial policy exists.
- No user-facing rollback semantics exist.
- No external trust/KMS/WORM exists.
- No public redaction/display policy exists.
- No public read model freshness policy exists.

Soft blockers / P3:

- More negative corpus.
- More UI state fixtures.
- Manual QA plan for public UI.
- External audit prompt pack for public surface.
- Static scan hardening for public surface wording.

## Manual QA And External Audit Pack

Questions for auditor:

- Can any visible public action execute without router and handler evidence?
- Can any disabled action become executable through stale state?
- Does any view expose handler id, callback or product command id?
- Does any copy imply release/commercial readiness?
- Does any copy imply KMS/WORM/cloud/external trust?
- Can bounded local export be mistaken for external/cloud export?
- Are raw payload, secrets and unsafe metadata blocked before display/export?
- Does corrupted ledger/checkpoint evidence fail closed?
- Are provider/cloud/network, DB/migration and live automation absent?

Checklist:

- UI safety reviewed.
- Command exposure reviewed.
- Local-only boundaries reviewed.
- Export boundaries reviewed.
- Evidence gates reviewed.
- Overclaim wording reviewed.
- Release/commercial readiness remains 0%.
- External trust claims remain 0%.
- Cloud/provider absence confirmed.

## Stop Packet

`PUBLIC_UI_OR_PRODUCT_COMMAND_HANDLER_EXPOSURE_REQUIRES_NEW_EXPLICIT_GO`

Allowed before that GO:

- docs-only;
- design-only;
- audit/read-only;
- test-plan-only;
- static scan/no-overclaim hardening;
- public UI read-only mock/preview with all actions disabled;
- internal-only/local-only readiness surfaces.

Requires new explicit GO:

- public UI action real;
- public/product command handler exposure;
- destructive user-facing action;
- unbounded physical export/write;
- external/cloud export;
- provider/cloud/network;
- DB/migration;
- KMS/WORM/external trust;
- live Browser/CDP/WCU/OCR/Recipes;
- release/commercial readiness.

## Decision

`GO_WITH_FINDINGS_PRODUCT_LEDGER_PUBLIC_SURFACE_READINESS_AND_LAUNCH_BLOCKER_MAP_DESIGN_ONLY_READY`.
