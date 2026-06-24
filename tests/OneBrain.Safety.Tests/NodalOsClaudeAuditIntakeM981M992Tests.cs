using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

// M981-M992 Claude audit intake for no-op harness prep. Audit-only / intake-only:
// these tests validate the audit package artifacts and gate invariants. They do
// NOT execute, unlock, or imply any runtime, manual QA, release, or store action.
[TestClass]
[TestCategory("ClaudeAuditIntake")]
[TestCategory("M981M992")]
public sealed class NodalOsClaudeAuditIntakeM981M992Tests
{
    private const string ScopePath = "artifacts/agent-operations/m981/claude-audit-scope-definition.json";
    private const string InventoryPath = "artifacts/agent-operations/m982/audit-evidence-inventory.json";
    private const string TraceabilityPath = "artifacts/agent-operations/m983/audit-source-map-traceability-matrix.json";
    private const string PromptPath = "artifacts/agent-operations/m984/claude-deep-audit-prompt.json";
    private const string PromptReportPath = "docs/reports/m984-claude-deep-audit-prompt.md";
    private const string IntakeSchemaPath = "artifacts/agent-operations/m985/audit-findings-intake-schema.json";
    private const string TriagePath = "artifacts/agent-operations/m986/audit-triage-matrix.json";
    private const string RemediationPath = "artifacts/agent-operations/m987/remediation-plan-placeholder.json";
    private const string HoldGatePath = "artifacts/agent-operations/m988/manual-qa-hold-gate.json";
    private const string ManifestPath = "artifacts/agent-operations/m989/claude-audit-package-manifest.json";
    private const string ReadinessPath = "artifacts/agent-operations/m990/audit-readiness-report.json";
    private const string NextBlockPath = "artifacts/agent-operations/m991/next-block-recommendation-after-audit.json";
    private const string ConsolidatedPath = "artifacts/agent-operations/m981-m992/claude-audit-intake-harness-prep-go-no-go.json";
    private const string FinalReportPath = "docs/reports/m992-claude-audit-intake-harness-prep.md";
    private const string BaseCommit = "02ceb0745e531b8e50604a8ef9a0a7b395d9697e";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string rel) => Path.Combine(RepoRoot(), rel);
    private static string ReadText(string rel) => File.ReadAllText(FullPath(rel));
    private static JsonDocument ReadJson(string rel) => JsonDocument.Parse(ReadText(rel));
    private static string Str(JsonDocument d, string p) => d.RootElement.GetProperty(p).GetString() ?? "";
    private static IEnumerable<string> Arr(JsonDocument d, string p) =>
        d.RootElement.GetProperty(p).EnumerateArray().Select(x => x.GetString() ?? "");

    [TestMethod]
    public void AllAuditIntakeArtifactsExist()
    {
        foreach (var path in new[]
        {
            ScopePath, InventoryPath, TraceabilityPath, PromptPath, PromptReportPath, IntakeSchemaPath,
            TriagePath, RemediationPath, HoldGatePath, ManifestPath, ReadinessPath, NextBlockPath,
            ConsolidatedPath, FinalReportPath
        })
            Assert.IsTrue(File.Exists(FullPath(path)), path);
    }

    // 1. Audit scope does not include runtime real as implemented.
    [TestMethod]
    public void AuditScopeDoesNotClaimRuntimeReal()
    {
        using var d = ReadJson(ScopePath);
        var notReal = Arr(d, "notAuditedAsImplementedReal").ToArray();
        CollectionAssert.Contains(notReal, "productive runtime");
        CollectionAssert.Contains(notReal, "PC Commander real");
        CollectionAssert.Contains(notReal, "manual QA execution");
        Assert.AreEqual("NO-GO", Str(d, "productiveRuntime"));
        Assert.AreEqual("NO-GO", Str(d, "manualQaExecution"));
    }

    // 2. Evidence inventory includes M933-M980.
    [TestMethod]
    public void EvidenceInventoryCoversM933ToM980()
    {
        using var d = ReadJson(InventoryPath);
        Assert.IsTrue(d.RootElement.GetProperty("coversM933ToM980").GetBoolean());
        var ranges = d.RootElement.GetProperty("items").EnumerateArray()
            .Select(x => x.GetProperty("milestoneRange").GetString()).ToArray();
        foreach (var r in new[] { "M933-M944", "M945-M956", "M957-M968", "M969-M980" })
            CollectionAssert.Contains(ranges, r);
    }

    // 3. Traceability matrix covers required claims.
    [TestMethod]
    public void TraceabilityCoversRequiredClaims()
    {
        using var d = ReadJson(TraceabilityPath);
        var claims = d.RootElement.GetProperty("rows").EnumerateArray()
            .Select(x => x.GetProperty("claim").GetString() ?? "").ToArray();
        foreach (var needle in new[] { "no side effects", "Metadata is fixture-only", "Manual QA NOT READY", "Harness prep", "Product files unchanged", "Bridge/CSP unchanged", "BrowserRuntimeSmoke caveat visible" })
            Assert.IsTrue(claims.Any(c => c.Contains(needle, StringComparison.OrdinalIgnoreCase)), needle);
    }

    // 4. Claude prompt demands the three decisions.
    [TestMethod]
    public void PromptDemandsThreeDecisions()
    {
        using var d = ReadJson(PromptPath);
        var decisions = Arr(d, "requiredDecision").ToArray();
        CollectionAssert.AreEquivalent(new[] { "AUDIT_GO", "AUDIT_CONDITIONAL_GO", "AUDIT_NO_GO" }, decisions);
        var report = ReadText(PromptReportPath);
        foreach (var token in new[] { "AUDIT_GO", "AUDIT_CONDITIONAL_GO", "AUDIT_NO_GO", "BLOCKER", "PEDIR AUDITORIA CLAUDE" })
            StringAssert.Contains(report, token);
    }

    // 5. Findings intake schema rejects "manual QA ready" without evidence.
    [TestMethod]
    public void IntakeSchemaRejectsManualQaReadyWithoutEvidence()
    {
        using var d = ReadJson(IntakeSchemaPath);
        var rejects = Arr(d, "rejectInvalidIf").ToArray();
        Assert.IsTrue(rejects.Any(r => r.Contains("manual QA ready without evidence", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(rejects.Any(r => r.Contains("declares runtime ready", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(rejects.Any(r => r.Contains("BrowserRuntimeSmoke caveat", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(d.RootElement.GetProperty("noFindingsFabricated").GetBoolean() == false);
    }

    // 6. Triage classifies accidental real unlock as BLOCKER.
    [TestMethod]
    public void TriageClassifiesUnlockAsBlocker()
    {
        using var d = ReadJson(TriagePath);
        var blockers = d.RootElement.GetProperty("severityClasses").GetProperty("BLOCKER")
            .EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
        Assert.IsTrue(blockers.Any(b => b.Contains("accidental real unlock", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(blockers.Any(b => b.Contains("manual QA ready without evidence", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(d.RootElement.GetProperty("accidentalRealUnlockIsBlocker").GetBoolean());
    }

    // 7. Remediation placeholder does not fabricate findings.
    [TestMethod]
    public void RemediationPlaceholderHasNoFabricatedFindings()
    {
        using var d = ReadJson(RemediationPath);
        Assert.AreEqual("PENDING_AUDIT", Str(d, "status"));
        Assert.IsFalse(d.RootElement.GetProperty("fabricatedFindings").GetBoolean());
        Assert.IsFalse(d.RootElement.GetProperty("anyDeclaredRemediated").GetBoolean());
        Assert.AreEqual(0, d.RootElement.GetProperty("findings").GetArrayLength());
    }

    // 8. Manual QA hold gate stays MANUAL_QA_HOLD_ACTIVE.
    [TestMethod]
    public void ManualQaHoldGateActive()
    {
        using var d = ReadJson(HoldGatePath);
        Assert.AreEqual("MANUAL_QA_HOLD_ACTIVE", Str(d, "gateState"));
        Assert.AreEqual("NO-GO", Str(d, "manualQaExecution"));
        Assert.AreEqual("NOT_READY_EVIDENCE_PENDING", Str(d, "manualQaTrigger"));
        Assert.IsFalse(d.RootElement.GetProperty("anyDisallowedStateSet").GetBoolean());
        var disallowed = Arr(d, "disallowedStates").ToArray();
        foreach (var s in new[] { "MANUAL_QA_READY", "MANUAL_QA_PASSED", "RUNTIME_READY", "PC_COMMANDER_READY" })
            CollectionAssert.Contains(disallowed, s);
    }

    // 9. Audit package manifest includes base commit and caveat.
    [TestMethod]
    public void ManifestIncludesBaseCommitAndCaveat()
    {
        using var d = ReadJson(ManifestPath);
        Assert.AreEqual(BaseCommit, Str(d, "baseCommit"));
        Assert.IsTrue(d.RootElement.GetProperty("containsBaseCommit").GetBoolean());
        Assert.IsTrue(d.RootElement.GetProperty("containsCaveat").GetBoolean());
        Assert.IsTrue(Arr(d, "knownCaveats").Any(c => c.Contains("BROWSER_RUNTIME_SMOKE", StringComparison.OrdinalIgnoreCase)));
    }

    // 10. Audit readiness report contains PEDIR AUDITORIA CLAUDE.
    [TestMethod]
    public void ReadinessReportRequestsAudit()
    {
        using var d = ReadJson(ReadinessPath);
        Assert.AreEqual("PEDIR AUDITORIA CLAUDE", Str(d, "instruction"));
        StringAssert.Contains(ReadText(FinalReportPath), "PEDIR AUDITORIA CLAUDE");
    }

    // 11. Next block recommendation depends on audit result.
    [TestMethod]
    public void NextBlockDependsOnAuditResult()
    {
        using var d = ReadJson(NextBlockPath);
        Assert.IsTrue(d.RootElement.GetProperty("dependsOnAuditResult").GetBoolean());
        var byResult = d.RootElement.GetProperty("recommendationByResult");
        Assert.IsTrue(byResult.TryGetProperty("AUDIT_GO", out _));
        Assert.IsTrue(byResult.TryGetProperty("AUDIT_CONDITIONAL_GO", out _));
        Assert.IsTrue(byResult.TryGetProperty("AUDIT_NO_GO", out _));
    }

    // 12-18. Consolidated invariants: product/Bridge unchanged, release/store/PC/runtime/provider/fs NO-GO.
    [TestMethod]
    public void ConsolidatedKeepsAllRestrictedSurfacesNoGo()
    {
        using var d = ReadJson(ConsolidatedPath);
        Assert.AreEqual("CLAUDE_AUDIT_INTAKE_HARNESS_PREP_READY_WITH_EXTERNAL_SMOKE_CAVEAT", Str(d, "decision"));
        Assert.IsFalse(d.RootElement.GetProperty("productFilesModified").GetBoolean());   // 12
        Assert.IsFalse(d.RootElement.GetProperty("bridgeCspModified").GetBoolean());       // 13
        StringAssert.Contains(Str(d, "publicRelease"), "NO-GO");                           // 14
        StringAssert.Contains(Str(d, "chromeWebStore"), "NO-GO");
        Assert.AreEqual("NO-GO", Str(d, "pcCommanderReal"));                               // 15
        StringAssert.Contains(Str(d, "productiveRuntimeUnlock"), "0%");                    // 16
        StringAssert.Contains(Str(d, "providerCloud"), "0%");                              // 17
        StringAssert.Contains(Str(d, "filesystemBrowserCapabilityUnlock"), "0%");         // 18
        Assert.AreEqual("MANUAL_QA_HOLD_ACTIVE", Str(d, "manualQaHold"));
    }

    // 19. BrowserRuntimeSmoke caveat still visible.
    [TestMethod]
    public void ExternalSmokeCaveatVisible()
    {
        using var d = ReadJson(ConsolidatedPath);
        StringAssert.Contains(Str(d, "externalSmokeCaveat"), "BROWSER_RUNTIME_SMOKE");
        StringAssert.Contains(Str(d, "fullSuiteConfidence"), "95%");
    }

    // 20. No secrets/leaks in the audit intake artifacts (no real sensitive values).
    [TestMethod]
    public void NoSecretsOrLeaksInIntakeArtifacts()
    {
        foreach (var path in new[] { ScopePath, InventoryPath, TraceabilityPath, PromptPath, IntakeSchemaPath, TriagePath, RemediationPath, HoldGatePath, ManifestPath, ReadinessPath, NextBlockPath, ConsolidatedPath })
        {
            var content = ReadText(path);
            foreach (var leak in new[] { "BEGIN RSA", "BEGIN PRIVATE KEY", "xoxb-", "sk-ant-", "AKIA", "-----BEGIN" })
                Assert.IsFalse(content.Contains(leak, StringComparison.Ordinal), $"{path} contains {leak}");
        }
    }

    // NODRIX remains out of scope (guardrail reference only).
    [TestMethod]
    public void NodrixRemainsOutOfScope()
    {
        using var d = ReadJson(ScopePath);
        StringAssert.Contains(Str(d, "nodrixScope"), "FROZEN");
    }
}
