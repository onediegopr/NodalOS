# NODAL OS — Sanitized Cloud/Claude Audit Packet

> Sanitized for external review. Contains NO secrets, tokens, `.env`, credentials, API
> keys, or user-absolute paths. Repo paths shown are repo-relative only. No source is
> uploaded here — this is an inventory + question set. Full-repo upload requires separate
> explicit human authorization.

## Executive summary
NODAL OS is a local-only, fail-closed "approval → local append-only ledger → evidence"
system. A small, well-engineered kernel is wrapped in a large, self-similar scaffold of
policy-status contracts and per-step executor/presenter classes, plus a very large audit-doc
corpus. The concern under review: architectural bloat ("grasa") that harms maintainability
and slows reaching a usable local product — a repeat of a prior project's failure mode.

## Repo state (sanitized)
- Repo: NODAL OS (local working copy; path redacted).
- Branch: `chrome-lab-001-extension-local-ai-bridge`.
- HEAD: `9e8a1f35…` (worktree clean, origin sync 0/0).
- Frontiers intentionally BLOCKED: active read precedence, latest pointer, product
  read-model authority, public/product exposure, Production route, workspace edit/delete,
  shell/subprocess, Browser/CDP/WCU/OCR/Recipes live, provider/cloud/network, DB/migration,
  KMS/WORM/external trust, release/commercial.

## File inventory (summary)
- 18 `src` projects. Product line concentrated in `src/OneBrain.Core/Approval/`: **69
  classes, ~29,900 LOC**.
- Tests: **516** Safety test files, **156** Recipes test files (single-assert/flag-heavy).
- Docs: **331** ADRs, **231** QA report dirs, **221** handoffs, **1,810**-line
  decision-log, **1,729** total markdown files.

## Class/model/function inventory (representative, sanitized names)
- Kernel: local append-only hash-chained JSONL ledger + head checkpoint + fail-closed
  verify; stores payload **hash only** + bounded safe metadata.
- 3 writer variants of that kernel (Disabled / LocalTempTest / LocalActive).
- Multiple evidence/ledger notions: Durable Stage1, Durable Stage2, LocalTempCheckpoint,
  ProductLedger LocalOnly.
- Per-node template repeated ~dozens of times: `Decision`+`State`+`ActionKind`+`Blocker`
  enums + `Options`+`Request`+`Result`+`Validation`+`Payload` records + one
  Executor/Presenter class (~600–994 LOC each).
- 4+ operator UI/preview surfaces producing DTO/HTML-string snapshots (no live product UI).
- Guard classes: several `*DesignOnlyProtected` / `*AntiCapabilityProof` asserting negatives.

## Route inventory
- `OneBrain.Pilot` (loopback web app): `/`, `/plan`, `/run` (executes allowlisted recipe via
  subprocess — **claim-coherence flag**), `/api/intent`, `/api/safety` (advertises
  "ZeroReadOnly"), dev-gated ProductLedger preview route.
- `OneBrain.ChromeLab.Bridge`: WebSocket bridge (separate lab footprint).
- Product Ledger classes themselves are NOT wired into a product runtime/DI.

## Test inventory
- Heavy on negative-claim and flag/string assertions; dual Safety+Recipes mirrors.
- Missing: writer concurrency, crash-mid-append recovery, cross-component integration,
  adversarial dual tamper (ledger+checkpoint).

## Docs inventory
- Very large; no single "current architecture" source of truth; state must be reconstructed
  from a long chain of near-identical closeouts.

## Real chain (implemented)
Product Ledger local-only writer → approval decision state → approved no-op / bounded action
→ local evidence + handoff draft writers → latest-state snapshot/manifest/aux evidence
(create-only, explicitly not authority). All fail-closed, hash-based, local-only.

## Suspected bloat points (for auditor to confirm/deny)
1. Per-node contract explosion (could be one generic result + shared claims/blockers).
2. Status-suffix class naming as domain model (61/69 files contain "ReadOnly").
3. Writer/ledger/evidence duplication.
4. Doc corpus larger than code; no source-of-truth doc.
5. Many compiled negative-guard/anti-capability classes.
6. Pilot `/run` real execution vs "ZeroReadOnly"/"no runtime" claims.

## Security constraints for the auditor
Do not propose enabling any blocked frontier. Preserve: fail-closed validation, hash-only
storage, path-confinement, checkpoint tamper-evidence, one static no-enable guard. Pruning
must be behavior-preserving.

## Questions for the external auditor
1. Is the per-node contract template genuine over-engineering, or justified isolation?
2. Which of the 69 Approval classes are essential vs mergeable vs deletable?
3. Is status-in-the-name a real maintainability defect here?
4. What is the minimum component set for a usable LOCAL product?
5. Where would pruning risk breaking the fail-closed security model?
6. Is the doc/test volume net-positive or net-noise, and what compaction is safe?
7. Deliver a KEEP/MERGE/DELETE/SIMPLIFY/FREEZE matrix + a phased pruning plan.

(See `external-cloud-auditor-prompt.md` for the exact prompt to paste.)
