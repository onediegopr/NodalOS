using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BrowserRuntimeRepeatabilityEvidencePlanRevalidation")]
[TestCategory("M1137")]
[TestCategory("M1138")]
[TestCategory("M1139")]
[TestCategory("M1140")]
[TestCategory("M1141")]
[TestCategory("M1142")]
[TestCategory("M1143")]
[TestCategory("M1144")]
[TestCategory("M1145")]
[TestCategory("M1146")]
[TestCategory("M1147")]
[TestCategory("M1148")]
[TestCategory("M1137M1148")]
public sealed class NodalOsBrowserRuntimeRepeatabilityEvidencePlanRevalidationM1137M1148Tests
{
    private const string IntakePath = "artifacts/agent-operations/m1137/repeatability-plan-revalidation-intake.json";
    private const string SchemaPath = "artifacts/agent-operations/m1138/evidence-schema-consistency-revalidation.json";
    private const string ThresholdPath = "artifacts/agent-operations/m1139/threshold-integrity-revalidation.json";
    private const string EligibilityPath = "artifacts/agent-operations/m1140/resolution-eligibility-revalidation.json";
    private const string ClaimGuardPath = "artifacts/agent-operations/m1141/claim-guard-revalidation.json";
    private const string RunbookPath = "artifacts/agent-operations/m1142/evidence-collection-runbook-revalidation.json";
    private const string FutureExecutionPath = "artifacts/agent-operations/m1143/future-evidence-execution-eligibility-matrix.json";
    private const string AbortPath = "artifacts/agent-operations/m1144/evidence-execution-abort-matrix.json";
    private const string NextPath = "artifacts/agent-operations/m1145/next-path-decision-matrix.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m1146/go-no-go-finalization.json";
    private const string FinalReportArtifactPath = "artifacts/agent-operations/m1147/final-report.json";
    private const string FinalValidationPath = "artifacts/agent-operations/m1148/final-artifacts-validations.json";
    private const string BlockGoNoGoPath = "artifacts/agent-operations/m1137-m1148/browserruntime-repeatability-evidence-plan-revalidation-go-no-go.json";
    private const string ReportPath = "docs/reports/m1148-browserruntime-repeatability-evidence-plan-revalidation.md";

    [TestMethod]
    public void revalidation_intake_records_positive_observations_but_unresolved_caveat()
    {
        using var doc = ReadJson(IntakePath);

        Assert.AreEqual("f74d0749ae15826711b60f0a70090edae4dab657", String(doc, "base_commit"));
        Assert.AreEqual("PASS", String(doc.RootElement.GetProperty("latest_observed_isolated_browser_runtime_smoke"), "result"));
        Assert.AreEqual(30, doc.RootElement.GetProperty("latest_observed_isolated_browser_runtime_smoke").GetProperty("passed").GetInt32());
        Assert.AreEqual(0, doc.RootElement.GetProperty("latest_observed_isolated_browser_runtime_smoke").GetProperty("skipped").GetInt32());
        Assert.AreEqual("VISIBLE_UNRESOLVED", String(doc, "current_caveat_state"));
        Assert.AreEqual("NOT_ELIGIBLE_EVIDENCE_PENDING", String(doc, "current_eligibility_state"));
        CollectionAssert.Contains(Array(doc, "resolution_blocked_by"), "three-run isolated threshold not collected in this block");
        CollectionAssert.Contains(Array(doc, "not_executed"), "runtime real");
        CollectionAssert.Contains(Array(doc, "not_executed"), "Bridge/CSP modification");
    }

    [TestMethod]
    public void schema_consistency_requires_30_0_0_0_clean_gate9_and_no_websocket_aborted()
    {
        using var doc = ReadJson(SchemaPath);
        var isolated = doc.RootElement.GetProperty("isolated_run_requirements");

        Assert.AreEqual(3, isolated.GetProperty("required_runs").GetInt32());
        Assert.AreEqual(30, isolated.GetProperty("passed_count").GetInt32());
        Assert.AreEqual(0, isolated.GetProperty("skipped_count").GetInt32());
        Assert.AreEqual(0, isolated.GetProperty("failed_count").GetInt32());
        Assert.AreEqual(0, isolated.GetProperty("inconclusive_count").GetInt32());
        Assert.AreEqual("CLEAN", String(isolated, "Gate9_status"));
        Assert.IsFalse(isolated.GetProperty("WebSocket_aborted").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("suite_schema_requirements").GetProperty("timeout_can_count_as_pass").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("suite_schema_requirements").GetProperty("hidden_caveat_allowed").GetBoolean());
        CollectionAssert.Contains(Array(doc, "boundary_scan_requirements"), "product/Bridge/CSP scan");
        CollectionAssert.Contains(Array(doc, "boundary_scan_requirements"), "false-clean-claim scan");
    }

