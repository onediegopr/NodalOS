using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BrowserRuntimeGate9ContainmentPlan")]
[TestCategory("M1113")]
[TestCategory("M1114")]
[TestCategory("M1115")]
[TestCategory("M1116")]
[TestCategory("M1117")]
[TestCategory("M1118")]
[TestCategory("M1119")]
[TestCategory("M1120")]
[TestCategory("M1121")]
[TestCategory("M1122")]
[TestCategory("M1123")]
[TestCategory("M1124")]
[TestCategory("M1113M1124")]
public sealed class NodalOsBrowserRuntimeGate9ContainmentPlanM1113M1124Tests
{
    private const string IntakePath = "artifacts/agent-operations/m1113/gate9-containment-remediation-intake.json";
    private const string ThresholdPath = "artifacts/agent-operations/m1114/repeatability-threshold-definition.json";
    private const string TransitionPath = "artifacts/agent-operations/m1115/caveat-state-transition-matrix.json";
    private const string RetryPath = "artifacts/agent-operations/m1116/retry-policy-hardening-plan.json";
    private const string QuarantinePath = "artifacts/agent-operations/m1117/quarantine-policy-hardening-plan.json";
    private const string EnvironmentPath = "artifacts/agent-operations/m1118/environment-cleanup-guidance-plan.json";
    private const string EvidencePath = "artifacts/agent-operations/m1119/repeatable-clean-evidence-plan.json";
    private const string ClaimGuardPath = "artifacts/agent-operations/m1120/claim-guard-hardening.json";
    private const string NextPath = "artifacts/agent-operations/m1121/next-path-decision-matrix.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m1122/go-no-go-finalization.json";
    private const string FinalReportArtifactPath = "artifacts/agent-operations/m1123/final-report.json";
    private const string FinalValidationPath = "artifacts/agent-operations/m1124/final-artifacts-validations.json";
    private const string BlockGoNoGoPath = "artifacts/agent-operations/m1113-m1124/browserruntime-gate9-containment-plan-go-no-go.json";
    private const string ReportPath = "docs/reports/m1124-browserruntime-gate9-containment-remediation-plan.md";

    [TestMethod]
    public void containment_intake_records_latest_clean_run_but_keeps_caveat_unresolved()
    {
        using var doc = ReadJson(IntakePath);

        Assert.AreEqual("EXTERNAL_SMOKE_CLEANUP_WEBSOCKET_ABORTED_ENVIRONMENT_TIMING", String(doc, "previous_caveat_classification"));
        Assert.AreEqual("PASS_30_PASSED_0_SKIPPED_0_FAILED", String(doc, "latest_isolated_result"));
        Assert.IsTrue(doc.RootElement.GetProperty("historical_gate9_websocket_aborted").GetBoolean());
        Assert.AreEqual("PASS_5544_PASSED_37_SKIPPED_0_FAILED", String(doc, "full_safety_rerun_result"));
        Assert.AreEqual("PASS_WITH_HISTORICAL_CAVEAT_VISIBLE", String(doc, "full_suite_final_result"));
        Assert.AreEqual("REPEATABILITY_AND_REVIEW_CRITERIA_NOT_MET", String(doc, "why_caveat_remains_unresolved"));
        Assert.AreEqual("plan-only containment/remediation planning", String(doc, "allowed_remediation_plan"));
        Assert.AreEqual("NO_RUNTIME_OR_TEST_BEHAVIOR_CHANGE", String(doc, "remediation_execution_not_allowed"));
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
    }

    [TestMethod]
    public void repeatability_threshold_requires_multiple_clean_runs_and_scope_scans()
    {
        using var doc = ReadJson(ThresholdPath);
        var required = Array(doc, "minimum_conditions");

        Assert.AreEqual(3, doc.RootElement.GetProperty("suggested_consecutive_clean_isolated_runs").GetInt32());
        Assert.AreEqual(1, doc.RootElement.GetProperty("required_full_safety_clean_runs").GetInt32());
        Assert.AreEqual(1, doc.RootElement.GetProperty("required_full_suite_clean_runs").GetInt32());
        CollectionAssert.Contains(required, "N consecutive clean BrowserRuntimeSmoke isolated runs");
        CollectionAssert.Contains(required, "zero skipped/inconclusive cleanup gates");
        CollectionAssert.Contains(required, "zero Gate 9 WebSocket aborted");
        CollectionAssert.Contains(required, "product/Bridge/CSP scan clean");
        CollectionAssert.Contains(required, "leak scan clean");
        CollectionAssert.Contains(required, "false-clean-claim scan clean");
        CollectionAssert.Contains(required, "review required before raising confidence");
        Assert.AreEqual("PLAN_ONLY_NOT_EXECUTED", String(doc, "execution_status"));
    }

