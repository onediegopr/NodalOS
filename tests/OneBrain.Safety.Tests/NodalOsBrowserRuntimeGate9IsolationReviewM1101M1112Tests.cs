using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BrowserRuntimeGate9IsolationReview")]
[TestCategory("M1101")]
[TestCategory("M1102")]
[TestCategory("M1103")]
[TestCategory("M1104")]
[TestCategory("M1105")]
[TestCategory("M1106")]
[TestCategory("M1107")]
[TestCategory("M1108")]
[TestCategory("M1109")]
[TestCategory("M1110")]
[TestCategory("M1111")]
[TestCategory("M1112")]
[TestCategory("M1101M1112")]
public sealed class NodalOsBrowserRuntimeGate9IsolationReviewM1101M1112Tests
{
    private const string IntakePath = "artifacts/agent-operations/m1101/browserruntime-gate9-intake.json";
    private const string ClassificationPath = "artifacts/agent-operations/m1102/gate9-failure-classification-matrix.json";
    private const string BoundaryPath = "artifacts/agent-operations/m1103/caveat-vs-product-safety-boundary.json";
    private const string IsolationPath = "artifacts/agent-operations/m1104/gate9-isolation-matrix.json";
    private const string QuarantinePath = "artifacts/agent-operations/m1105/quarantine-policy-review.json";
    private const string RetryPath = "artifacts/agent-operations/m1106/retry-rerun-interpretation-rules.json";
    private const string FuturePath = "artifacts/agent-operations/m1107/future-gate9-resolution-options.json";
    private const string EvidencePath = "artifacts/agent-operations/m1108/caveat-resolution-evidence-requirements.json";
    private const string ClaimGuardPath = "artifacts/agent-operations/m1109/final-claim-guard-update.json";
    private const string NextPath = "artifacts/agent-operations/m1110/next-path-decision-after-gate9-review.json";
    private const string FinalReportArtifactPath = "artifacts/agent-operations/m1111/final-report.json";
    private const string FinalValidationPath = "artifacts/agent-operations/m1112/final-artifacts-validations.json";
    private const string BlockGoNoGoPath = "artifacts/agent-operations/m1101-m1112/browserruntime-gate9-isolation-review-go-no-go.json";
    private const string ReportPath = "docs/reports/m1112-browserruntime-gate9-isolation-review.md";

    [TestMethod]
    public void gate9_intake_records_latest_visible_caveat_without_unlocks()
    {
        using var doc = ReadJson(IntakePath);

        Assert.AreEqual("VISIBLE_UNRESOLVED_EXTERNAL_SMOKE_CAVEAT", String(doc, "latest_known_status"));
        Assert.AreEqual("Gate 9 WebSocket aborted", String(doc, "gate9_signal"));
        Assert.AreEqual("PREVIOUS_PASS_WITH_VISIBLE_CAVEAT_29_PASSED_1_SKIPPED_0_FAILED_AND_PREVIOUS_GATE9_ABORTED_REPRODUCTION", String(doc, "isolated_result_history"));
        Assert.AreEqual("PASS_30_PASSED_0_SKIPPED_0_FAILED", String(doc, "latest_isolated_result"));
        Assert.AreEqual("INITIAL_TIMEOUT_NOT_COUNTED_AS_PASS", String(doc, "full_safety_timeout_history"));
        Assert.AreEqual("PASS_5544_PASSED_37_SKIPPED_0_FAILED", String(doc, "full_safety_rerun_result"));
        Assert.AreEqual("PASS_WITH_VISIBLE_CAVEAT", String(doc, "full_suite_final_result"));
        Assert.AreEqual(37, doc.RootElement.GetProperty("skipped_inconclusive_count").GetInt32());
        Assert.IsFalse(doc.RootElement.GetProperty("product_bridge_csp_changed").GetBoolean());
        Assert.AreEqual("NO-GO", String(doc, "release_store"));
        Assert.AreEqual(95, doc.RootElement.GetProperty("confidence").GetInt32());
    }

