using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("OperatorConfirmationPendingProtocolHardeningR2")]
[TestCategory("M1077")]
[TestCategory("M1078")]
[TestCategory("M1079")]
[TestCategory("M1080")]
[TestCategory("M1081")]
[TestCategory("M1082")]
[TestCategory("M1083")]
[TestCategory("M1084")]
[TestCategory("M1085")]
[TestCategory("M1086")]
[TestCategory("M1087")]
[TestCategory("M1088")]
[TestCategory("M1077M1088")]
public sealed class NodalOsOperatorConfirmationPendingHardeningR2M1077M1088Tests
{
    private const string AcceptancePath = "artifacts/agent-operations/m1077/future-operator-confirmation-acceptance-criteria.json";
    private const string ClassifierPath = "artifacts/agent-operations/m1078/confirmation-context-classifier.json";
    private const string SpoofPath = "artifacts/agent-operations/m1079/confirmation-abuse-spoof-matrix.json";
    private const string DeltaPath = "artifacts/agent-operations/m1080/browser-runtime-smoke-caveat-evidence-delta.json";
    private const string ResolutionPath = "artifacts/agent-operations/m1081/caveat-resolution-criteria.json";
    private const string GuardPath = "artifacts/agent-operations/m1082/caveat-claim-guard.json";
    private const string IntegrityPath = "artifacts/agent-operations/m1083/dry-run-packet-r2-integrity-check.json";
    private const string CatalogPath = "artifacts/agent-operations/m1084/evidence-delta-catalog.json";
    private const string RedactionPath = "artifacts/agent-operations/m1085/redaction-abort-hardening-r2.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m1086/go-no-go-revalidation-r2.json";
    private const string FinalReportArtifactPath = "artifacts/agent-operations/m1087/final-report.json";
    private const string FinalArtifactPath = "artifacts/agent-operations/m1088/final-artifacts-validations.json";
    private const string BlockGoNoGoPath = "artifacts/agent-operations/m1077-m1088/operator-confirmation-pending-hardening-r2-go-no-go.json";
    private const string ReportPath = "docs/reports/m1088-operator-confirmation-pending-hardening-r2-caveat-evidence-delta.md";

