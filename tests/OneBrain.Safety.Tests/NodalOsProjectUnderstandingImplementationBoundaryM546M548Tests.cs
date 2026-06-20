using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ProjectUnderstandingImplementationBoundary")]
[TestCategory("PathJailPrototype")]
[TestCategory("ScanFixtureMatrix")]
[TestCategory("ImplementationBoundary")]
[TestCategory("ProjectUnderstandingDryRunReview")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsProjectUnderstandingImplementationBoundaryM546M548Tests
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

    private readonly NodalOsProjectUnderstandingImplementationBoundaryService boundaryService = new();
    private readonly NodalOsProjectUnderstandingImplementationBoundaryJsonSerializer boundarySerializer = new();
    private readonly NodalOsPathJailPrototypeService pathService = new();
    private readonly NodalOsPathJailPrototypeJsonSerializer pathSerializer = new();
    private readonly NodalOsScanFixtureMatrixService matrixService = new();
    private readonly NodalOsScanFixtureMatrixJsonSerializer matrixSerializer = new();

    [TestMethod]
    public void ImplementationBoundaryAdr_DeclaresSafeImplementationBoundary()
    {
        var adr = Adr();

        AssertContains(adr, "Direct operational scan.");
        AssertContains(adr, "Direct content access.");
        AssertContains(adr, "Direct LLM context construction.");
        AssertContains(adr, "synthetic fixtures");
        AssertContains(adr, "Cloud scan.");
        AssertContains(adr, "Implicit consent.");
        AssertContains(adr, "Broad crawler.");
        AssertSafeOutput(adr);
    }

    [TestMethod]
    public void ImplementationBoundaryDecision_RequiresFutureGatesAndEnablesNoOperations()
    {
        var decision = BoundaryDecision();

        Assert.IsTrue(decision.AdrDefined);
        Assert.IsTrue(decision.RequiresPathJailPrototypeContract);
        Assert.IsTrue(decision.RequiresScanFixtureMatrix);
        Assert.IsTrue(decision.RequiresSyntheticOnlyTests);
        Assert.IsTrue(decision.RequiresDryRunSimulatorContract);
        Assert.IsTrue(decision.RequiresAuditCheckpoint);
        Assert.IsTrue(decision.RequiresExplicitUserConsent);
        Assert.IsTrue(decision.RequiresNoMutationGuarantee);
        Assert.IsTrue(decision.RequiresCancellationSemantics);
        Assert.IsFalse(decision.EnablesRealScan);
        Assert.IsFalse(decision.EnablesRealFilesystemAccess);
        Assert.IsFalse(decision.EnablesIndexing);
        Assert.IsFalse(decision.EnablesVectorization);
        Assert.IsFalse(decision.EnablesLlmContext);
        Assert.IsFalse(decision.EnablesProviderActivity);
        AssertSafeOutput(boundarySerializer.Serialize(decision));
    }

    [TestMethod]
    public void PathJailPrototype_IsPrototypeOnlySyntheticAndNonOperational()
    {
        var prototype = Prototype();

        Assert.IsTrue(prototype.IsPrototypeOnly);
        Assert.IsTrue(prototype.SyntheticRootOnly);
        Assert.IsFalse(prototype.UsesRealFilesystem);
        Assert.IsFalse(prototype.PerformsRealCanonicalization);
        Assert.IsFalse(prototype.PerformsDirectoryListing);
        Assert.IsFalse(prototype.PerformsFileRead);
        Assert.IsFalse(prototype.PerformsFileHash);
        Assert.IsFalse(prototype.CanMutateFilesystem);
        Assert.IsFalse(prototype.CanAuthorizeScan);
        AssertSafeOutput(pathSerializer.SerializePrototype(prototype));
    }

    [TestMethod]
    public void PathJailCandidates_ProducePreviewOnlyNonAuthorizingDecisions()
    {
        var decisions = Candidates().Select(pathService.Decide).ToArray();

        Assert.IsTrue(decisions.Any(d => d.AllowedForFutureScanPreview));
        Assert.IsTrue(decisions.Any(d => !d.AllowedForFutureScanPreview));
        Assert.IsTrue(decisions.All(d => d.RequiresAudit));
        Assert.IsTrue(decisions.Any(d => d.RequiresUserReview));
        AssertSafeOutput(string.Join(Environment.NewLine, decisions.Select(pathSerializer.SerializeDecision)));
    }

    [TestMethod]
    public void PathJailPrototypeReadiness_RemainsBlockedForRealUse()
    {
        var readiness = pathService.Evaluate(Prototype());

        Assert.IsFalse(readiness.ReadyForRealPathJail);
        Assert.IsFalse(readiness.ReadyForRealCanonicalization);
        Assert.IsFalse(readiness.ReadyForDirectoryListing);
        Assert.IsFalse(readiness.ReadyForFileRead);
        Assert.IsFalse(readiness.ReadyForFileHash);
        Assert.IsFalse(readiness.ReadyForRealScan);
        Assert.IsTrue(readiness.MissingRequirementsRedacted.Count > 0);
        Assert.IsTrue(readiness.BlockersRedacted.Count > 0);
        AssertSafeOutput(pathSerializer.SerializeReadiness(readiness));
    }

    [TestMethod]
    public void ScanFixtureMatrix_IsSyntheticOnlyAndPerformsNoOperationalWork()
    {
        var matrix = Matrix();

        Assert.IsTrue(matrix.UsesSyntheticFixturesOnly);
        Assert.IsFalse(matrix.UsesRealFilesystem);
        Assert.IsFalse(matrix.PerformsDirectoryListing);
        Assert.IsFalse(matrix.PerformsFileRead);
        Assert.IsFalse(matrix.PerformsFileHash);
        Assert.IsFalse(matrix.PerformsSecretDetectionOnRealData);
        Assert.IsFalse(matrix.PerformsIndexing);
        Assert.IsFalse(matrix.PerformsVectorization);
        Assert.IsFalse(matrix.BuildsLlmContext);
        AssertSafeOutput(matrixSerializer.SerializeMatrix(matrix));
    }

    [TestMethod]
    public void ScanFixtureMatrix_IncludesAllRequiredFixtureCategories()
    {
        var categories = Matrix().Fixtures.Select(f => f.Category).ToHashSet();

        Assert.AreEqual(Enum.GetValues<NodalOsScanFixtureCategory>().Length, categories.Count);
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.EmptyWorkspace));
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.SmallSourceTree));
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.DependencyFolder));
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.GeneratedOutput));
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.HiddenItem));
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.EnvironmentMarker));
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.SensitiveName));
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.BinaryMedia));
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.SymlinkLike));
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.OutsideJailPath));
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.CaseSensitivity));
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.DeepTree));
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.MaxFiles));
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.MaxBytes));
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.Cancellation));
        Assert.IsTrue(categories.Contains(NodalOsScanFixtureCategory.NoMutation));
    }

    [TestMethod]
    public void ScanFixtureMatrix_ExpectedOutcomesCoverRequiredDispositionAndNeverLeaveLocalPolicy()
    {
        var outcomes = Matrix().Fixtures.Select(f => f.ExpectedOutcome).ToArray();
        var dispositions = outcomes.Select(o => o.Disposition).ToHashSet();

        Assert.IsTrue(dispositions.Contains(NodalOsScanFixtureExpectedDisposition.ExcludedPreview));
        Assert.IsTrue(dispositions.Contains(NodalOsScanFixtureExpectedDisposition.BlockedPreview));
        Assert.IsTrue(dispositions.Contains(NodalOsScanFixtureExpectedDisposition.RequiresReview));
        Assert.IsTrue(dispositions.Contains(NodalOsScanFixtureExpectedDisposition.RedactedPreview));
        Assert.IsTrue(dispositions.Contains(NodalOsScanFixtureExpectedDisposition.AuditRequired));
        Assert.IsTrue(outcomes.All(o => o.NeverSentToLlm));
        Assert.IsTrue(outcomes.All(o => o.NeverSentToCloud));
    }

    [TestMethod]
    public void ScanFixtureMatrixReadiness_AllowsSyntheticTestsOnly()
    {
        var readiness = matrixService.Evaluate(Matrix());

        Assert.IsTrue(readiness.ReadyForSyntheticDryRunTests);
        Assert.IsFalse(readiness.ReadyForRealScan);
        Assert.IsFalse(readiness.ReadyForRealFilesystemAccess);
        Assert.IsFalse(readiness.ReadyForIndexing);
        Assert.IsFalse(readiness.ReadyForVectorization);
        Assert.IsFalse(readiness.ReadyForLlmContext);
        AssertSafeOutput(matrixSerializer.SerializeReadiness(readiness));
    }

    [TestMethod]
    public void StaticPreviewArtifact_HasNoExternalScriptsOrNetworkResources()
    {
        var html = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m548", "project-understanding-implementation-boundary-preview.html"));

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_MarkImplementationBoundaryReadyAndKeepRealReadinessFalse()
    {
        var summary = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m548", "project-understanding-implementation-boundary-summary.json"));
        var path = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m548", "path-jail-prototype-contract.json"));
        var matrix = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m548", "scan-fixture-matrix.json"));

        AssertContains(summary, "\"decision\": \"PROJECT_UNDERSTANDING_IMPLEMENTATION_BOUNDARY_READY\"");
        AssertContains(path, "\"isPrototypeOnly\": true");
        AssertContains(path, "\"usesRealFilesystem\": false");
        AssertContains(matrix, "\"usesSyntheticFixturesOnly\": true");
        AssertContains(matrix, "\"readyForSyntheticDryRunTests\": true");
        AssertContains(matrix, "\"readyForRealScan\": false");
        AssertSafeOutput(summary + path + matrix);
    }

    [TestMethod]
    public void Serializers_AreDeterministicAndSafe()
    {
        Assert.AreEqual(boundarySerializer.Serialize(BoundaryDecision()), boundarySerializer.Serialize(BoundaryDecision()));
        Assert.AreEqual(pathSerializer.SerializePrototype(Prototype()), pathSerializer.SerializePrototype(Prototype()));
        Assert.AreEqual(pathSerializer.SerializeCandidates(Candidates()), pathSerializer.SerializeCandidates(Candidates()));
        Assert.AreEqual(matrixSerializer.SerializeMatrix(Matrix()), matrixSerializer.SerializeMatrix(Matrix()));
    }

    [TestMethod]
    public void Boundary_NewImplementationBoundaryFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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

    private NodalOsProjectUnderstandingImplementationBoundaryDecision BoundaryDecision() =>
        boundaryService.CreateDecision();

    private NodalOsPathJailPrototypeContract Prototype() =>
        pathService.CreatePrototype();

    private IReadOnlyList<NodalOsPathJailCandidatePreview> Candidates() =>
        pathService.CreateCandidates();

    private NodalOsScanFixtureMatrix Matrix() =>
        matrixService.CreateMatrix();

    private static string Adr() =>
        TextStore.ReadAllText(PathFor("docs", "architecture", "project-understanding-implementation-boundary-adr.md"));

    private static string NewSource()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsProjectUnderstandingImplementationBoundaryContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsPathJailPrototypeContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsScanFixtureMatrixContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsProjectUnderstandingImplementationBoundaryServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsPathJailPrototypeServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsScanFixtureMatrixServices.cs")
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
