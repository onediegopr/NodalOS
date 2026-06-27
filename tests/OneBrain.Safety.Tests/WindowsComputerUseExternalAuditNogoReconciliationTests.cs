using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.WindowsComputerUse;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("WindowsComputerUseFixtureSafe")]
[TestCategory("WindowsComputerUseExternalAuditNogoReconciliation")]
public sealed class WindowsComputerUseExternalAuditNogoReconciliationTests
{
    [TestMethod]
    public void ExternalAuditStatusMapsContainmentPassToLiveAdvanceNoGo()
    {
        var record = ComputerUseExternalAuditReconciliation.Current();

        Assert.AreEqual("AUDIT_CONTAINMENT_PASS_BUT_LIVE_ADVANCE_NO_GO", record.Decision);
        Assert.AreEqual(ComputerUseExternalAuditContainmentStatus.ContainmentPass, record.ContainmentStatus);
        Assert.AreEqual(ComputerUseExternalAuditLiveAdvanceStatus.LiveAdvanceNoGo, record.LiveAdvanceStatus);
        Assert.IsFalse(record.CurrentCodeDefectFound);
        Assert.IsFalse(record.AuditorRanBuild);
        Assert.IsFalse(record.AuditorRanTests);
        Assert.IsFalse(record.BehavioralLiveSafetyProven);
    }

    [TestMethod]
    public void ContainmentPassDoesNotGrantLiveAuthorization()
    {
        var record = ComputerUseExternalAuditReconciliation.Current();
        var gate = ComputerUseReadOnlyLiveGateCatalog.Evaluate(new ComputerUseReadOnlyLiveGateRequest());

        Assert.AreEqual(ComputerUseExternalAuditContainmentStatus.ContainmentPass, record.ContainmentStatus);
        Assert.IsFalse(record.LiveReadPermitted);
        Assert.IsFalse(record.ActionAuthorityGranted);
        Assert.IsFalse(record.ProductAutomationEnabled);
        Assert.IsFalse(gate.LiveReadPermitted);
        Assert.IsFalse(gate.ActionAuthorityGranted);
        Assert.IsFalse(gate.ProductAutomationEnabled);
    }

    [TestMethod]
    public void ReconciliationReportRecordsNoGoWithoutInventingAuditorBuildOrTestPass()
    {
        var report = ReadJson(Path.Combine("docs", "qa", "computer-use", "wcu-037a-external-audit-nogo-reconciliation", "report.json"));
        var audit = report.RootElement.GetProperty("external_audit");
        var auth = report.RootElement.GetProperty("authorization_state");

        Assert.AreEqual("AUDIT_CONTAINMENT_PASS_BUT_LIVE_ADVANCE_NO_GO", report.RootElement.GetProperty("audit_result").GetString());
        Assert.AreEqual("NO_GO", audit.GetProperty("live_advance").GetString());
        Assert.IsFalse(audit.GetProperty("current_code_defect_found").GetBoolean());
        Assert.IsFalse(audit.GetProperty("auditor_ran_dotnet_build").GetBoolean());
        Assert.IsFalse(audit.GetProperty("auditor_ran_dotnet_test").GetBoolean());
        Assert.IsFalse(audit.GetProperty("behavioral_live_safety_proven").GetBoolean());
        Assert.IsFalse(auth.GetProperty("live_prototype_authorized").GetBoolean());
        Assert.IsTrue(auth.GetProperty("live_remains_blocked").GetBoolean());
    }

    [TestMethod]
    public void NoReportDecisionCanClaimLivePrototypeAllowed()
    {
        var repoRoot = FindRepoRoot();
        foreach (var path in Directory.EnumerateFiles(Path.Combine(repoRoot, "docs", "qa", "computer-use"), "report.json", SearchOption.AllDirectories))
        {
            using var report = JsonDocument.Parse(File.ReadAllText(path));
            if (report.RootElement.TryGetProperty("decision", out var decision))
            {
                Assert.AreNotEqual("GO_WCU_READ_ONLY_LIVE_PROTOTYPE_GATED_ALLOWED", decision.GetString(), path);
            }

            if (report.RootElement.TryGetProperty("authorization_state", out var authorization) &&
                authorization.TryGetProperty("live_prototype_authorized", out var authorized))
            {
                Assert.IsFalse(authorized.GetBoolean(), path);
            }
        }
    }

    [TestMethod]
    public void PromptsDoNotMarkLivePrototypeAsDirectlyAllowed()
    {
        var repoRoot = FindRepoRoot();
        var promptRoot = Path.Combine(repoRoot, "docs", "prompts", "computer-use");
        foreach (var path in Directory.EnumerateFiles(promptRoot, "*.md", SearchOption.TopDirectoryOnly))
        {
            var text = File.ReadAllText(path);
            if (!text.Contains("WCU-037-044", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            StringAssert.Contains(text, "BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO");
            Assert.IsFalse(text.Contains("Add a live provider only", StringComparison.OrdinalIgnoreCase), path);
            Assert.IsFalse(text.Contains("Expected Outcome\r\n\r\nA gated", StringComparison.OrdinalIgnoreCase), path);
            Assert.IsFalse(text.Contains("Expected Outcome\n\nA gated", StringComparison.OrdinalIgnoreCase), path);
        }
    }

    [TestMethod]
    public void UpdatedReportRecommendsContainmentOnlyNextWork()
    {
        var report = ReadJson(Path.Combine("docs", "qa", "computer-use", "wcu-031-036-read-only-live-design-gate-audit-pack", "report.json"));

        Assert.AreEqual(
            "WCU-CONTAINMENT-PROPERTY-AUDIT-001 — REDACTION/EVIDENCE/NO-LIVE NEGATIVE PROPERTY LOCK",
            report.RootElement.GetProperty("next_recommended_block").GetString());
        StringAssert.Contains(report.RootElement.GetProperty("blocked_block").GetString()!, "BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO");
    }

    private static JsonDocument ReadJson(string relativePath)
    {
        var repoRoot = FindRepoRoot();
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(repoRoot, relativePath)));
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root from test output directory.");
        return AppContext.BaseDirectory;
    }
}
