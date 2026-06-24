using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ManualQaOperatorChecklistGate")]
[TestCategory("M1041")]
[TestCategory("M1042")]
[TestCategory("M1043")]
[TestCategory("M1044")]
[TestCategory("M1045")]
[TestCategory("M1046")]
[TestCategory("M1047")]
[TestCategory("M1048")]
[TestCategory("M1049")]
[TestCategory("M1050")]
[TestCategory("M1051")]
[TestCategory("M1052")]
[TestCategory("M1041M1052")]
public sealed class NodalOsManualQaOperatorChecklistGateM1041M1052Tests
{
    private const string ConfirmationPath = "artifacts/agent-operations/m1041/operator-confirmation-detection.json";
    private const string RoutePath = "artifacts/agent-operations/m1042/route-selection-gate.json";
    private const string ScopePath = "artifacts/agent-operations/m1043/operator-scope-acknowledgement-contract.json";
    private const string ChecklistPath = "artifacts/agent-operations/m1044/evidence-capture-checklist-hardening.json";
    private const string CaveatPath = "artifacts/agent-operations/m1045/browser-runtime-smoke-caveat-containment.json";
    private const string NamingPath = "artifacts/agent-operations/m1046/safe-evidence-folder-naming-protocol.json";
    private const string RedactionPath = "artifacts/agent-operations/m1047/redaction-review-checklist-human-evidence.json";
    private const string AbortPath = "artifacts/agent-operations/m1048/abort-matrix-revalidation.json";
    private const string SessionPath = "artifacts/agent-operations/m1049/manual-qa-session-status-contract.json";
    private const string GoNoGoMatrixPath = "artifacts/agent-operations/m1050/go-no-go-matrix-update.json";
    private const string FinalReportArtifactPath = "artifacts/agent-operations/m1051/final-report.json";
    private const string FinalArtifactPath = "artifacts/agent-operations/m1052/final-artifacts-validations.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m1041-m1052/manual-qa-operator-checklist-gate-go-no-go.json";
    private const string ReportPath = "docs/reports/m1052-manual-qa-operator-checklist-gate-protocol-hardening.md";