    [TestMethod]
    public void threshold_integrity_requires_three_runs_not_one()
    {
        using var doc = ReadJson(ThresholdPath);
        var threshold = doc.RootElement.GetProperty("required_threshold");

        Assert.AreEqual(3, threshold.GetProperty("consecutive_isolated_browserruntime_runs").GetInt32());
        Assert.IsFalse(threshold.GetProperty("websocket_aborted_allowed").GetBoolean());
        Assert.IsTrue(threshold.GetProperty("formal_review_required").GetBoolean());
        Assert.AreEqual("POSITIVE_EVIDENCE_ONLY", String(doc.RootElement.GetProperty("latest_observed_single_clean_isolated_run"), "status"));
        Assert.IsTrue(doc.RootElement.GetProperty("latest_observed_single_clean_isolated_run").GetProperty("insufficient_for_resolution").GetBoolean());
        Assert.AreEqual("NOT_ELIGIBLE_EVIDENCE_PENDING", String(doc, "resolution_eligibility"));
        Assert.AreEqual(95, doc.RootElement.GetProperty("confidence").GetInt32());
    }

    [TestMethod]
    public void eligibility_remains_pending_and_rejects_missing_threshold_items()
    {
        using var doc = ReadJson(EligibilityPath);
        var rejectionCases = Array(doc, "rejection_cases");

        Assert.AreEqual("NOT_ELIGIBLE_EVIDENCE_PENDING", String(doc, "resolution_eligibility"));
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
        Assert.AreEqual(95, doc.RootElement.GetProperty("confidence").GetInt32());
        Assert.IsFalse(doc.RootElement.GetProperty("suite_clean_claim_allowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("repeatability_proven").GetBoolean());
        CollectionAssert.Contains(rejectionCases, "less than 3 isolated runs");
        CollectionAssert.Contains(rejectionCases, "any WebSocket aborted");
        CollectionAssert.Contains(rejectionCases, "missing formal review");
    }

    [TestMethod]
    public void claim_guard_blocks_false_claims_while_not_eligible()
    {
        using var doc = ReadJson(ClaimGuardPath);
        var blocked = Array(doc, "blocked_while_not_eligible_or_unresolved");

        CollectionAssert.Contains(blocked, "caveat-resolved");
        CollectionAssert.Contains(blocked, "suite-clean");
        CollectionAssert.Contains(blocked, "confidence above 95");
        CollectionAssert.Contains(blocked, "release-ready");
        CollectionAssert.Contains(blocked, "manual-QA-ready");
        CollectionAssert.Contains(blocked, "runtime-ready");
        CollectionAssert.Contains(blocked, "PC-Commander-ready");
        CollectionAssert.Contains(blocked, "repeatability-proven");
        Assert.AreEqual("NO-GO", String(doc, "manual_qa_execution"));
        Assert.AreEqual("NO-GO", String(doc, "runtime_real"));
    }

    [TestMethod]
    public void runbook_remains_plan_only_and_forbids_mutation_or_secret_dumping()
    {
        using var doc = ReadJson(RunbookPath);

        Assert.AreEqual("PLAN_ONLY_REVALIDATED", String(doc, "runbook_status"));
        Assert.IsFalse(doc.RootElement.GetProperty("execution_started").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("evidence_collection_executed").GetBoolean());
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "manual QA real");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "runtime real");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "product/Bridge/CSP modification");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "filesystem cleanup");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "process kill");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "secret/log dumping");
    }

    [TestMethod]
    public void future_execution_requires_exact_phrase_and_defaults_to_not_requested()
    {
        using var doc = ReadJson(FutureExecutionPath);

        Assert.AreEqual("EXECUTE_BROWSERRUNTIME_REPEATABILITY_EVIDENCE_COLLECTION_ONLY", String(doc, "future_execution_phrase_required"));
        Assert.IsFalse(doc.RootElement.GetProperty("phrase_present_in_current_user_request").GetBoolean());
        Assert.AreEqual("NOT_REQUESTED", String(doc, "future_execution_status"));
        Assert.AreEqual("plan-only/revalidation-only", String(doc, "default_next_block"));
        CollectionAssert.Contains(Array(doc.RootElement.GetProperty("if_phrase_present_in_future"), "still_forbidden"), "product changes");
        CollectionAssert.Contains(Array(doc.RootElement.GetProperty("if_phrase_present_in_future"), "still_forbidden"), "cleanup mutation");
        Assert.AreEqual("NO-GO", String(doc, "manual_qa_execution"));
    }

    [TestMethod]
    public void abort_matrix_rejects_single_run_resolution_and_cleanup_mutation()
    {
        using var doc = ReadJson(AbortPath);
        var abortIf = Array(doc, "abort_if");

        CollectionAssert.Contains(abortIf, "single run treated as resolution");
        CollectionAssert.Contains(abortIf, "timeout counted as pass");
        CollectionAssert.Contains(abortIf, "confidence above 95 claimed");
        CollectionAssert.Contains(abortIf, "process kill requested");
        CollectionAssert.Contains(abortIf, "temp cleanup mutation requested");
        CollectionAssert.Contains(abortIf, "product files changed");
        CollectionAssert.Contains(abortIf, "Bridge/CSP changed");
        Assert.AreEqual("NOT_REQUESTED_NOT_EXECUTED", String(doc, "current_block_execution"));
    }

    [TestMethod]
    public void next_path_defaults_to_hold_gate_without_execution_phrase()
    {
        using var doc = ReadJson(NextPath);
        var decisions = doc.RootElement.GetProperty("decisions");

        Assert.AreEqual("M1149-M1160 - Repeatability Evidence Plan Hold + Operator/Execution Request Gate", String(decisions, "NO_EXECUTION_PHRASE"));
        Assert.AreEqual("M1149-M1160 - BrowserRuntimeSmoke Repeatability Evidence Collection Execution, no product/runtime changes", String(decisions, "EXECUTION_PHRASE_APPEARS"));
        Assert.AreEqual("M1149-M1160 - Gate 9 Retry/Quarantine Policy Remediation", String(decisions, "GATE9_FAILS_AGAIN"));
        Assert.AreEqual("M1149-M1160 - Repeatability Evidence Plan Hold + Operator/Execution Request Gate", String(doc, "recommendation"));
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(doc, "operator_confirmation"));
    }

    [TestMethod]
    public void go_no_go_preserves_runtime_product_bridge_and_release_boundaries()
    {
        using var doc = ReadJson(GoNoGoPath);
        var noGo = Array(doc, "no_go");

        CollectionAssert.Contains(Array(doc, "go"), "plan revalidation");
        CollectionAssert.Contains(Array(doc, "go"), "future execution eligibility matrix");
        CollectionAssert.Contains(noGo, "manual QA execution");
        CollectionAssert.Contains(noGo, "safe evidence capture real");
        CollectionAssert.Contains(noGo, "runtime real");
        CollectionAssert.Contains(noGo, "PC Commander real");
        CollectionAssert.Contains(noGo, "provider/cloud");
        CollectionAssert.Contains(noGo, "product files");
        CollectionAssert.Contains(noGo, "Bridge/CSP");
        CollectionAssert.Contains(noGo, "release/store");
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
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

        Assert.AreEqual("BROWSERRUNTIMESMOKE_REPEATABILITY_EVIDENCE_PLAN_REVALIDATED_WITH_EXTERNAL_SMOKE_CAVEAT", String(doc, "decision"));
        Assert.AreEqual("PROTOCOL_HARDENING_ONLY", String(doc, "route_taken"));
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(doc, "operator_confirmation"));
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
        IntakePath, SchemaPath, ThresholdPath, EligibilityPath, ClaimGuardPath, RunbookPath, FutureExecutionPath,
        AbortPath, NextPath, GoNoGoPath, FinalReportArtifactPath, FinalValidationPath, BlockGoNoGoPath, ReportPath
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