    [TestMethod]
    public void state_transition_matrix_prevents_direct_unresolved_to_resolved()
    {
        using var doc = ReadJson(TransitionPath);

        CollectionAssert.Contains(Array(doc, "states"), "VISIBLE_UNRESOLVED");
        CollectionAssert.Contains(Array(doc, "states"), "CONTAINMENT_PLAN_READY");
        CollectionAssert.Contains(Array(doc, "states"), "CONTAINMENT_POLICY_HARDENED");
        CollectionAssert.Contains(Array(doc, "states"), "REPEATABILITY_EVIDENCE_PENDING");
        CollectionAssert.Contains(Array(doc, "states"), "ELIGIBLE_FOR_RESOLUTION_REVIEW");
        CollectionAssert.Contains(Array(doc, "states"), "RESOLVED_AFTER_REVIEW");
        Assert.IsFalse(doc.RootElement.GetProperty("allows_direct_visible_unresolved_to_resolved").GetBoolean());
        Assert.AreEqual("VISIBLE_UNRESOLVED", String(doc, "current_state"));
        Assert.AreEqual("CONTAINMENT_PLAN_READY", String(doc, "target_state_for_this_block"));
    }

    [TestMethod]
    public void retry_policy_treats_timeout_separately_and_does_not_convert_rerun_pass_to_clean()
    {
        using var doc = ReadJson(RetryPath);
        var rules = doc.RootElement.GetProperty("rules");

        Assert.AreEqual("record_fail_family_and_keep_caveat_visible", String(rules, "initial_fail"));
        Assert.AreEqual("same_family_external_smoke_caveat", String(rules, "same_family_gate9_fail"));
        Assert.AreEqual("not_pass_requires_rerun_or_abort", String(rules, "timeout"));
        Assert.AreEqual("pass_with_caveat_only", String(rules, "rerun_pass_with_caveat"));
        Assert.AreEqual("fail_visible_no_clean_claim", String(rules, "rerun_fail"));
        Assert.AreEqual("candidate_evidence_only_until_threshold_met", String(rules, "repeatable_clean_run"));
        Assert.IsFalse(doc.RootElement.GetProperty("rerun_pass_can_claim_clean").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("timeout_can_count_as_pass").GetBoolean());
    }

    [TestMethod]
    public void quarantine_policy_remains_visible_not_hidden_and_confidence_limited()
    {
        using var doc = ReadJson(QuarantinePath);

        Assert.AreEqual("BROWSERRUNTIME_GATE9_EXTERNAL_SMOKE_CAVEAT_VISIBLE", String(doc, "quarantine_label"));
        Assert.IsTrue(doc.RootElement.GetProperty("visible").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("hidden").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("clean_claim_allowed").GetBoolean());
        Assert.AreEqual("95%", String(doc, "confidence_until_criteria_met"));
        CollectionAssert.Contains(Array(doc, "blocks"), "release/store");
        CollectionAssert.Contains(Array(doc, "blocks"), "manual QA trigger");
        CollectionAssert.Contains(Array(doc, "permits"), "plan-only docs/artifacts/tests work");
    }