    [TestMethod]
    public void failure_classification_requires_evidence_before_product_or_bridge_regression_claim()
    {
        using var doc = ReadJson(ClassificationPath);

        CollectionAssert.Contains(Array(doc, "possible_causes"), "EXTERNAL_SMOKE_CLEANUP");
        CollectionAssert.Contains(Array(doc, "possible_causes"), "TEMP_PROFILE_LOCK");
        CollectionAssert.Contains(Array(doc, "possible_causes"), "WEBSOCKET_ABORTED");
        CollectionAssert.Contains(Array(doc, "possible_causes"), "CDP_PROFILE_RACE");
        CollectionAssert.Contains(Array(doc, "possible_causes"), "ENVIRONMENT_TIMING");
        CollectionAssert.Contains(Array(doc, "possible_causes"), "PRODUCT_REGRESSION");
        CollectionAssert.Contains(Array(doc, "possible_causes"), "BRIDGE_CSP_REGRESSION");
        CollectionAssert.Contains(Array(doc, "current_classification"), "EXTERNAL_SMOKE_CLEANUP");
        CollectionAssert.Contains(Array(doc, "current_classification"), "WEBSOCKET_ABORTED");
        CollectionAssert.Contains(Array(doc, "current_classification"), "ENVIRONMENT_TIMING");
        Assert.AreEqual("REQUIRED", String(doc, "evidence_before_product_regression_claim"));
        Assert.AreEqual("REQUIRED", String(doc, "evidence_before_bridge_csp_regression_claim"));
    }

    [TestMethod]
    public void caveat_product_boundary_keeps_no_go_and_unchanged_boundaries()
    {
        using var doc = ReadJson(BoundaryPath);

        Assert.AreEqual("VISIBLE_UNRESOLVED_EXTERNAL_SMOKE_CAVEAT", String(doc, "browserruntime_caveat"));
        Assert.AreEqual("unchanged", String(doc, "product_files_boundary"));
        Assert.AreEqual("unchanged", String(doc, "bridge_csp_boundary"));
        Assert.AreEqual("NO-GO", String(doc, "runtime_unlock_boundary"));
        Assert.AreEqual("NO-GO", String(doc, "manual_qa_boundary"));
        Assert.AreEqual("NO-GO", String(doc, "release_store_boundary"));
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_unlocks_anything").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("caveat_blocks_clean_claims").GetBoolean());
    }

    [TestMethod]
    public void isolation_matrix_records_pass_fail_timeout_rerun_and_scope_signals_separately()
    {
        using var doc = ReadJson(IsolationPath);
        var signals = doc.RootElement.GetProperty("signals").EnumerateArray().ToArray();

        Assert.IsTrue(signals.Any(row => String(row, "signal") == "isolated PASS with caveat" && String(row, "requires_rerun") == "YES"));
        Assert.IsTrue(signals.Any(row => String(row, "signal") == "isolated FAIL Gate 9" && String(row, "likely_class") == "WEBSOCKET_ABORTED"));
        Assert.IsTrue(signals.Any(row => String(row, "signal") == "full safety timeout" && String(row, "observed_evidence") == "timeout not counted as pass"));
        Assert.IsTrue(signals.Any(row => String(row, "signal") == "full safety rerun PASS" && String(row, "blocks_clean_claim") == "YES"));
        Assert.IsTrue(signals.Any(row => String(row, "signal") == "recipes PASS" && String(row, "blocks_release_store") == "NO"));
        Assert.IsTrue(signals.Any(row => String(row, "signal") == "product/Bridge scan PASS" && String(row, "likely_class") == "BOUNDARY_UNCHANGED"));
    }

