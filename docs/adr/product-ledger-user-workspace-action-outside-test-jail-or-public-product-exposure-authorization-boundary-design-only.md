# Product Ledger User Workspace Action Outside Test-Jail Or Public Product Exposure Authorization Boundary

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_USER_WORKSPACE_OR_PUBLIC_PRODUCT_AUTHORIZATION_BOUNDARY_DESIGN_ONLY_READY`

## Scope

This block is design-only, readiness-only, audit-only, test-only and guard-only.

It does not implement:

- Any action outside the workspace test-jail.
- Any user-selected path.
- Any public/product exposure.
- Any Production route.
- Any shell/subprocess or command execution.
- Any Browser/CDP/WCU/OCR/Recipes live authority.
- Any provider/cloud/network, DB/migration, KMS/WORM/external trust or release/commercial claim.

## Backward Reality Check

Current implemented chain:

`candidate -> approval persisted -> approved no-op execution -> bounded internal marker -> LocalApprovedHandoffReportDraft -> LocalWorkspaceTestJailHandoffDraftCreateOnly -> workspace test-jail create-only/no-overwrite -> evidence/read-model/operator surface`

Confirmed implemented:

- `LocalApprovedHandoffReportDraft`.
- `LocalWorkspaceTestJailHandoffDraftCreateOnly`.
- Real local write only inside workspace test-jail.
- Create-only/no-overwrite.
- Canonical final path under jail.
- Reparse/symlink/junction fail-closed.
- Redaction-before-persistence.
- Evidence refs.
- Operator surface/read-model state.

Confirmed not implemented:

- Workspace write outside the test-jail.
- User-selected path.
- Public/product path.
- Production route.
- Shell/subprocess.
- Command execution.
- Cloud/provider/network/DB.
- Browser/CDP/OCR/WCU/Recipes live.
- KMS/WORM/compliance custody.
- Release/commercial readiness.

## Authorization Matrix

| Option | User value | Technical delta | Files/routes/stores touched | Write scope | Risk summary | Blockers | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A. Controlled user-workspace allowlisted write outside test-jail | Highest near-term real value because it creates a useful local handoff artifact in an operator-visible workspace boundary. | New design then later executor/route/read-model/tests; no public path. | Future Core executor, Pilot Development-only route, operator surface state, Safety/Recipes tests. | Create-only markdown under an allowlisted workspace-owned boundary such as `docs/nodal-os/handoffs/`. | Medium path/privacy risk; low destructive risk if create-only/no-overwrite and no user-selected path. | Needs explicit implementation GO, canonicalization/reparse proof, redaction proof, exact idempotency and cleanup/rollback spec. | Recommended next frontier as design-only first. |
| B. Public/product exposure of local operator surface/action path | Higher discoverability, but lower safety because it moves authority toward users before workspace write semantics are proven outside test-jail. | Public route/surface hardening, auth/product policy, route exposure controls, UX acceptance. | Product/public UI surface, routing, possibly auth/policy layers. | Could expose read/action path publicly. | Higher security/overclaim/release risk. | Public/product path, Production route and release/commercial decisions are explicitly not authorized. | Not now. |
| C. Local/internal hardening before crossing boundary | Reduces risk and preserves safety posture. | Additional property/corpus/static scans around test-jail and predecessor chain. | Tests/docs only, possibly no source. | None beyond current test-jail. | Low risk but lower user value. | Does not answer the next real user value frontier. | Useful if P2/P3 drift appears; not primary next frontier. |
| D. Fixture-only workspace outside test-jail | Safe incremental evidence for path rules without touching real workspace. | New fixture/test-only path planner and static scans. | Tests/docs or test-only helper. | Fixture workspace only. | Low path/destructive risk, but limited product value. | May become too incremental if repeated. | Acceptable fallback if implementation GO for A is withheld. |

## Recommendation

Recommend option A, but only as the next design-only boundary, not direct implementation.

Future recommended action:

`LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly`

Future action kind:

`LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly`

Future route:

`POST /internal/product-ledger/approval/create-user-workspace-allowlisted-handoff-draft`

Future state route:

`GET /internal/product-ledger/approval/user-workspace-allowlisted-handoff-draft-state`

Future executor:

`ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor`

Why public/product remains blocked:

- The route and write chain is still Development-only and local/internal.
- Public/product exposure would require product policy, UX, auth, release and commercial decisions outside this authorization.
- No Production route or public UI action is authorized.

Why leaving the test-jail can add value:

- It creates a real operator-visible handoff draft in a stable workspace boundary.
- It tests the same approval/write discipline against a useful workspace location.
- It keeps the action non-destructive if create-only/no-overwrite and path authority remain internal.

Least risky candidate:

Create a redacted markdown handoff draft under:

`<workspace-root>/docs/nodal-os/handoffs/`

The exact root must be resolved internally from the repository/workspace root, not from payload, query, header, UI or env.

## Future Boundary Spec

- Future action name: `LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly`.
- Future action kind: `LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly`.
- Future route name: `LocalUserWorkspaceAllowlistedHandoffDraftRoute`.
- Future executor name: `ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor`.
- Allowed workspace root source: internally resolved repo/workspace root with explicit local-only boundary proof.
- Allowed output boundary: `docs/nodal-os/handoffs/`.
- Forbidden output boundaries: repo root, arbitrary `docs/`, source trees, tests, `.git`, user profile folders, temp unless fixture-only, network shares, absolute payload paths and any path outside the allowlisted boundary.
- Path jail rules: combine only internally generated relative filename under canonical allowlisted boundary.
- Canonicalization/reparse rules: canonicalize root and final path before write; final path must be inside the canonical boundary by path-segment comparison.
- Symlink/junction fail-closed rules: check boundary root and parent directory for reparse metadata; fail closed when uncertain.
- Create-only/no-overwrite rules: use create-new semantics; exact same content can be idempotent replay; different existing content blocks.
- No user-selected arbitrary path.
- No payload-controlled root.
- No filesystem scan.
- Filename strategy: deterministic allowlisted slug plus evidence hash prefix.
- Extension allowlist: `.md` only.
- Redaction-before-persistence required.
- Evidence refs required from approval, no-op, bounded marker, workspace test-jail predecessor and current request.
- Idempotency rules: stable exact replay or blocked conflict; no overwrite.
- Rollback/cleanup rules: procedural only; no automatic destructive cleanup route.
- Failure modes: fail closed on missing chain, missing evidence, hash mismatch, malformed payload, path uncertainty, redaction rejection and IO uncertainty.
- DOM/read-model expectations: local operator surface shows state, safe relative path, content hash prefix, blockers, evidence refs and negative flags.
- Production blocking: route remains unmapped outside Development.
- Public/product blocking: no public route, no public UI action, no product command handler.
- Static scan rules: future names may appear only in docs/tests until an implementation GO is granted.

## Future Test Plan Before Implementation

Positive:

- Approved chain can create exactly one allowed draft outside test-jail but inside allowlisted workspace boundary.
- Safe relative path.
- Canonical final path under allowed workspace boundary.
- Content redacted.
- Evidence refs generated.
- Operator surface shows state.
- Exact idempotency stable.

Negative:

- Pending/rejected/request-changes blocked.
- Expired/invalid blocked.
- Missing no-op blocked.
- Missing bounded marker blocked.
- Missing workspace test-jail predecessor blocked.
- Tampered candidate blocked.
- Hash mismatch blocked.
- Malformed payload blocked.
- Unknown actionKind blocked.
- Absolute path blocked.
- Traversal and encoded traversal blocked.
- Symlink/junction escape blocked or fail-closed.
- Overwrite different content blocked.
- User-selected arbitrary path blocked.
- Payload-controlled root blocked.
- Filesystem scan absent.
- Command field blocked.
- Shell/subprocess blocked.
- URL/network/provider field blocked.
- Production route 404.
- Public/product absent.

Security/static:

- No secrets in output.
- Redaction before write.
- No cloud/network/DB.
- No Browser/CDP/OCR/WCU/Recipes live.
- No Pilot `/run`.
- No KMS/WORM/compliance custody claim.
- No release/commercial claim.
- Future names only in docs/tests unless implementation is later authorized.

## Exact Next GO Required

`AUTHORIZE_NODAL_OS_USER_WORKSPACE_ALLOWLISTED_HANDOFF_DRAFT_BOUNDARY_DESIGN_ONLY`

That next block may design the specific user-workspace allowlist in more detail, but still must not implement the writer unless a later implementation GO is granted.

