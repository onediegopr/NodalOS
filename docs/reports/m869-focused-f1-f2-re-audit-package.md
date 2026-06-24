# M869 - Focused F1/F2 Re-Audit Package

Project: NODAL OS.

Audited commit: `b7457b86d4d5704c7567fc67035c8117fc54e5c0`.

Baseline before remediation: `fe0f63b80c0e83b8a514ff30b1754d467ebf10de`.

Diff summary from `fe0f63b` to `b7457b8`: 7 files changed, 1805 insertions, 36 deletions. Scope stayed in tests/safety, docs/reports, and artifacts/agent-operations.

Files touched in M863-M868:

- `artifacts/agent-operations/m863-m868/audit-quality-remediation-go-no-go.json`
- `artifacts/agent-operations/m863-m868/claude-partial-recovery.patch`
- `docs/reports/m868-audit-quality-remediation-report.md`
- `tests/OneBrain.Safety.Tests/NodalOsAuditQualityRemediationM863M868Tests.cs`
- `tests/OneBrain.Safety.Tests/SimulatedAuditTimelineReplay.cs`
- `tests/OneBrain.Safety.Tests/SimulatedDryRunOrchestrator.cs`
- `tests/OneBrain.Safety.Tests/SimulatedRedactor.cs`

F1 original finding: `NoExecutionProof` and `SideEffectSinkInvocations` were too hardcoded or tautological.

F1 remediation: `NoExecutionProof.SideEffectSinkInvocations` now derives from `RecordingSideEffectSink.InvocationCount` through `NoExecutionProof.FromSink`. Tests inject side-effect records and assert the proof becomes non-clean.

F2 original finding: adversarial redaction was theatrical and did not use fake secret-like payloads.

F2 remediation: fake secret-like values are injected into `RawAuditPayload`, passed through `SimulatedRedactor` and exported through `SimulatedRedactingExporter`. Raw values are removed from the final export when redaction is enabled, and the disabled path proves the test is non-vacuous.

Validation evidence from M863-M868:

- Build: PASS, 0 warnings, 0 errors.
- Filter M863-M868: PASS, 48 tests.
- BrowserRuntimeSmoke isolated: PASS, 20 tests.
- Full safety: PASS, 5293 passed, 37 skipped.
- Recipes: PASS, 635 passed.
- Full suite: PASS.

NO-GO boundaries remain intact: runtime productive execution disabled, provider/cloud live calls disabled, filesystem/browser/capability unlock disabled, public release NO-GO, Chrome Web Store NO-GO, signed public ZIP false, product files modified false, Bridge/CSP modified false.

Focused auditor questions are recorded in `artifacts/agent-operations/m869/focused-f1-f2-re-audit-package.json`.
