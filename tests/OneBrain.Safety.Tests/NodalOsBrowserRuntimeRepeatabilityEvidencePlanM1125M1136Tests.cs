using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BrowserRuntimeRepeatabilityEvidencePlanPrep")]
[TestCategory("M1125")]
[TestCategory("M1126")]
[TestCategory("M1127")]
[TestCategory("M1128")]
[TestCategory("M1129")]
[TestCategory("M1130")]
[TestCategory("M1131")]
[TestCategory("M1132")]
[TestCategory("M1133")]
[TestCategory("M1134")]
[TestCategory("M1135")]
[TestCategory("M1136")]
[TestCategory("M1125M1136")]
public sealed class NodalOsBrowserRuntimeRepeatabilityEvidencePlanM1125M1136Tests
{
    private const string FoundationPath = "artifacts/agent-operations/m1125/repeatability-evidence-package-foundation.json";
    private const string IsolatedSchemaPath = "artifacts/agent-operations/m1126/three-isolated-clean-runs-evidence-schema.json";
    private const string SuiteSchemaPath = "artifacts/agent-operations/m1127/full-safety-full-suite-evidence-schema.json";
    private const string BoundarySchemaPath = "artifacts/agent-operations/m1128/boundary-scan-evidence-schema.json";
    private const string ReviewPath = "artifacts/agent-operations/m1129/resolution-review-checklist.json";
    private const string EligibilityPath = "artifacts/agent-operations/m1130/resolution-eligibility-gate.json";
    private const string ClaimGuardPath = "artifacts/agent-operations/m1131/repeatability-claim-guard.json";
    private const string RunbookPath = "artifacts/agent-operations/m1132/evidence-collection-runbook-plan.json";
    private const string NextPath = "artifacts/agent-operations/m1133/next-path-decision-matrix.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m1134/go-no-go-finalization.json";
    private const string FinalReportArtifactPath = "artifacts/agent-operations/m1135/final-report.json";
    private const string FinalValidationPath = "artifacts/agent-operations/m1136/final-artifacts-validations.json";
    private const string BlockGoNoGoPath = "artifacts/agent-operations/m1125-m1136/browserruntime-repeatability-evidence-plan-go-no-go.json";
    private const string ReportPath = "docs/reports/m1136-browserruntime-repeatability-evidence-plan-prep.md";

    [TestMethod]
    public void repeatability_package_requires_three_isolated_clean_runs_and_no_go_locks()
    {
        using var doc = ReadJson(FoundationPath);

        Assert.AreEqual("3f527d289442d2f67ad81968476c0b3782049b11", String(doc, "base_commit"));
        Assert.AreEqual("chrome-lab-001-extension-local-ai-bridge", String(doc, "branch"));
        Assert.AreEqual("C:\\DESARROLLO\\NodalOS\\Codigo-m12-audit", String(doc, "worktree"));
        Assert.AreEqual("VISIBLE_UNRESOLVED", String(doc, "caveat_current_state"));
        Assert.AreEqual("REPEATABILITY_EVIDENCE_PENDING", String(doc, "target_state"));
        Assert.AreEqual(3, doc.RootElement.GetProperty("required_run_count").GetInt32());
        CollectionAssert.Contains(Array(doc, "required_validations"), "BrowserRuntimeSmoke isolated clean run x3");
        CollectionAssert.Contains(Array(doc, "required_scans"), "product/Bridge/CSP scan");
        CollectionAssert.Contains(Array(doc, "forbidden_claims"), "caveat-resolved");
        CollectionAssert.Contains(Array(doc, "no_go_locks"), "runtime real");
        CollectionAssert.Contains(Array(doc, "no_go_locks"), "release/store");
    }

    [TestMethod]
    public void isolated_run_schema_requires_30_passed_no_skip_no_fail_and_clean_gate9()
    {
        using var doc = ReadJson(IsolatedSchemaPath);

        CollectionAssert.Contains(Array(doc, "fields"), "run_id");
        CollectionAssert.Contains(Array(doc, "fields"), "timestamp");
        CollectionAssert.Contains(Array(doc, "fields"), "Gate9_status");
        CollectionAssert.Contains(Array(doc, "fields"), "WebSocket_aborted");
        Assert.AreEqual(3, doc.RootElement.GetProperty("required_runs").GetInt32());
        Assert.AreEqual(30, doc.RootElement.GetProperty("required_counts").GetProperty("passed_count").GetInt32());
        Assert.AreEqual(0, doc.RootElement.GetProperty("required_counts").GetProperty("skipped_count").GetInt32());
        Assert.AreEqual(0, doc.RootElement.GetProperty("required_counts").GetProperty("failed_count").GetInt32());
        Assert.AreEqual(0, doc.RootElement.GetProperty("required_counts").GetProperty("inconclusive_count").GetInt32());
        Assert.AreEqual("CLEAN", String(doc.RootElement.GetProperty("required_gate"), "Gate9_status"));
        Assert.IsFalse(doc.RootElement.GetProperty("required_gate").GetProperty("WebSocket_aborted").GetBoolean());
        Assert.AreEqual("SCHEMA_ONLY_NOT_EXECUTED", String(doc, "execution_status"));
    }

