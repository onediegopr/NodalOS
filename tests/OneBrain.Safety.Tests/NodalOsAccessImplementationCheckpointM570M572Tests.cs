using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AuditCheckpointReview")]
[TestCategory("ProductiveConsentDesign")]
[TestCategory("DisabledAccessRoadmap")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsAccessImplementationCheckpointM570M572Tests
{
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
        "s" + "k-"
    ];

    private readonly NodalOsPerCapabilityAccessGateService gateService = new();
    private readonly NodalOsAuditCheckpointReviewService checkpointService = new();
    private readonly NodalOsAuditCheckpointReviewJsonSerializer checkpointSerializer = new();
    private readonly NodalOsProductiveConsentDesignDraftService consentDesignService = new();
    private readonly NodalOsProductiveConsentDesignDraftJsonSerializer consentDesignSerializer = new();
    private readonly NodalOsDisabledAccessRoadmapService roadmapService = new();
    private readonly NodalOsDisabledAccessRoadmapJsonSerializer roadmapSerializer = new();

    [TestMethod]
    public void AuditCheckpointReview_IsCheckpointOnlyAndCannotAuthorize()
    {
        var review = Checkpoint();

        Assert.IsTrue(review.IsCheckpointOnly);
        Assert.IsFalse(review.CanAuthorizeImplementation);
        Assert.IsFalse(review.CanEnableRealAccess);
        Assert.IsFalse(review.CanAccessFilesystem);
        Assert.IsFalse(review.CanBuildLlmContext);
        Assert.IsFalse(review.CanUseCloud);
        Assert.IsFalse(review.CanTriggerRuntime);
        AssertSafeOutput(checkpointSerializer.Serialize(review));
    }

    [TestMethod]
    public void AuditCheckpointReview_CoversRequiredScope()
    {
        var scope = Checkpoint().CoveredScope.ToHashSet();

        foreach (var item in Enum.GetValues<NodalOsAuditCheckpointScopeItem>())
            Assert.IsTrue(scope.Contains(item), $"Missing checkpoint scope: {item}");
    }

    [TestMethod]
    public void AuditCheckpointDecision_BlocksDirectImplementation()
    {
        var decision = Checkpoint().Decision;

        Assert.IsTrue(decision.GovernanceBaselineReady);
        Assert.IsFalse(decision.ReadyForDirectRealImplementation);
        Assert.IsFalse(decision.ReadyForProductiveConsentImplementation);
        Assert.IsFalse(decision.ReadyForRealPathJailImplementation);
        Assert.IsFalse(decision.ReadyForRealFilesystemAccess);
        Assert.IsFalse(decision.ReadyForRealScan);
        Assert.IsFalse(decision.ReadyForLlmContext);
    }

    [TestMethod]
    public void ProductiveConsentDesignDraft_IsDesignOnlyAndNonOperational()
    {
        var draft = ConsentDesign();

        Assert.IsTrue(draft.IsDesignOnly);
        Assert.IsFalse(draft.UsesProductivePersistence);
        Assert.IsFalse(draft.PersistsConsent);
        Assert.IsFalse(draft.EnforcesConsent);
        Assert.IsFalse(draft.CanAuthorizeCapability);
        Assert.IsFalse(draft.CanAuthorizeFilesystemAccess);
        Assert.IsFalse(draft.CanBuildLlmContext);
        Assert.IsFalse(draft.CanUseCloud);
        AssertSafeOutput(consentDesignSerializer.Serialize(draft));
    }

    [TestMethod]
    public void ConsentDataSafetyRules_BlockSensitiveMaterialContentAndImplicitLlmCloudPermission()
    {
        var rules = ConsentDesign().DataSafetyRules.Select(rule => rule.Kind).ToHashSet();

        Assert.IsTrue(rules.Contains(NodalOsConsentDataSafetyRuleKind.NoSensitiveMaterialInConsentRecords));
        Assert.IsTrue(rules.Contains(NodalOsConsentDataSafetyRuleKind.NoContentPayloadInConsentRecords));
        Assert.IsTrue(rules.Contains(NodalOsConsentDataSafetyRuleKind.ConsentCannotImplyLlmOrCloudPermission));
        Assert.IsTrue(rules.Contains(NodalOsConsentDataSafetyRuleKind.ContentAccessConsentCannotImplyRepresentationOrLlmContext));
        Assert.IsTrue(ConsentDesign().DataSafetyRules.All(rule => rule.BlocksProductiveUseIfMissing));
    }

    [TestMethod]
    public void ConsentDesignDecision_BlocksProductiveImplementationPersistenceAndEnforcement()
    {
        var decision = ConsentDesign().Decision;

        Assert.IsTrue(decision.ReadyForDesignReview);
        Assert.IsFalse(decision.ReadyForProductiveImplementation);
        Assert.IsFalse(decision.ReadyForConsentPersistence);
        Assert.IsFalse(decision.ReadyForConsentEnforcement);
        Assert.IsFalse(decision.ReadyForFilesystemAccess);
        Assert.IsFalse(decision.ReadyForLlmContext);
    }

    [TestMethod]
    public void DisabledAccessRoadmap_IsRoadmapOnlyAndCannotAuthorize()
    {
        var roadmap = Roadmap();

        Assert.IsTrue(roadmap.IsRoadmapOnly);
        Assert.IsFalse(roadmap.CanAuthorizeImplementation);
        Assert.IsFalse(roadmap.CanEnableRealAccess);
        AssertSafeOutput(roadmapSerializer.Serialize(roadmap));
    }

    [TestMethod]
    public void RoadmapPhases_AreDisabledByDefaultAndNonOperational()
    {
        var phases = Roadmap().Phases;

        foreach (var phase in Enum.GetValues<NodalOsDisabledAccessPhaseKind>())
            Assert.IsTrue(phases.Any(item => item.PhaseKind == phase), $"Missing roadmap phase: {phase}");

        foreach (var phase in phases)
        {
            Assert.IsTrue(phase.DisabledByDefault);
            Assert.IsFalse(phase.UsesRealFilesystem);
            Assert.IsFalse(phase.EnablesRealAccess);
            Assert.IsTrue(phase.RequiresFutureAudit);
        }
    }

    [TestMethod]
    public void RoadmapBlockers_IncludeRequiredSafetyAndAuditBlockers()
    {
        var blockers = Roadmap().Blockers.ToHashSet();

        foreach (var blocker in Enum.GetValues<NodalOsDisabledAccessRoadmapBlockerKind>())
            Assert.IsTrue(blockers.Contains(blocker), $"Missing roadmap blocker: {blocker}");
    }

    [TestMethod]
    public void RoadmapDecision_AllowsOnlyNextGovernedDesignPhase()
    {
        var decision = Roadmap().Decision;

        Assert.IsTrue(decision.ReadyForNextGovernedDesignPhase);
        Assert.IsFalse(decision.ReadyForRealImplementation);
        Assert.IsFalse(decision.ReadyForFilesystemAccess);
        Assert.IsFalse(decision.ReadyForRealScan);
        Assert.IsFalse(decision.ReadyForLlmContext);
    }

    [TestMethod]
    public void StaticPreviewArtifact_HasNoExternalScriptsOrNetworkResources()
    {
        var html = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m572", "access-implementation-checkpoint-preview.html"));

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_DeclareCheckpointDesignAndRoadmapBlocked()
    {
        var checkpoint = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m572", "audit-checkpoint-review.json"));
        var design = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m572", "productive-consent-design-draft.json"));
        var roadmap = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m572", "disabled-access-roadmap.json"));

        AssertContains(checkpoint, "\"isCheckpointOnly\": true");
        AssertContains(checkpoint, "\"readyForDirectRealImplementation\": false");
        AssertContains(design, "\"isDesignOnly\": true");
        AssertContains(design, "\"persistsConsent\": false");
        AssertContains(roadmap, "\"isRoadmapOnly\": true");
        AssertContains(roadmap, "\"readyForNextGovernedDesignPhase\": true");
        AssertSafeOutput(checkpoint + design + roadmap);
    }

    [TestMethod]
    public void Boundary_NewCheckpointDesignRoadmapFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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
        AssertDoesNotContain(source, "File" + ".Delete");
        AssertDoesNotContain(source, "File" + ".Move");
        AssertDoesNotContain(source, "Directory" + ".");
        AssertDoesNotContain(source, "File" + "Info");
        AssertDoesNotContain(source, "Directory" + "Info");
    }

    private NodalOsAuditCheckpointReview Checkpoint() => checkpointService.CreateReview();
    private NodalOsProductiveConsentDesignDraft ConsentDesign() => consentDesignService.CreateDraft(gateService.CreateGates());
    private NodalOsDisabledAccessRoadmap Roadmap() => roadmapService.CreateRoadmap(Checkpoint(), ConsentDesign());

    private static void AssertContains(string value, string expected) => StringAssert.Contains(value, expected);

    private static void AssertSafeOutput(string value)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(value, marker);

        AssertDoesNotContain(value, "NEXA");
        AssertDoesNotContain(value, "NODRIX");
        AssertDoesNotContain(value, "HOTEP");
    }

    private static void AssertDoesNotContain(string value, string forbidden) =>
        Assert.IsFalse(value.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Unexpected content: {forbidden}");

    private static string NewSource()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsAuditCheckpointReviewContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsProductiveConsentDesignDraftContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsDisabledAccessRoadmapContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsAuditCheckpointReviewServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsProductiveConsentDesignDraftServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsDisabledAccessRoadmapServices.cs")
        };

        return string.Join(Environment.NewLine, files.Select(TextStore.ReadAllText));
    }

    private static string PathFor(params string[] segments) => Path.Combine([FindRepoRoot(), .. segments]);

    private static string FindRepoRoot()
    {
        var current = Path.GetFullPath(AppContext.BaseDirectory);
        while (!string.IsNullOrEmpty(current) && !TextStore.Exists(Path.Combine(current, "OneBrain.slnx")))
            current = Path.GetDirectoryName(current) ?? string.Empty;

        return string.IsNullOrEmpty(current) ? throw new InvalidOperationException("Repository root not found.") : current;
    }
}
