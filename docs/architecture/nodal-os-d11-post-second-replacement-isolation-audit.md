# NODAL OS D11 Post-Second-Replacement Isolation Audit

Date: 2026-07-07

Mode: test/audit/docs-only. This block changes no `src/`, implements no third replacement, changes no CI, enables no runtime/product behavior, opens no public/product surface, registers no services, creates no command handlers, touches no Product Ledger runtime/latest-state/handoff/writer paths and makes no release/commercial claim.

Decision: `GO_WITH_FINDINGS_POST_SECOND_REPLACEMENT_ISOLATION_EQUIVALENCE_AUDIT_READY`.

## Scope

D11 audits the D10 second minimal replacement:

- D7 target: `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`.
- D10 target: `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`.
- D4 candidate: `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`.

D11 adds focused Safety evidence only:

- `tests/OneBrain.Safety.Tests/ApprovalExecutionPostSecondReplacementD11Tests.cs`.

## Audit Result

D10 remains equivalent and isolated after the second replacement.

Verified:

- D10 command/execution exception remains exact to `ApprovalExecutionDesignOnlyProtected.cs`.
- D7 and D10 exceptions are independent file-exact exceptions, not a broad command/execution allowlist.
- Candidate references remain limited to the D4 candidate source, D7 target, D10 target, Safety tests and docs/logs.
- Candidate references are absent from Pilot, CLI, command paths, CI, AgentOperations, Browser/CDP/runtime/perception paths and external automation roots.
- `ApprovalExecutionDesignOnlyProtected` remains read-only, design-only, preview-only and blocked/no-go.
- D4 remains non-authoritative and cannot override existing hard-block decisions.
- D1/D2 remain test/design-only and were not promoted into production source.
- D7+D10 together do not create a common authority by accumulation.
- Existing hard-block tests remain authoritative.

## Command/Execution Boundary

The D10 source contains command/execution vocabulary only as blocked design evidence:

- `CommandHandlerAvailable` remains false.
- `NoCommandHandler` remains true.
- previews are labels and do not execute approvals.
- no `IServiceCollection`, service registration, route mapping, command handler, process, shell, file-write, DB, network, KMS or WORM implementation is introduced.

D11 confirms that similar future files would not be automatically allowed: path checks are exact to D7 and D10, not name-pattern based.

## Reference Boundary

Allowed source references after D11 remain exactly:

- `src/OneBrain.Core/Approval/NodalOsCommonBoundaryClaimsCandidate.cs`.
- `src/OneBrain.Core/Approval/ReentryDecisionPacketReadOnly.cs`.
- `src/OneBrain.Core/Approval/ApprovalExecutionDesignOnlyProtected.cs`.

No candidate reference is allowed in:

- Pilot or CLI.
- command/handler paths.
- route/DI/service registration paths.
- Product Ledger runtime, latest-state, handoff, writer or public/product paths.
- Browser/CDP/WCU/OCR/Recipes live paths.
- provider/cloud/network/DB/KMS/WORM/external-trust paths.
- CI/workflow files.

## Source Bloat Trajectory

- D7 net source impact: `+70`.
- D10 net source impact: `+70`.
- cumulative D7+D10 source impact is net `+140` source lines.
- source bloat reduction remains `0%`.

The D-series has so far proven equivalence/isolation, not reduced source bloat. A future block should either remain explicitly proof-only and say so, or shift to a separate source-reduction plan/audit with rollback criteria.

## Preserved Non-Goals

- no `src/` changes in D11.
- no D4 candidate modification.
- no D7 source target modification.
- no D10 source target modification.
- no third replacement.
- no new source candidate.
- no CI change.
- no route/DI/service registration.
- no command/shell/subprocess capability.
- no Product Ledger runtime/latest-state/handoff/writer activation.
- no public/product exposure.
- no Production route.
- no latest pointer, active read precedence or product authority.
- no provider/cloud/network, DB/migration, KMS/WORM or external trust.
- no Browser/CDP/WCU/OCR/Recipes live behavior.
- no release/commercial readiness.

## Findings

- P0: 0.
- P1: 0.
- P2: 0.
- P3: a third proof-only replacement would further increase source size unless it is explicitly planned as proof-only.
- P4: D7+D10 evidence is stronger, but real source bloat reduction is still `0%`.

## Validation Evidence

- Core build: PASS, 0 warnings / 0 errors.
- Pilot build: PASS, 0 warnings / 0 errors.
- Solution build: PASS, 1 inherited Recipes analyzer warning / 0 errors.
- Product Ledger Safety: 275/275 PASS.
- Product Ledger Recipes: 72/72 PASS.
- `TestCategory=NodalOsTier1Safety`: 127/127 PASS.
- `TestCategory=CommonContracts`: 101/101 PASS.
- `TestCategory=SourceCandidate`: 82/82 PASS.
- `TestCategory=NoRuntimeWiring`: 101/101 PASS.
- `TestCategory=NoAuthority`: 63/63 PASS.
- `TestCategory=NoDoubleTruth`: 63/63 PASS.
- `TestCategory=PostReplacementAudit`: 37/37 PASS.
- `TestCategory=ApprovalExecution`: 27/27 PASS.
- Static guard catalog: 9/9 PASS.
- PublicProductBlock: 46/46 PASS.
- ProductionRouteBlock: 39/39 PASS.
- Reentry Safety: 28/28 PASS.
- Reentry Recipes: 4/4 PASS.
- ApprovalExecution Safety: 16/16 PASS.
- ApprovalExecution Recipes: 3/3 PASS.
- D11 focused: 12/12 PASS.
- Safety discovery: 6469 tests.
- Recipes discovery: 1580 tests.
- Post-D10 no-runtime reference scan: PASS.
- D7/D10 command/runtime exception scan: PASS.
- Added-line forbidden enablement scan: PASS.
- `git diff --check`: PASS.

## Next Recommended Block

`D12 source-reduction plan/audit only`.

Reason: after two additive proof-only replacements and a cumulative `+140` net source line impact, the healthier next step is a docs/audit-only reduction plan that decides whether to pursue actual safe source reduction or pause. Do not default to a third proof-only replacement.