    [TestMethod]
    public void full_safety_full_suite_schema_rejects_timeout_as_pass_and_hidden_gate9_fail()
    {
        using var doc = ReadJson(SuiteSchemaPath);

        CollectionAssert.Contains(Array(doc, "schemas"), "full safety clean");
        CollectionAssert.Contains(Array(doc, "schemas"), "full suite general clean");
        CollectionAssert.Contains(Array(doc, "schemas"), "recipes clean");
        Assert.IsFalse(doc.RootElement.GetProperty("timeout_can_count_as_pass").GetBoolean());
        Assert.AreEqual("PASS", String(doc, "recipes_required_result"));
        Assert.IsFalse(doc.RootElement.GetProperty("hidden_gate9_fail_allowed").GetBoolean());
        CollectionAssert.Contains(Array(doc, "future_resolution_requirements"), "full safety without external caveat");
        CollectionAssert.Contains(Array(doc, "future_resolution_requirements"), "full suite general without external caveat");
    }

    [TestMethod]
    public void boundary_scan_schema_requires_product_bridge_leak_false_clean_json_and_diff_checks()
    {
        using var doc = ReadJson(BoundarySchemaPath);
        var required = Array(doc, "required_pass_scans");

        CollectionAssert.Contains(required, "product files scan");
        CollectionAssert.Contains(required, "Bridge/CSP scan");
        CollectionAssert.Contains(required, "leak scan");
        CollectionAssert.Contains(required, "false-clean-claim scan");
        CollectionAssert.Contains(required, "git diff check");
        CollectionAssert.Contains(required, "JSON parse");
        Assert.AreEqual("ALL_PASS_REQUIRED_BEFORE_ELIGIBLE_FOR_REVIEW", String(doc, "eligibility_rule"));
    }

    [TestMethod]
    public void resolution_review_checklist_requires_all_threshold_evidence_and_reviewer_decision()
    {
        using var doc = ReadJson(ReviewPath);
        var checklist = Array(doc, "checklist");

        CollectionAssert.Contains(checklist, "3 isolated clean runs verified");
        CollectionAssert.Contains(checklist, "full safety clean verified");
        CollectionAssert.Contains(checklist, "suite-clean-without-caveat evidence verified");
        CollectionAssert.Contains(checklist, "boundary scans clean");
        CollectionAssert.Contains(checklist, "no skipped/inconclusive Gate 9");
        CollectionAssert.Contains(checklist, "no WebSocket aborted");
        CollectionAssert.Contains(checklist, "no product/Bridge/CSP drift");
        CollectionAssert.Contains(checklist, "no release/store claims");
        CollectionAssert.Contains(checklist, "reviewer decision");
        CollectionAssert.Contains(checklist, "remaining caveats");
        Assert.AreEqual("REVIEW_NOT_STARTED", String(doc, "review_status"));
    }

    [TestMethod]
    public void eligibility_gate_remains_not_eligible_and_rejects_missing_or_dirty_evidence()
    {
        using var doc = ReadJson(EligibilityPath);
        var ineligible = Array(doc, "ineligible_if");

        Assert.AreEqual("NOT_ELIGIBLE_EVIDENCE_PENDING", String(doc, "resolution_eligibility"));
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
        Assert.AreEqual(95, doc.RootElement.GetProperty("confidence").GetInt32());
        CollectionAssert.Contains(ineligible, "any run missing");
        CollectionAssert.Contains(ineligible, "any skip/inconclusive");
        CollectionAssert.Contains(ineligible, "any Gate 9 fail");
        CollectionAssert.Contains(ineligible, "any WebSocket aborted");
        CollectionAssert.Contains(ineligible, "any timeout counted as PASS");
        CollectionAssert.Contains(ineligible, "any boundary scan missing");
        CollectionAssert.Contains(ineligible, "false-clean claim appears");
    }

