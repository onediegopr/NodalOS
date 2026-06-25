using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("RepeatabilityEvidencePlanHold")]
[TestCategory("M1149")]
[TestCategory("M1150")]
[TestCategory("M1151")]
[TestCategory("M1152")]
[TestCategory("M1153")]
[TestCategory("M1154")]
[TestCategory("M1155")]
[TestCategory("M1156")]
[TestCategory("M1157")]
[TestCategory("M1158")]
[TestCategory("M1159")]
[TestCategory("M1160")]
[TestCategory("M1149M1160")]
public sealed class NodalOsRepeatabilityEvidencePlanHoldM1149M1160Tests
{
    private const string IntakePath = "artifacts/agent-operations/m1149/repeatability-evidence-plan-hold-intake.json";
    private const string PhraseGatePath = "artifacts/agent-operations/m1150/exact-phrase-requirement-gate.json";
    private const string ContextClassifierPath = "artifacts/agent-operations/m1151/execution-phrase-context-classifier.json";
    private const string HoldStatePath = "artifacts/agent-operations/m1152/hold-state-revalidation.json";
    private const string AbortPath = "artifacts/agent-operations/m1153/abort-matrix-revalidation.json";
    private const string ClaimGuardPath = "artifacts/agent-operations/m1154/claim-guard-hold-revalidation.json";
    private const string ScanPath = "artifacts/agent-operations/m1155/leak-and-false-clean-scan-revalidation.json";
    private const string BoundaryPath = "artifacts/agent-operations/m1156/product-bridge-csp-boundary-revalidation.json";
    private const string FutureRoutePath = "artifacts/agent-operations/m1157/future-execution-route-preconditions.json";
    private const string NextPath = "artifacts/agent-operations/m1158/next-path-decision-matrix.json";
    private const string FinalReportArtifactPath = "artifacts/agent-operations/m1159/final-report.json";
    private const string FinalValidationPath = "artifacts/agent-operations/m1160/final-artifacts-validations.json";
    private const string BlockGoNoGoPath = "artifacts/agent-operations/m1149-m1160/repeatability-evidence-plan-hold-go-no-go.json";
    private const string ReportPath = "docs/reports/m1160-repeatability-evidence-plan-hold-execution-request-gate.md";