    [TestMethod]
    public void environment_cleanup_guidance_is_plan_only_and_forbids_real_cleanup_actions()
    {
        using var doc = ReadJson(EnvironmentPath);

        Assert.AreEqual("PLAN_ONLY", String(doc, "implementation_status"));
        CollectionAssert.Contains(Array(doc, "guidance"), "temp profile cleanup review");
        CollectionAssert.Contains(Array(doc, "guidance"), "CDP profile lock observation");
        CollectionAssert.Contains(Array(doc, "guidance"), "WebSocket close timing observation");
        CollectionAssert.Contains(Array(doc, "guidance"), "process/port/profile release observation");
        CollectionAssert.Contains(Array(doc, "guidance"), "repeat-run logging plan");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "process kill");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "temp file delete");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "filesystem mutation");
        CollectionAssert.Contains(Array(doc, "forbidden_actions"), "test behavior modification");
    }

    [TestMethod]
    public void repeatable_clean_evidence_plan_excludes_secrets_raw_logs_and_sensitive_dumps()
    {
        using var doc = ReadJson(EvidencePath);

        CollectionAssert.Contains(Array(doc, "required_fields"), "run ids");
        CollectionAssert.Contains(Array(doc, "required_fields"), "timestamps");
        CollectionAssert.Contains(Array(doc, "required_fields"), "isolated BrowserRuntimeSmoke results");
        CollectionAssert.Contains(Array(doc, "required_fields"), "skip/inconclusive counts");
        CollectionAssert.Contains(Array(doc, "required_fields"), "Gate 9 status");
        CollectionAssert.Contains(Array(doc, "required_fields"), "review decision");
        CollectionAssert.Contains(Array(doc, "forbidden_evidence"), "raw logs with secrets");
        CollectionAssert.Contains(Array(doc, "forbidden_evidence"), "environment dumps");
        CollectionAssert.Contains(Array(doc, "forbidden_evidence"), "credentials");
        CollectionAssert.Contains(Array(doc, "forbidden_evidence"), "tokens");
        CollectionAssert.Contains(Array(doc, "forbidden_evidence"), "cookies");
        CollectionAssert.Contains(Array(doc, "forbidden_evidence"), "large raw console dumps");
    }

    [TestMethod]
    public void claim_guard_blocks_clean_confidence_release_manual_runtime_and_capture_until_review_state()
    {
        using var doc = ReadJson(ClaimGuardPath);
        var blocked = Array(doc, "blocked_if_state_not_resolved_after_review");

        Assert.AreEqual("VISIBLE_UNRESOLVED", String(doc, "current_state"));
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
    public void next_path_recommends_repeatability_evidence_plan_prep()
    {
        using var doc = ReadJson(NextPath);
        var decisions = doc.RootElement.GetProperty("decisions");

        Assert.AreEqual("M1125-M1136 - BrowserRuntimeSmoke Repeatability Evidence Plan Prep", String(decisions, "CONTAINMENT_PLAN_READY_NO_EXECUTION"));
        Assert.AreEqual("M1125-M1136 - Safe Evidence Checklist Prep Gate with Caveat Visible, still no dangerous actions", String(decisions, "DIRECT_OPERATOR_CONFIRMATION_WITH_CAVEAT_UNRESOLVED"));
        Assert.AreEqual("M1125-M1136 - Gate 9 Retry/Quarantine Policy Remediation", String(decisions, "REPEATED_GATE9_FAILURES_CONTINUE"));
        Assert.AreEqual("M1125-M1136 - BrowserRuntimeSmoke Repeatability Evidence Plan Prep", String(doc, "recommendation"));
    }

    [TestMethod]
    public void go_no_go_finalization_preserves_all_no_go_boundaries()
    {
        using var doc = ReadJson(GoNoGoPath);
        var noGo = Array(doc, "no_go");

        CollectionAssert.Contains(Array(doc, "go"), "containment remediation plan");
        CollectionAssert.Contains(Array(doc, "go"), "repeatability threshold");
        CollectionAssert.Contains(Array(doc, "go"), "state transition matrix");
        CollectionAssert.Contains(noGo, "manual QA execution");
        CollectionAssert.Contains(noGo, "safe evidence capture real");
        CollectionAssert.Contains(noGo, "runtime real");
        CollectionAssert.Contains(noGo, "PC Commander real");
        CollectionAssert.Contains(noGo, "provider/cloud");
        CollectionAssert.Contains(noGo, "filesystem/browser/capability");
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

        Assert.AreEqual("BROWSERRUNTIMESMOKE_GATE9_CONTAINMENT_REMEDIATION_PLAN_READY_WITH_EXTERNAL_SMOKE_CAVEAT", String(doc, "decision"));
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
            foreach (var forbidden in new[] { "sk-", "AKIA", "BEGIN PRIVATE KEY", "Authorization: Bearer", "full suite clean", "100% confidence", "caveat resolved", "release ready", "manual QA ready", "runtime ready", "PC Commander ready", "process killed", "temp files cleaned" })
            {
                Assert.IsFalse(content.Contains(forbidden, StringComparison.Ordinal), $"{file} contains forbidden wording {forbidden}");
            }
        }
    }

    private static string[] AllPaths() =>
    [
        IntakePath, ThresholdPath, TransitionPath, RetryPath, QuarantinePath, EnvironmentPath, EvidencePath,
        ClaimGuardPath, NextPath, GoNoGoPath, FinalReportArtifactPath, FinalValidationPath, BlockGoNoGoPath,
        ReportPath
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
