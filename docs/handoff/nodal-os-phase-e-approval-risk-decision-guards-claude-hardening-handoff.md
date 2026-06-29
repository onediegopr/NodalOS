# Handoff: Phase E Approval Risk Decision Guards With Claude Hardening

Decision target: `GO_PHASE_E_APPROVAL_RISK_DECISION_GUARDS_WITH_CLAUDE_HARDENING_READY`

## Summary

Phase E now has read-only risk/decision guard contracts and fixture coverage for approval preview decisions. The guard remains in-memory and does not execute approval, mutate approval state or expose product actions.

## Files Touched

- `src/OneBrain.Core/Approval/ApprovalRiskDecisionReadOnlyGuard.cs`
  - Adds the read-only risk/decision guard contract, fixtures and evaluator.
- `src/OneBrain.Core/Evidence/EvidenceIntelligencePersistenceDesign.cs`
  - Removes unreachable schema compatibility `Rejected` branch and keeps unsafe schema compatibility fail-closed as `Blocked`.
- `tests/OneBrain.Recipes.Tests/ApprovalHumanReviewReadOnlyFoundationTests.cs`
  - Adds risk/decision guard functional fixture coverage.
- `tests/OneBrain.Recipes.Tests/EvidenceIntelligencePersistenceDesignTests.cs`
  - Adds schema compatibility decision model coverage.
- `tests/OneBrain.Safety.Tests/ApprovalHumanReviewReadOnlyFoundationSafetyTests.cs`
  - Adds risk/decision no-side-effect, no writer/policy reference and no artifact file creation checks.
- `tests/OneBrain.Safety.Tests/WorkspaceContextReadOnlyFoundationSafetyTests.cs`
  - Adds exact expected decision assertions and Blocked/Excluded distinction for Phase D guard fixtures.
- `docs/adr/phase-e-approval-risk-decision-guards-claude-hardening.md`
  - Documents guard design and Claude P2/P3 disposition.
- `docs/qa/phase-e-approval-risk-decision-guards-claude-hardening/report.md`
  - Records QA scope, validation matrix and risk posture.
- `docs/handoff/nodal-os-phase-e-approval-risk-decision-guards-claude-hardening-handoff.md`
  - This handoff.

## Status

- Approval execution: disabled.
- Approval state mutation: disabled.
- Product actions: disabled.
- Runtime/live: disabled.
- Filesystem product IO: disabled.
- DB/dependency: disabled.
- Provider/cloud: disabled.
- Semantic/vector: disabled.
- LLM live: disabled.
- Durable memory: disabled.
- Release/commercial: NO-GO.

## Claude P2/P3 Disposition

- `P2-001`: mitigated with source scan tests and ADR constraint; preexisting writer remains untouched.
- `P2-002`: documented; mitigated with scans and non-product artifact snapshot test.
- `P2-003`: fixed by collapsing unreachable `Rejected` to `Blocked`.
- `P2-004`: fixed with expected-decision assertions in Phase D guard tests.
- `P3-001`: reinforced with expected issue assertions.
- `P3-002`: actual command equivalents documented in QA/final validation.
- `P3-003`: deferred as P3 debt.
- `P3-004`: helper naming improved.

## Next Recommended Block

`PHASE_E_HUMAN_REVIEW_EVIDENCE_CONTEXT_LINKS_READ_ONLY`

Rationale: after risk/decision guards, the next safe step is to harden the evidence/context links that support human review before any visible approval packet surface.
