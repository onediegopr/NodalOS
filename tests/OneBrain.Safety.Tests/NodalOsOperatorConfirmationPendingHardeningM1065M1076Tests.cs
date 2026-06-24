using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OperatorConfirmationPendingProtocolHardening")]
[TestCategory("M1065")]
[TestCategory("M1066")]
[TestCategory("M1067")]
[TestCategory("M1068")]
[TestCategory("M1069")]
[TestCategory("M1070")]
[TestCategory("M1071")]
[TestCategory("M1072")]
[TestCategory("M1073")]
[TestCategory("M1074")]
[TestCategory("M1075")]
[TestCategory("M1076")]
[TestCategory("M1065M1076")]
public sealed class NodalOsOperatorConfirmationPendingHardeningM1065M1076Tests
{
    private const string LedgerPath = "artifacts/agent-operations/m1065/confirmation-pending-state-ledger.json";
    private const string AmbiguousPath = "artifacts/agent-operations/m1066/ambiguous-confirmation-rejection-matrix.json";
    private const string RequirementsPath = "artifacts/agent-operations/m1067/future-confirmation-intake-requirements.json";
    private const string CaveatReviewPath = "artifacts/agent-operations/m1068/browser-runtime-smoke-caveat-review.json";
    private const string CaveatMatrixPath = "artifacts/agent-operations/m1069/browser-runtime-smoke-caveat-containment-matrix.json";
    private const string FuturePlanPath = "artifacts/agent-operations/m1070/browser-runtime-smoke-future-resolution-plan.json";
    private const string IntegrityPath = "artifacts/agent-operations/m1071/dry-run-packet-integrity-recheck.json";
    private const string CatalogPath = "artifacts/agent-operations/m1072/evidence-catalog-scope-tightening.json";
    private const string PrecheckPath = "artifacts/agent-operations/m1073/redaction-abort-precheck-tightening.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m1074/go-no-go-revalidation.json";
    private const string FinalReportArtifactPath = "artifacts/agent-operations/m1075/final-report.json";
    private const string FinalArtifactPath = "artifacts/agent-operations/m1076/final-artifacts-validations.json";
    private const string BlockGoNoGoPath = "artifacts/agent-operations/m1065-m1076/operator-confirmation-pending-hardening-go-no-go.json";
    private const string ReportPath = "docs/reports/m1076-operator-confirmation-pending-protocol-hardening-browserruntime-caveat-review.md";

