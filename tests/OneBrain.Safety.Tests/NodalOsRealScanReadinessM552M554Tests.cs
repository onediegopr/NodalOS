using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("RealScanReadiness")]
[TestCategory("SyntheticDryRunUiResults")]
[TestCategory("FixtureCoverageReport")]
[TestCategory("ProjectUnderstandingImplementationBoundary")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsRealScanReadinessM552M554Tests
{
    private static readonly string[] ForbiddenNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SensitiveMarkers =
    [
        "Bear" + "er ",
        "Authorization:",
        "Cook" + "ie:",
        "password",
        "raw " + "secret",
        "api" + "_key",
        "access" + "_token",
        "refresh" + "_token",
        "private key",
        "s" + "k-",
        "connection string"
    ];

    private readonly NodalOsSyntheticDryRunUiResultsJsonSerializer uiSerializer = new();
    private readonly NodalOsFixtureCoverageReportJsonSerializer coverageSerializer = new();
    private readonly NodalOsRealScanReadinessAdrJsonSerializer adrSerializer = new();

    [TestMethod]
    public void SyntheticDryRunUiResults_AreStaticReadOnlyNoOpAndSyntheticOnly()
    {
        var preview = UiPreview();

        Assert.IsTrue(preview.IsStaticPreview);
        Assert.IsTrue(preview.IsReadOnly);
        Assert.IsTrue(preview.IsNoOp);
        Assert.IsTrue(preview.UsesSyntheticFixturesOnly);
        Assert.IsFalse(preview.UsesRealFilesystem);
        Assert.IsFalse(preview.PerformsRealScan);
        Assert.IsFalse(preview.PerformsDirectoryListing);
        Assert.IsFalse(preview.PerformsFileRead);
        Assert.IsFalse(preview.PerformsFileHash);
        Assert.IsFalse(preview.PerformsIndexing);
        Assert.IsFalse(preview.PerformsRepresentationBuild);
        Assert.IsFalse(preview.BuildsLlmContext);
        Assert.IsFalse(preview.CallsProvider);
        Assert.IsFalse(preview.UsesCloud);
        AssertSafeOutput(uiSerializer.SerializePreview(preview));
    }

    [TestMethod]
    public void SyntheticDryRunUiResults_IncludeCountsAndDisclosures()
    {
        var preview = UiPreview();

        Assert.IsTrue(preview.Sections.IncludedPreviewCount > 0);
        Assert.IsTrue(preview.Sections.ExcludedPreviewCount > 0);
        Assert.IsTrue(preview.Sections.BlockedPreviewCount > 0);
        Assert.IsTrue(preview.Sections.RequiresReviewCount > 0);
        Assert.IsTrue(preview.Sections.RedactedPreviewCount > 0);
        Assert.IsTrue(preview.Sections.AuditRequiredCount > 0);
        Assert.IsTrue(preview.DisclosuresRedacted.Count >= 7);
        Assert.IsTrue(preview.DisclosuresRedacted.Any(d => d.Contains("synthetic", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(preview.DisclosuresRedacted.Any(d => d.Contains("workspace filesystem", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(preview.DisclosuresRedacted.Any(d => d.Contains("LLM context", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void FixtureCoverageReport_IsSyntheticOnlyAndNonAuthorizing()
    {
        var report = CoverageReport();

        Assert.IsTrue(report.IsSyntheticCoverageOnly);
        Assert.IsFalse(report.UsesRealFilesystem);
        Assert.IsFalse(report.CanAuthorizeRealScan);
        Assert.AreEqual(Enum.GetValues<NodalOsScanFixtureCategory>().Length, report.TotalFixtureCategories);
        Assert.AreEqual(report.TotalFixtureCategories, report.CoveredFixtureCategories);
        Assert.AreEqual(100m, report.CoveragePercent);
        Assert.AreEqual(NodalOsFixtureCoverageStatus.CompleteSyntheticCoverage, report.CoverageStatus);
        AssertSafeOutput(coverageSerializer.SerializeReport(report));
    }

    [TestMethod]
    public void FixtureCoverageReport_IncludesRequiredDimensions()
    {
        var report = CoverageReport();
        var dimensions = report.CoverageDimensions.Select(d => d.Kind).ToHashSet();

        foreach (var kind in Enum.GetValues<NodalOsFixtureCoverageDimensionKind>())
            Assert.IsTrue(dimensions.Contains(kind), $"Missing coverage dimension: {kind}");

        Assert.IsTrue(report.CoverageDimensions.All(d => d.IsCovered));
    }

    [TestMethod]
    public void FixtureCoverageDecision_ClosesSyntheticCoverageOnly()
    {
        var decision = CoverageReport().CoverageDecision;

        Assert.IsTrue(decision.ReadyForSyntheticCoverageCloseout);
        Assert.IsFalse(decision.ReadyForRealScan);
        Assert.IsFalse(decision.ReadyForFilesystemAccess);
        Assert.IsFalse(decision.ReadyForRealPathJail);
        Assert.IsFalse(decision.ReadyForRealSecretDetection);
        Assert.IsFalse(decision.ReadyForRealExclusionEnforcement);
        Assert.IsFalse(decision.ReadyForIndexing);
        Assert.IsFalse(decision.ReadyForRepresentationBuild);
        Assert.IsFalse(decision.ReadyForLlmContext);
    }

    [TestMethod]
    public void RealScanReadinessAdrSummary_DeclaresNotReadyAndSyntheticBaselineReady()
    {
        var summary = AdrSummary();

        Assert.AreEqual(NodalOsRealScanReadinessDecisionStatus.RealScanNotReadySyntheticBaselineReady, summary.DecisionStatus);
        Assert.IsFalse(summary.RealScanReady);
        Assert.IsTrue(summary.SyntheticBaselineReady);
        Assert.IsTrue(summary.FuturePathJailPrototypeMayProceedIfDisabledByDefault);
        Assert.IsTrue(summary.RealFilesystemAccessBlocked);
        Assert.IsTrue(summary.DirectoryEnumerationBlocked);
        Assert.IsTrue(summary.ContentAccessBlocked);
        Assert.IsTrue(summary.ContentFingerprintingBlocked);
        Assert.IsTrue(summary.IndexingBlocked);
        Assert.IsTrue(summary.RepresentationBuildBlocked);
        Assert.IsTrue(summary.LlmContextBlocked);
        Assert.IsTrue(summary.CloudBlocked);
        Assert.IsTrue(summary.ProviderBlocked);
        Assert.IsTrue(summary.RuntimeBlocked);
        Assert.IsTrue(summary.FixtureCoverageNecessaryNotSufficient);
        AssertSafeOutput(adrSerializer.SerializeSummary(summary));
    }

    [TestMethod]
    public void RealScanReadinessAdrDocument_StatesRequiredDecisions()
    {
        var adr = TextStore.ReadAllText(PathFor("docs", "architecture", "real-scan-readiness-after-synthetic-dry-run-adr.md"));

        AssertContains(adr, "REAL_SCAN_NOT_READY_SYNTHETIC_BASELINE_READY");
        AssertContains(adr, "NODAL OS is not ready for operational scan behavior.");
        AssertContains(adr, "Synthetic baseline is ready as a governance and prototype baseline.");
        AssertContains(adr, "Operational folder enumeration, content access, content fingerprinting, indexing, representation build, and LLM context remain blocked.");
        AssertContains(adr, "Future operational scan behavior must remain read-only, local-only, consent-gated, path-jail-gated, redaction-first, evidence-first, and audit-logged.");
        AssertContains(adr, "Synthetic fixture coverage is necessary but not sufficient.");
        AssertSafeOutput(adr);
    }

    [TestMethod]
    public void StaticPreviewArtifact_HasNoExternalScriptsOrNetworkResources()
    {
        var html = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m554", "real-scan-readiness-preview.html"));

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_DeclareSyntheticCloseoutAndRealReadinessBlocked()
    {
        var ui = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m554", "synthetic-dry-run-ui-results.json"));
        var coverage = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m554", "fixture-coverage-report.json"));
        var adr = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m554", "real-scan-readiness-adr-summary.json"));

        AssertContains(ui, "\"isStaticPreview\": true");
        AssertContains(ui, "\"usesSyntheticFixturesOnly\": true");
        AssertContains(coverage, "\"readyForSyntheticCoverageCloseout\": true");
        AssertContains(coverage, "\"readyForRealScan\": false");
        AssertContains(adr, "\"decision\": \"REAL_SCAN_READINESS_ADR_READY\"");
        AssertContains(adr, "\"decisionStatus\": \"RealScanNotReadySyntheticBaselineReady\"");
        AssertSafeOutput(ui + coverage + adr);
    }

    [TestMethod]
    public void Boundary_NewReadinessFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
    {
        var source = NewSource();

        AssertDoesNotContain(source, "OneBrain." + "BrowserExecutor" + ".Cdp");
        AssertDoesNotContain(source, "Http" + "Client");
        AssertDoesNotContain(source, "Client" + "WebSocket");
        AssertDoesNotContain(source, "Process" + ".Start");
        AssertDoesNotContain(source, "System.Diagnostics." + "Process");
        AssertDoesNotContain(source, "Background" + "Service");
        AssertDoesNotContain(source, "Task.Run");
        AssertDoesNotContain(source, "File" + ".Read");
        AssertDoesNotContain(source, "File" + ".Write");
        AssertDoesNotContain(source, "Directory" + ".");
        AssertDoesNotContain(source, "File" + "Info");
        AssertDoesNotContain(source, "Directory" + "Info");
    }

    private static NodalOsSyntheticDryRunUiResultsPreview UiPreview() =>
        NodalOsSyntheticDryRunUiResultsFixtures.Preview();

    private static NodalOsFixtureCoverageReport CoverageReport() =>
        NodalOsFixtureCoverageReportFixtures.Report();

    private static NodalOsRealScanReadinessAdrSummary AdrSummary() =>
        new NodalOsRealScanReadinessAdrService().CreateSummary();

    private static string NewSource()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsSyntheticDryRunUiResultsContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsFixtureCoverageReportContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsRealScanReadinessAdrContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsSyntheticDryRunUiResultsServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsFixtureCoverageReportServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsRealScanReadinessAdrServices.cs")
        };

        return string.Join(Environment.NewLine, files.Select(TextStore.ReadAllText));
    }

    private static void AssertSafeOutput(string value)
    {
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(value, name);

        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(value, marker);
    }

    private static void AssertContains(string value, string expected) =>
        StringAssert.Contains(value, expected);

    private static void AssertDoesNotContain(string value, string forbidden) =>
        Assert.IsFalse(value.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Unexpected content: {forbidden}");

    private static string PathFor(params string[] segments) =>
        Path.Combine([FindRepoRoot(), .. segments]);

    private static string FindRepoRoot()
    {
        var current = Path.GetFullPath(AppContext.BaseDirectory);
        while (!string.IsNullOrEmpty(current) && !TextStore.Exists(Path.Combine(current, "OneBrain.slnx")))
            current = Path.GetDirectoryName(current) ?? string.Empty;

        return string.IsNullOrEmpty(current) ? throw new InvalidOperationException("Repository root not found.") : current;
    }
}

