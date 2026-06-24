using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ManualQaEvidenceCaptureProtocolPrep")]
[TestCategory("M1029")]
[TestCategory("M1030")]
[TestCategory("M1031")]
[TestCategory("M1032")]
[TestCategory("M1033")]
[TestCategory("M1034")]
[TestCategory("M1035")]
[TestCategory("M1036")]
[TestCategory("M1037")]
[TestCategory("M1038")]
[TestCategory("M1039")]
[TestCategory("M1040")]
[TestCategory("M1029M1040")]
public sealed class NodalOsManualQaEvidenceCaptureProtocolM1029M1040Tests
{
    private const string IntakePath = "artifacts/agent-operations/m1029/followup-re-audit-result-intake.json";
    private const string StatusPath = "artifacts/agent-operations/m1030/accepted-remediation-status-consolidation.json";
    private const string CaveatPath = "artifacts/agent-operations/m1031/external-smoke-caveat-continuity-ledger.json";
    private const string ProtocolPath = "artifacts/agent-operations/m1032/manual-qa-evidence-capture-protocol-prep.json";
    private const string OperatorGatePath = "artifacts/agent-operations/m1033/operator-confirmation-gate.json";
    private const string SessionPath = "artifacts/agent-operations/m1034/evidence-capture-session-skeleton.json";
    private const string ReviewPath = "artifacts/agent-operations/m1035/evidence-review-intake-prep.json";
    private const string AbortPath = "artifacts/agent-operations/m1036/manual-qa-abort-matrix.json";
    private const string GoNoGoMatrixPath = "artifacts/agent-operations/m1037/manual-qa-go-no-go-matrix.json";
    private const string NextPath = "artifacts/agent-operations/m1038/next-path-recommendation.json";
    private const string FinalReportArtifactPath = "artifacts/agent-operations/m1039/final-report.json";
    private const string FinalArtifactPath = "artifacts/agent-operations/m1040/final-artifacts-validations.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m1029-m1040/manual-qa-evidence-capture-protocol-go-no-go.json";
    private const string ReportPath = "docs/reports/m1040-manual-qa-evidence-capture-protocol-execution-prep.md";

    [TestMethod]
    public void followup_reaudit_intake_records_conditional_go_and_no_go_boundaries()
    {
        using var doc = ReadJson(IntakePath);

        Assert.AreEqual("FOLLOWUP_REAUDIT_CONDITIONAL_GO", String(doc, "decision"));
        CollectionAssert.Contains(Array(doc, "acceptedFindings"), "F-001");
        CollectionAssert.Contains(Array(doc, "acceptedFindings"), "F-002");
        CollectionAssert.Contains(Array(doc, "acceptedFindings"), "F-003 hold");
        CollectionAssert.Contains(Array(doc, "newFindings"), "BrowserRuntimeSmoke Gate 9 WebSocket aborted external smoke caveat");
        Assert.AreEqual("accepted for safety-freeze/test-only remediation", String(doc, "f001Status"));
        Assert.AreEqual("accepted for safety-freeze/test-only remediation", String(doc, "f002Status"));
        Assert.AreEqual("HELD_FOR_REAL_CHANNEL_ACCEPTED", String(doc, "f003Status"));
        Assert.AreEqual("NO-GO", String(doc, "manualQaExecution"));
        Assert.AreEqual("NO-GO", String(doc, "runtimeReal"));
        Assert.AreEqual("NO-GO", String(doc, "releaseStore"));
    }

    [TestMethod]
    public void accepted_remediation_status_consolidates_f001_f002_and_f003_hold()
    {
        using var doc = ReadJson(StatusPath);

        Assert.AreEqual("accepted", String(doc.RootElement.GetProperty("f001"), "status"));
        CollectionAssert.Contains(Array(doc.RootElement.GetProperty("f001"), "acceptedControls"), "path-connected measured proof");
        CollectionAssert.Contains(Array(doc.RootElement.GetProperty("f001"), "acceptedControls"), "shared sink identity");
        CollectionAssert.Contains(Array(doc.RootElement.GetProperty("f001"), "acceptedControls"), "path-level negative tests");
        Assert.AreEqual("accepted", String(doc.RootElement.GetProperty("f002"), "status"));
        CollectionAssert.Contains(Array(doc.RootElement.GetProperty("f002"), "acceptedControls"), "generic redaction");
        CollectionAssert.Contains(Array(doc.RootElement.GetProperty("f002"), "acceptedControls"), "structured key matching");
        CollectionAssert.Contains(Array(doc.RootElement.GetProperty("f002"), "acceptedControls"), "metadata-only safe summary");
        Assert.AreEqual("HELD_FOR_REAL_CHANNEL", String(doc.RootElement.GetProperty("f003"), "status"));
        Assert.AreEqual("future default-deny contract only", String(doc.RootElement.GetProperty("f003"), "defaultDenyStatus"));
    }