    [TestMethod]
    public void quarantine_policy_remains_visible_not_hidden_and_blocks_release_store()
    {
        using var doc = ReadJson(QuarantinePath);

        Assert.AreEqual("BrowserRuntimeSmoke cleanup/Gate 9 external smoke caveat", String(doc, "what_is_quarantined"));
        Assert.IsTrue(doc.RootElement.GetProperty("visible").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("hidden").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("clean_claim_allowed").GetBoolean());
        Assert.AreEqual("95%", String(doc, "confidence"));
        Assert.AreEqual("BLOCKED", String(doc, "release_store"));
        Assert.AreEqual("NOT_READY_EVIDENCE_PENDING", String(doc, "manual_qa_trigger"));
    }

    [TestMethod]
    public void retry_rules_do_not_count_timeout_or_rerun_pass_as_clean()
    {
        using var doc = ReadJson(RetryPath);

        Assert.AreEqual("FAIL_OR_INCOMPLETE", String(doc.RootElement.GetProperty("rules"), "initial_fail"));
        Assert.AreEqual("PASS_WITH_CAVEAT_ONLY", String(doc.RootElement.GetProperty("rules"), "rerun_pass_with_caveat"));
        Assert.AreEqual("NOT_PASS", String(doc.RootElement.GetProperty("rules"), "timeout"));
        Assert.AreEqual("VISIBLE_CAVEAT", String(doc.RootElement.GetProperty("rules"), "same_family_gate9_fail"));
        Assert.AreEqual("PASS_WITH_CAVEAT_ONLY", String(doc.RootElement.GetProperty("rules"), "final_pass_with_caveat"));
        Assert.IsFalse(doc.RootElement.GetProperty("rerun_pass_can_claim_clean").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("confidence_can_exceed_95").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_can_be_resolved_by_rerun_only").GetBoolean());
    }

    [TestMethod]
    public void future_resolution_options_are_plan_only_and_do_not_touch_product_or_bridge()
    {
        using var doc = ReadJson(FuturePath);
        var options = Array(doc, "options");

        CollectionAssert.Contains(options, "environment cleanup guidance");
        CollectionAssert.Contains(options, "temp profile cleanup delay review");
        CollectionAssert.Contains(options, "CDP profile lock investigation");
        CollectionAssert.Contains(options, "WebSocket aborted handling review");
        CollectionAssert.Contains(options, "test fixture timeout/rerun policy");
        CollectionAssert.Contains(options, "separate external smoke bucket");
        CollectionAssert.Contains(options, "independent repeated clean isolated runs");
        Assert.AreEqual("PLAN_ONLY", String(doc, "implementation_status"));
        Assert.AreEqual("unchanged", String(doc, "product_files"));
        Assert.AreEqual("unchanged", String(doc, "bridge_csp"));
    }

    [TestMethod]
    public void resolution_evidence_requires_no_skipped_gate_no_gate9_and_clean_suites()
    {
        using var doc = ReadJson(EvidencePath);
        var required = Array(doc, "required_evidence");

        CollectionAssert.Contains(required, "clean BrowserRuntimeSmoke isolated run");
        CollectionAssert.Contains(required, "no skipped/inconclusive cleanup gate");
        CollectionAssert.Contains(required, "no Gate 9 WebSocket aborted");
        CollectionAssert.Contains(required, "repeatable clean result or defined threshold");
        CollectionAssert.Contains(required, "full safety suite without external caveat");
        CollectionAssert.Contains(required, "full suite general without external caveat");
        CollectionAssert.Contains(required, "product/Bridge/CSP scan clean");
        CollectionAssert.Contains(required, "leak scan clean");
        CollectionAssert.Contains(required, "false-clean-claim scan clean");
        CollectionAssert.Contains(required, "review before release/store claims");
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
    }

    [TestMethod]
    public void claim_guard_blocks_false_clean_confidence_release_manual_qa_runtime_and_capture_claims()
    {
        using var doc = ReadJson(ClaimGuardPath);
        var blocked = Array(doc, "blocked_when_caveat_unresolved");

        CollectionAssert.Contains(blocked, "suite-clean");
        CollectionAssert.Contains(blocked, "confidence above 95");
        CollectionAssert.Contains(blocked, "caveat-resolved");
        CollectionAssert.Contains(blocked, "release-ready");
        CollectionAssert.Contains(blocked, "Chrome-Web-Store-ready");
        CollectionAssert.Contains(blocked, "manual-QA-ready");
        CollectionAssert.Contains(blocked, "runtime-ready");
        CollectionAssert.Contains(blocked, "PC-Commander-ready");
        CollectionAssert.Contains(blocked, "safe-evidence-capture-started");
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
    }

    [TestMethod]
    public void next_path_prefers_gate9_containment_before_operator_evidence_capture()
    {
        using var doc = ReadJson(NextPath);

        Assert.AreEqual("M1113-M1124 - BrowserRuntimeSmoke Gate 9 Containment Remediation Plan", String(doc.RootElement.GetProperty("decisions"), "CAVEAT_UNRESOLVED"));
        Assert.AreEqual("M1113-M1124 - BrowserRuntimeSmoke Retry/Quarantine Policy Hardening", String(doc.RootElement.GetProperty("decisions"), "BETTER_CLASSIFIED_BUT_UNRESOLVED"));
        Assert.AreEqual("M1113-M1124 - Safe Evidence Checklist Prep Gate, caveat visibility retained and no dangerous actions", String(doc.RootElement.GetProperty("decisions"), "DIRECT_OPERATOR_CONFIRMATION_APPEARS"));
        Assert.AreEqual("Prefer Gate 9 Containment Remediation Plan before any operator evidence capture.", String(doc, "recommendation"));
    }

    [TestMethod]
    public void final_artifacts_exist_and_preserve_all_no_go_boundaries()
    {
        foreach (var path in AllPaths())
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }

        using var doc = ReadJson(FinalValidationPath);

        Assert.AreEqual("BROWSERRUNTIMESMOKE_GATE9_ISOLATION_REVIEW_READY_WITH_EXTERNAL_SMOKE_CAVEAT", String(doc, "decision"));
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
    public void go_no_go_artifact_keeps_release_runtime_provider_product_and_bridge_blocked()
    {
        using var doc = ReadJson(BlockGoNoGoPath);
        var noGo = Array(doc, "no_go");

        CollectionAssert.Contains(noGo, "manual QA execution");
        CollectionAssert.Contains(noGo, "safe evidence capture real");
        CollectionAssert.Contains(noGo, "runtime real");
        CollectionAssert.Contains(noGo, "PC Commander real");
        CollectionAssert.Contains(noGo, "provider/cloud");
        CollectionAssert.Contains(noGo, "filesystem/browser/capability unlock");
        CollectionAssert.Contains(noGo, "product files");
        CollectionAssert.Contains(noGo, "Bridge/CSP");
        CollectionAssert.Contains(noGo, "release/store");
        CollectionAssert.Contains(noGo, "suite-clean claim");
        CollectionAssert.Contains(noGo, "confidence above 95 claim");
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
    }

    [TestMethod]
    public void artifacts_contain_no_secret_leaks_or_forbidden_clean_claims()
    {
        foreach (var file in AllPaths())
        {
            var content = File.ReadAllText(FullPath(file));
            foreach (var forbidden in new[] { "sk-", "AKIA", "BEGIN PRIVATE KEY", "Authorization: Bearer", "full suite clean", "100% confidence", "caveat resolved", "release ready", "manual QA ready", "runtime ready", "PC Commander ready" })
            {
                Assert.IsFalse(content.Contains(forbidden, StringComparison.Ordinal), $"{file} contains forbidden wording {forbidden}");
            }
        }
    }

    private static string[] AllPaths() =>
    [
        IntakePath, ClassificationPath, BoundaryPath, IsolationPath, QuarantinePath, RetryPath, FuturePath,
        EvidencePath, ClaimGuardPath, NextPath, FinalReportArtifactPath, FinalValidationPath, BlockGoNoGoPath,
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