    [TestMethod]
    public void repeatability_claim_guard_blocks_false_claims_while_not_eligible()
    {
        using var doc = ReadJson(ClaimGuardPath);
        var blocked = Array(doc, "blocked_when_not_eligible_or_unresolved");

        Assert.AreEqual("NOT_ELIGIBLE_EVIDENCE_PENDING", String(doc, "resolution_eligibility"));
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
        CollectionAssert.Contains(blocked, "caveat-resolved");
        CollectionAssert.Contains(blocked, "suite-clean");
        CollectionAssert.Contains(blocked, "confidence above 95");
        CollectionAssert.Contains(blocked, "release-ready");
        CollectionAssert.Contains(blocked, "Chrome-Web-Store-ready");
        CollectionAssert.Contains(blocked, "manual-QA-ready");
        CollectionAssert.Contains(blocked, "runtime-ready");
        CollectionAssert.Contains(blocked, "PC-Commander-ready");
        CollectionAssert.Contains(blocked, "safe-evidence-capture-started");
    }

    [TestMethod]
    public void runbook_plan_is_plan_only_and_forbids_product_runtime_manual_qa_and_cleanup_actions()
    {
        using var doc = ReadJson(RunbookPath);

        Assert.AreEqual("PLAN_ONLY", String(doc, "implementation_status"));
        CollectionAssert.Contains(Array(doc, "safe_future_steps"), "confirm branch/head/worktree");
        CollectionAssert.Contains(Array(doc, "safe_future_steps"), "run isolated BrowserRuntimeSmoke 3 times");
        CollectionAssert.Contains(Array(doc, "safe_future_steps"), "run full safety");
        CollectionAssert.Contains(Array(doc, "safe_future_steps"), "run full suite general");
        CollectionAssert.Contains(Array(doc, "safe_future_steps"), "run false-clean scan");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "manual QA real");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "runtime real");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "product/Bridge/CSP modification");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "filesystem cleanup");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "process kill");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "release/store claim");
    }

    [TestMethod]
    public void next_path_recommends_plan_only_revalidation_unless_user_requests_execution()
    {
        using var doc = ReadJson(NextPath);
        var decisions = doc.RootElement.GetProperty("decisions");

        Assert.AreEqual("M1137-M1148 - BrowserRuntimeSmoke Repeatability Evidence Collection Plan-Only Revalidation", String(decisions, "EVIDENCE_PACKAGE_ONLY_PREPARED"));
        Assert.AreEqual("M1137-M1148 - BrowserRuntimeSmoke Repeatability Evidence Collection Execution, still no product/runtime changes", String(decisions, "USER_EXPLICITLY_REQUESTS_ACTUAL_REPEATED_RUNS"));
        Assert.AreEqual("M1137-M1148 - Gate 9 Retry/Quarantine Policy Remediation", String(decisions, "GATE9_FAILS_AGAIN"));
        Assert.AreEqual("M1137-M1148 - BrowserRuntimeSmoke Repeatability Evidence Collection Plan-Only Revalidation", String(doc, "recommendation"));
    }

    [TestMethod]
    public void go_no_go_finalization_preserves_no_go_boundaries()
    {
        using var doc = ReadJson(GoNoGoPath);
        var noGo = Array(doc, "no_go");

        CollectionAssert.Contains(Array(doc, "go"), "repeatability evidence package prep");
        CollectionAssert.Contains(Array(doc, "go"), "3 isolated run schema");
        CollectionAssert.Contains(Array(doc, "go"), "resolution eligibility gate");
        CollectionAssert.Contains(noGo, "manual QA execution");
        CollectionAssert.Contains(noGo, "safe evidence capture real");
        CollectionAssert.Contains(noGo, "runtime real");
        CollectionAssert.Contains(noGo, "PC Commander real");
        CollectionAssert.Contains(noGo, "provider/cloud");
        CollectionAssert.Contains(noGo, "product files");
        CollectionAssert.Contains(noGo, "Bridge/CSP");
        CollectionAssert.Contains(noGo, "release/store");
        CollectionAssert.Contains(noGo, "suite-clean claim");
        CollectionAssert.Contains(noGo, "confidence above 95 claim");
        CollectionAssert.Contains(noGo, "caveat-resolved claim");
    }

    [TestMethod]
    public void final_artifacts_exist_and_record_expected_decision()
    {
        foreach (var path in AllPaths())
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }

        using var doc = ReadJson(FinalValidationPath);

        Assert.AreEqual("BROWSERRUNTIMESMOKE_REPEATABILITY_EVIDENCE_PLAN_PREP_READY_WITH_EXTERNAL_SMOKE_CAVEAT", String(doc, "decision"));
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
        FoundationPath, IsolatedSchemaPath, SuiteSchemaPath, BoundarySchemaPath, ReviewPath, EligibilityPath,
        ClaimGuardPath, RunbookPath, NextPath, GoNoGoPath, FinalReportArtifactPath, FinalValidationPath,
        BlockGoNoGoPath, ReportPath
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