    [TestMethod]
    public void missing_operator_confirmation_selects_protocol_hardening_only()
    {
        using var confirmation = ReadJson(ConfirmationPath);
        using var route = ReadJson(RoutePath);

        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(confirmation, "confirmation_status"));
        Assert.IsFalse(confirmation.RootElement.GetProperty("confirmation_phrase_detected").GetBoolean());
        Assert.AreEqual("NO-GO", String(confirmation, "manual_qa_execution_status"));
        Assert.AreEqual("PROTOCOL_HARDENING_ONLY", String(route, "route"));
        Assert.AreEqual("MANUAL_QA_PROTOCOL_HARDENING_READY_WITH_EXTERNAL_SMOKE_CAVEAT", String(route, "expected_decision"));
    }

    [TestMethod]
    public void explicit_confirmation_phrase_is_defined_but_not_assumed()
    {
        using var route = ReadJson(RoutePath);

        Assert.AreEqual("OPERATOR_CONFIRMATION_GRANTED_FOR_SAFE_EVIDENCE_CAPTURE_ONLY", String(route, "required_explicit_phrase"));
        Assert.AreEqual("SAFE_EVIDENCE_CHECKLIST_PREP_ONLY", String(route.RootElement.GetProperty("routes"), "when_explicit_confirmation"));
        Assert.AreEqual("PROTOCOL_HARDENING_ONLY", String(route.RootElement.GetProperty("routes"), "when_confirmation_pending"));
        CollectionAssert.Contains(Array(route, "rejected_ambiguous_inputs"), "operator approved");
        CollectionAssert.Contains(Array(route, "rejected_ambiguous_inputs"), "QA ready");
    }

    [TestMethod]
    public void operator_scope_contract_forbids_runtime_pc_commander_release_and_dangerous_actions()
    {
        using var doc = ReadJson(ScopePath);
        var forbidden = Array(doc, "forbidden_actions");

        CollectionAssert.Contains(Array(doc, "allowed_actions"), "review checklist");
        CollectionAssert.Contains(Array(doc, "allowed_actions"), "confirm product files unchanged");
        CollectionAssert.Contains(Array(doc, "allowed_actions"), "confirm Bridge/CSP unchanged");
        CollectionAssert.Contains(forbidden, "shell");
        CollectionAssert.Contains(forbidden, "filesystem write");
        CollectionAssert.Contains(forbidden, "runtime real");
        CollectionAssert.Contains(forbidden, "PC Commander real");
        CollectionAssert.Contains(forbidden, "release/store");
    }

    [TestMethod]
    public void checklist_hardening_keeps_execution_not_started_and_manual_qa_not_passed()
    {
        using var doc = ReadJson(ChecklistPath);

        CollectionAssert.Contains(Array(doc, "required_evidence"), "operator confirmation evidence");
        CollectionAssert.Contains(Array(doc, "required_evidence"), "BrowserRuntimeSmoke caveat evidence");
        CollectionAssert.Contains(Array(doc, "required_evidence"), "product files unchanged proof");
        Assert.IsFalse(doc.RootElement.GetProperty("execution_started").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manual_qa_passed").GetBoolean());
        Assert.AreEqual("NO-GO", String(doc, "manual_qa_execution"));
    }

    [TestMethod]
    public void browser_runtime_smoke_caveat_containment_blocks_clean_and_hundred_percent_claims()
    {
        using var doc = ReadJson(CaveatPath);

        Assert.AreEqual("Gate 9 WebSocket aborted", String(doc, "caveat"));
        Assert.IsTrue(doc.RootElement.GetProperty("external_smoke_caveat_visible").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("full_suite_general_clean_claim_allowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("hundred_percent_confidence_claim_allowed").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("blocks_release_store").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("invalidates_f001_f002_accepted_safety_freeze_remediation").GetBoolean());
    }

    [TestMethod]
    public void safe_evidence_naming_protocol_forbids_raw_secrets_and_log_dumps()
    {
        using var doc = ReadJson(NamingPath);
        var forbidden = Array(doc, "forbidden_content");

        CollectionAssert.Contains(Array(doc, "safe_names"), "screenshots-redacted");
        CollectionAssert.Contains(Array(doc, "safe_names"), "operator-notes-redacted");
        CollectionAssert.Contains(forbidden, "raw secrets");
        CollectionAssert.Contains(forbidden, "raw logs extensive");
        CollectionAssert.Contains(forbidden, "credentials");
        CollectionAssert.Contains(forbidden, "cookies");
        Assert.AreEqual("PLAN_ONLY", String(doc, "protocol_mode"));
    }

    [TestMethod]
    public void redaction_checklist_covers_credentials_session_browser_and_provider_secrets()
    {
        using var doc = ReadJson(RedactionPath);
        var covered = Array(doc, "must_review_and_redact");

        CollectionAssert.Contains(covered, "API keys");
        CollectionAssert.Contains(covered, "tokens");
        CollectionAssert.Contains(covered, "passwords");
        CollectionAssert.Contains(covered, "private keys");
        CollectionAssert.Contains(covered, "authorization headers");
        CollectionAssert.Contains(covered, "cookies");
        CollectionAssert.Contains(covered, "session ids");
        CollectionAssert.Contains(covered, "browser session data");
        CollectionAssert.Contains(covered, "provider/cloud secrets");
    }

    [TestMethod]
    public void abort_matrix_aborts_if_confirmation_missing_for_capture_or_scope_drift_occurs()
    {
        using var doc = ReadJson(AbortPath);
        var abortIf = Array(doc, "abort_if");

        CollectionAssert.Contains(abortIf, "operator confirmation missing for capture route");
        CollectionAssert.Contains(abortIf, "wrong branch");
        CollectionAssert.Contains(abortIf, "wrong worktree");
        CollectionAssert.Contains(abortIf, "dirty worktree");
        CollectionAssert.Contains(abortIf, "product files changed");
        CollectionAssert.Contains(abortIf, "Bridge/CSP changed");
        CollectionAssert.Contains(abortIf, "BrowserRuntimeSmoke caveat hidden");
        CollectionAssert.Contains(abortIf, "full-suite clean claimed incorrectly");
        CollectionAssert.Contains(abortIf, "runtime real claim");
        CollectionAssert.Contains(abortIf, "release/store claim");
    }

    [TestMethod]
    public void manual_qa_session_status_contract_never_allows_ready_passed_runtime_or_release_claims()
    {
        using var doc = ReadJson(SessionPath);

        CollectionAssert.Contains(Array(doc, "allowed_statuses"), "PROTOCOL_HARDENING_ONLY");
        CollectionAssert.Contains(Array(doc, "allowed_statuses"), "SAFE_EVIDENCE_CHECKLIST_PREP_ONLY");
        CollectionAssert.Contains(Array(doc, "forbidden_statuses"), "MANUAL_QA_PASSED");
        CollectionAssert.Contains(Array(doc, "forbidden_statuses"), "RUNTIME_READY");
        CollectionAssert.Contains(Array(doc, "forbidden_statuses"), "PC_COMMANDER_READY");
        CollectionAssert.Contains(Array(doc, "forbidden_statuses"), "RELEASE_READY");
        Assert.AreEqual("PROTOCOL_HARDENING_ONLY", String(doc, "current_session_status"));
    }

    [TestMethod]
    public void go_no_go_matrix_preserves_all_no_go_boundaries()
    {
        using var doc = ReadJson(GoNoGoMatrixPath);
        var noGo = Array(doc, "no_go");

        CollectionAssert.Contains(Array(doc, "go"), "protocol hardening");
        CollectionAssert.Contains(Array(doc, "go"), "operator gate");
        CollectionAssert.Contains(Array(doc, "go"), "caveat containment");
        CollectionAssert.Contains(noGo, "manual QA execution");
        CollectionAssert.Contains(noGo, "runtime real");
        CollectionAssert.Contains(noGo, "PC Commander real");
        CollectionAssert.Contains(noGo, "provider/cloud");
        CollectionAssert.Contains(noGo, "filesystem/browser/capability unlock");
        CollectionAssert.Contains(noGo, "product files");
        CollectionAssert.Contains(noGo, "Bridge/CSP");
        CollectionAssert.Contains(noGo, "release/store");
        Assert.IsFalse(doc.RootElement.GetProperty("full_suite_clean_claim_allowed").GetBoolean());
    }

    [TestMethod]
    public void final_artifacts_exist_and_record_protocol_hardening_decision()
    {
        foreach (var path in AllPaths())
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }

        using var final = ReadJson(FinalArtifactPath);
        Assert.AreEqual("MANUAL_QA_PROTOCOL_HARDENING_READY_WITH_EXTERNAL_SMOKE_CAVEAT", String(final, "decision"));
        Assert.AreEqual("PROTOCOL_HARDENING_ONLY", String(final, "route_taken"));
        Assert.AreEqual("OPERATOR_CONFIRMATION_PENDING", String(final, "operator_confirmation_status"));
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
        ConfirmationPath, RoutePath, ScopePath, ChecklistPath, CaveatPath, NamingPath, RedactionPath,
        AbortPath, SessionPath, GoNoGoMatrixPath, FinalReportArtifactPath, FinalArtifactPath, GoNoGoPath,
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