    [TestMethod]
    public void hold_intake_records_pending_statuses_and_no_go_locks()
    {
        using var doc = ReadJson(IntakePath);

        Assert.AreEqual("6e9701b1da17b3d175d8bd77589e603450dd25fd", String(doc, "base_commit"));
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(doc, "operator_confirmation"));
        Assert.AreEqual("NOT_STARTED", String(doc, "safe_evidence_capture"));
        Assert.AreEqual("NOT_ELIGIBLE_EVIDENCE_PENDING", String(doc, "resolution_eligibility"));
        Assert.AreEqual("VISIBLE_UNRESOLVED", String(doc, "browserruntime_caveat"));
        Assert.AreEqual(95, doc.RootElement.GetProperty("full_suite_confidence").GetInt32());
        Assert.AreEqual("NO-GO", String(doc, "manual_qa_execution"));
        Assert.IsFalse(doc.RootElement.GetProperty("product_files_modified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridge_csp_modified").GetBoolean());
    }

    [TestMethod]
    public void exact_phrase_gate_rejects_prompt_rule_context_and_keeps_execution_not_requested()
    {
        using var doc = ReadJson(PhraseGatePath);

        Assert.AreEqual("EXECUTE_BROWSERRUNTIME_REPEATABILITY_EVIDENCE_COLLECTION_ONLY", String(doc, "required_phrase"));
        Assert.AreEqual("PROMPT_RULE_NOT_DIRECT_EXECUTION_INSTRUCTION", String(doc, "current_request_phrase_context"));
        Assert.IsFalse(doc.RootElement.GetProperty("direct_execution_instruction_detected").GetBoolean());
        Assert.AreEqual("NOT_REQUESTED", String(doc, "future_execution_status"));
        Assert.IsFalse(doc.RootElement.GetProperty("if_phrase_absent_or_rule_only").GetProperty("evidence_collection_executed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("if_phrase_absent_or_rule_only").GetProperty("resolution_transition_executed").GetBoolean());
        CollectionAssert.Contains(Array(doc.RootElement.GetProperty("if_phrase_direct_in_future"), "forbidden_scope"), "process kill");
        CollectionAssert.Contains(Array(doc.RootElement.GetProperty("if_phrase_direct_in_future"), "forbidden_scope"), "cleanup mutation");
    }

    [TestMethod]
    public void context_classifier_allows_only_direct_user_execution_instruction()
    {
        using var doc = ReadJson(ContextClassifierPath);
        var ineligible = Array(doc, "ineligible_contexts");

        Assert.AreEqual("DIRECT_USER_EXECUTION_INSTRUCTION", String(doc, "eligible_context"));
        CollectionAssert.Contains(ineligible, "PROMPT_RULE");
        CollectionAssert.Contains(ineligible, "DOC_EXAMPLE");
        CollectionAssert.Contains(ineligible, "CODE_FENCE");
        CollectionAssert.Contains(ineligible, "AMBIGUOUS_TEXT");
        Assert.AreEqual("PROMPT_RULE", String(doc, "current_context_classification"));
        Assert.IsFalse(doc.RootElement.GetProperty("current_context_eligible").GetBoolean());
        Assert.AreEqual("NOT_REQUESTED", String(doc, "execution_request_status"));
    }

    [TestMethod]
    public void hold_state_preserves_plan_only_and_unresolved_caveat()
    {
        using var doc = ReadJson(HoldStatePath);

        Assert.AreEqual("ACTIVE", String(doc, "hold_state"));
        Assert.IsTrue(doc.RootElement.GetProperty("plan_only").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("revalidation_only").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("evidence_collection_executed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("resolution_transition_executed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("repeatability_proven").GetBoolean());
        Assert.AreEqual(95, doc.RootElement.GetProperty("confidence").GetInt32());
        CollectionAssert.Contains(Array(doc, "no_go_locks"), "runtime real");
        CollectionAssert.Contains(Array(doc, "no_go_locks"), "release/store");
    }

    [TestMethod]
    public void abort_matrix_rejects_drift_false_clean_and_mutation_requests()
    {
        using var doc = ReadJson(AbortPath);
        var abortIf = Array(doc, "abort_if");

        CollectionAssert.Contains(abortIf, "product files changed");
        CollectionAssert.Contains(abortIf, "Bridge/CSP changed");
        CollectionAssert.Contains(abortIf, "timeout counted as pass");
        CollectionAssert.Contains(abortIf, "single run treated as resolution");
        CollectionAssert.Contains(abortIf, "confidence above 95 claimed");
        CollectionAssert.Contains(abortIf, "process kill requested");
        CollectionAssert.Contains(abortIf, "temp cleanup mutation requested");
        Assert.AreEqual("NOT_REQUESTED_NOT_EXECUTED", String(doc, "current_block_execution"));
        Assert.AreEqual("NO-GO", String(doc, "runtime_real"));
    }

    [TestMethod]
    public void claim_guard_blocks_ready_release_clean_and_repeatability_claims()
    {
        using var doc = ReadJson(ClaimGuardPath);
        var blocked = Array(doc, "blocked_while_hold_active");

        CollectionAssert.Contains(blocked, "manual-QA-ready");
        CollectionAssert.Contains(blocked, "runtime-ready");
        CollectionAssert.Contains(blocked, "PC-Commander-ready");
        CollectionAssert.Contains(blocked, "release-ready");
        CollectionAssert.Contains(blocked, "suite-clean");
        CollectionAssert.Contains(blocked, "caveat-resolved");
        CollectionAssert.Contains(blocked, "confidence above 95");
        CollectionAssert.Contains(blocked, "repeatability-proven");
        Assert.IsFalse(doc.RootElement.GetProperty("product_files_modified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridge_csp_modified").GetBoolean());
    }

    [TestMethod]
    public void scans_require_no_leaks_and_no_false_clean_claims()
    {
        using var doc = ReadJson(ScanPath);

        Assert.IsTrue(doc.RootElement.GetProperty("leak_scan_required").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("false_clean_claim_scan_required").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("raw_logs_allowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("secret_values_allowed").GetBoolean());
        CollectionAssert.Contains(Array(doc, "forbidden_claim_patterns"), "confidence above 95");
        CollectionAssert.Contains(Array(doc, "forbidden_claim_patterns"), "repeatability-proven");
    }

    [TestMethod]
    public void boundary_revalidation_allows_only_tests_docs_artifacts()
    {
        using var doc = ReadJson(BoundaryPath);

        CollectionAssert.Contains(Array(doc, "allowed_paths"), "tests/OneBrain.Safety.Tests");
        CollectionAssert.Contains(Array(doc, "allowed_paths"), "docs/reports");
        CollectionAssert.Contains(Array(doc, "allowed_paths"), "artifacts/agent-operations");
        Assert.IsFalse(doc.RootElement.GetProperty("product_files_modified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridge_csp_modified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtime_real_modified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("release_store_modified").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("scope_scan_required").GetBoolean());
    }

    [TestMethod]
    public void future_route_requires_phrase_and_does_not_run_collection_now()
    {
        using var doc = ReadJson(FutureRoutePath);

        Assert.AreEqual("EXECUTE_BROWSERRUNTIME_REPEATABILITY_EVIDENCE_COLLECTION_ONLY", String(doc, "required_direct_phrase"));
        Assert.IsFalse(doc.RootElement.GetProperty("current_phrase_direct").GetBoolean());
        Assert.AreEqual("NOT_REQUESTED", String(doc, "future_execution_status"));
        CollectionAssert.Contains(Array(doc, "preconditions_if_requested_later"), "no product files changed");
        CollectionAssert.Contains(Array(doc, "preconditions_if_requested_later"), "no process kill");
        CollectionAssert.Contains(Array(doc, "preconditions_if_requested_later"), "no cleanup mutation");
        Assert.IsFalse(doc.RootElement.GetProperty("current_block_runs_collection").GetBoolean());
    }

    [TestMethod]
    public void next_path_defaults_to_hold_continuity_without_direct_phrase()
    {
        using var doc = ReadJson(NextPath);
        var matrix = doc.RootElement.GetProperty("decision_matrix");

        Assert.AreEqual("M1161-M1172 - Repeatability Evidence Plan Hold Continuity + Execution Phrase Watch", String(matrix, "NO_DIRECT_EXECUTION_PHRASE"));
        Assert.AreEqual("M1161-M1172 - BrowserRuntimeSmoke Repeatability Evidence Collection Execution, no product/runtime changes", String(matrix, "DIRECT_EXECUTION_PHRASE_APPEARS"));
        Assert.AreEqual("M1161-M1172 - Gate 9 Retry/Quarantine Policy Remediation", String(matrix, "GATE9_FAILS_AGAIN"));
        Assert.AreEqual("M1161-M1172 - Repeatability Evidence Plan Hold Continuity + Execution Phrase Watch", String(doc, "recommended_next"));
        Assert.AreEqual(95, doc.RootElement.GetProperty("confidence").GetInt32());
    }

    [TestMethod]
    public void final_artifacts_exist_and_record_expected_decision()
    {
        foreach (var path in AllPaths())
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }

        using var doc = ReadJson(FinalValidationPath);

        Assert.AreEqual("REPEATABILITY_EVIDENCE_PLAN_HOLD_READY_WITH_EXTERNAL_SMOKE_CAVEAT", String(doc, "decision"));
        Assert.AreEqual("PROTOCOL_HARDENING_ONLY", String(doc, "route_taken"));
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(doc, "operator_confirmation"));
        Assert.AreEqual("NOT_REQUESTED", String(doc, "future_execution_status"));
        Assert.AreEqual("NOT_STARTED", String(doc, "safe_evidence_capture"));
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
        Assert.AreEqual("NO-GO", String(doc, "manual_qa_execution"));
        Assert.AreEqual("NO-GO", String(doc, "runtime_real"));
        Assert.AreEqual("NO-GO", String(doc, "pc_commander_real"));
        Assert.IsFalse(doc.RootElement.GetProperty("product_files_modified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridge_csp_modified").GetBoolean());
    }

    [TestMethod]
    public void artifacts_contain_no_secret_leaks_or_forbidden_clean_claims()
    {
        foreach (var file in AllPaths())
        {
            var content = File.ReadAllText(FullPath(file));
            foreach (var forbidden in new[] { "sk-", "AKIA", "BEGIN PRIVATE KEY", "Authorization: Bearer", "full suite clean", "100% confidence", "caveat resolved", "release ready", "manual QA ready", "runtime ready", "PC Commander ready", "repeatability proven", "evidence captured" })
            {
                Assert.IsFalse(content.Contains(forbidden, StringComparison.Ordinal), $"{file} contains forbidden wording {forbidden}");
            }
        }
    }

    private static string[] AllPaths() =>
    [
        IntakePath, PhraseGatePath, ContextClassifierPath, HoldStatePath, AbortPath, ClaimGuardPath, ScanPath,
        BoundaryPath, FutureRoutePath, NextPath, FinalReportArtifactPath, FinalValidationPath, BlockGoNoGoPath, ReportPath
    ];

    private static string FullPath(string relativePath) => Path.Combine(RepoRoot(), relativePath);

    private static JsonDocument ReadJson(string relativePath) => JsonDocument.Parse(File.ReadAllText(FullPath(relativePath)));

    private static string String(JsonDocument doc, string property) => doc.RootElement.GetProperty(property).GetString() ?? string.Empty;

    private static string String(JsonElement element, string property) => element.GetProperty(property).GetString() ?? string.Empty;

    private static string[] Array(JsonDocument doc, string property) => Array(doc.RootElement, property);

    private static string[] Array(JsonElement element, string property) => element.GetProperty(property).EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? Environment.CurrentDirectory;
    }
}
