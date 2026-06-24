using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ReAuditPackage")]
[TestCategory("M1005")]
[TestCategory("M1006")]
[TestCategory("M1007")]
[TestCategory("M1008")]
[TestCategory("M1009")]
[TestCategory("M1010")]
[TestCategory("M1011")]
[TestCategory("M1012")]
[TestCategory("M1013")]
[TestCategory("M1014")]
[TestCategory("M1015")]
[TestCategory("M1016")]
[TestCategory("M1005M1016")]
public sealed class NodalOsReAuditPackageM1005M1016Tests
{
    private const string ScopePath = "artifacts/agent-operations/m1005/re-audit-scope.json";
    private const string InventoryPath = "artifacts/agent-operations/m1006/re-audit-evidence-inventory.json";
    private const string MatrixPath = "artifacts/agent-operations/m1007/before-after-traceability-matrix.json";
    private const string PromptPath = "artifacts/agent-operations/m1008/claude-re-audit-prompt.json";
    private const string PromptMarkdownPath = "artifacts/agent-operations/m1008/claude-re-audit-prompt.md";
    private const string IntakePath = "artifacts/agent-operations/m1009/re-audit-result-intake-schema.json";
    private const string DecisionRulesPath = "artifacts/agent-operations/m1010/re-audit-decision-rules.json";
    private const string ManualQaHoldPath = "artifacts/agent-operations/m1011/manual-qa-hold-reaffirmation.json";
    private const string RuntimeHoldPath = "artifacts/agent-operations/m1012/runtime-real-hold-reaffirmation.json";
    private const string NextPath = "artifacts/agent-operations/m1013/next-path-recommendation-by-re-audit-result.json";
    private const string ReadinessPath = "artifacts/agent-operations/m1014/re-audit-readiness-report.json";
    private const string FinalArtifactsPath = "artifacts/agent-operations/m1015/final-re-audit-package-artifacts.json";
    private const string FinalReportPath = "artifacts/agent-operations/m1016/final-report.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m1005-m1016/re-audit-package-remediated-noop-harness-go-no-go.json";
    private const string ReportPath = "docs/reports/m1016-re-audit-package-remediated-noop-harness-safety.md";
    private const string BaseCommit = "64666a587e208e17d81b0ba181741e6ac4e52485";

    [TestMethod]
    public void re_audit_scope_covers_f001_f002_f003_and_no_go_boundaries()
    {
        using var doc = ReadJson(ScopePath);
        var findings = Array(doc, "findingsInScope");

        CollectionAssert.Contains(findings, "F-001");
        CollectionAssert.Contains(findings, "F-002");
        CollectionAssert.Contains(findings, "F-003");
        CollectionAssert.Contains(Array(doc, "f001ReviewItems"), "NoSideEffectProof.FromSink(sink)");
        CollectionAssert.Contains(Array(doc, "f002ReviewItems"), "structured forbidden-field redactor");
        CollectionAssert.Contains(Array(doc, "f003ReviewItems"), "DefaultDenyCommandInterceptor future contract");
        Assert.AreEqual("NO-GO", String(doc, "manualQaExecution"));
        Assert.AreEqual("NO-GO", String(doc, "runtimeReal"));
        Assert.AreEqual("NO-GO", String(doc, "pcCommanderReal"));
        Assert.AreEqual("OPEN_BROWSER_RUNTIME_SMOKE_CLEANUP_EXTERNAL_QUARANTINED_VISIBLE", String(doc, "browserRuntimeSmokeCaveat"));
    }

    [TestMethod]
    public void evidence_inventory_includes_remediation_and_prior_line_artifacts()
    {
        using var doc = ReadJson(InventoryPath);
        var paths = doc.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("path").GetString() ?? string.Empty)
            .ToArray();

