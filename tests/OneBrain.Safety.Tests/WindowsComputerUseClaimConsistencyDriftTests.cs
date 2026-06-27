using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.WindowsComputerUse;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("WindowsComputerUseFixtureSafe")]
[TestCategory("WindowsComputerUseClaimConsistencyDrift")]
public sealed class WindowsComputerUseClaimConsistencyDriftTests
{
    private static readonly string[] CurrentContainmentReportDirs =
    [
        "wcu-containment-property-audit-001-redaction-evidence-no-live",
        "wcu-containment-property-audit-002-bridge-handoff-redaction",
        "wcu-containment-property-audit-003-report-json-claim-drift"
    ];

    [TestMethod]
    public void CanonicalClaimCatalogLocksRequiredClaims()
    {
        var claims = ComputerUseClaimConsistencyCatalog.Claims;
        var ids = claims.Select(c => c.ClaimId).ToArray();

        CollectionAssert.Contains(ids, "contained_artifact");
        CollectionAssert.Contains(ids, "live_prototype_authorized");
        CollectionAssert.Contains(ids, "live_remains_blocked");
        CollectionAssert.Contains(ids, "current_code_defect_found");
        CollectionAssert.Contains(ids, "wcu_037_044_status");
        CollectionAssert.Contains(ids, "live_read_permitted");
        CollectionAssert.Contains(ids, "action_authority_granted");
        CollectionAssert.Contains(ids, "product_automation_enabled");
        CollectionAssert.Contains(ids, "browser_live_cdp_enabled");
        CollectionAssert.Contains(ids, "safe_injection_live_enabled");
        CollectionAssert.Contains(ids, "public_release_unlock");
        CollectionAssert.Contains(ids, "paid_beta_unlock");
        Assert.IsTrue(claims.All(c =>
            !string.IsNullOrWhiteSpace(c.SourceOfTruth) &&
            c.ArtifactsThatMustMatch.Count > 0 &&
            !string.IsNullOrWhiteSpace(c.RegressionTest) &&
            !string.IsNullOrWhiteSpace(c.FailureBehavior)));
        StringAssert.Contains(ComputerUseClaimConsistencyCatalog.BlockedLivePrototypeStatus, "BLOCKED_PENDING");
        StringAssert.Contains(ComputerUseClaimConsistencyCatalog.BlockedLivePrototypeStatus, "EXTERNAL_GO");
    }

    [TestMethod]
    public void CurrentReportJsonFilesKeepCanonicalBlockedClaims()
    {
        foreach (var dir in CurrentContainmentReportDirs)
        {
            using var document = ReadReport(dir);
            var root = document.RootElement;

            AssertJsonBool(root, "contained_artifact", true, dir);
            AssertJsonBool(root, "live_prototype_authorized", false, dir);
            AssertJsonBool(root, "live_remains_blocked", true, dir);
            AssertJsonBool(root, "current_code_defect_found", false, dir);
            AssertJsonString(root, "wcu_037_044_status", ComputerUseClaimConsistencyCatalog.BlockedLivePrototypeStatus, dir);

            var authority = root.GetProperty("authority");
            AssertJsonBool(authority, "LiveReadPermitted", false, dir);
            AssertJsonBool(authority, "ActionAuthorityGranted", false, dir);
            AssertJsonBool(authority, "ProductAutomationEnabled", false, dir);

            AssertNoForbiddenLiveGo(root.GetRawText(), dir);
        }
    }

    [TestMethod]
    public void LatestReportJsonCarriesClaimConsistencyLocks()
    {
        using var document = ReadReport("wcu-containment-property-audit-003-report-json-claim-drift");
        var root = document.RootElement;
        var claims = root.GetProperty("canonical_claims");

        AssertJsonString(root, "report_json_claim_consistency", "LOCKED", "report003");
        AssertJsonString(root, "handoff_prompt_wording_drift", "LOCKED", "report003");
        AssertJsonString(root, "cross_artifact_consistency", "PASS", "report003");
        AssertJsonBool(root, "wcu_031_036_reopened", false, "report003");
        AssertJsonBool(root, "sidepanel_hash_debt_touched", false, "report003");
        AssertJsonBool(claims, "browser_live_cdp_enabled", false, "report003");
        AssertJsonBool(claims, "safe_injection_live_enabled", false, "report003");
        AssertJsonBool(claims, "public_release_unlock", false, "report003");
        AssertJsonBool(claims, "paid_beta_unlock", false, "report003");
    }

