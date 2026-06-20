using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SyntheticDryRunSimulator")]
[TestCategory("FixtureResultReview")]
[TestCategory("ScanBoundaryAudit")]
[TestCategory("ProjectUnderstandingImplementationBoundary")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsSyntheticDryRunSimulatorM549M551Tests
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

    private readonly NodalOsSyntheticDryRunSimulatorService simulatorService = new();
    private readonly NodalOsSyntheticDryRunSimulatorJsonSerializer simulatorSerializer = new();
    private readonly NodalOsFixtureResultReviewService reviewService = new();
    private readonly NodalOsFixtureResultReviewJsonSerializer reviewSerializer = new();
    private readonly NodalOsScanBoundaryAuditService auditService = new();
    private readonly NodalOsScanBoundaryAuditJsonSerializer auditSerializer = new();

    [TestMethod]
    public void SimulatorContract_IsSyntheticOnlyAndNonOperational()
    {
        var contract = SimulatorContract();

        Assert.IsTrue(contract.UsesSyntheticFixturesOnly);
        Assert.IsFalse(contract.UsesRealFilesystem);
        Assert.IsFalse(contract.PerformsRealScan);
        Assert.IsFalse(contract.PerformsDirectoryListing);
        Assert.IsFalse(contract.PerformsFileRead);
        Assert.IsFalse(contract.PerformsFileHash);
        Assert.IsFalse(contract.PerformsSecretDetectionOnRealData);
        Assert.IsFalse(contract.AppliesExclusionsToRealFilesystem);
        Assert.IsFalse(contract.PerformsIndexing);
        Assert.IsFalse(contract.PerformsVectorization);
        Assert.IsFalse(contract.BuildsLlmContext);
        Assert.IsFalse(contract.CallsProvider);
        Assert.IsFalse(contract.UsesCloud);
        Assert.IsTrue(contract.IsSimulationOnly);
        AssertSafeOutput(simulatorSerializer.SerializeContract(contract));
    }

    [TestMethod]
    public void SimulatorInputs_UseSyntheticMetadataOnly()
    {
        foreach (var input in Inputs())
        {
            Assert.IsFalse(input.ContainsRawFileContent);
            Assert.IsFalse(input.ContainsRawSecret);
            Assert.IsTrue(input.DeclaredSyntheticPathRef.StartsWith("synthetic-fixture-ref-", StringComparison.Ordinal));
        }

        AssertSafeOutput(simulatorSerializer.SerializeInputs(Inputs()));
    }

    [TestMethod]
    public void SimulatorResult_CountsDeclaredFixtureOutcomes()
    {
        var result = SimulationResult();

        Assert.IsTrue(result.IncludedPreviewCount > 0);
        Assert.IsTrue(result.ExcludedPreviewCount > 0);
        Assert.IsTrue(result.BlockedPreviewCount > 0);
        Assert.IsTrue(result.RequiresReviewCount > 0);
        Assert.IsTrue(result.RedactedPreviewCount > 0);
        Assert.IsTrue(result.AuditRequiredCount > 0);
        Assert.AreEqual(Matrix().Fixtures.Count, result.PolicyDecisions.Count);
        AssertSafeOutput(simulatorSerializer.SerializeResult(result));
    }

    [TestMethod]
    public void SimulatorReadiness_AllowsSyntheticSimulationOnly()
    {
        var readiness = simulatorService.Evaluate(SimulatorContract());

        Assert.IsTrue(readiness.ReadyForSyntheticSimulation);
        Assert.IsFalse(readiness.ReadyForRealDryRun);
        Assert.IsFalse(readiness.ReadyForRealScan);
        Assert.IsFalse(readiness.ReadyForRealFilesystemAccess);
        Assert.IsFalse(readiness.ReadyForRealSecretDetection);
        Assert.IsFalse(readiness.ReadyForIndexing);
        Assert.IsFalse(readiness.ReadyForVectorization);
        Assert.IsFalse(readiness.ReadyForLlmContext);
        AssertSafeOutput(simulatorSerializer.SerializeReadiness(readiness));
    }

    [TestMethod]
    public void FixtureResultReview_IsReviewOnlyNoOpAndNonAuthorizing()
    {
        var review = Review();

        Assert.IsTrue(review.IsReviewOnly);
        Assert.IsTrue(review.IsNoOp);
        Assert.IsFalse(review.CanAuthorizeRealScan);
        Assert.IsFalse(review.CanAuthorizeFilesystemAccess);
        Assert.IsFalse(review.CanAuthorizeIndexing);
        Assert.IsFalse(review.CanAuthorizeLlmContext);
        Assert.AreEqual(Matrix().Fixtures.Count, review.ReviewedFixtureResults.Count);
        AssertSafeOutput(reviewSerializer.SerializeReview(review));
    }

    [TestMethod]
    public void ReviewedFixtureResults_NeverLeaveLocalPolicy()
    {
        foreach (var result in Review().ReviewedFixtureResults)
        {
            Assert.IsTrue(result.NeverSentToLlm);
            Assert.IsTrue(result.NeverSentToCloud);
            Assert.IsTrue(result.MatchesExpectation);
        }
    }

    [TestMethod]
    public void FixtureReviewOptions_AreNoOpAndNonAuthorizing()
    {
        foreach (var option in Enum.GetValues<NodalOsFixtureResultReviewOption>())
        {
            var result = reviewService.ApplyOption(option);

            Assert.IsTrue(result.IsNoOp);
            Assert.IsFalse(result.MutatesState);
            Assert.IsFalse(result.AuthorizesRealScan);
            Assert.IsFalse(result.AuthorizesFilesystemAccess);
            Assert.IsFalse(result.AuthorizesIndexing);
            Assert.IsFalse(result.AuthorizesLlmContext);
            AssertSafeOutput(reviewSerializer.SerializeAction(result));
        }
    }

    [TestMethod]
    public void FixtureReviewReadiness_AllowsSyntheticReviewOnly()
    {
        var readiness = reviewService.Evaluate(Review());

        Assert.IsTrue(readiness.ReadyForSyntheticFixtureReview);
        Assert.IsFalse(readiness.ReadyForRealScan);
        Assert.IsFalse(readiness.ReadyForFilesystemAccess);
        Assert.IsFalse(readiness.ReadyForLlmContext);
        AssertSafeOutput(reviewSerializer.SerializeReadiness(readiness));
    }

    [TestMethod]
    public void ScanBoundaryAudit_CoversAllDimensionsAndPassesSyntheticLayer()
    {
        var audit = Audit();

        Assert.AreEqual(NodalOsScanBoundaryAuditStatus.Pass, audit.AuditStatus);
        Assert.AreEqual(Enum.GetValues<NodalOsScanBoundaryAuditDimension>().Length, audit.Findings.Count);
        Assert.IsTrue(audit.Findings.All(f => f.Status == NodalOsScanBoundaryAuditStatus.Pass));
        Assert.IsTrue(audit.BoundaryDecision.SyntheticLayerReady);
        AssertSafeOutput(auditSerializer.SerializeAudit(audit));
    }

    [TestMethod]
    public void ScanBoundaryDecision_KeepsAllRealReadinessBlocked()
    {
        var decision = Audit().BoundaryDecision;

        Assert.IsFalse(decision.ReadyForRealScan);
        Assert.IsFalse(decision.ReadyForRealFilesystemAccess);
        Assert.IsFalse(decision.ReadyForRealPathJail);
        Assert.IsFalse(decision.ReadyForRealSecretDetection);
        Assert.IsFalse(decision.ReadyForRealExclusionEnforcement);
        Assert.IsFalse(decision.ReadyForIndexing);
        Assert.IsFalse(decision.ReadyForVectorization);
        Assert.IsFalse(decision.ReadyForLlmContext);
        Assert.IsTrue(decision.RequiredBeforeRealScanRedacted.Count > 0);
        Assert.IsTrue(decision.RequiredBeforeFilesystemAccessRedacted.Count > 0);
    }

    [TestMethod]
    public void ScanBoundaryAudit_FailsWhenForbiddenMarkersAppear()
    {
        var markers = new[]
        {
            "RealScan=true",
            "DirectoryListing=true",
            "FileRead=true",
            "FileHash=true",
            "FilesystemApi=true",
            "CanonicalizationReal=true",
            "IndexingTrue",
            "VectorizationTrue",
            "LlmContextTrue",
            "PromptGenerated=true",
            "ProviderCall=true",
            "NetworkCall=true",
            "CloudCall=true",
            "RuntimeExecution=true"
        };

        Assert.AreEqual(NodalOsScanBoundaryAuditStatus.Fail, auditService.EvaluateMarkers(markers));
    }

    [TestMethod]
    public void StaticPreviewArtifact_HasNoExternalScriptsOrNetworkResources()
    {
        var html = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m551", "synthetic-dry-run-simulator-preview.html"));

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_MarkSyntheticSimulatorReadyAndKeepRealReadinessFalse()
    {
        var summary = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m551", "synthetic-dry-run-simulator-summary.json"));
        var review = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m551", "fixture-result-review.json"));
        var audit = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m551", "scan-boundary-audit.json"));

        AssertContains(summary, "\"decision\": \"SYNTHETIC_DRY_RUN_SIMULATOR_READY\"");
        AssertContains(summary, "\"usesSyntheticFixturesOnly\": true");
        AssertContains(summary, "\"readyForRealScan\": false");
        AssertContains(review, "\"isReviewOnly\": true");
        AssertContains(audit, "\"syntheticLayerReady\": true");
        AssertContains(audit, "\"readyForRealFilesystemAccess\": false");
        AssertSafeOutput(summary + review + audit);
    }

    [TestMethod]
    public void Serializers_AreDeterministicAndSafe()
    {
        Assert.AreEqual(simulatorSerializer.SerializeResult(SimulationResult()), simulatorSerializer.SerializeResult(SimulationResult()));
        Assert.AreEqual(reviewSerializer.SerializeReview(Review()), reviewSerializer.SerializeReview(Review()));
        Assert.AreEqual(auditSerializer.SerializeAudit(Audit()), auditSerializer.SerializeAudit(Audit()));
    }

    [TestMethod]
    public void Boundary_NewSyntheticSimulatorFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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

    private NodalOsScanFixtureMatrix Matrix() => NodalOsScanFixtureMatrixFixtures.Matrix();

    private NodalOsSyntheticDryRunSimulatorContract SimulatorContract() =>
        simulatorService.CreateContract(Matrix(), NodalOsPathJailPrototypeFixtures.Prototype());

    private IReadOnlyList<NodalOsSyntheticSimulationInput> Inputs() =>
        simulatorService.CreateInputs(Matrix());

    private NodalOsSyntheticDryRunSimulationResult SimulationResult() =>
        simulatorService.Simulate(SimulatorContract(), Inputs());

    private NodalOsFixtureResultReview Review() =>
        reviewService.CreateReview(SimulationResult(), Matrix());

    private NodalOsScanBoundaryAudit Audit() =>
        auditService.CreateAudit(SimulatorContract(), Review(), NodalOsPathJailPrototypeFixtures.Prototype(), Matrix());

    private static string NewSource()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsSyntheticDryRunSimulatorContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsFixtureResultReviewContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsScanBoundaryAuditContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsSyntheticDryRunSimulatorServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsFixtureResultReviewServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsScanBoundaryAuditServices.cs")
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
