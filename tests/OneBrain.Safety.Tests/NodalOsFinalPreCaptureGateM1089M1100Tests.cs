using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("FinalPreCaptureGateCaveatCriteriaAudit")]
[TestCategory("M1089")]
[TestCategory("M1090")]
[TestCategory("M1091")]
[TestCategory("M1092")]
[TestCategory("M1093")]
[TestCategory("M1094")]
[TestCategory("M1095")]
[TestCategory("M1096")]
[TestCategory("M1097")]
[TestCategory("M1098")]
[TestCategory("M1099")]
[TestCategory("M1100")]
[TestCategory("M1089M1100")]
public sealed class NodalOsFinalPreCaptureGateM1089M1100Tests
{
    private const string LedgerPath = "artifacts/agent-operations/m1089/final-pre-capture-gate-ledger.json";
    private const string EligibilityPath = "artifacts/agent-operations/m1090/direct-confirmation-eligibility-matrix.json";
    private const string AbortPath = "artifacts/agent-operations/m1091/final-pre-capture-abort-conditions.json";
    private const string CriteriaAuditPath = "artifacts/agent-operations/m1092/caveat-resolution-criteria-audit.json";
    private const string DeltaLedgerPath = "artifacts/agent-operations/m1093/caveat-evidence-delta-ledger.json";
    private const string ClaimGuardPath = "artifacts/agent-operations/m1094/caveat-claim-guard-finalization.json";
    private const string FutureReviewPath = "artifacts/agent-operations/m1095/browserruntime-future-review-recommendation.json";
    private const string DryRunPath = "artifacts/agent-operations/m1096/dry-run-lock-finalization.json";
    private const string NextPathPath = "artifacts/agent-operations/m1097/next-path-decision-matrix.json";
    private const string NoGoPath = "artifacts/agent-operations/m1098/no-go-revalidation-final.json";
    private const string FinalReportArtifactPath = "artifacts/agent-operations/m1099/final-report.json";
    private const string FinalValidationPath = "artifacts/agent-operations/m1100/final-artifacts-validations.json";
    private const string BlockGoNoGoPath = "artifacts/agent-operations/m1089-m1100/final-pre-capture-gate-go-no-go.json";
    private const string ReportPath = "docs/reports/m1100-final-pre-capture-gate-caveat-resolution-criteria-audit.md";

