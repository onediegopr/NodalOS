# Product Ledger Public/Product Or User-Workspace Action Authorization Readiness

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_PUBLIC_PRODUCT_OR_USER_WORKSPACE_ACTION_AUTHORIZATION_READINESS_DESIGN_ONLY_READY`

## Scope

This block is design-only, readiness-only, audit-only and test-only. It compares the next large frontier after `LocalApprovedHandoffReportDraft` and recommends the next authorization window.

No public/product exposure, Production route, user-workspace action, productive export, shell/subprocess, command execution, provider/cloud/network, DB/migration, KMS/WORM/external trust, Browser/CDP/WCU/OCR/Recipes live, Pilot `/run`, release/commercial readiness or compliance custody claim is implemented here.

## Backward Reality Check

The real current chain is:

`candidate -> approval persisted -> approved no-op execution -> bounded internal completion marker -> LocalApprovedHandoffReportDraft -> create-only/no-overwrite under docs/test-output/product-ledger/approved-local-handoff-drafts/ -> evidence/read-model/operator surface`

Current implemented capability:

- Persisted local approval decision state.
- Approved no-op execution state.
- Bounded internal completion marker.
- `LocalApprovedHandoffReportDraft`.
- Development-only route `POST /internal/product-ledger/approval/create-local-handoff-draft`.
- Development-only state route `GET /internal/product-ledger/approval/local-handoff-draft-state`.
- Create-only/no-overwrite draft under `docs/test-output/product-ledger/approved-local-handoff-drafts/`.
- Operator surface draft state with relative path, content hash, evidence refs and blockers.

Still not implemented:

- No public/product path.
- No Production route.
- No user-workspace action.
- No productive export.
- No shell/subprocess.
- No command execution.
- No Browser/CDP/WCU/OCR/Recipes live.
- No Pilot `/run`.
- No provider/cloud/network.
- No DB/migration.
- No KMS/WORM/external trust or compliance custody.
- No release/commercial readiness.

## Authorization Matrix

| Option | User value | Technical delta | Files/routes/stores touched | Write scope | Destructive risk | Security risk | Privacy risk | Rollback/cleanup need | Idempotency need | Redaction need | Evidence need | Auditability | Overclaim risk | Test burden | Blockers | Recommendation | Why now / why not now |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A. Public/product exposure of operator surface and handoff draft flow | Makes current local flow visible as product-facing capability | Public route/surface contract, product routing, stronger auth/visibility policy, public copy and support boundary | Pilot route map, surface models, product UI/read model docs, launch blockers | None if read-only first, but public exposure changes trust boundary | Medium; public affordances can be misread as execution authority | Medium-high; public surface increases attack and claim surface | Medium; evidence may reveal internal metadata if copy/redaction is weak | Low for read-only, high for misclaimed launches | Medium; repeated public reads must be stable | Required for evidence text and operator notes | Required to prove no execution/write | Good if read-only, weaker if launched too soon | High | High | No launch posture, no product ownership/signoff, no public threat model finalization | Not recommended now | Current value is internal/local validation; public exposure would amplify claims before workspace action value is proven |
| B. First controlled user-workspace action | Creates tangible user value by writing a safe artifact where a user would expect work output | Future workspace jail policy, path canonicalization, create-only writer, cleanup/read-model evidence | Future Core boundary, Development-only route, operator surface, Safety/Recipes tests | One file under allowlisted workspace test-jail only | Low-medium if create-only/no-overwrite and jail enforced | Medium; workspace boundary is sensitive | Medium; output content must be redacted and path-safe | Required; cleanup must stay inside jail | High; exact replay or conflict block | Mandatory before persistence | Mandatory: approval chain, hash, refs, relative path | Strong if all writes are evidence-bound | Medium | High | Needs separate implementation GO, path jail spec, canonicalization tests, no arbitrary path | Recommended as next frontier, but design-only first | More real value than public read-only exposure while still controllable through a test-jail and create-only rules |
| C. Keep local/internal and harden persistence/latest-state/audit | Reduces risk before crossing any frontier | Harden stores, latest-state durability, replay/read model and audit docs | Existing local stores, route state, QA/audit docs | Existing local/test-output only | Low | Low | Low | Low | Medium | Medium | Medium | Strong | Low | Medium | Delays real workspace value | Useful fallback, not primary recommendation | Good if risk tolerance is low; current tests already cover enough to design the next frontier |
| D. Workspace test-jail boundary proof pack before implementation | Narrows option B into a safer pre-implementation packet | Design-only threat model plus property/static test plan for jail, cleanup, idempotency and redaction | Docs/tests only | None in this block | Very low | Very low | Very low | Design only | Design only | Design only | Design only | Strong | Low | Medium | Still requires later implementation GO | Recommended immediate next macro-block | It turns option B into an auditable implementation-ready boundary without writing to user workspace yet |

## Recommendation

Recommend option D as the immediate next macro-block and option B as the next real frontier after that design gate:

`NODAL_OS_WORKSPACE_TEST_JAIL_HANDOFF_DRAFT_BOUNDARY_DESIGN_ONLY`

The future first workspace action should be:

`LocalWorkspaceTestJailHandoffDraftCreateOnly`

Future route name:

`POST /internal/product-ledger/approval/create-workspace-test-jail-handoff-draft`

Why not public/product yet:

- Public/product exposure changes trust and support boundaries before a workspace action has proven practical user value.
- Public copy can create overclaims around release, compliance custody, productive export or action authority.
- Current operator surface is useful internally but not yet packaged for public threat model, product ownership or launch review.

Why workspace action is more valuable:

- It proves Nodal OS can move from local evidence to a tightly bounded user-relevant artifact.
- It can reuse the existing approval -> no-op -> bounded marker -> handoff draft chain.
- It can remain local/internal/Development-only and create-only while still testing real-world workspace constraints.

Least risky workspace action:

Create one new redacted Markdown handoff draft inside a future allowlisted workspace test-jail. The action must not accept arbitrary paths from payload, query, headers, environment or UI.

## Future Boundary Spec

Future action name: `LocalWorkspaceTestJailHandoffDraftCreateOnly`.

Future route name: `POST /internal/product-ledger/approval/create-workspace-test-jail-handoff-draft`.

Required pre-state:

- Persisted approval decision state is `ApprovedLocalOnly`.
- Approved no-op execution is completed.
- Bounded internal completion marker is completed.
- `LocalApprovedHandoffReportDraft` predecessor exists and has matching evidence hash.
- Current candidate evidence hash matches approval, no-op, bounded marker and predecessor exactly.
- Evidence refs are present and safe.

Allowed output boundary:

- A future pre-registered workspace test-jail root only.
- Recommended shape: `<registered-workspace-test-jail>/.nodal/product-ledger/handoff-drafts/`.
- The test-jail root must be supplied by a trusted fixture/configured boundary, not by request payload or query.

Forbidden output boundaries:

- Any user-selected arbitrary path.
- Any path outside the registered workspace test-jail.
- Repo root outside explicit test-output or test-jail boundaries.
- Desktop, Downloads, Documents, home directory roots, temp directories outside the jail, network shares, UNC paths and symlink/junction escapes.

Path jail rules:

- Canonicalize before write.
- Reparse after parent creation before write.
- Reject absolute path payload fields.
- Reject `..`, alternate separators, encoded traversal and path-like content in draft material.
- Reject symlink/junction escapes.
- Do not enumerate the workspace.

Create-only/no-overwrite rules:

- Use create-new semantics.
- Exact idempotent replay may return replay success if content hash matches.
- Existing different content must block.
- No append, truncate, replace, delete, move or chmod-like operation.

Other rules:

- No shell/subprocess.
- No command execution.
- No filesystem scan.
- No Browser/CDP/WCU/OCR/Recipes live.
- No Pilot `/run`.
- No provider/cloud/network.
- No DB/migration.
- No KMS/WORM/external trust.
- No release/commercial or compliance custody claim.
- Redaction-before-persistence is mandatory.
- Output must include evidence refs, relative jail path, content hash and negative assertions.
- Production route remains 404.
- Public/product path remains blocked unless separately authorized.
- Operator surface may show state, relative path, hash, evidence refs, blockers and cleanup instructions.

Failure modes:

- Missing approval/no-op/bounded/predecessor blocks.
- Tampered hash blocks.
- Missing or unsafe evidence refs block.
- Any path, command, URL, provider, DB, KMS/WORM, live automation or release intent blocks.
- IO errors fail closed with no retry loop outside the jail.

Rollback/cleanup:

- Cleanup is manual or future test-only cleanup inside the same registered jail.
- No cleanup can delete outside the jail.
- No cleanup is authorized in this block.

## Test Plan Before Implementation

Positive:

- Approved chain can perform exactly one allowed workspace test-jail action.
- Output stays inside allowlisted boundary.
- Evidence refs are generated.
- Operator surface shows state, relative path, hash and blockers.
- Idempotency is stable for exact replay.

Negative:

- Pending/rejected/request-changes approval blocks.
- Expired/invalid state blocks.
- Tamper/hash mismatch blocks.
- Missing no-op blocks.
- Missing bounded marker blocks.
- Missing predecessor handoff draft blocks.
- Arbitrary path, absolute path, traversal and encoded traversal block.
- Overwrite and existing different file block.
- Filesystem scan is absent.
- Shell/subprocess and command fields block.
- URL/network/provider/DB fields block.
- Production route returns 404.
- Public/product path remains absent unless separately authorized.

Security/static:

- No secrets in output.
- Redaction required.
- No release/commercial claim.
- No KMS/WORM/compliance custody claim.
- No cloud/network/DB.
- No Browser/CDP/OCR/WCU/Recipes live.
- No Pilot `/run`.
- Future names appear only in docs/tests until a separate implementation GO.

## Exact Next GO Required From Diego

`AUTHORIZE_NODAL_OS_WORKSPACE_TEST_JAIL_HANDOFF_DRAFT_BOUNDARY_DESIGN_ONLY`

That GO should still be design/test-plan only. A later, separate implementation GO would be required before any user-workspace write exists.
