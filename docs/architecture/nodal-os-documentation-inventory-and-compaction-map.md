# NODAL OS Documentation Inventory And Compaction Map

Date: 2026-07-07

Decision: `GO_WITH_FINDINGS_BLOCK_A_DOCS_COMPACTION_AND_RUN_CLAIM_RECONCILIATION_READY`

Baseline HEAD: `9610ae01fb721a17374845d862ed78d2d78eedfd`

## Inventory

Measured in the current working tree before compaction:

| Area | Count |
| --- | ---: |
| ADR markdown files under `docs/adr` | 331 |
| QA directories under `docs/qa` | 231 |
| Handoff markdown files under `docs/handoff` | 221 |
| Markdown files under `docs` | 1733 |
| Audit markdown files under `docs/audit` | 6 |

The inventory confirms the editorial audit finding: documentation is now larger and harder to navigate than the local product kernel it describes.

## Classification Rules

| Classification | Meaning | Default action |
| --- | --- | --- |
| `CANONICAL_KEEP` | Current source of truth or load-bearing security/product documentation. | Keep visible and link from canonical index. |
| `CANONICAL_INDEX` | Index document that points to canonical and archived material. | Keep visible; update instead of creating another parallel doc. |
| `ARCHIVE_LEGACY` | Historical traceability that should not be read as current authority. | Keep, but move/index in a future archive block. |
| `COMPACT_INTO_LOG` | Repetitive QA/handoff closeout content. | Summarize in `docs/qa/qa-log.md` or `docs/handoff/handoff-log.md`. |
| `DUPLICATE_OR_RECURSIVE` | Repeats another audit/ADR/QA result without changing behavior. | Archive or merge into a log in a future block. |
| `DELETE_CANDIDATE_FUTURE_ONLY` | Candidate for deletion only after archive/index and explicit GO. | Do not delete in Block A. |
| `DO_NOT_DELETE_SECURITY_RELEVANT` | Captures fail-closed, redaction, path confinement, hash/checkpoint, or claim-boundary evidence. | Keep or compact without weakening meaning. |

## Canonical Keep Set

- `docs/architecture/nodal-os-current-local-internal-architecture.md`
- `docs/architecture/nodal-os-documentation-governance.md`
- `docs/architecture/nodal-os-simplification-backlog.md`
- `docs/adr/ADR_CANONICAL_INDEX.md`
- `docs/audit/nodal-os-run-claim-coherence-reconciliation.md`
- `docs/audit/nodal-os-full-system-cloud-editorial-bloat-architecture-audit/report.md`
- `docs/audit/nodal-os-full-system-cloud-editorial-bloat-architecture-audit/simplification-plan.md`
- `docs/qa/qa-log.md`
- `docs/handoff/handoff-log.md`
- `docs/decision-log.md` as traceability plus current head note.

## Archive Legacy Buckets

These are not deleted in this block.

- Historical pause and phase closeout packets with broad `NO_RUNTIME_NO_EXECUTION` labels.
- Per-macro-block external-audit read-only closeouts that repeat the same anti-capabilities.
- Handoff files that only restate a QA report.
- QA report directories that only restate ADR/handoff content.
- Design-only packets that have already been promoted to implementation or superseded.

## Compact Into Logs

Future blocks should compact these into one rolling log entry per block:

- Product Ledger local-only line QA closeouts.
- Product Ledger handoff closeouts.
- Repeated external-audit read-only packets.
- Readiness-only and design-only guard reports that do not change code.

## Duplicate Or Recursive Patterns

- ADR + QA + handoff triplets for every micro-step.
- Safety and Recipes docs that assert the same negative capability list.
- External audit docs immediately followed by handoff docs with identical findings.
- Repeated "no provider/cloud/network/DB/KMS/WORM/release" paragraphs.

## Delete Candidates For Future Only

No files are deleted in Block A. Future deletion candidates must first be archived/indexed:

- Redundant external-audit read-only handoffs.
- Obsolete design-only packets superseded by implementation and audit.
- Duplicate QA directories whose evidence is represented in `qa-log.md`.
- Historical per-step reports after a canonical architecture index exists.

## Security Relevant Docs To Preserve

Do not delete or weaken docs that capture:

- fail-closed behavior;
- redaction-before-persistence;
- Product Ledger path confinement;
- local append-only/hash-chain/checkpoint evidence;
- `/run` claim scoping;
- static no-enable guard intent;
- release/commercial NO-GO;
- public/product/Production route blockers.

## Future Physical Archive Plan

Block A indexes and marks; it does not move files. A future explicit archive block may:

1. Create `docs/archive/YYYY-MM/`.
2. Move legacy per-block ADR/QA/handoff docs there.
3. Preserve relative links from canonical indexes.
4. Keep security load-bearing docs discoverable.
5. Run link/static checks before and after.