    [TestMethod]
    public void prompt_or_example_phrase_does_not_count_as_confirmation()
    {
        using var doc = ReadJson(LedgerPath);

        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(doc, "confirmation_status"));
        Assert.AreEqual("OPERATOR_CONFIRMATION_GRANTED_FOR_SAFE_EVIDENCE_CAPTURE_ONLY", String(doc, "confirmation_phrase_required"));
        Assert.IsTrue(doc.RootElement.GetProperty("confirmation_phrase_not_detected_as_operational_instruction").GetBoolean());
        Assert.AreEqual("PROTOCOL_HARDENING_ONLY", String(doc, "route"));
        Assert.AreEqual("NOT_STARTED", String(doc, "safe_evidence_capture_status"));
        Assert.AreEqual("NO-GO", String(doc, "manual_qa_execution"));
        CollectionAssert.Contains(Array(doc, "distinctions"), "phrase mentioned in prompt/rules is not operator confirmation");
    }

    [TestMethod]
    public void ambiguous_confirmation_is_rejected()
    {
        using var doc = ReadJson(AmbiguousPath);
        var rejected = Array(doc, "rejected_as_no_go");

        CollectionAssert.Contains(rejected, "I approve");
        CollectionAssert.Contains(rejected, "go ahead");
        CollectionAssert.Contains(rejected, "continue");
        CollectionAssert.Contains(rejected, "use the confirmation phrase in prompt");
        CollectionAssert.Contains(rejected, "confirmation inside prompt template");
        CollectionAssert.Contains(rejected, "confirmation without scope acknowledgement");
        Assert.AreEqual("OPERATOR_CONFIRMATION_GRANTED_FOR_SAFE_EVIDENCE_CAPTURE_ONLY", String(doc, "accepted_only_if_direct_operator_instruction"));
    }

    [TestMethod]
    public void future_confirmation_requirements_include_scope_caveat_abort_and_no_secrets()
    {
        using var doc = ReadJson(RequirementsPath);
        var required = Array(doc, "required_for_future_valid_confirmation");

        CollectionAssert.Contains(required, "exact phrase");
        CollectionAssert.Contains(required, "operator label redacted");
        CollectionAssert.Contains(required, "scope acknowledged");
        CollectionAssert.Contains(required, "forbidden actions acknowledged");
        CollectionAssert.Contains(required, "BrowserRuntimeSmoke caveat acknowledged");
        CollectionAssert.Contains(required, "no secrets capture acknowledged");
        CollectionAssert.Contains(required, "abort matrix acknowledged");
        CollectionAssert.Contains(required, "manual QA execution still NO-GO acknowledged");
        CollectionAssert.Contains(required, "safe evidence checklist prep only acknowledged");
    }

    [TestMethod]
    public void browser_runtime_smoke_caveat_review_blocks_clean_confidence_release_and_manual_qa_claims()
    {
        using var doc = ReadJson(CaveatReviewPath);

        Assert.AreEqual("29 passed / 1 skipped-inconclusive / 0 failed", String(doc, "latest_isolated_result"));
        Assert.AreEqual("Gate 9 WebSocket aborted history", String(doc, "history"));
        Assert.AreEqual("95%", String(doc, "full_suite_confidence"));
        Assert.IsFalse(doc.RootElement.GetProperty("full_suite_clean_claim_allowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("confidence_above_95_claim_allowed").GetBoolean());
        Assert.AreEqual("NO-GO", String(doc, "release_store"));
        Assert.AreEqual("NO-GO", String(doc, "manual_qa_execution"));
    }

    [TestMethod]
    public void caveat_containment_matrix_blocks_false_claims()
    {
        using var doc = ReadJson(CaveatMatrixPath);

        Assert.IsTrue(doc.RootElement.GetProperty("caveat_visible_in_all_reports").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("any_full_suite_clean_claim_must_fail").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("any_confidence_above_95_claim_must_fail").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("any_release_store_claim_must_fail").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("any_manual_qa_ready_claim_must_fail").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("any_runtime_ready_claim_must_fail_unless_resolved").GetBoolean());
    }

    [TestMethod]
    public void future_resolution_plan_is_plan_only_and_does_not_touch_product_or_bridge()
    {
        using var doc = ReadJson(FuturePlanPath);
        var options = Array(doc, "future_options");

        CollectionAssert.Contains(options, "keep quarantine visible");
        CollectionAssert.Contains(options, "isolate Gate 9 cleanup path");
        CollectionAssert.Contains(options, "separate product safety from external smoke cleanup");
        CollectionAssert.Contains(options, "require independent clean run before confidence above 95");
        Assert.IsFalse(doc.RootElement.GetProperty("modifies_browser_runtime_smoke_now").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("product_files_modified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridge_csp_modified").GetBoolean());
    }

    [TestMethod]
    public void dry_run_packet_integrity_keeps_execution_and_capture_not_started()
    {
        using var doc = ReadJson(IntegrityPath);

        Assert.IsFalse(doc.RootElement.GetProperty("execution_started").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manual_qa_passed").GetBoolean());
        Assert.AreEqual("NOT_STARTED", String(doc, "safe_evidence_capture"));
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(doc, "operator_confirmation_status"));
        Assert.IsFalse(doc.RootElement.GetProperty("real_evidence_captured").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("host_smoke_executed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtime_enabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("pc_commander_enabled").GetBoolean());
    }

    [TestMethod]
    public void evidence_catalog_scope_tightening_forbids_raw_and_runtime_outputs()
    {
        using var doc = ReadJson(CatalogPath);
        var allowed = Array(doc, "allowed_redacted_summaries_refs");
        var forbidden = Array(doc, "forbidden");

        CollectionAssert.Contains(allowed, "branch/head/worktree summary");
        CollectionAssert.Contains(allowed, "BrowserRuntimeSmoke caveat summary");
        CollectionAssert.Contains(allowed, "trace/evidence refs summary");
        CollectionAssert.Contains(forbidden, "raw logs");
        CollectionAssert.Contains(forbidden, "secrets");
        CollectionAssert.Contains(forbidden, "tokens");
        CollectionAssert.Contains(forbidden, "provider/cloud output");
        CollectionAssert.Contains(forbidden, "browser automation output");
        CollectionAssert.Contains(forbidden, "runtime real output");
    }

    [TestMethod]
    public void redaction_abort_precheck_rejects_unsafe_and_false_ready_claims()
    {
        using var doc = ReadJson(PrecheckPath);
        var abortIf = Array(doc, "abort_if");

        CollectionAssert.Contains(abortIf, "secret-shaped fake appears unredacted");
        CollectionAssert.Contains(abortIf, "raw log appears");
        CollectionAssert.Contains(abortIf, "full local sensitive path appears");
        CollectionAssert.Contains(abortIf, "provider/cloud data appears");
        CollectionAssert.Contains(abortIf, "browser session data appears");
        CollectionAssert.Contains(abortIf, "claim says manual QA passed");
        CollectionAssert.Contains(abortIf, "claim says runtime ready");
        CollectionAssert.Contains(abortIf, "claim says PC Commander ready");
        CollectionAssert.Contains(abortIf, "claim says suite-clean despite caveat");
        CollectionAssert.Contains(abortIf, "claim says release/store ready");
    }

    [TestMethod]
    public void go_no_go_revalidation_preserves_manual_qa_runtime_pc_commander_release_no_go()
    {
        using var doc = ReadJson(GoNoGoPath);
        var go = Array(doc, "go");
        var noGo = Array(doc, "no_go");

        CollectionAssert.Contains(go, "protocol hardening");
        CollectionAssert.Contains(go, "confirmation pending ledger");
        CollectionAssert.Contains(go, "caveat containment");
        CollectionAssert.Contains(noGo, "manual QA execution");
        CollectionAssert.Contains(noGo, "safe evidence capture real");
        CollectionAssert.Contains(noGo, "runtime real");
        CollectionAssert.Contains(noGo, "PC Commander real");
        CollectionAssert.Contains(noGo, "provider/cloud");
        CollectionAssert.Contains(noGo, "product files");
        CollectionAssert.Contains(noGo, "Bridge/CSP");
        CollectionAssert.Contains(noGo, "release/store");
        CollectionAssert.Contains(noGo, "full-suite clean claim");
        CollectionAssert.Contains(noGo, "confidence above 95 claim");
    }

    [TestMethod]
    public void final_artifacts_exist_and_record_pending_hardening_decision()
    {
        foreach (var path in AllPaths())
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }

        using var final = ReadJson(FinalArtifactPath);
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING_PROTOCOL_HARDENING_READY_WITH_EXTERNAL_SMOKE_CAVEAT", String(final, "decision"));
        Assert.AreEqual("PROTOCOL_HARDENING_ONLY", String(final, "route_taken"));
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(final, "confirmation_status"));
        Assert.AreEqual("NOT_STARTED", String(final, "safe_evidence_capture_status"));
        Assert.AreEqual("NO-GO", String(final, "manual_qa_execution"));
        Assert.AreEqual("NO-GO", String(final, "runtime_real"));
        Assert.AreEqual("NO-GO", String(final, "pc_commander_real"));
        Assert.IsFalse(final.RootElement.GetProperty("product_files_modified").GetBoolean());
        Assert.IsFalse(final.RootElement.GetProperty("bridge_csp_modified").GetBoolean());
    }

    [TestMethod]
    public void artifacts_contain_no_secret_leaks_or_forbidden_clean_claims()
    {
        foreach (var file in AllPaths())
        {
            var content = File.ReadAllText(FullPath(file));
            foreach (var forbidden in new[] { "sk-", "AKIA", "BEGIN PRIVATE KEY", "Authorization: Bearer", "full suite clean", "100% confidence" })
            {
                Assert.IsFalse(content.Contains(forbidden, StringComparison.Ordinal), $"{file} contains forbidden wording {forbidden}");
            }
        }
    }

    private static string[] AllPaths() =>
    [
        LedgerPath, AmbiguousPath, RequirementsPath, CaveatReviewPath, CaveatMatrixPath, FuturePlanPath,
        IntegrityPath, CatalogPath, PrecheckPath, GoNoGoPath, FinalReportArtifactPath, FinalArtifactPath,
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