    [TestMethod]
    public void external_smoke_caveat_ledger_blocks_full_suite_clean_and_100_percent_claims()
    {
        using var doc = ReadJson(CaveatPath);

        Assert.AreEqual("BrowserRuntimeSmoke Gate 9 WebSocket aborted", String(doc, "caveat"));
        Assert.IsTrue(doc.RootElement.GetProperty("externalSmokeCaveatVisible").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("fullSuiteGeneralClean").GetBoolean());
        Assert.AreEqual("95%", String(doc, "fullSuiteConfidence"));
        Assert.IsFalse(doc.RootElement.GetProperty("allows100PercentConfidenceClaim").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("invalidatesF001F002Remediation").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("blocksPublicReleaseStore").GetBoolean());
    }

    [TestMethod]
    public void manual_qa_evidence_capture_protocol_is_protocol_prep_only()
    {
        using var doc = ReadJson(ProtocolPath);

        CollectionAssert.Contains(Array(doc, "requiredEvidence"), "host visible evidence");
        CollectionAssert.Contains(Array(doc, "requiredEvidence"), "Bridge/CSP unchanged proof");
        CollectionAssert.Contains(Array(doc, "requiredEvidence"), "BrowserRuntimeSmoke caveat documented");
        Assert.AreEqual("NO-GO", String(doc, "manualQaExecution"));
        Assert.AreEqual("NOT_READY_EVIDENCE_PENDING", String(doc, "manualQaTrigger"));
        Assert.IsTrue(doc.RootElement.GetProperty("operatorConfirmationRequired").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("executionStarted").GetBoolean());
    }

    [TestMethod]
    public void operator_confirmation_gate_rejects_ready_passed_runtime_and_release_claims()
    {
        using var doc = ReadJson(OperatorGatePath);

        CollectionAssert.Contains(Array(doc, "allowedStates"), "OPERATOR_CONFIRMATION_PENDING");
        CollectionAssert.Contains(Array(doc, "allowedStates"), "OPERATOR_CONFIRMED_FOR_FUTURE_QA_PREP_ONLY");
        CollectionAssert.Contains(Array(doc, "rejectedClaims"), "MANUAL_QA_READY");
        CollectionAssert.Contains(Array(doc, "rejectedClaims"), "MANUAL_QA_PASSED");
        CollectionAssert.Contains(Array(doc, "rejectedClaims"), "RUNTIME_READY");
        CollectionAssert.Contains(Array(doc, "rejectedClaims"), "PC_COMMANDER_READY");
        Assert.AreEqual("NO-GO", String(doc, "manualQaExecution"));
    }

    [TestMethod]
    public void evidence_session_skeleton_remains_planned_only()
    {
        using var doc = ReadJson(SessionPath);

        Assert.AreEqual("PLANNED_ONLY", String(doc, "sessionStatus"));
        Assert.IsFalse(doc.RootElement.GetProperty("executionStarted").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("manualQaPassed").GetBoolean());
        Assert.AreEqual("NO-GO", String(doc, "manualQaExecutionStatus"));
        Assert.AreEqual("NO-GO", String(doc, "runtimeStatus"));
        CollectionAssert.Contains(Array(doc, "forbiddenActions"), "shell arbitrary");
        CollectionAssert.Contains(Array(doc, "abortConditions"), "Bridge/CSP changed");
    }

    [TestMethod]
    public void evidence_review_intake_rejects_unsafe_claims_and_secret_leaks()
    {
        using var doc = ReadJson(ReviewPath);
        var rejectIf = Array(doc, "rejectIf");

        CollectionAssert.Contains(rejectIf, "manual QA passed claim without full review");
        CollectionAssert.Contains(rejectIf, "runtime ready claim");
        CollectionAssert.Contains(rejectIf, "PC Commander ready claim");
        CollectionAssert.Contains(rejectIf, "release ready claim");
        CollectionAssert.Contains(rejectIf, "secret leaks");
        CollectionAssert.Contains(rejectIf, "unredacted credentials");
        CollectionAssert.Contains(rejectIf, "product files changed without scope");
        CollectionAssert.Contains(rejectIf, "Bridge/CSP changed without scope");
    }