        Assert.IsTrue(paths.Any(path => path.Contains("NodalOsAuditFindingsRemediationM993M1004Tests.cs", StringComparison.Ordinal)));
        Assert.IsTrue(paths.Any(path => path.Contains("m1004-audit-findings-remediation.md", StringComparison.Ordinal)));
        Assert.IsTrue(paths.Any(path => path.Contains("m993-m1004", StringComparison.Ordinal)));
        Assert.IsTrue(paths.Any(path => path.Contains("m981-m992", StringComparison.Ordinal)));
        Assert.IsTrue(paths.Any(path => path.Contains("m933", StringComparison.Ordinal)));
        Assert.IsTrue(doc.RootElement.GetProperty("productBridgeBoundaryIncluded").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("browserRuntimeSmokeCaveatIncluded").GetBoolean());
    }

    [TestMethod]
    public void before_after_matrix_explains_each_finding()
    {
        using var doc = ReadJson(MatrixPath);
        var rows = doc.RootElement.GetProperty("rows").EnumerateArray().ToArray();

        Assert.AreEqual(3, rows.Length);
        AssertRow(rows, "F-001", "hardcoded constants", "sink-derived measured proof");
        AssertRow(rows, "F-002", "4-substring denylist", "structured fields");
        AssertRow(rows, "F-003", "wording ambiguous", "classification-only");

        foreach (var row in rows)
        {
            Assert.AreEqual("protected", row.GetProperty("noGoProtected").GetString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.GetProperty("reAuditQuestion").GetString()));
        }
    }

    [TestMethod]
    public void claude_prompt_demands_reaudit_decisions_and_specific_review()
    {
        using var doc = ReadJson(PromptPath);
        var decisions = Array(doc, "requiredDecision");

        CollectionAssert.AreEquivalent(new[] { "RE_AUDIT_GO", "RE_AUDIT_CONDITIONAL_GO", "RE_AUDIT_NO_GO" }, decisions);
        CollectionAssert.Contains(Array(doc, "requiredSeverity"), "BLOCKER");
        CollectionAssert.Contains(Array(doc, "mustReview"), "F-001 FromSink negative tests");
        CollectionAssert.Contains(Array(doc, "mustReview"), "F-002 realistic-shaped fake secrets");
        CollectionAssert.Contains(Array(doc, "mustReview"), "F-003 held not enforced guard");
        Assert.AreEqual("NO-GO", String(doc, "manualQaExecution"));
        Assert.AreEqual("NO-GO", String(doc, "runtimeReal"));
        StringAssert.Contains(File.ReadAllText(FullPath(PromptMarkdownPath)), "PEDIR RE-AUDITORIA CLAUDE");
    }

    [TestMethod]
    public void re_audit_intake_schema_rejects_runtime_manual_qa_release_ready_claims()
    {
        using var doc = ReadJson(IntakePath);
        var required = Array(doc, "requiredFields");
        var rejects = Array(doc, "rejectIf");

        CollectionAssert.Contains(required, "base_commit");
        CollectionAssert.Contains(required, "decision");
        CollectionAssert.Contains(required, "f001_status");
        CollectionAssert.Contains(rejects, "declares runtime ready");
        CollectionAssert.Contains(rejects, "declares manual QA passed");
        CollectionAssert.Contains(rejects, "declares PC Commander ready");
        CollectionAssert.Contains(rejects, "declares release ready");
        CollectionAssert.Contains(rejects, "ignores BrowserRuntimeSmoke caveat");
        CollectionAssert.Contains(rejects, "ignores F-003 real-channel hold");
        Assert.AreEqual(BaseCommit, String(doc, "baseCommit"));
    }

    [TestMethod]
    public void decision_rules_do_not_unlock_qa_or_runtime()
    {
        using var doc = ReadJson(DecisionRulesPath);
        var go = Array(doc, "reAuditGoRequires");
        var noGo = Array(doc, "reAuditNoGoIf");

        CollectionAssert.Contains(go, "F-001 accepted");
        CollectionAssert.Contains(go, "F-002 accepted");
        CollectionAssert.Contains(go, "F-003 hold accepted");
        CollectionAssert.Contains(go, "still no runtime/manual QA unlock");
        CollectionAssert.Contains(noGo, "F-001 still tautological");
        CollectionAssert.Contains(noGo, "F-002 still leaks realistic-shaped fake secrets");
        CollectionAssert.Contains(noGo, "F-003 wording misleading");
        CollectionAssert.Contains(noGo, "caveat hidden");
        Assert.IsFalse(doc.RootElement.GetProperty("autoUnlocksManualQa").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("autoUnlocksRuntimeReal").GetBoolean());
    }

    [TestMethod]
    public void manual_qa_hold_and_runtime_hold_are_reaffirmed()
    {
        using var manual = ReadJson(ManualQaHoldPath);
        using var runtime = ReadJson(RuntimeHoldPath);

        Assert.AreEqual("MANUAL_QA_HOLD_ACTIVE", String(manual, "manualQaHold"));
        Assert.AreEqual("NO-GO", String(manual, "manualQaExecution"));
        Assert.AreEqual("NOT_READY_EVIDENCE_PENDING", String(manual, "manualQaTrigger"));
        Assert.AreEqual("NO-GO", String(runtime, "runtimeReal"));
        Assert.AreEqual("NO-GO", String(runtime, "pcCommanderReal"));
        Assert.AreEqual("NO-GO", String(runtime, "providerCloud"));
        Assert.AreEqual("HELD_FOR_REAL_CHANNEL", String(runtime, "f003Status"));
    }

    [TestMethod]
    public void next_path_recommendation_is_by_reaudit_result()
    {
        using var doc = ReadJson(NextPath);
        var recommendations = doc.RootElement.GetProperty("recommendationByDecision");

        Assert.IsTrue(recommendations.TryGetProperty("RE_AUDIT_GO", out var go));
        Assert.IsTrue(recommendations.TryGetProperty("RE_AUDIT_CONDITIONAL_GO", out var conditional));
        Assert.IsTrue(recommendations.TryGetProperty("RE_AUDIT_NO_GO", out var noGo));
        StringAssert.Contains(go.GetString() ?? string.Empty, "Manual QA Evidence Capture Protocol Execution Prep");
        StringAssert.Contains(conditional.GetString() ?? string.Empty, "Re-Audit Followup Remediation Block");
        StringAssert.Contains(noGo.GetString() ?? string.Empty, "Safety Freeze");
    }

    [TestMethod]
    public void final_package_artifacts_exist_and_request_claude_reaudit()
    {
        foreach (var path in new[]
        {
            ScopePath, InventoryPath, MatrixPath, PromptPath, PromptMarkdownPath, IntakePath,
            DecisionRulesPath, ManualQaHoldPath, RuntimeHoldPath, NextPath, ReadinessPath,
            FinalArtifactsPath, FinalReportPath, GoNoGoPath, ReportPath
        })
        {
            Assert.IsTrue(File.Exists(FullPath(path)), path);
        }

        using var doc = ReadJson(FinalReportPath);
        Assert.AreEqual("PEDIR RE-AUDITORIA CLAUDE", String(doc, "reauditRecommendation"));
        Assert.AreEqual("RE_AUDIT_PACKAGE_REMEDIATED_NOOP_HARNESS_SAFETY_READY_WITH_EXTERNAL_SMOKE_CAVEAT", String(doc, "decision"));
        Assert.IsFalse(doc.RootElement.GetProperty("auditGoDeclared").GetBoolean());
    }

    [TestMethod]
    public void go_no_go_preserves_all_restricted_surfaces()
    {
        using var doc = ReadJson(GoNoGoPath);

        Assert.AreEqual("RE_AUDIT_PACKAGE_REMEDIATED_NOOP_HARNESS_SAFETY_READY_WITH_EXTERNAL_SMOKE_CAVEAT", String(doc, "decision"));
        Assert.AreEqual("NO-GO", String(doc, "manualQaExecution"));
        Assert.AreEqual("NOT_READY_EVIDENCE_PENDING", String(doc, "manualQaTrigger"));
        Assert.AreEqual("NO-GO", String(doc, "runtimeReal"));
        Assert.AreEqual("NO-GO", String(doc, "pcCommanderReal"));
        Assert.AreEqual("NO-GO", String(doc, "releaseStore"));
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("bridgeCspModified").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("auditGoDeclared").GetBoolean());
        StringAssert.Contains(String(doc, "fullSuiteConfidence"), "95%");
    }

    [TestMethod]
    public void browser_runtime_smoke_caveat_remains_visible()
    {
        using var doc = ReadJson(ReadinessPath);

        Assert.AreEqual("OPEN_BROWSER_RUNTIME_SMOKE_CLEANUP_EXTERNAL_QUARANTINED_VISIBLE", String(doc, "browserRuntimeSmokeCaveat"));
        StringAssert.Contains(String(doc, "fullSuiteConfidence"), "95%");
    }

    [TestMethod]
    public void package_artifacts_do_not_contain_raw_secret_like_values()
    {
        foreach (var path in new[] { ScopePath, InventoryPath, MatrixPath, PromptPath, IntakePath, DecisionRulesPath, ManualQaHoldPath, RuntimeHoldPath, NextPath, ReadinessPath, FinalArtifactsPath, FinalReportPath, GoNoGoPath })
        {
            var content = File.ReadAllText(FullPath(path));
            foreach (var leak in new[] { "sk-ant-fake-", "sk-proj-fake-", "AKIAFAKE", "BEGIN FAKE PRIVATE KEY", "fake.jwt.like.value", "fake_password", "xoxb-fake", "ghp_fake", "fake-session", "fake-secret-value", "fake-key-value" })
            {
                Assert.IsFalse(content.Contains(leak, StringComparison.Ordinal), $"{path} contains {leak}");
            }
        }
    }

    private static void AssertRow(JsonElement[] rows, string finding, string before, string after)
    {
        var row = rows.Single(item => item.GetProperty("finding").GetString() == finding);
        StringAssert.Contains(row.GetProperty("before").GetString() ?? string.Empty, before);
        StringAssert.Contains(row.GetProperty("after").GetString() ?? string.Empty, after);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) => Path.Combine(RepoRoot(), relativePath);
    private static JsonDocument ReadJson(string relativePath) => JsonDocument.Parse(File.ReadAllText(FullPath(relativePath)));
    private static string String(JsonDocument doc, string property) => doc.RootElement.GetProperty(property).GetString() ?? string.Empty;
    private static string[] Array(JsonDocument doc, string property) => doc.RootElement.GetProperty(property).EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
}
