# Product Ledger First Real User-Facing Local Action Path Readiness Design-Only

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_FIRST_REAL_USER_FACING_LOCAL_ACTION_READINESS_DESIGN_ONLY_READY`

## Scope

This is a design-only/readiness-only/test-only packet for the next frontier: the first real user-facing local action path.

No real user-facing action is implemented in this block. No user file write, route execution, public/product path, Production execution, shell/subprocess, command execution, Pilot `/run`, Browser/CDP/WCU/OCR/Recipes live, provider/cloud/network, DB/migration, KMS/WORM/external trust, release/commercial or compliance custody claim is introduced.

Explicit negative boundary:

- No real user-facing action.
- No user file write.
- No public/product path.
- No Production execution.
- No shell/subprocess.
- No arbitrary command execution.
- No Pilot `/run`.
- No Browser/CDP/WCU/OCR/Recipes live.
- No provider/cloud/network.
- No DB/migration.
- No KMS/WORM/external trust.
- No release/commercial.

## Backward Roadmap Check

Current implemented chain:

`candidate -> operator decision persisted -> approved no-op execution -> bounded internal completion marker -> evidence/read-model/operator surface`

Confirmed still absent:

- Real user-facing action.
- User file write authorization.
- Public/product action path.
- Production execution.
- Shell/subprocess or arbitrary command execution.
- Provider/cloud/network or DB/migration.
- KMS/WORM/external trust.
- Browser/CDP/WCU/OCR/Recipes live.
- Pilot `/run`.
- Release/commercial readiness.

## Candidate Matrix

| Candidate action | User-visible value | Files/resources touched | Write scope | Destructive risk | Rollback story | Idempotency story | Redaction needs | Evidence needs | Testability | Policy risks | Overclaim risks | Why now / why not now | Recommendation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A. Internal-only user-visible status marker | Shows a visible local status in the operator surface | Internal state only | Existing internal store | Very low | Clear marker removal or next-state replacement | Same marker is replay-safe | Low | Approval/no-op/bounded refs | High | Not actually a user-facing file/action | Could overclaim real user-facing value | Safe but too close to existing bounded marker | Not recommended |
| B. Local approved report artifact generation in temp/test controlled folder | Produces a local visible report artifact | Local temp/test output | Controlled temp/test folder | Low if no overwrite | Delete generated temp/test artifact | Stable content hash and exact-match replay | Medium | Approval, redaction, output hash | High | May look like export | Could be confused with product export | Useful but less durable as a handoff artifact | Runner-up |
| C. Local approved handoff/report draft in repo-controlled `docs/test-output` | Produces a concrete user-visible draft artifact inside an allowlisted repo path | `docs/test-output/product-ledger/approved-local-handoff-drafts/` only | Single allowlisted repo-controlled output file | Low if create-only/no overwrite | Safe cleanup by deleting generated test-output artifact in controlled path | Same approval/action/hash creates same file or exact-match replay; conflicting content blocks | High | Approval, redaction, bounded pre-state, output hash, surface ref | High | Requires strict path allowlist and no arbitrary path | Could be mistaken for public/product readiness if wording drifts | Best balance: visible real artifact, local, bounded, verifiable | Recommended |
| D. Local approved evidence export preview file test-safe | Produces evidence/export-like file | Controlled test-safe path | Export-like output | Medium | Delete generated preview | Exact-match replay | High | Export-specific evidence | Medium-high | Could blur with export/product claims | Could overclaim export readiness | Valuable later after C hardens path/write policy | Not recommended now |
| E. Repo-detected safer option: append internal-only timeline entry | Visible in operator surface timeline | Internal timeline/read-model only | Internal store | Low | Marker removal or replacement | Replay-safe | Low-medium | Approval/timeline refs | High | Still not a real user-facing artifact | Same limitation as A | Safe but does not cross the intended frontier | Not recommended |

## Recommendation

Recommended first real action candidate: `LocalApprovedHandoffReportDraft`.

Recommended future action kind: `LocalApprovedHandoffReportDraft`.

Recommended future route name: `POST /internal/product-ledger/approval/create-local-handoff-draft`.

This is recommended because it is local-only, internal-only, Development-only at first, user-visible, non-destructive, easy to verify, easy to clean up, valuable for handoff workflows and can be constrained to a single repo-controlled allowlisted path.

## Recommended Boundary Spec

Exact action name: Local approved handoff/report draft.

Exact action kind: `LocalApprovedHandoffReportDraft`.

Future route: `POST /internal/product-ledger/approval/create-local-handoff-draft`.

Output boundary:

- Allowed root: `docs/test-output/product-ledger/approved-local-handoff-drafts/`.
- Allowed file extension: `.md`.
- Allowed filename source: deterministic action id plus short approved evidence hash.
- Example future file pattern: `docs/test-output/product-ledger/approved-local-handoff-drafts/local-approved-handoff-draft-{actionId}-{hashPrefix}.md`.

Forbidden paths:

- Any absolute path from body, query, header, environment or UI.
- Any `..`, symlink/reparse/junction, UNC path or drive-root path.
- Any path outside `docs/test-output/product-ledger/approved-local-handoff-drafts/`.
- Workspace source files, docs canon files, user profile folders, temp roots not owned by the action, Product Ledger active ledger paths and export folders.

Allowed payload schema:

- `explicitLocalApprovedHandoffDraftScope: true`.
- `developmentMode: true`.
- `localMode: true`.
- `internalMode: true`.
- `actionId`.
- `actionKind: LocalApprovedHandoffReportDraft`.
- `approvalId`.
- `noOpExecutionId`.
- `boundedActionExecutionId`.
- `candidateActionKind`.
- `candidateEvidenceHash`.
- `currentEvidenceHash`.
- `draftTitle`.
- `redactedDraftSummary`.
- `evidenceReferences`.

Forbidden payload fields:

- `path`, `absolutePath`, `directory`, `outputPath`, `command`, `shell`, `subprocess`, `url`, `provider`, `network`, `db`, `kms`, `worm`, `browser`, `ocr`, `wcu`, `recipes`, `pilotRun`, `release`, `commercial`, raw payload or secret fields.

Maximum body size:

- 8 KiB unless a smaller route-specific limit is chosen during implementation.

Required pre-state:

- Persisted approval decision: `ApprovedLocalOnly`.
- Completed approved no-op execution.
- Completed bounded internal completion marker.
- Current candidate evidence hash equals approval, no-op and bounded state hash exactly.
- Evidence references are present, fresh and non-sensitive.

Redaction rules:

- No raw payload, secret-like, PII-like, token-like, absolute path, UNC path, URL, command line, provider credential or unredacted operator note may appear in the output artifact.
- Any operator-supplied text must pass the existing redaction-before-persistence/service policy before artifact creation.

Idempotency rules:

- Same action id, approval id, candidate hash and redacted content hash returns the same result.
- Existing exact-match file may be treated as idempotent replay.
- Existing file with different content blocks fail-closed.
- Different action id for the same approved candidate blocks unless a future policy explicitly allows multiple drafts.

Rollback/cleanup rules:

- Create-only, no overwrite.
- Future implementation must include a safe cleanup story for generated `docs/test-output` artifact.
- Cleanup must remain bounded to the allowlisted generated file and must never accept arbitrary path input.

Failure modes and fail-closed behavior:

- Missing approval/no-op/bounded pre-state blocks.
- Pending/rejected/request-changes/expired/tampered state blocks.
- Hash mismatch blocks.
- Missing evidence blocks.
- Malformed payload blocks.
- Unknown action kind blocks.
- Path traversal/absolute path/UNC/reparse risk blocks.
- Existing conflicting file blocks.
- Redaction failure blocks.
- Any IO exception blocks and records no product claim.

DOM/render requirements:

- Operator surface must show action readiness, output artifact relative path, output hash prefix, evidence refs, blockers, local/internal/Development labels and negative flags.
- Surface must state no public/product path, no Production route, no shell/subprocess, no command execution, no cloud/network/DB, no KMS/WORM/compliance custody and no release/commercial.

Static scan rules:

- Forbid product/public/Production activation claims.
- Forbid shell/subprocess/arbitrary command execution.
- Forbid Browser/CDP/WCU/OCR/Recipes live and Pilot `/run`.
- Forbid cloud/provider/network, DB/migration, KMS/WORM/external trust.
- Forbid arbitrary `File.WriteAllText` outside the single future implementation file and controlled tests.
- Allow only negative/no-go/design-only references in this readiness block.

Production/public blocking rules:

- Production route must be 404.
- No public UI action.
- No product command handler.
- No productive DI/service registration.
- No release/commercial readiness.

## Future Implementation Test Plan

Positive:

- Approved candidate creates exactly one allowed handoff draft artifact.
- Evidence refs are generated and surfaced.
- Operator surface shows result, relative path and output hash prefix.
- Repeating the same request is idempotent.

Negative:

- Pending/rejected/request-changes/expired/tampered cannot act.
- Missing approval/no-op/bounded state blocks.
- Hash mismatch blocks.
- Missing evidence blocks.
- Malformed payload blocks.
- Unknown action kind blocks.
- Path traversal blocks.
- Absolute path blocks.
- Arbitrary path blocks.
- UNC/reparse/junction path blocks.
- Overwrite blocks.
- Existing file conflict blocks or exact-match replay only.
- Command field blocks.
- URL/network/provider field blocks.
- Shell/subprocess blocks.
- Production route 404.
- Public/product path absent.

Security:

- No secrets in output.
- Redaction applied to notes/context.
- No filesystem scan.
- No unbounded export.
- No cloud/network/DB.
- No KMS/WORM/compliance custody claim.
- No release/commercial claim.

Static:

- Forbid public/product/Production activation.
- Forbid shell/subprocess/arbitrary command execution.
- Forbid arbitrary file write outside future allowlisted implementation file.
- Forbid browser/OCR/WCU/Recipes live.
- Allow only negative/no-go/design-only mentions in readiness docs.

## Decision

`GO_WITH_FINDINGS_FIRST_REAL_USER_FACING_LOCAL_ACTION_READINESS_DESIGN_ONLY_READY`

## Required Next GO

Next implementation requires explicit GO for:

`NODAL_OS_LOCAL_APPROVED_HANDOFF_REPORT_DRAFT_IMPLEMENTATION_WINDOW`

That GO must still prohibit public/product path, Production execution, shell/subprocess, arbitrary path, user-selected file paths, network/cloud/DB, KMS/WORM/external trust, live automation and release/commercial readiness.
