using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ProjectUnderstandingDryRunPolicy")]
[TestCategory("SecretDetectionPolicyPreview")]
[TestCategory("ExclusionPolicyPack")]
[TestCategory("ScanDryRun")]
[TestCategory("ProjectUnderstandingScanGate")]
[TestCategory("PathJailPreconditions")]
[TestCategory("ConsentScopePreview")]
[TestCategory("RealScanAuditGate")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsProjectUnderstandingDryRunPolicyM540M542Tests
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

    private readonly NodalOsSecretDetectionPolicyPreviewService secretService = new();
    private readonly NodalOsSecretDetectionPolicyPreviewJsonSerializer secretSerializer = new();
    private readonly NodalOsExclusionPolicyPackService exclusionService = new();
    private readonly NodalOsExclusionPolicyPackJsonSerializer exclusionSerializer = new();
    private readonly NodalOsScanDryRunService dryRunService = new();
    private readonly NodalOsScanDryRunJsonSerializer dryRunSerializer = new();

    [TestMethod]
    public void SecretDetectionPolicyPreview_IsPreviewOnlyAndDoesNotUseOperationalContent()
    {
        var policy = SecretPolicy();

        Assert.IsTrue(policy.IsPreviewOnly);
        Assert.IsFalse(policy.UsesRealContent);
        Assert.IsFalse(policy.ReadsFiles);
        Assert.IsFalse(policy.PerformsSecretDetectionOnRealData);
        Assert.IsTrue(policy.CanBlockScan);
        Assert.IsTrue(policy.CanRedactFindings);
        Assert.IsTrue(policy.RequiresUserReview);
        Assert.IsTrue(policy.RequiresAuditBeforeEnablement);
        Assert.AreEqual(Enum.GetValues<NodalOsSensitivePolicyCategory>().Length, policy.Categories.Count);
        AssertSafeOutput(secretSerializer.SerializePolicy(policy));
    }

    [TestMethod]
    public void SecretDetectionPolicyPreview_CannotExposeSensitiveValuesOrSendToExternalSurfaces()
    {
        var readiness = SecretReadiness();

        Assert.IsFalse(readiness.CanReadFile);
        Assert.IsFalse(readiness.CanInspectRealContent);
        Assert.IsFalse(readiness.CanEmitRawSecret);
        Assert.IsFalse(readiness.CanPersistRawSecret);
        Assert.IsFalse(readiness.CanSendSecretToLlm);
        Assert.IsFalse(readiness.CanSendSecretToCloud);
        Assert.IsFalse(readiness.ReadyForRealSecretDetection);
        Assert.IsFalse(readiness.ReadyForRealScan);
        Assert.IsFalse(readiness.ReadyForLlmContextBuild);
        Assert.IsTrue(readiness.MissingRequirementsRedacted.Count > 0);
        Assert.IsTrue(readiness.BlockersRedacted.Count > 0);
        AssertSafeOutput(secretSerializer.SerializeReadiness(readiness));
    }

    [TestMethod]
    public void ExclusionPolicyPack_IsPreviewOnlyAndDoesNotUseOperationalFilesystem()
    {
        var pack = ExclusionPack();

        Assert.IsTrue(pack.IsPreviewOnly);
        Assert.IsFalse(pack.UsesRealFilesystem);
        Assert.IsFalse(pack.DirectoryListingPerformed);
        Assert.IsFalse(pack.FileReadPerformed);
        Assert.AreEqual(Enum.GetValues<NodalOsExclusionPolicyGroup>().Length, pack.Rules.Count);
        AssertSafeOutput(exclusionSerializer.SerializePack(pack));
    }

    [TestMethod]
    public void ExclusionPolicyPack_IncludesDefaultPolicyGroups()
    {
        var groups = ExclusionPack().Rules.Select(rule => rule.Group).ToHashSet();

        Assert.IsTrue(groups.Contains(NodalOsExclusionPolicyGroup.DependencyFolders));
        Assert.IsTrue(groups.Contains(NodalOsExclusionPolicyGroup.BuildOutputs));
        Assert.IsTrue(groups.Contains(NodalOsExclusionPolicyGroup.CacheFolders));
        Assert.IsTrue(groups.Contains(NodalOsExclusionPolicyGroup.VcsMetadata));
        Assert.IsTrue(groups.Contains(NodalOsExclusionPolicyGroup.BinaryMediaHeavyFolders));
        Assert.IsTrue(groups.Contains(NodalOsExclusionPolicyGroup.EnvironmentFiles));
        Assert.IsTrue(groups.Contains(NodalOsExclusionPolicyGroup.SecretLikeFiles));
        Assert.IsTrue(groups.Contains(NodalOsExclusionPolicyGroup.GeneratedArtifacts));
        Assert.IsTrue(groups.Contains(NodalOsExclusionPolicyGroup.Logs));
        Assert.IsTrue(groups.Contains(NodalOsExclusionPolicyGroup.TemporaryFiles));
        Assert.IsTrue(groups.Contains(NodalOsExclusionPolicyGroup.VendorFolders));
        Assert.IsTrue(groups.Contains(NodalOsExclusionPolicyGroup.NodeModulesLikeFolders));
        Assert.IsTrue(groups.Contains(NodalOsExclusionPolicyGroup.BinObjLikeFolders));
    }

    [TestMethod]
    public void ExclusionPolicyPack_CannotApplyToOperationalSurfaces()
    {
        var readiness = ExclusionReadiness();

        Assert.IsFalse(readiness.CanReadDirectory);
        Assert.IsFalse(readiness.CanReadFile);
        Assert.IsFalse(readiness.CanApplyToRealFilesystem);
        Assert.IsFalse(readiness.CanCreateIndex);
        Assert.IsFalse(readiness.CanBuildLlmContext);
        Assert.IsFalse(readiness.ReadyForRealExclusionEnforcement);
        Assert.IsFalse(readiness.ReadyForRealScan);
        Assert.IsFalse(readiness.ReadyForIndexing);
        Assert.IsFalse(readiness.ReadyForVectorization);
        AssertSafeOutput(exclusionSerializer.SerializeReadiness(readiness));
    }

    [TestMethod]
    public void ScanDryRunRequest_IsContractOnlyAndDoesNotPerformOperationalWork()
    {
        var request = DryRunRequest();

        Assert.IsTrue(request.IsDryRunOnly);
        Assert.IsFalse(request.UsesRealFilesystem);
        Assert.IsFalse(request.PerformsDirectoryListing);
        Assert.IsFalse(request.PerformsFileRead);
        Assert.IsFalse(request.PerformsFileHash);
        Assert.IsFalse(request.PerformsIndexing);
        Assert.IsFalse(request.PerformsVectorization);
        Assert.IsFalse(request.BuildsLlmContext);
        AssertSafeOutput(dryRunSerializer.SerializeRequest(request));
    }

    [TestMethod]
    public void ScanDryRunResult_RemainsBlockedAndReferencesRealScanAuditGate()
    {
        var result = DryRunResult();

        Assert.IsTrue(result.GateStillBlocked);
        Assert.IsFalse(result.ReadyForRealDryRun);
        Assert.IsFalse(result.ReadyForRealScan);
        Assert.IsFalse(result.ReadyForFileRead);
        Assert.IsFalse(result.ReadyForIndexing);
        Assert.IsFalse(result.ReadyForVectorization);
        Assert.IsFalse(result.ReadyForLlmContext);
        Assert.AreEqual("real-scan-audit-gate-m539", result.RealScanAuditGateRef);
        Assert.IsTrue(result.BlockedCapabilitiesRedacted.Count > 0);
        Assert.IsTrue(result.RequiredNextGatesRedacted.Count > 0);
        AssertSafeOutput(dryRunSerializer.SerializeResult(result));
    }

    [TestMethod]
    public void ScanDryRunEventsPreview_DoesNotWireRealEventBusOrPersistence()
    {
        var result = DryRunResult();

        Assert.IsTrue(result.EventsPreview.Count > 0);
        Assert.IsTrue(result.EventsPreview.All(e => e.EmitsToRealEventBus == false));
        Assert.IsTrue(result.EventsPreview.All(e => e.ProductivePersistenceUsed == false));
        Assert.IsTrue(result.EventsPreview.Any(e => e.Kind == NodalOsScanDryRunEventPreviewKind.WouldStopBlocked));
    }

    [TestMethod]
    public void StaticPreviewArtifact_HasNoExternalScriptsOrNetworkResources()
    {
        var html = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m542", "project-understanding-dry-run-policy-preview.html"));

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_MarkDryRunPolicyReadyAndKeepOperationalReadinessFalse()
    {
        var secret = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m542", "secret-detection-policy-preview.json"));
        var exclusion = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m542", "exclusion-policy-pack.json"));
        var dryRun = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m542", "scan-dry-run-contract.json"));

        AssertContains(secret, "\"isPreviewOnly\": true");
        AssertContains(secret, "\"usesRealContent\": false");
        AssertContains(exclusion, "\"usesRealFilesystem\": false");
        AssertContains(dryRun, "\"decision\": \"PROJECT_UNDERSTANDING_DRY_RUN_POLICY_READY\"");
        AssertContains(dryRun, "\"readyForRealScan\": false");
        AssertContains(dryRun, "\"gateStillBlocked\": true");
        AssertSafeOutput(secret + exclusion + dryRun);
    }

    [TestMethod]
    public void Serializers_AreDeterministicAndSafe()
    {
        Assert.AreEqual(secretSerializer.SerializePolicy(SecretPolicy()), secretSerializer.SerializePolicy(SecretPolicy()));
        Assert.AreEqual(exclusionSerializer.SerializePack(ExclusionPack()), exclusionSerializer.SerializePack(ExclusionPack()));
        Assert.AreEqual(dryRunSerializer.SerializeResult(DryRunResult()), dryRunSerializer.SerializeResult(DryRunResult()));
    }

    [TestMethod]
    public void Boundary_NewDryRunPolicyFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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

    private NodalOsSecretDetectionPolicyPreview SecretPolicy() => secretService.CreatePolicyPreview();

    private NodalOsSecretDetectionReadinessResult SecretReadiness() => secretService.Evaluate(SecretPolicy());

    private NodalOsExclusionPolicyPack ExclusionPack() => exclusionService.CreatePack();

    private NodalOsExclusionPolicyReadinessResult ExclusionReadiness() => exclusionService.Evaluate(ExclusionPack());

    private NodalOsScanDryRunRequest DryRunRequest() =>
        dryRunService.CreateRequest(
            NodalOsPathJailPreconditionsFixtures.Preconditions(),
            NodalOsConsentScopePreviewFixtures.Consent(),
            SecretPolicy(),
            ExclusionPack());

    private NodalOsScanDryRunResult DryRunResult() =>
        dryRunService.Evaluate(DryRunRequest(), NodalOsRealScanAuditGateFixtures.Gate());

    private static string NewSource()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsSecretDetectionPolicyPreviewContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsExclusionPolicyPackContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsScanDryRunContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsSecretDetectionPolicyPreviewServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsExclusionPolicyPackServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsScanDryRunServices.cs")
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
