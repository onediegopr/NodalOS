using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ProjectUnderstandingScanGate")]
[TestCategory("PathJailPreconditions")]
[TestCategory("ConsentScopePreview")]
[TestCategory("RealScanAuditGate")]
[TestCategory("ProjectUnderstandingPreconditions")]
[TestCategory("AssignmentArchiveReview")]
[TestCategory("NextPhaseAdr")]
[TestCategory("WorkspaceReadinessContext")]
[TestCategory("WorkspaceLocalModel")]
[TestCategory("UserContext")]
[TestCategory("ContextIntakePreview")]
[TestCategory("ProjectUnderstandingPolicy")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsProjectUnderstandingScanGateM537M539Tests
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

    private readonly NodalOsPathJailPreconditionsService pathService = new();
    private readonly NodalOsPathJailPreconditionsJsonSerializer pathSerializer = new();
    private readonly NodalOsConsentScopePreviewService consentService = new();
    private readonly NodalOsConsentScopePreviewJsonSerializer consentSerializer = new();
    private readonly NodalOsRealScanAuditGateService gateService = new();
    private readonly NodalOsRealScanAuditGateJsonSerializer gateSerializer = new();

    [TestMethod]
    public void PathJailPreconditions_DeclaresAllOperationalReadinessFalse()
    {
        var readiness = PathReadiness();

        Assert.IsFalse(readiness.ReadyForRealPathJail);
        Assert.IsFalse(readiness.ReadyForFilesystemScan);
        Assert.IsFalse(readiness.ReadyForFileRead);
        Assert.IsFalse(readiness.ReadyForFileHashing);
        Assert.IsFalse(readiness.ReadyForDirectoryListing);
        AssertSafeOutput(pathSerializer.SerializeReadiness(readiness));
    }

    [TestMethod]
    public void PathJailPreconditions_DeclaresAllForbiddenCapabilitiesFalse()
    {
        var readiness = PathReadiness();

        Assert.IsFalse(readiness.CanResolveRealPath);
        Assert.IsFalse(readiness.CanReadDirectory);
        Assert.IsFalse(readiness.CanReadFile);
        Assert.IsFalse(readiness.CanHashFile);
        Assert.IsFalse(readiness.CanFollowSymlink);
        Assert.IsFalse(readiness.CanMutateFilesystem);
        Assert.IsFalse(readiness.CanCreateIndex);
        Assert.IsFalse(readiness.CanBuildLlmContext);
    }

    [TestMethod]
    public void PathJailPreconditions_RequireCorePolicies()
    {
        var p = PathPreconditions();

        AssertText(p.RequiredCanonicalizationPolicy);
        AssertText(p.RequiredPathContainmentPolicy);
        AssertText(p.RequiredSymlinkPolicy);
        AssertText(p.RequiredCaseSensitivityPolicy);
        AssertText(p.RequiredDriveBoundaryPolicy);
        AssertText(p.RequiredNetworkSharePolicy);
        AssertText(p.RequiredExcludedFoldersPolicy);
        AssertSafeOutput(pathSerializer.SerializePreconditions(p));
    }

    [TestMethod]
    public void PathJailPreconditions_RequireNoMutationCancellationEvidenceTimelineAndAudit()
    {
        var p = PathPreconditions();

        AssertText(p.RequiredNoMutationGuarantee);
        AssertText(p.RequiredCancellationPolicy);
        AssertText(p.RequiredEvidencePlan);
        AssertText(p.RequiredTimelinePlan);
        AssertText(p.RequiredAuditBeforeEnablement);
        Assert.AreEqual(NodalOsPathJailPreconditionsStatus.PreconditionsDrafted, p.Status);
    }

    [TestMethod]
    public void ConsentRequestDraft_IsDraftOnlyNoOpAndCannotApproveOperationalScan()
    {
        var request = Consent();

        Assert.IsTrue(request.IsDraftOnly);
        Assert.IsTrue(request.IsNoOp);
        Assert.IsFalse(request.CanApproveRealScan);
        Assert.IsFalse(request.CloudDefault);
        Assert.IsFalse(request.LlmDefault);
        Assert.IsTrue(request.NoMutationGuarantee);
        AssertSafeOutput(consentSerializer.SerializeConsent(request));
    }

    [TestMethod]
    public void ScopePreview_IsEstimatedOnlyAndDoesNotUseOperationalFilesystem()
    {
        var scope = Scope();

        Assert.IsTrue(scope.EstimatedOnly);
        Assert.IsFalse(scope.UsesRealFilesystem);
        Assert.IsFalse(scope.DirectoryListingPerformed);
        Assert.IsFalse(scope.FileReadPerformed);
        Assert.IsFalse(scope.FileHashPerformed);
        Assert.IsTrue(scope.IncludePatternsRedacted.Count > 0);
        Assert.IsTrue(scope.ExcludePatternsRedacted.Count > 0);
        AssertSafeOutput(consentSerializer.SerializeScope(scope));
    }

    [TestMethod]
    public void ConsentOptions_AreNoOpAndNonAuthorizing()
    {
        foreach (var option in Enum.GetValues<NodalOsConsentScopeOption>())
        {
            var result = consentService.ApplyOption(Consent(), option);

            Assert.IsTrue(result.IsNoOp);
            Assert.IsFalse(result.MutatesState);
            Assert.IsFalse(result.AuthorizesRealScan);
            Assert.IsFalse(result.AuthorizesFileRead);
            Assert.IsFalse(result.AuthorizesIndexing);
            Assert.IsFalse(result.AuthorizesVectorization);
            Assert.IsFalse(result.AuthorizesLlmContext);
            Assert.IsTrue(result.RequiresFutureExplicitGate);
            AssertSafeOutput(consentSerializer.SerializeResult(result));
        }
    }

    [TestMethod]
    public void RealScanAuditGate_DeclaresAllReadinessFalse()
    {
        var decision = GateDecision();

        Assert.IsFalse(decision.ReadyForRealScan);
        Assert.IsFalse(decision.ReadyForDirectoryListing);
        Assert.IsFalse(decision.ReadyForFileRead);
        Assert.IsFalse(decision.ReadyForFileHashing);
        Assert.IsFalse(decision.ReadyForIndexing);
        Assert.IsFalse(decision.ReadyForVectorization);
        Assert.IsFalse(decision.ReadyForLlmContextBuild);
        Assert.IsFalse(decision.ReadyForCloudSync);
        AssertSafeOutput(gateSerializer.SerializeDecision(decision));
    }

    [TestMethod]
    public void RealScanAuditGate_IncludesAllAuditDimensionsAndBlocks()
    {
        var gate = Gate();

        Assert.AreEqual(NodalOsRealScanAuditStatus.Blocked, gate.AuditStatus);
        Assert.AreEqual(Enum.GetValues<NodalOsRealScanAuditDimension>().Length, gate.Findings.Count);
        Assert.IsTrue(gate.Findings.All(f => f.Status == NodalOsRealScanAuditStatus.Blocked));
        AssertSafeOutput(gateSerializer.SerializeGate(gate));
    }

    [TestMethod]
    public void RealScanAuditGate_IncludesRequiredBeforeLists()
    {
        var decision = GateDecision();

        Assert.IsTrue(decision.RequiredBeforeRealScanRedacted.Count > 0);
        Assert.IsTrue(decision.RequiredBeforeFileReadRedacted.Count > 0);
        Assert.IsTrue(decision.RequiredBeforeIndexingRedacted.Count > 0);
        Assert.IsTrue(decision.RequiredBeforeLlmContextRedacted.Count > 0);
        Assert.IsTrue(decision.RequiredBeforeCloudSyncRedacted.Count > 0);
    }

    [TestMethod]
    public void RealScanAuditGate_FailsWhenImplementationMarkersAppear()
    {
        var markers = new[]
        {
            "DirectoryListingPerformed=true",
            "FileReadPerformed=true",
            "FileHashPerformed=true",
            "GitCommand=true",
            "VectorContextCreated=true",
            "IndexCreated=true",
            "LlmContextBuilt=true",
            "PromptGenerated=true",
            "ProviderCall=true",
            "NetworkCall=true",
            "CloudSync=true",
            "RuntimeExecution=true"
        };

        Assert.AreEqual(NodalOsRealScanAuditStatus.Failed, gateService.EvaluateImplementationMarkers(markers));
    }

    [TestMethod]
    public void StaticPreviewArtifact_HasNoExternalScriptsOrNetworkResources()
    {
        var html = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m539", "project-understanding-scan-gate-preview.html"));

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_MarkProjectUnderstandingScanGateReady()
    {
        var path = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m539", "path-jail-preconditions-summary.json"));
        var consent = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m539", "consent-scope-preview-summary.json"));
        var gate = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m539", "real-scan-audit-gate.json"));

        AssertContains(path, "\"readyForRealPathJail\": false");
        AssertContains(consent, "\"canApproveRealScan\": false");
        AssertContains(gate, "\"decision\": \"PROJECT_UNDERSTANDING_SCAN_GATE_READY\"");
        AssertContains(gate, "\"readyForRealScan\": false");
        AssertSafeOutput(path + consent + gate);
    }

    [TestMethod]
    public void Boundary_NewScanGateFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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

    private NodalOsPathJailPreconditions PathPreconditions() => pathService.CreatePreconditions();

    private NodalOsPathJailReadinessResult PathReadiness() => pathService.Evaluate(PathPreconditions());

    private NodalOsConsentRequestDraft Consent() => consentService.CreateConsentDraft();

    private NodalOsScopePreviewContract Scope() => consentService.CreateScopePreview();

    private NodalOsRealScanAuditGate Gate() => gateService.CreateGate(PathPreconditions(), Scope());

    private NodalOsRealScanGateDecision GateDecision() => gateService.Decide(Gate());

    private static void AssertText(string value) =>
        Assert.IsFalse(string.IsNullOrWhiteSpace(value));

    private static string NewSource()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsPathJailPreconditionsContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsConsentScopePreviewContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsRealScanAuditGateContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsPathJailPreconditionsServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsConsentScopePreviewServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsRealScanAuditGateServices.cs")
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