    [TestMethod]
    public void ExternalAuditReportRemainsNoGoAndDoesNotBecomeLiveGo()
    {
        using var document = ReadReport("wcu-037a-external-audit-nogo-reconciliation");
        var text = document.RootElement.GetRawText();

        StringAssert.Contains(text, "AUDIT_CONTAINMENT_PASS_BUT_LIVE_ADVANCE_NO_GO");
        StringAssert.Contains(text, "BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO");
        AssertNoForbiddenLiveGo(text, "wcu-037a");
    }

    [TestMethod]
    public void HandoffsPromptsAndMarkdownDoNotRecommendLiveAsExecutableNextStep()
    {
        var repoRoot = FindRepoRoot();
        var files = Directory.EnumerateFiles(Path.Combine(repoRoot, "docs", "handoff"), "nodal-os-wcu-*.md")
            .Concat(Directory.EnumerateFiles(Path.Combine(repoRoot, "docs", "prompts", "computer-use"), "next-wcu-*.md"))
            .Concat(Directory.EnumerateFiles(Path.Combine(repoRoot, "docs", "qa", "computer-use"), "report.md", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(Path.Combine(repoRoot, "docs", "architecture", "computer-use"), "windows-computer-use*.md"))
            .ToArray();

        foreach (var path in files)
        {
            var text = File.ReadAllText(path);
            if (!IsCanonicalForbiddenWordingMatrix(path))
                AssertNoForbiddenLiveGo(text, path);

            if (text.Contains("WCU-037-044", StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(
                    text.Contains("BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("blocked", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("NO-GO", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("no go", StringComparison.OrdinalIgnoreCase),
                    path);
            }
        }
    }

    [TestMethod]
    public void CrossArtifactConsistencyReportMatchesCanonicalCatalog()
    {
        var repoRoot = FindRepoRoot();
        var consistencyPath = Path.Combine(repoRoot, "docs", "qa", "computer-use", "wcu-containment-property-audit-003-report-json-claim-drift", "cross-artifact-consistency.json");
        using var document = JsonDocument.Parse(File.ReadAllText(consistencyPath));
        var root = document.RootElement;
        var claims = root.GetProperty("canonical_claims");

        AssertJsonBool(root, "consistent", true, "cross-artifact");
        AssertJsonBool(root, "drift_found", false, "cross-artifact");
        AssertJsonBool(claims, "contained_artifact", true, "cross-artifact");
        AssertJsonBool(claims, "live_prototype_authorized", false, "cross-artifact");
        AssertJsonBool(claims, "live_remains_blocked", true, "cross-artifact");
        AssertJsonString(claims, "wcu_037_044_status", ComputerUseClaimConsistencyCatalog.BlockedLivePrototypeStatus, "cross-artifact");
        AssertJsonBool(claims, "live_read_permitted", false, "cross-artifact");
        AssertJsonBool(claims, "action_authority_granted", false, "cross-artifact");
        AssertJsonBool(claims, "product_automation_enabled", false, "cross-artifact");
        AssertJsonBool(claims, "browser_live_cdp_enabled", false, "cross-artifact");
        AssertJsonBool(claims, "safe_injection_live_enabled", false, "cross-artifact");
        AssertJsonBool(claims, "public_release_unlock", false, "cross-artifact");
        AssertJsonBool(claims, "paid_beta_unlock", false, "cross-artifact");
    }

    [TestMethod]
    public void ClaimConsistencyEvaluatorFailsClosedOnDriftAndPassesCurrentSnapshots()
    {
        var snapshots = new[]
        {
            Snapshot("latest-report", new Dictionary<string, string>
            {
                ["contained_artifact"] = "true",
                ["live_prototype_authorized"] = "false",
                ["live_remains_blocked"] = "true",
                ["current_code_defect_found"] = "false",
                ["wcu_037_044_status"] = ComputerUseClaimConsistencyCatalog.BlockedLivePrototypeStatus,
                ["live_read_permitted"] = "false",
                ["action_authority_granted"] = "false",
                ["product_automation_enabled"] = "false",
                ["browser_live_cdp_enabled"] = "false",
                ["safe_injection_live_enabled"] = "false",
                ["public_release_unlock"] = "false",
                ["paid_beta_unlock"] = "false"
            })
        };
        var clean = ComputerUseClaimConsistencyCatalog.Evaluate(snapshots);
        Assert.IsTrue(clean.Consistent);
        Assert.AreEqual(0, clean.Findings.Count);

        var drift = ComputerUseClaimConsistencyCatalog.Evaluate([
            Snapshot("drift-report", new Dictionary<string, string>
            {
                ["contained_artifact"] = "true",
                ["live_prototype_authorized"] = "true",
                ["live_remains_blocked"] = "false",
                ["current_code_defect_found"] = "false",
                ["wcu_037_044_status"] = "UNBLOCKED",
                ["live_read_permitted"] = "true",
                ["action_authority_granted"] = "true",
                ["product_automation_enabled"] = "true",
                ["browser_live_cdp_enabled"] = "true",
                ["safe_injection_live_enabled"] = "true",
                ["public_release_unlock"] = "true",
                ["paid_beta_unlock"] = "true"
            }, "\"live_prototype_authorized\": true safe to start live implementation product automation ready")
        ]);

        Assert.IsFalse(drift.Consistent);
        Assert.IsTrue(drift.Findings.Any(f => f.Severity == ComputerUseClaimDriftSeverity.Critical));
        Assert.IsTrue(drift.Findings.All(f => f.BlocksLiveAdvance));
    }

    [TestMethod]
    public void NextPromptRemainsContainmentOnlyAndDoesNotPointToLivePrototype()
    {
        var repoRoot = FindRepoRoot();
        var prompt = File.ReadAllText(Path.Combine(repoRoot, "docs", "prompts", "computer-use", "next-wcu-containment-property-audit-004-prompt.md"));

        StringAssert.Contains(prompt, "containment-only");
        StringAssert.Contains(prompt, "STATIC SCAN HARNESS + PROTECTED BOUNDARY CONSOLIDATION");
        StringAssert.Contains(prompt, ComputerUseClaimConsistencyCatalog.BlockedLivePrototypeStatus);
        Assert.IsFalse(prompt.Contains("READ-ONLY LIVE PROTOTYPE GATED", StringComparison.OrdinalIgnoreCase));
        AssertNoForbiddenLiveGo(prompt, "next-wcu-containment-property-audit-004-prompt.md");
    }

    private static ComputerUseArtifactClaimSnapshot Snapshot(
        string id,
        IReadOnlyDictionary<string, string> claims,
        string text = "") =>
        new(id, "fixture", claims, text, HistoricalOnly: false);

    private static JsonDocument ReadReport(string dir)
    {
        var path = Path.Combine(FindRepoRoot(), "docs", "qa", "computer-use", dir, "report.json");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static void AssertJsonBool(JsonElement element, string propertyName, bool expected, string context)
    {
        Assert.IsTrue(element.TryGetProperty(propertyName, out var value), $"{context}:{propertyName}");
        Assert.AreEqual(expected, value.GetBoolean(), $"{context}:{propertyName}");
    }

    private static void AssertJsonString(JsonElement element, string propertyName, string expected, string context)
    {
        Assert.IsTrue(element.TryGetProperty(propertyName, out var value), $"{context}:{propertyName}");
        Assert.AreEqual(expected, value.GetString(), $"{context}:{propertyName}");
    }

    private static void AssertNoForbiddenLiveGo(string value, string context)
    {
        Assert.IsFalse(value.Contains("GO_WCU_READ_ONLY_LIVE_PROTOTYPE_GATED_ALLOWED", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("\"live_prototype_authorized\": true", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("live prototype authorized: YES", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("containment PASS = live GO", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("safe to start live implementation", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("\"LiveReadPermitted\": true", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("\"ActionAuthorityGranted\": true", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("\"ProductAutomationEnabled\": true", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("high confidence = authorization", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("evidence = authorization", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("product automation ready", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("desktop automation enabled", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("browser live enabled", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("CDP live enabled", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("Safe Injection live enabled", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("public release unlocked", StringComparison.OrdinalIgnoreCase), context);
        Assert.IsFalse(value.Contains("paid beta unlocked", StringComparison.OrdinalIgnoreCase), context);
    }

    private static bool IsCanonicalForbiddenWordingMatrix(string path) =>
        Path.GetFileName(path).Equals("windows-computer-use-report-json-claim-consistency-matrix-v1.md", StringComparison.OrdinalIgnoreCase);

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return current.FullName;

            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root from test output directory.");
        return AppContext.BaseDirectory;
    }
}
