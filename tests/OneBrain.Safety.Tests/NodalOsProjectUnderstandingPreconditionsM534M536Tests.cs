using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ProjectUnderstandingPreconditions")]
[TestCategory("AssignmentArchiveReview")]
[TestCategory("NextPhaseAdr")]
[TestCategory("AssignmentReviewHistory")]
[TestCategory("HandoffCompare")]
[TestCategory("PlannerGovernance")]
[TestCategory("AssignmentReview")]
[TestCategory("PlannerHandoff")]
[TestCategory("AssignmentSafetyAudit")]
[TestCategory("AssignmentUi")]
[TestCategory("TaskGraphInteraction")]
[TestCategory("PlannerUxAcceptance")]
[TestCategory("MissionPlanPreview")]
[TestCategory("AssignmentEngine")]
[TestCategory("PromptGovernance")]
[TestCategory("ByokProvider")]
[TestCategory("ProjectUnderstandingPolicy")]
[TestCategory("ContextIntakePreview")]
[TestCategory("UserContext")]
[TestCategory("WorkspaceReadinessContext")]
[TestCategory("WorkspaceMetadataHealth")]
[TestCategory("WorkspaceStorageMissionSwitcher")]
[TestCategory("WorkspaceLocalModel")]
[TestCategory("MissionControlVisualPolish")]
[TestCategory("MissionControlGuidance")]
[TestCategory("MissionControlInteractionNoOp")]
[TestCategory("MissionControlShellReadOnly")]
[TestCategory("AuditAPreUiBoundaryNaming")]
[TestCategory("ApprovalUxHandoffObservability")]
[TestCategory("ApprovalTimelineEvidence")]
[TestCategory("CoreRuntimeRegistryEventBusRedaction")]
[TestCategory("NewTopicsIntake")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsProjectUnderstandingPreconditionsM534M536Tests
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

    private readonly NodalOsProjectUnderstandingPreconditionsService preconditionsService = new();
    private readonly NodalOsProjectUnderstandingPreconditionsJsonSerializer preconditionsSerializer = new();
    private readonly NodalOsAssignmentArchiveReviewService archiveService = new();
    private readonly NodalOsAssignmentArchiveReviewJsonSerializer archiveSerializer = new();
    private readonly NodalOsAssignmentArchiveReviewMarkdownRenderer archiveMarkdown = new();
    private readonly NodalOsNextPhaseAdrService adrService = new();
    private readonly NodalOsNextPhaseAdrJsonSerializer adrSerializer = new();

    // --- M534: Project Understanding Preconditions ---

    [TestMethod]
    public void ProjectUnderstandingPreconditions_DeclaresReadyForRealProjectUnderstandingFalse()
    {
        var readiness = CreateReadiness();

        Assert.IsFalse(readiness.ReadyForRealProjectUnderstanding);
        AssertSafeOutput(preconditionsSerializer.SerializeReadiness(readiness));
    }

    [TestMethod]
    public void ProjectUnderstandingPreconditions_DeclaresReadyForFilesystemScanFalse()
    {
        var readiness = CreateReadiness();

        Assert.IsFalse(readiness.ReadyForFilesystemScan);
    }

    [TestMethod]
    public void ProjectUnderstandingPreconditions_DeclaresReadyForLlmContextBuildFalse()
    {
        var readiness = CreateReadiness();

        Assert.IsFalse(readiness.ReadyForLlmContextBuild);
    }

    [TestMethod]
    public void ProjectUnderstandingPreconditions_BlocksRealScanDirectoryListingFileReadHashingAndGitCommands()
    {
        var readiness = CreateReadiness();

        Assert.IsFalse(readiness.ReadyForFilesystemScan);
        Assert.IsFalse(readiness.ReadyForIndexing);
        Assert.IsFalse(readiness.ReadyForEmbeddings);
        Assert.IsTrue(readiness.BlockersRedacted.Any(b => b.Contains("scan", StringComparison.OrdinalIgnoreCase)
            || b.Contains("filesystem", StringComparison.OrdinalIgnoreCase)
            || b.Contains("jail", StringComparison.OrdinalIgnoreCase)));
        AssertSafeOutput(preconditionsSerializer.SerializeReadiness(readiness));
    }

    [TestMethod]
    public void ProjectUnderstandingPreconditions_RequiresConsentPathJailScanScopePreviewAndNoMutationGuarantee()
    {
        var p = CreatePreconditions();

        Assert.IsFalse(string.IsNullOrWhiteSpace(p.RequiredConsent));
        Assert.IsFalse(string.IsNullOrWhiteSpace(p.RequiredPathJailValidation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(p.RequiredScanScopePreview));
        Assert.IsFalse(string.IsNullOrWhiteSpace(p.RequiredNoMutationGuarantee));
        Assert.IsTrue(p.RequiredConsent.Contains("consent", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(p.RequiredPathJailValidation.Contains("jail", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(p.RequiredScanScopePreview.Contains("preview", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(p.RequiredNoMutationGuarantee.Contains("read-only", StringComparison.OrdinalIgnoreCase));
        AssertSafeOutput(preconditionsSerializer.SerializePreconditions(p));
    }

    [TestMethod]
    public void ProjectUnderstandingPreconditions_RequiresRedactionPolicyAndSecretDetectionPolicy()
    {
        var p = CreatePreconditions();

        Assert.IsFalse(string.IsNullOrWhiteSpace(p.RequiredRedactionPolicy));
        Assert.IsFalse(string.IsNullOrWhiteSpace(p.RequiredSecretDetectionPolicy));
        Assert.IsFalse(string.IsNullOrWhiteSpace(p.RequiredExclusionPolicy));
        AssertSafeOutput(preconditionsSerializer.SerializePreconditions(p));
    }

    [TestMethod]
    public void ProjectUnderstandingPreconditions_RequiresCancellationSemanticsAndAuditBeforeRealScan()
    {
        var p = CreatePreconditions();

        Assert.IsFalse(string.IsNullOrWhiteSpace(p.RequiredCancellationSemantics));
        Assert.IsFalse(string.IsNullOrWhiteSpace(p.RequiredAuditBeforeRealScan));
        Assert.IsFalse(string.IsNullOrWhiteSpace(p.RequiredNoLlmBeforeContextApproval));
        Assert.IsTrue(p.RequiredCancellationSemantics.Contains("cancel", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(p.RequiredAuditBeforeRealScan.Contains("audit", StringComparison.OrdinalIgnoreCase));
        AssertSafeOutput(preconditionsSerializer.SerializePreconditions(p));
    }

    // --- M535: Assignment Archive Review ---

    [TestMethod]
    public void AssignmentArchiveReview_IncludesM519ThroughM533CloseoutCoverage()
    {
        var review = CreateArchiveReview();
        var milestoneIds = review.ClosedMilestones.Select(m => m.MilestoneId).ToList();

        CollectionAssert.Contains(milestoneIds, "M519-M521");
        CollectionAssert.Contains(milestoneIds, "M522-M524");
        CollectionAssert.Contains(milestoneIds, "M525-M527");
        CollectionAssert.Contains(milestoneIds, "M528-M530");
        CollectionAssert.Contains(milestoneIds, "M531-M533");
        AssertSafeOutput(archiveSerializer.Serialize(review));
    }

    [TestMethod]
    public void AssignmentArchiveReview_CanArchiveAsGovernanceBaselineOnly()
    {
        var review = CreateArchiveReview();

        Assert.IsTrue(review.ArchiveStatus.CanArchiveAsGovernanceBaseline);
    }

    [TestMethod]
    public void AssignmentArchiveReview_CannotBeUsedAsRuntimeBaseline()
    {
        var review = CreateArchiveReview();

        Assert.IsFalse(review.ArchiveStatus.CanUseAsRuntimeBaseline);
        Assert.IsFalse(review.CanAuthorizeExecution);
    }

    [TestMethod]
    public void AssignmentArchiveReview_CannotBeUsedAsPlannerImplementation()
    {
        var review = CreateArchiveReview();

        Assert.IsFalse(review.ArchiveStatus.CanUseAsPlannerImplementation);
        Assert.IsFalse(review.CanCallPlanner);
    }

    [TestMethod]
    public void AssignmentArchiveReview_CannotBeUsedAsLlmPromptSource()
    {
        var review = CreateArchiveReview();

        Assert.IsFalse(review.ArchiveStatus.CanUseAsLlmPromptSource);
        Assert.IsFalse(review.CanCallLlm);
    }

    [TestMethod]
    public void AssignmentArchiveReview_CannotBeUsedAsFilesystemAuthority()
    {
        var review = CreateArchiveReview();

        Assert.IsFalse(review.ArchiveStatus.CanUseAsFilesystemAuthority);
        Assert.IsFalse(review.CanAccessFilesystem);
        Assert.IsFalse(review.CanCallCloud);
    }

    // --- M536: Next Phase ADR ---

    [TestMethod]
    public void NextPhaseAdr_DeclaresNoDirectMoveToRealProjectUnderstanding()
    {
        var adr = CreateAdr();

        Assert.IsFalse(adr.RealProjectUnderstandingAllowed);
        CollectionAssert.Contains(adr.Decisions.ToList(), NodalOsNextPhaseAdrDecision.NoDirectMoveToRealProjectUnderstanding);
        AssertSafeOutput(adrSerializer.Serialize(adr));
    }

    [TestMethod]
    public void NextPhaseAdr_DeclaresRealScanAndFilesystemAndIndexingAndEmbeddingsAndLlmContextAndCloudBlocked()
    {
        var adr = CreateAdr();

        Assert.IsFalse(adr.RealScanAllowed);
        Assert.IsFalse(adr.FilesystemReadAllowed);
        Assert.IsFalse(adr.FilesystemHashAllowed);
        Assert.IsFalse(adr.GitCommandsAllowed);
        Assert.IsFalse(adr.EmbeddingsAllowed);
        Assert.IsFalse(adr.IndexingAllowed);
        Assert.IsFalse(adr.LlmContextBuildAllowed);
        Assert.IsFalse(adr.PromptGenerationAllowed);
        Assert.IsFalse(adr.LlmProviderCallAllowed);
        Assert.IsFalse(adr.CloudSyncAllowed);
        CollectionAssert.Contains(adr.Decisions.ToList(), NodalOsNextPhaseAdrDecision.RealScanBlockedUntilFutureMilestone);
        CollectionAssert.Contains(adr.Decisions.ToList(), NodalOsNextPhaseAdrDecision.LlmContextBuildBlockedUntilFutureMilestone);
    }

    [TestMethod]
    public void NextPhaseAdr_DeclaresAssignmentPlannerOutputsAreRefsAndGovernanceContextOnly()
    {
        var adr = CreateAdr();

        CollectionAssert.Contains(adr.Decisions.ToList(), NodalOsNextPhaseAdrDecision.AssignmentOutputsAreRefsAndGovernanceContextOnly);
        CollectionAssert.Contains(adr.Decisions.ToList(), NodalOsNextPhaseAdrDecision.MockHistoryIsNotSourceOfTruth);
    }

    [TestMethod]
    public void NextPhaseAdr_DeclaresLlmRequiresGoverningPolicyBeforeReal()
    {
        var adr = CreateAdr();

        CollectionAssert.Contains(adr.Decisions.ToList(), NodalOsNextPhaseAdrDecision.ByokAndProviderPolicyRequiredBeforeLlm);
        Assert.IsTrue(adr.RequiredNextMilestones.Any(m => m.BlocksLlmContext));
    }

    [TestMethod]
    public void NextPhaseAdr_DeclaresFilesystemRequiresPathJailAndConsentAndAuditBeforeReal()
    {
        var adr = CreateAdr();

        CollectionAssert.Contains(adr.Decisions.ToList(), NodalOsNextPhaseAdrDecision.PathJailAndConsentRequiredBeforeFilesystem);
        CollectionAssert.Contains(adr.Decisions.ToList(), NodalOsNextPhaseAdrDecision.SeparateAuditRequiredBeforeRuntime);
        Assert.IsTrue(adr.RequiredNextMilestones.Any(m => m.BlocksRealScan));
    }

    // --- Static HTML Preview ---

    [TestMethod]
    public void StaticHtmlPreview_ContainsNoExternalScriptsCdnNetworkCallsOrTelemetry()
    {
        var preview = NodalOsProjectUnderstandingPreconditionsFixtures.Preview();

        Assert.IsTrue(preview.Deterministic);
        Assert.IsFalse(preview.ContainsExternalResource);
        Assert.IsFalse(preview.ContainsScript);
        Assert.IsFalse(preview.ContainsInlineData);
        Assert.IsFalse(preview.ContainsNetworkCall);
        Assert.IsFalse(preview.ContainsAnalyticsBeacon);
        AssertDoesNotContain(preview.HtmlRedacted, "<script");
        AssertDoesNotContain(preview.HtmlRedacted, "https://");
        AssertDoesNotContain(preview.HtmlRedacted, "http://");
        AssertSafeOutput(preview.HtmlRedacted);
    }

    // --- Boundary / Guardrail ---

    [TestMethod]
    public void Boundary_NewM534M536Files_DoNotReferenceBrowserExecutorCdp()
    {
        var source = NewSource();

        AssertDoesNotContain(source, "OneBrain." + "BrowserExecutor" + ".Cdp");
    }

    [TestMethod]
    public void Boundary_NewM534M536Files_DoNotIntroduceDangerousApis()
    {
        var source = NewSource();

        AssertDoesNotContain(source, "Http" + "Client");
        AssertDoesNotContain(source, "Client" + "WebSocket");
        AssertDoesNotContain(source, "Process" + ".Start");
        AssertDoesNotContain(source, "sche" + "duler");
        AssertDoesNotContain(source, "wor" + "ker");
        AssertDoesNotContain(source, "que" + "ue");
        AssertDoesNotContain(source, "tele" + "metry");
        AssertDoesNotContain(source, "File.Write");
        AssertDoesNotContain(source, "File.Read");
        AssertDoesNotContain(source, "Directory.");
        AssertDoesNotContain(source, "Background" + "Service");
        AssertDoesNotContain(source, "Task.Run");
    }

    [TestMethod]
    public void Boundary_NewM534M536Files_DoNotContainSensitiveMarkersOrForbiddenNames()
    {
        var source = NewSource();

        AssertSafeOutput(source);
    }

    // --- Artifacts ---

    [TestMethod]
    public void Artifacts_M534M536_ExistAndPassSafetyChecks()
    {
        var preconditionsJson = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m536", "project-understanding-preconditions-summary.json"));
        var archiveJson = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m536", "assignment-archive-review.json"));
        var archiveMd = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m536", "assignment-archive-review.md"));
        var adrJson = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m536", "next-phase-adr-summary.json"));
        var html = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m536", "project-understanding-preconditions-preview.html"));

        AssertContains(preconditionsJson, "\"readyForRealProjectUnderstanding\": false");
        AssertContains(preconditionsJson, "\"readyForFilesystemScan\": false");
        AssertContains(archiveJson, "\"canArchiveAsGovernanceBaseline\": true");
        AssertContains(archiveJson, "\"canUseAsRuntimeBaseline\": false");
        AssertContains(adrJson, "\"realProjectUnderstandingAllowed\": false");
        AssertContains(adrJson, "\"realScanAllowed\": false");
        AssertContains(archiveMd, "NODAL OS");
        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "cdn");
        AssertSafeOutput(preconditionsJson + archiveJson + archiveMd + adrJson + html);
    }

    // --- Helpers ---

    private NodalOsProjectUnderstandingPreconditions CreatePreconditions() =>
        NodalOsProjectUnderstandingPreconditionsFixtures.Preconditions();

    private NodalOsProjectUnderstandingReadinessResult CreateReadiness() =>
        NodalOsProjectUnderstandingPreconditionsFixtures.Readiness();

    private NodalOsAssignmentArchiveReview CreateArchiveReview() =>
        NodalOsAssignmentArchiveReviewFixtures.ArchiveReview();

    private NodalOsNextPhaseAdr CreateAdr() =>
        NodalOsNextPhaseAdrFixtures.Adr();

    private static string NewSource()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsProjectUnderstandingPreconditionsContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsAssignmentArchiveReviewContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsNextPhaseAdrContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsProjectUnderstandingPreconditionsServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsAssignmentArchiveReviewServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsNextPhaseAdrServices.cs")
        };

        return string.Join(Environment.NewLine, files.Select(System.IO.File.ReadAllText));
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
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !System.IO.File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
            current = current.Parent;

        return current?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