    [TestMethod]
    public void abort_matrix_aborts_on_scope_drift_hidden_caveat_and_missing_proofs()
    {
        using var doc = ReadJson(AbortPath);
        var abortIf = Array(doc, "abortIf");

        CollectionAssert.Contains(abortIf, "wrong worktree");
        CollectionAssert.Contains(abortIf, "wrong branch");
        CollectionAssert.Contains(abortIf, "dirty worktree");
        CollectionAssert.Contains(abortIf, "product files changed");
        CollectionAssert.Contains(abortIf, "Bridge/CSP changed");
        CollectionAssert.Contains(abortIf, "BrowserRuntimeSmoke caveat hidden");
        CollectionAssert.Contains(abortIf, "full-suite clean claimed incorrectly");
        CollectionAssert.Contains(abortIf, "operator confirmation missing");
        CollectionAssert.Contains(abortIf, "secret captured");
    }

    [TestMethod]
    public void go_no_go_matrix_preserves_no_go_statuses_and_allows_only_protocol_readiness()
    {
        using var doc = ReadJson(GoNoGoMatrixPath);

        CollectionAssert.Contains(Array(doc, "goOnlyFor"), "protocol prepared");
        CollectionAssert.Contains(Array(doc, "goOnlyFor"), "operator gate defined");
        CollectionAssert.Contains(Array(doc, "goOnlyFor"), "caveat visible");
        CollectionAssert.Contains(Array(doc, "noGoFor"), "manual QA execution");
        CollectionAssert.Contains(Array(doc, "noGoFor"), "runtime real");
        CollectionAssert.Contains(Array(doc, "noGoFor"), "PC Commander real");
        CollectionAssert.Contains(Array(doc, "noGoFor"), "release/store");
        Assert.IsFalse(doc.RootElement.GetProperty("manualQaReadyClaimAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("fullSuiteCleanClaimAllowed").GetBoolean());
    }

    [TestMethod]
    public void next_path_recommendation_requires_explicit_operator_confirmation()
    {
        using var doc = ReadJson(NextPath);

        Assert.IsTrue(doc.RootElement.GetProperty("operatorConfirmationRequired").GetBoolean());
        StringAssert.Contains(String(doc, "ifOperatorExplicitlyApproves"), "Manual QA Evidence Capture Operator Checklist Execution");
        StringAssert.Contains(String(doc, "ifNoOperatorConfirmation"), "Manual QA Protocol Hardening");
        StringAssert.Contains(String(doc, "ifBrowserRuntimeSmokeRemainsBlocking"), "BrowserRuntimeSmoke Caveat Isolation");
        Assert.IsFalse(doc.RootElement.GetProperty("manualQaExecutionStarted").GetBoolean());
    }

    [TestMethod]
    public void final_artifacts_exist_preserve_boundaries_and_do_not_claim_full_suite_clean()
    {
        foreach (var path in new[]
        {
            IntakePath, StatusPath, CaveatPath, ProtocolPath, OperatorGatePath, SessionPath,
            ReviewPath, AbortPath, GoNoGoMatrixPath, NextPath, FinalReportArtifactPath,
            FinalArtifactPath, GoNoGoPath, ReportPath
        })
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }

        using var final = ReadJson(FinalArtifactPath);
        Assert.AreEqual("MANUAL_QA_EVIDENCE_CAPTURE_PROTOCOL_EXECUTION_PREP_READY_WITH_EXTERNAL_SMOKE_CAVEAT", String(final, "decision"));
        Assert.AreEqual("NO-GO", String(final, "manualQaExecution"));
        Assert.AreEqual("NO-GO", String(final, "runtimeReal"));
        Assert.AreEqual("NO-GO", String(final, "pcCommanderReal"));
        Assert.IsFalse(final.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(final.RootElement.GetProperty("bridgeCspModified").GetBoolean());
        Assert.IsFalse(final.RootElement.GetProperty("fullSuiteCleanClaimed").GetBoolean());
    }

    [TestMethod]
    public void artifacts_contain_no_secret_leaks_or_forbidden_ready_claims()
    {
        var files = new[]
        {
            IntakePath, StatusPath, CaveatPath, ProtocolPath, OperatorGatePath, SessionPath,
            ReviewPath, AbortPath, GoNoGoMatrixPath, NextPath, FinalReportArtifactPath,
            FinalArtifactPath, GoNoGoPath, ReportPath
        };

        foreach (var file in files)
        {
            var content = File.ReadAllText(FullPath(file));
            foreach (var forbidden in new[] { "sk-", "AKIA", "BEGIN PRIVATE KEY", "Authorization: Bearer", "full suite clean", "100% confidence" })
            {
                Assert.IsFalse(content.Contains(forbidden, StringComparison.Ordinal), $"{file} contains forbidden wording {forbidden}");
            }
        }
    }

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
