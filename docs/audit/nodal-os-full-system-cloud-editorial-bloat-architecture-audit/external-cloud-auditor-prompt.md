# External Cloud/Claude Auditor Prompt (paste manually)

> How to use: no automated cloud auditor was executed from Codex in this window. To get a
> real external opinion, paste the prompt below into your chosen external model (Claude/GPT),
> together with `cloud-audit-packet.md`. Do NOT paste secrets, tokens, `.env`, credentials,
> or user-absolute paths. Do NOT upload the full raw repo without separate human
> authorization. Paste the sanitized packet only.

---

You are a brutally honest senior external auditor: principal engineer + product architect +
safety reviewer. You are reviewing NODAL OS, a local-only, fail-closed system
(approval → local append-only hash-chained ledger → evidence). I will give you a sanitized
inventory (no code, no secrets).

Context: the owner fears architectural bloat ("grasa") — too many contracts, candidates,
status-suffixed classes, per-step executor/presenter layers, guards, ADRs, handoffs and
test-safe layers — the same failure mode that hurt a prior project. The kernel is small and
good; the scaffold around it is large and self-similar.

Do NOT assume more contracts = better. Prioritize a usable LOCAL product. Be direct.

Your tasks:
1. Judge whether the per-node contract template (each step redeclaring
   Decision/State/ActionKind/Blocker enums + Options/Request/Result/Validation/Payload
   records + one executor, ~600–1000 LOC each) is over-engineering or justified. Explain.
2. Identify the FAT: what to merge, simplify, or delete; and what is essential security that
   must NOT be touched (fail-closed validation, hash-only storage, path-confinement,
   checkpoint tamper-evidence, one static no-enable guard).
3. Call out over-engineering explicitly, including naming (status facts encoded in type
   names; 61/69 files contain "ReadOnly").
4. Reconcile: a loopback `/run` endpoint executes allowlisted recipes via subprocess while
   the system advertises "ZeroReadOnly" / "no runtime". Rate the severity of that claim.
5. Propose the MINIMUM component set (target ~10) for a usable local product.
6. Judge whether the doc corpus (331 ADRs, 221 handoffs, 231 QA dirs, 1,810-line
   decision-log, 1,729 md) is net value or net noise, and propose safe compaction.
7. Judge the test posture (672 test files, flag/string-heavy, mirrored Safety/Recipes,
   missing concurrency/integration) and what to keep as a required smoke gate.

Deliverables:
- A KEEP / MERGE / DELETE / SIMPLIFY / FREEZE matrix over the main components.
- A phased pruning plan (docs → naming → tests → design merge → source refactor under GO →
  product surface → final audit), each phase with risk, benefit, and no-go boundaries.
- A short list: "things NOT to implement yet" and "things NOT to touch (security-load-bearing)".
- Scores 0–100 for: bloat, simplification urgency, product-core health, security-core health,
  doc noise, test noise.

Constraints: recommend nothing that enables blocked frontiers (active read precedence, latest
pointer, product authority, public/product exposure, Production route, shell/subprocess,
DB/KMS/WORM/cloud, release/commercial). All pruning must be behavior-preserving.