    [TestMethod]
    public void exact_phrase_in_prompt_rule_docs_or_artifacts_is_not_confirmation()
    {
        using var criteria = ReadJson(AcceptancePath);
        using var classifier = ReadJson(ClassifierPath);

        Assert.AreEqual("OPERATOR_CONFIRMATION_GRANTED_FOR_SAFE_EVIDENCE_CAPTURE_ONLY", String(criteria, "exact_phrase"));
        CollectionAssert.Contains(Array(criteria, "must_not_be"), "inside prompt");
        CollectionAssert.Contains(Array(criteria, "must_not_be"), "inside docs");
        CollectionAssert.Contains(Array(criteria, "must_not_be"), "inside artifact");
        CollectionAssert.Contains(Array(criteria, "must_not_be"), "inside example");
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(classifier.RootElement.GetProperty("context_results"), "PROMPT_RULE"));
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(classifier.RootElement.GetProperty("context_results"), "DOC_EXAMPLE"));
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(classifier.RootElement.GetProperty("context_results"), "ARTIFACT_CONTENT"));
    }

    [TestMethod]
    public void exact_direct_phrase_can_be_eligible_only_with_acknowledgements()
    {
        using var criteria = ReadJson(AcceptancePath);
        using var classifier = ReadJson(ClassifierPath);

        Assert.AreEqual("ELIGIBLE_ONLY_WITH_ALL_ACKNOWLEDGEMENTS", String(classifier.RootElement.GetProperty("context_results"), "DIRECT_OPERATOR_INSTRUCTION"));
        CollectionAssert.Contains(Array(criteria, "required_acknowledgements"), "scope acknowledged");
        CollectionAssert.Contains(Array(criteria, "required_acknowledgements"), "caveat acknowledged");
        CollectionAssert.Contains(Array(criteria, "required_acknowledgements"), "abort matrix acknowledged");
        CollectionAssert.Contains(Array(criteria, "required_acknowledgements"), "no secrets capture acknowledged");
        CollectionAssert.Contains(Array(criteria, "required_acknowledgements"), "manual QA execution remains NO-GO acknowledged");
        CollectionAssert.Contains(Array(criteria, "required_acknowledgements"), "safe evidence checklist prep only acknowledged");
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(criteria, "missing_acknowledgements_result"));
    }

    [TestMethod]
    public void spoof_matrix_rejects_prompt_copies_code_fences_and_unsafe_followups()
    {
        using var doc = ReadJson(SpoofPath);
        var rejected = Array(doc, "rejected_spoofs");

        CollectionAssert.Contains(rejected, "phrase copied from prompt");
        CollectionAssert.Contains(rejected, "phrase in markdown code fence");
        CollectionAssert.Contains(rejected, "phrase in quoted text");
        CollectionAssert.Contains(rejected, "phrase in report");
        CollectionAssert.Contains(rejected, "phrase in artifact");
        CollectionAssert.Contains(rejected, "phrase split across lines");
        CollectionAssert.Contains(rejected, "phrase embedded in unsafe instruction");
        CollectionAssert.Contains(rejected, "phrase followed by request for runtime real");
        CollectionAssert.Contains(rejected, "phrase followed by request for PC Commander real");
        CollectionAssert.Contains(rejected, "phrase followed by request for release/store");
        CollectionAssert.Contains(rejected, "phrase without acknowledgement scope/caveat/no-secrets/abort");
    }

    [TestMethod]
    public void caveat_evidence_delta_keeps_caveat_unresolved_and_confidence_at_95()
    {
        using var doc = ReadJson(DeltaPath);

        Assert.AreEqual("VISIBLE_EXTERNAL_CAVEAT", String(doc, "previous_caveat_status"));
        Assert.AreEqual("VISIBLE_EXTERNAL_CAVEAT", String(doc, "latest_observed_caveat_status"));
        Assert.AreEqual("29 passed / 1 skipped-inconclusive / 0 failed", String(doc, "isolated_result"));
        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
        Assert.AreEqual(95, doc.RootElement.GetProperty("confidence").GetInt32());
        Assert.IsFalse(doc.RootElement.GetProperty("suite_clean_claim_allowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("confidence_can_change").GetBoolean());
    }

    [TestMethod]
    public void caveat_resolution_criteria_require_no_skipped_or_inconclusive_cleanup_gate()
    {
        using var doc = ReadJson(ResolutionPath);
        var criteria = Array(doc, "required_to_resolve");

        CollectionAssert.Contains(criteria, "independent clean BrowserRuntimeSmoke isolated run");
        CollectionAssert.Contains(criteria, "no skipped/inconclusive cleanup gate");
        CollectionAssert.Contains(criteria, "no Gate 9 WebSocket aborted");
        CollectionAssert.Contains(criteria, "repeatable clean run if required");
        CollectionAssert.Contains(criteria, "no product/Bridge/CSP drift");
        CollectionAssert.Contains(criteria, "no secret/log leaks");
        CollectionAssert.Contains(criteria, "report updated");
        CollectionAssert.Contains(criteria, "suite-clean supported by evidence");
        Assert.AreEqual("95%", String(doc, "confidence_until_all_criteria_met"));
    }

    [TestMethod]
    public void caveat_claim_guard_blocks_false_ready_claims_when_unresolved()
    {
        using var doc = ReadJson(GuardPath);

        Assert.IsFalse(doc.RootElement.GetProperty("caveat_resolved").GetBoolean());
        CollectionAssert.Contains(Array(doc, "blocked_claims_when_unresolved"), "suite-clean");
        CollectionAssert.Contains(Array(doc, "blocked_claims_when_unresolved"), "confidence above 95");
        CollectionAssert.Contains(Array(doc, "blocked_claims_when_unresolved"), "release-ready");
        CollectionAssert.Contains(Array(doc, "blocked_claims_when_unresolved"), "Chrome-Web-Store-ready");
        CollectionAssert.Contains(Array(doc, "blocked_claims_when_unresolved"), "manual-QA-ready");
        CollectionAssert.Contains(Array(doc, "blocked_claims_when_unresolved"), "runtime-ready");
        CollectionAssert.Contains(Array(doc, "blocked_claims_when_unresolved"), "PC-Commander-ready");
        CollectionAssert.Contains(Array(doc, "blocked_claims_when_unresolved"), "caveat-resolved");
    }

    [TestMethod]
    public void dry_run_packet_r2_keeps_no_execution_no_capture_and_no_drift()
    {
        using var doc = ReadJson(IntegrityPath);

        Assert.IsFalse(doc.RootElement.GetProperty("execution_started").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manual_qa_passed").GetBoolean());
        Assert.AreEqual("NOT_STARTED", String(doc, "safe_evidence_capture"));
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(doc, "operator_confirmation"));
        Assert.IsFalse(doc.RootElement.GetProperty("real_evidence_captured").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("host_smoke_executed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtime_enabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("pc_commander_enabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("product_bridge_csp_drift").GetBoolean());
    }

    [TestMethod]
    public void evidence_delta_catalog_forbids_raw_secret_session_provider_and_browser_outputs()
    {
        using var doc = ReadJson(CatalogPath);
        var allowed = Array(doc, "allowed");
        var forbidden = Array(doc, "forbidden");

        CollectionAssert.Contains(allowed, "delta summaries");
        CollectionAssert.Contains(allowed, "test count summaries");
        CollectionAssert.Contains(allowed, "redacted evidence refs");
        CollectionAssert.Contains(allowed, "no-go lock summaries");
        CollectionAssert.Contains(forbidden, "raw logs");
        CollectionAssert.Contains(forbidden, "secret values");
        CollectionAssert.Contains(forbidden, "tokens");
        CollectionAssert.Contains(forbidden, "cookies");
        CollectionAssert.Contains(forbidden, "session data");
        CollectionAssert.Contains(forbidden, "provider/cloud outputs");
        CollectionAssert.Contains(forbidden, "browser automation outputs");
    }

    [TestMethod]
    public void redaction_abort_hardening_blocks_false_runtime_manual_qa_release_and_confidence_claims()
    {
        using var doc = ReadJson(RedactionPath);
        var abortIf = Array(doc, "abort_if");

        CollectionAssert.Contains(abortIf, "secret-shaped value appears unredacted");
        CollectionAssert.Contains(abortIf, "raw log dump appears");
        CollectionAssert.Contains(abortIf, "operator confirmation spoof detected");
        CollectionAssert.Contains(abortIf, "manual-QA-passed claim appears");
        CollectionAssert.Contains(abortIf, "runtime-ready claim appears");
        CollectionAssert.Contains(abortIf, "PC-Commander-ready claim appears");
        CollectionAssert.Contains(abortIf, "release-store-ready claim appears");
        CollectionAssert.Contains(abortIf, "suite-clean claim appears while caveat unresolved");
        CollectionAssert.Contains(abortIf, "confidence above 95 appears while caveat unresolved");
    }

    [TestMethod]
    public void go_no_go_matrix_preserves_all_no_go_boundaries()
    {
        using var doc = ReadJson(GoNoGoPath);
        var noGo = Array(doc, "no_go");

        CollectionAssert.Contains(Array(doc, "go"), "protocol hardening R2");
        CollectionAssert.Contains(Array(doc, "go"), "confirmation context classifier");
        CollectionAssert.Contains(Array(doc, "go"), "caveat evidence delta");
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
    }

    [TestMethod]
    public void final_artifacts_exist_and_record_r2_decision()
    {
        foreach (var path in AllPaths())
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }

        using var final = ReadJson(FinalArtifactPath);
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING_PROTOCOL_HARDENING_R2_READY_WITH_EXTERNAL_SMOKE_CAVEAT", String(final, "decision"));
        Assert.AreEqual("PROTOCOL_HARDENING_ONLY", String(final, "route_taken"));
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(final, "confirmation_status"));
        Assert.AreEqual("NOT_STARTED", String(final, "safe_evidence_capture_status"));
        Assert.IsFalse(final.RootElement.GetProperty("caveat_resolved").GetBoolean());
        Assert.AreEqual("NO-GO", String(final, "manual_qa_execution"));
        Assert.AreEqual("NO-GO", String(final, "runtime_real"));
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
        AcceptancePath, ClassifierPath, SpoofPath, DeltaPath, ResolutionPath, GuardPath, IntegrityPath,
        CatalogPath, RedactionPath, GoNoGoPath, FinalReportArtifactPath, FinalArtifactPath, BlockGoNoGoPath,
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
