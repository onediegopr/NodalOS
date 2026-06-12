# Roadmap Reconciliation After HITO-129

## Scope

This document reconciles the formal roadmap, the implementation history, and the next approved execution sequence after HITO-129.

## Formal Handoffs vs Implemented Work

The formal handoff trail documents HITO-082 through HITO-117 as the explicit planned sequence:

- HITO-082+083+084: Pilot UI shell, intent router, demo UX
- HITO-085+086+087: Recording, candidate timeline, human annotation
- HITO-088+089+090: Approval UX, recipe confidence, supervised demo flow
- HITO-091+092+093: AI model router, OpenAI profile configuration, Pilot AI config console
- HITO-094+095+096: Run history, execution UI, AI usage and audit logs
- HITO-097+098+099: Recipe editor, variable manager, validation/linter
- HITO-100+101+102: Process memory, workflow retrieval, app profile manager
- HITO-103+104+105: Pilot GUI real testing, UX hardening, local end-to-end flow
- HITO-106+107+108: Pilot onboarding, tooltips, guided UX refactor
- HITO-109+110+111: Promote candidate flow, supervised playback v0, first usable business flow
- HITO-112+113+114: Demo/real separation, preflight policy cleanup, fixture governance
- HITO-115: UIA CacheRequest snapshot v1
- HITO-116+117: RuntimeId / stable element identity and wait events v0

After that documented sequence, the explicit recommendation at the time was HITO-118+119+120 as the next formal block:

- HITO-118+119+120: Executor harness tests, first supervised benign real click, post-action verification and evidence

## What Actually Happened After the Formal Plan

HITO-118 through HITO-129 were executed as a technical extension of the platform, centered on executor harness hardening, evidence, replay, run trace linking, and related perception/executor safety work.

Those blocks are valid technical progress, but they were not part of the original explicit roadmap list captured in the earlier handoffs. In practice, the program moved from the planned Pilot/recording/approval path into deeper executor/perception infrastructure.

That means the roadmap and the implementation timeline diverged.

## Reconciliation Result

The project should now treat HITO-121 through HITO-129 as:

- executed technical extension work
- useful and preserved
- not a reason to continue inventing new HITO numbers without another explicit planning lock

The next sequence after HITO-129 must be blocked and re-approved before further implementation continues.

## Candidate Branch Preserved But Not Approved

There is a preserved branch with additional multi-step harness work:

- `candidate-multistep-harness-step-evidence-failure-recovery`

This branch exists as preserved technical work only.

It is not approved as an official HITO-130+131+132 closure.
It is not merged into `master`.
It should be evaluated later against the reconciled roadmap before publication.

## Governance Decision

From this point:

- HITO-130+ must not be inferred on the fly
- new implementation blocks must be explicitly named and locked before coding
- candidate branches that explore valid technical ideas may exist, but they do not become official roadmap closures until approved

## Practical Consequence

The roadmap after HITO-129 is reset to a deliberate execution plan, documented separately in:

- `docs/roadmap/next-execution-plan-after-hito-129.md`