    [TestMethod]
    public void final_pre_capture_gate_keeps_confirmation_pending_and_capture_not_started()
    {
        using var doc = ReadJson(LedgerPath);

        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(doc, "operator_confirmation"));
        Assert.AreEqual("NOT_STARTED", String(doc, "safe_evidence_capture"));
        Assert.AreEqual("NO-GO", String(doc, "manual_qa_execution"));
        Assert.AreEqual("NOT_READY_EVIDENCE_PENDING", String(doc, "manual_qa_trigger"));
        Assert.AreEqual("NO-GO", String(doc, "runtime_real"));
        Assert.AreEqual("NO-GO", String(doc, "pc_commander_real"));
        Assert.AreEqual("unchanged", String(doc, "product_files"));
        Assert.AreEqual("unchanged", String(doc, "bridge_csp"));
        Assert.AreEqual("visible_unresolved", String(doc, "browserruntime_caveat"));
        Assert.AreEqual(95, doc.RootElement.GetProperty("full_suite_confidence").GetInt32());
    }

    [TestMethod]
    public void direct_confirmation_eligibility_requires_exact_direct_phrase_and_all_acknowledgements()
    {
        using var doc = ReadJson(EligibilityPath);

        Assert.AreEqual("OPERATOR_CONFIRMATION_GRANTED_FOR_SAFE_EVIDENCE_CAPTURE_ONLY", String(doc, "exact_phrase"));
        Assert.AreEqual("DIRECT_OPERATOR_INSTRUCTION", String(doc, "required_context"));
        CollectionAssert.Contains(Array(doc, "required_acknowledgements"), "scope acknowledged");
        CollectionAssert.Contains(Array(doc, "required_acknowledgements"), "caveat acknowledged");
        CollectionAssert.Contains(Array(doc, "required_acknowledgements"), "abort matrix acknowledged");
        CollectionAssert.Contains(Array(doc, "required_acknowledgements"), "no secrets capture acknowledged");
        CollectionAssert.Contains(Array(doc, "required_acknowledgements"), "manual QA execution remains NO-GO acknowledged");
        CollectionAssert.Contains(Array(doc, "required_acknowledgements"), "safe evidence checklist prep only acknowledged");
        CollectionAssert.Contains(Array(doc, "required_acknowledgements"), "no shell acknowledged");
        CollectionAssert.Contains(Array(doc, "required_acknowledgements"), "no filesystem write acknowledged");
        CollectionAssert.Contains(Array(doc, "required_acknowledgements"), "no runtime real acknowledged");
        CollectionAssert.Contains(Array(doc, "required_acknowledgements"), "no PC Commander real acknowledged");
        CollectionAssert.Contains(Array(doc, "required_acknowledgements"), "no product/Bridge/CSP changes acknowledged");
        CollectionAssert.Contains(Array(doc, "required_acknowledgements"), "no release/store acknowledged");
    }

    [TestMethod]
    public void prompt_docs_artifact_code_fence_and_missing_acknowledgements_are_not_eligible()
    {
        using var doc = ReadJson(EligibilityPath);
        var notEligible = Array(doc, "not_eligible_if");

        CollectionAssert.Contains(notEligible, "phrase appears in prompt/rule/docs/artifact/example");
        CollectionAssert.Contains(notEligible, "phrase appears inside code fence");
        CollectionAssert.Contains(notEligible, "phrase appears as quote");
        CollectionAssert.Contains(notEligible, "phrase is paraphrased");
        CollectionAssert.Contains(notEligible, "phrase is translated");
        CollectionAssert.Contains(notEligible, "phrase is partial");
        CollectionAssert.Contains(notEligible, "phrase followed by request for runtime/PC Commander/release/shell/filesystem/provider/cloud");
        CollectionAssert.Contains(notEligible, "acknowledgements missing");
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(doc, "not_eligible_result"));
    }

    [TestMethod]
    public void final_abort_conditions_reject_hidden_caveat_false_claims_drift_and_leaks()
    {
        using var doc = ReadJson(AbortPath);
        var abortIf = Array(doc, "abort_if");

        CollectionAssert.Contains(abortIf, "operator confirmation pending");
        CollectionAssert.Contains(abortIf, "ambiguous confirmation");
        CollectionAssert.Contains(abortIf, "missing acknowledgements");
        CollectionAssert.Contains(abortIf, "BrowserRuntimeSmoke caveat hidden");
        CollectionAssert.Contains(abortIf, "suite-clean claim");
        CollectionAssert.Contains(abortIf, "confidence above 95 claim");
        CollectionAssert.Contains(abortIf, "manual-QA-ready-or-passed claim");
        CollectionAssert.Contains(abortIf, "runtime-ready claim");
        CollectionAssert.Contains(abortIf, "PC-Commander-ready claim");
        CollectionAssert.Contains(abortIf, "release-store-ready claim");
        CollectionAssert.Contains(abortIf, "product files changed");
        CollectionAssert.Contains(abortIf, "Bridge/CSP changed");
        CollectionAssert.Contains(abortIf, "secret/log leak detected");
        CollectionAssert.Contains(abortIf, "raw evidence captured");
    }

    [TestMethod]
    public void caveat_resolution_audit_keeps_caveat_unresolved_without_required_clean_evidence()
    {
        using var doc = ReadJson(CriteriaAuditPath);
        var required = Array(doc, "required_to_resolve");

        CollectionAssert.Contains(required, "independent clean BrowserRuntimeSmoke isolated run");
        CollectionAssert.Contains(required, "no skipped/inconclusive cleanup gate");
        CollectionAssert.Contains(required, "no Gate 9 WebSocket aborted");
        CollectionAssert.Contains(required, "repeatable clean run if required");
        CollectionAssert.Contains(required, "full safety suite without external caveat");
        CollectionAssert.Contains(required, "no product/Bridge/CSP drift");
        CollectionAssert.Contains(required, "no secret/log leak");
        CollectionAssert.Contains(required, "report updated");
        CollectionAssert.Contains(required, "separate review before release/store claim");
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
        Assert.AreEqual(95, doc.RootElement.GetProperty("confidence").GetInt32());
        Assert.IsFalse(doc.RootElement.GetProperty("full_suite_clean_claim_allowed").GetBoolean());
    }

    [TestMethod]
    public void caveat_delta_ledger_records_gate9_reproduction_and_final_pass_with_visible_caveat()
    {
        using var doc = ReadJson(DeltaLedgerPath);

        Assert.AreEqual("VISIBLE_EXTERNAL_CAVEAT", String(doc, "previous_status"));
        Assert.AreEqual("VISIBLE_EXTERNAL_CAVEAT", String(doc, "latest_observed_status"));
        Assert.IsTrue(doc.RootElement.GetProperty("gate9_websocket_aborted_reproduced").GetBoolean());
        Assert.AreEqual("PASS_WITH_VISIBLE_CAVEAT", String(doc, "final_full_suite"));
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
        Assert.AreEqual(95, doc.RootElement.GetProperty("confidence").GetInt32());
        Assert.AreEqual("NO-GO", String(doc, "release_store"));
        Assert.AreEqual("NO-GO", String(doc, "manual_qa_execution"));
    }

    [TestMethod]
    public void caveat_claim_guard_blocks_false_claims_when_unresolved()
    {
        using var doc = ReadJson(ClaimGuardPath);
        var blocked = Array(doc, "blocked_when_caveat_unresolved");

        CollectionAssert.Contains(blocked, "suite-clean");
        CollectionAssert.Contains(blocked, "confidence above 95");
        CollectionAssert.Contains(blocked, "release-ready");
        CollectionAssert.Contains(blocked, "Chrome-Web-Store-ready");
        CollectionAssert.Contains(blocked, "manual-QA-ready");
        CollectionAssert.Contains(blocked, "runtime-ready");
        CollectionAssert.Contains(blocked, "PC-Commander-ready");
        CollectionAssert.Contains(blocked, "safe-evidence-capture-started");
        CollectionAssert.Contains(blocked, "operator-confirmed");
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
    }

    [TestMethod]
    public void future_review_recommends_browser_runtime_gate9_review_without_modifying_product_or_bridge()
    {
        using var doc = ReadJson(FutureReviewPath);

        CollectionAssert.Contains(Array(doc, "options"), "M1101-M1112 - BrowserRuntimeSmoke Gate 9 Isolation Review");
        CollectionAssert.Contains(Array(doc, "options"), "M1101-M1112 - Operator Confirmation Intake if direct operator confirmation is provided");
        Assert.AreEqual("DO_NOT_MODIFY_NOW", String(doc, "browser_runtime_smoke_changes"));
        Assert.AreEqual("unchanged", String(doc, "product_files"));
        Assert.AreEqual("unchanged", String(doc, "bridge_csp"));
        Assert.IsFalse(doc.RootElement.GetProperty("confidence_can_increase").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_can_be_resolved_without_evidence").GetBoolean());
    }

    [TestMethod]
    public void dry_run_locks_keep_no_execution_no_pass_no_capture_and_no_runtime()
    {
        using var doc = ReadJson(DryRunPath);

        Assert.AreEqual("protocol-only", String(doc, "dry_run_packet"));
        Assert.IsFalse(doc.RootElement.GetProperty("execution_started").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manual_qa_passed").GetBoolean());
        Assert.AreEqual("NOT_STARTED", String(doc, "safe_evidence_capture"));
        Assert.IsFalse(doc.RootElement.GetProperty("real_evidence_captured").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("host_smoke_executed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtime_enabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("pc_commander_enabled").GetBoolean());
    }

    [TestMethod]
    public void next_path_prefers_gate9_review_while_confirmation_pending()
    {
        using var doc = ReadJson(NextPathPath);

        Assert.AreEqual("M1101-M1112 - BrowserRuntimeSmoke Gate 9 Isolation Review OR Protocol Hardening Hold", String(doc.RootElement.GetProperty("decisions"), "OPERATOR_CONFIRMATION_PENDING"));
        Assert.AreEqual("M1101-M1112 - Safe Evidence Checklist Prep Gate, still no dangerous actions", String(doc.RootElement.GetProperty("decisions"), "OPERATOR_CONFIRMATION_GRANTED_FOR_SAFE_EVIDENCE_CAPTURE_ONLY_WITH_ACKNOWLEDGEMENTS"));
        Assert.AreEqual("M1101-M1112 - BrowserRuntimeSmoke Caveat Containment Remediation", String(doc.RootElement.GetProperty("decisions"), "BROWSERRUNTIME_SMOKE_DETERIORATES"));
        Assert.AreEqual("Prefer BrowserRuntimeSmoke Gate 9 Isolation Review before manual evidence capture path because caveat keeps causing repeated instability.", String(doc, "recommendation"));
    }

    [TestMethod]
    public void no_go_revalidation_preserves_all_boundaries()
    {
        using var doc = ReadJson(NoGoPath);
        var noGo = Array(doc, "no_go");

        CollectionAssert.Contains(Array(doc, "go"), "final pre-capture gate");
        CollectionAssert.Contains(Array(doc, "go"), "confirmation eligibility matrix");
        CollectionAssert.Contains(Array(doc, "go"), "caveat resolution criteria audit");
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

        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING_FINAL_PRE_CAPTURE_GATE_READY_WITH_EXTERNAL_SMOKE_CAVEAT", String(doc, "decision"));
        Assert.AreEqual("PROTOCOL_HARDENING_ONLY", String(doc, "route_taken"));
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(doc, "operator_confirmation"));
        Assert.AreEqual("NOT_STARTED", String(doc, "safe_evidence_capture"));
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
        Assert.AreEqual("NO-GO", String(doc, "manual_qa_execution"));
        Assert.AreEqual("NO-GO", String(doc, "runtime_real"));
        Assert.IsFalse(doc.RootElement.GetProperty("product_files_modified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridge_csp_modified").GetBoolean());
    }

    [TestMethod]
    public void artifacts_contain_no_secret_leaks_or_forbidden_claims()
    {
        foreach (var file in AllPaths())
        {
            var content = File.ReadAllText(FullPath(file));
            foreach (var forbidden in new[] { "sk-", "AKIA", "BEGIN PRIVATE KEY", "Authorization: Bearer", "full suite clean", "100% confidence", "operator confirmed", "safe evidence captured" })
            {
                Assert.IsFalse(content.Contains(forbidden, StringComparison.Ordinal), $"{file} contains forbidden wording {forbidden}");
            }
        }
    }

    private static string[] AllPaths() =>
    [
        LedgerPath, EligibilityPath, AbortPath, CriteriaAuditPath, DeltaLedgerPath, ClaimGuardPath,
        FutureReviewPath, DryRunPath, NextPathPath, NoGoPath, FinalReportArtifactPath, FinalValidationPath,
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
