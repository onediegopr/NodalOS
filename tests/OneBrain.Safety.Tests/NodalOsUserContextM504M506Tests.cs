using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsUserContextM504M506Tests
{
    private static readonly string[] ForbiddenNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SensitiveMarkers = ["Bearer ", "Authorization:", "Cookie:", "password", "secret", "api_key", "access_token", "refresh_token", "id_token", "private key"];
    private readonly NodalOsUserContextService service = new();
    private readonly NodalOsUserContextValidator validator = new();
    private readonly NodalOsUserContextJsonSerializer serializer = new();

    [TestMethod]
    public void Capture_CreatesUserSummary()
    {
        var capture = service.CreateCapture(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace(), NodalOsUserContextCaptureType.UserSummary);

        Assert.AreEqual(NodalOsUserContextCaptureType.UserSummary, capture.CaptureType);
        Assert.IsTrue(validator.ValidateCapture(capture).IsValid);
    }

    [TestMethod]
    public void Capture_CreatesRequiredCaptureTypes()
    {
        var types = new[]
        {
            NodalOsUserContextCaptureType.UserTechStack,
            NodalOsUserContextCaptureType.UserFolderStructureHint,
            NodalOsUserContextCaptureType.UserImportantFileHint,
            NodalOsUserContextCaptureType.UserConstraint,
            NodalOsUserContextCaptureType.UserRiskNote,
            NodalOsUserContextCaptureType.UserBusinessContext,
            NodalOsUserContextCaptureType.UserArchitectureNote,
            NodalOsUserContextCaptureType.UserTodo,
            NodalOsUserContextCaptureType.UserUnknown
        };

        foreach (var type in types)
        {
            var capture = service.CreateCapture(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace(), type);

            Assert.AreEqual(type, capture.CaptureType);
            Assert.IsTrue(validator.ValidateCapture(capture).IsValid, type.ToString());
        }
    }

    [TestMethod]
    public void Capture_FolderAndImportantFileHintsDoNotVerifyOrReadFilesystem()
    {
        var folder = service.CreateCapture(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace(), NodalOsUserContextCaptureType.UserFolderStructureHint);
        var important = service.CreateCapture(NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace(), NodalOsUserContextCaptureType.UserImportantFileHint);

        Assert.IsFalse(folder.FilesystemVerificationAllowed);
        Assert.IsFalse(folder.ReadsFiles);
        Assert.IsFalse(important.FilesystemVerificationAllowed);
        Assert.IsFalse(important.ReadsFiles);
    }

    [TestMethod]
    public void Capture_DeclaresUserProvidedProvenanceAndBoundary()
    {
        var capture = NodalOsUserContextFixtures.UserSummaryCapture();

        AssertContains(capture.DeclaredProvenanceRedacted, "user-provided");
        Assert.IsTrue(capture.UserProvidedOnly);
        Assert.IsNotNull(capture.BoundaryDecision);
        Assert.IsTrue(capture.BoundaryDecision.SafeForDisplay);
    }

    [TestMethod]
    public void Capture_BlocksSensitiveRawAndCredentialContent()
    {
        var invalid = service.CreateCapture(
            NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace(),
            NodalOsUserContextCaptureType.UserSummary,
            "Cookie: session=abc raw payload private key",
            NodalOsContextSensitivityLevel.RawPayloadBlocked);

        Assert.IsFalse(invalid.ValidationResult.IsValid);
        Assert.IsFalse(invalid.BoundaryDecision.SafeForDisplay);
    }

    [TestMethod]
    public void Capture_DoesNotUseProviderCloudGitVectorsPromptOrFilesystem()
    {
        var capture = NodalOsUserContextFixtures.UserSummaryCapture();

        Assert.IsFalse(capture.ReadsFiles);
        Assert.IsFalse(capture.UsesGit);
        Assert.IsFalse(capture.CreatesVectorIndex);
        Assert.IsFalse(capture.CallsLlmProvider);
        Assert.IsFalse(capture.CreatesPrompt);
        Assert.IsFalse(capture.CreatesRealProjectUnderstanding);
        Assert.IsFalse(capture.CanAuthorizeExecution);
        Assert.IsFalse(capture.ChangesWorkspaceProductively);
    }

    [TestMethod]
    public void Capture_SerializationIsSafe()
    {
        var json = serializer.SerializeCapture(NodalOsUserContextFixtures.UserSummaryCapture());

        AssertSafeSerialized(json);
    }

    [TestMethod]
    public void ReviewCard_CreatesSafeForDisplayAndExport()
    {
        var capture = NodalOsUserContextFixtures.UserSummaryCapture();
        var display = service.CreateReviewCard(capture, NodalOsContextReviewCardStatus.SafeForDisplay);
        var export = service.CreateReviewCard(capture, NodalOsContextReviewCardStatus.SafeForExport);

        Assert.AreEqual(NodalOsContextReviewCardStatus.SafeForDisplay, display.Status);
        Assert.AreEqual(NodalOsContextReviewCardStatus.SafeForExport, export.Status);
        Assert.IsTrue(validator.ValidateReviewCard(display).IsValid);
        Assert.IsTrue(validator.ValidateReviewCard(export).IsValid);
    }

    [TestMethod]
    public void ReviewCard_CreatesRequiresReviewAndBlockedStatuses()
    {
        var capture = NodalOsUserContextFixtures.UserSummaryCapture();
        var statuses = new[]
        {
            NodalOsContextReviewCardStatus.RequiresReview,
            NodalOsContextReviewCardStatus.BlockedSensitive,
            NodalOsContextReviewCardStatus.BlockedSecret,
            NodalOsContextReviewCardStatus.BlockedRawPayload,
            NodalOsContextReviewCardStatus.DiscardedMock,
            NodalOsContextReviewCardStatus.Draft
        };

        foreach (var status in statuses)
        {
            var card = service.CreateReviewCard(capture, status);

            Assert.AreEqual(status, card.Status);
            Assert.IsTrue(card.UserOptionsAreNoOp);
        }
    }

    [TestMethod]
    public void ReviewCard_ShowsLabelsUsageMissingInfoAndQuestions()
    {
        var card = NodalOsUserContextFixtures.SafeReviewCard();

        Assert.IsFalse(string.IsNullOrWhiteSpace(card.ProvenanceLabelRedacted));
        Assert.IsFalse(string.IsNullOrWhiteSpace(card.ConfidenceLabelRedacted));
        Assert.IsFalse(string.IsNullOrWhiteSpace(card.FreshnessLabelRedacted));
        Assert.IsFalse(string.IsNullOrWhiteSpace(card.SensitivityLabelRedacted));
        Assert.IsTrue(card.AllowedUsageChipsRedacted.Count > 0);
        Assert.IsTrue(card.DisallowedUsageChipsRedacted.Count > 0);
        Assert.IsTrue(card.MissingInformationRedacted.Count > 0);
        Assert.IsTrue(card.QuestionsForUserRedacted.Count > 0);
    }

    [TestMethod]
    public void ReviewCard_IsNoOpNonAuthoritativeNoPrompt()
    {
        var card = NodalOsUserContextFixtures.SafeReviewCard();

        Assert.IsTrue(card.UserOptionsAreNoOp);
        Assert.IsFalse(card.CanAuthorizeExecution);
        Assert.IsFalse(card.RuntimeExecutionAllowed);
        Assert.IsFalse(card.CallsLlmProvider);
        Assert.IsFalse(card.CreatesPrompt);
        Assert.IsFalse(card.MutatesRuntime);
    }

    [TestMethod]
    public void ReviewCard_SerializationIsSafe()
    {
        var json = serializer.SerializeReviewCard(NodalOsUserContextFixtures.SafeReviewCard());

        AssertSafeSerialized(json);
    }

    [TestMethod]
    public void EvidenceLink_CreatesRequiredLinkTypesRefOnly()
    {
        var capture = NodalOsUserContextFixtures.UserSummaryCapture();
        var card = service.CreateReviewCard(capture);
        var types = new[]
        {
            NodalOsContextEvidenceLinkType.SupportsContext,
            NodalOsContextEvidenceLinkType.UserClaimReference,
            NodalOsContextEvidenceLinkType.ClarificationNeeded,
            NodalOsContextEvidenceLinkType.ContradictionSuspected,
            NodalOsContextEvidenceLinkType.RelatedTimelineEvent,
            NodalOsContextEvidenceLinkType.RelatedEvidenceRef,
            NodalOsContextEvidenceLinkType.FutureVerificationNeeded
        };

        foreach (var type in types)
        {
            var link = service.CreateEvidenceLink(capture, card, capture.EvidenceRefs[0], type);

            Assert.AreEqual(type, link.LinkType);
            Assert.AreEqual(NodalOsContextEvidenceLinkStatus.LinkedRefOnly, link.LinkStatus);
            Assert.IsTrue(link.RefOnly);
            Assert.IsTrue(validator.ValidateEvidenceLink(link, capture.EvidenceRefs[0]).IsValid, type.ToString());
        }
    }

    [TestMethod]
    public void EvidenceLink_BlocksUnsafeEvidence()
    {
        var link = service.CreateUnsafeEvidenceLink(NodalOsUserContextFixtures.UserSummaryCapture());

        Assert.AreEqual(NodalOsContextEvidenceLinkStatus.BlockedUnsafeEvidence, link.LinkStatus);
        Assert.IsFalse(link.ValidationResult.IsValid);
        Assert.IsTrue(link.IncludesRawPayload);
    }

    [TestMethod]
    public void EvidenceLink_DoesNotIncludeRawPayloadsOrVerifyTruth()
    {
        var link = NodalOsUserContextFixtures.EvidenceLink();

        Assert.IsFalse(link.IncludesRawPayload);
        Assert.IsFalse(link.IncludesScreenshotInline);
        Assert.IsFalse(link.IncludesDomRaw);
        Assert.IsFalse(link.IncludesNetworkRaw);
        Assert.IsFalse(link.ReadsFiles);
        Assert.IsFalse(link.ValidatesRealContent);
        Assert.IsFalse(link.ConvertsClaimToAuthoritativeTruth);
        Assert.IsFalse(link.CanAuthorizeExecution);
    }

    [TestMethod]
    public void EvidenceLink_SerializationIsSafe()
    {
        var json = serializer.SerializeEvidenceLink(NodalOsUserContextFixtures.EvidenceLink());

        AssertSafeSerialized(json);
    }

    [TestMethod]
    public void Boundary_NewUserContextFiles_DoNotReferenceRuntimePrimitives()
    {
        var source = NewUserContextSource();

        AssertDoesNotContain(source, "OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(source, "HttpClient");
        AssertDoesNotContain(source, "ClientWebSocket");
        AssertDoesNotContain(source, "Process.Start");
        AssertDoesNotContain(source, "System.Diagnostics.Process");
        AssertDoesNotContain(source, "BackgroundService");
        AssertDoesNotContain(source, "Task.Run");
        AssertDoesNotContain(source, "ExecuteAsync");
        AssertDoesNotContain(source, "OpenAI");
        AssertDoesNotContain(source, "TelemetryClient");
        AssertDoesNotContain(source, "AnalyticsClient");
        AssertDoesNotContain(source, "File.Write");
        AssertDoesNotContain(source, "File.Read");
        AssertDoesNotContain(source, "Directory.");
        AssertDoesNotContain(source, "Process ");
    }

    [TestMethod]
    public void ExistingSafetyContinuity_PreviousLayersRemainSafe()
    {
        var readiness = NodalOsWorkspaceReadinessFixtures.ReadyForUserProvidedContextIntake();
        var metadata = NodalOsWorkspaceMetadataFixtures.MockIndex();
        var storage = NodalOsWorkspaceMissionFixtures.StorageWithDraftAndActive();
        var shell = NodalOsMissionControlShellFixtures.ShellPreview();

        Assert.IsFalse(readiness.RuntimeExecutionAllowed);
        Assert.IsTrue(metadata.MockOnly);
        Assert.AreEqual(2, storage.ListWorkspaces().Count);
        Assert.IsFalse(shell.CanAuthorizeExecution);
        Assert.IsFalse(metadata.RealFilesystemScanAllowed);
    }

    [TestMethod]
    public void ArtifactMarksUserContextReviewLinkingReady()
    {
        var artifact = File.ReadAllText(PathFor("artifacts", "agent-operations", "m506", "user-context-capture-review-linking-summary.json"));

        AssertContains(artifact, "\"userProvidedContextCapture\": true");
        AssertContains(artifact, "\"contextReviewCards\": true");
        AssertContains(artifact, "\"contextEvidenceLinking\": true");
        AssertContains(artifact, "\"contextEvidenceLinkingRefOnly\": true");
        AssertContains(artifact, "\"promptCreationIntroduced\": false");
    }

    private static void AssertSafeSerialized(string text)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(text, marker);
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(text, name);
    }

    private static string NewUserContextSource() =>
        string.Join(Environment.NewLine,
            new[]
            {
                PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsUserContextContracts.cs"),
                PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsUserContextServices.cs")
            }.Select(File.ReadAllText));

    private static void AssertContains(string text, string expected) =>
        Assert.IsTrue(text.Contains(expected, StringComparison.OrdinalIgnoreCase), expected);

    private static void AssertDoesNotContain(string text, string unexpected) =>
        Assert.IsFalse(text.Contains(unexpected, StringComparison.Ordinal), unexpected);

    private static string PathFor(params string[] parts)
    {
        var root = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(new[] { root }.Concat(parts).ToArray());
            if (File.Exists(candidate) || Directory.Exists(candidate))
                return candidate;
            var parent = Directory.GetParent(root);
            if (parent is null)
                break;
            root = parent.FullName;
        }

        return Path.Combine(new[] { AppContext.BaseDirectory }.Concat(parts).ToArray());
    }
}
