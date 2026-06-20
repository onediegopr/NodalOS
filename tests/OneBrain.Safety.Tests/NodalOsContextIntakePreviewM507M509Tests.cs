using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsContextIntakePreviewM507M509Tests
{
    private static readonly string[] ForbiddenNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SensitiveMarkers = ["Bearer ", "Authorization:", "Cookie:", "password", "secret", "api_key", "access_token", "refresh_token", "id_token", "private key"];
    private readonly NodalOsContextIntakePreviewService service = new();
    private readonly NodalOsContextIntakePreviewJsonSerializer serializer = new();

    [TestMethod]
    public void Preview_ShowsCapturesCardsEvidenceCountsAndQuestions()
    {
        var (preview, _, _) = NodalOsContextIntakePreviewFixtures.SafeContextIntakeSet();

        Assert.AreEqual(1, preview.ContextCaptures.Count);
        Assert.AreEqual(1, preview.ReviewCards.Count);
        Assert.AreEqual(1, preview.EvidenceLinks.Count);
        Assert.AreEqual(1, preview.SafeCount);
        Assert.AreEqual(0, preview.BlockedCount);
        Assert.AreEqual(0, preview.RequiresReviewCount);
        Assert.IsTrue(preview.MissingInformationRedacted.Count > 0);
        Assert.IsTrue(preview.QuestionsForUserRedacted.Count > 0);
    }

    [TestMethod]
    public void Preview_ShowsLabelsUsageDisclosuresAndGuardrails()
    {
        var (preview, _, _) = NodalOsContextIntakePreviewFixtures.SafeContextIntakeSet();

        Assert.IsTrue(preview.ProvenanceLabelsRedacted.Contains("user-provided"));
        Assert.IsTrue(preview.ConfidenceLabelsRedacted.Count > 0);
        Assert.IsTrue(preview.FreshnessLabelsRedacted.Count > 0);
        Assert.IsTrue(preview.SensitivityLabelsRedacted.Count > 0);
        Assert.IsTrue(preview.AllowedUsageChipsRedacted.Count > 0);
        Assert.IsTrue(preview.DisallowedUsageChipsRedacted.Count > 0);
        AssertContains(preview.NoFilesReadDisclosureRedacted, "No files were read");
        AssertContains(preview.NoLlmDisclosureRedacted, "No LLM");
        AssertContains(preview.NoPromptCreationDisclosureRedacted, "No provider prompt");
        AssertContains(preview.NoRealProjectUnderstandingDisclosureRedacted, "Real project understanding has not started");
        Assert.IsTrue(preview.GuardrailExplainersRedacted.Count > 0);
    }

    [TestMethod]
    public void Preview_RenderedHtmlIsStaticRedactedAndUsesNodalOs()
    {
        var (preview, summary, report) = NodalOsContextIntakePreviewFixtures.SafeContextIntakeSet();
        var html = service.RenderStaticHtml(preview, summary, report);

        AssertContains(html, "NODAL OS");
        AssertContains(html, "Context Intake UI Preview");
        AssertContains(html, "Read-only preview");
        AssertContains(html, "No runtime");
        AssertContains(html, "No LLM");
        AssertContains(html, "No prompt");
        AssertContains(html, "No files were read");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Preview_SerializationIsSafeAndNonAuthoritative()
    {
        var (preview, _, _) = NodalOsContextIntakePreviewFixtures.SafeContextIntakeSet();
        var json = serializer.SerializePreview(preview);

        Assert.IsTrue(preview.StaticPreviewOnly);
        Assert.IsTrue(preview.ReadOnlyPreview);
        Assert.IsTrue(preview.UserProvidedAndUnverified);
        Assert.IsFalse(preview.CanAuthorizeExecution);
        Assert.IsFalse(preview.RuntimeExecutionAllowed);
        Assert.IsFalse(preview.CallsLlmProvider);
        Assert.IsFalse(preview.CreatesPrompt);
        Assert.IsFalse(preview.ReadsFiles);
        Assert.IsFalse(preview.VerifiesPaths);
        Assert.IsFalse(preview.MutatesProductiveState);
        AssertSafeOutput(json);
    }

    [TestMethod]
    public void ValidationSummary_CountsCapturesBlockedReviewMissingQuestionsAndLinks()
    {
        var workspace = NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace();
        var userContext = new NodalOsUserContextService();
        var safeCapture = userContext.CreateCapture(workspace, NodalOsUserContextCaptureType.UserSummary);
        var blockedCapture = userContext.CreateCapture(workspace, NodalOsUserContextCaptureType.UserRiskNote, "redacted blocked context", NodalOsContextSensitivityLevel.RawPayloadBlocked);
        var safeCard = userContext.CreateReviewCard(safeCapture, NodalOsContextReviewCardStatus.SafeForDisplay);
        var reviewCard = userContext.CreateReviewCard(safeCapture, NodalOsContextReviewCardStatus.RequiresReview);
        var blockedCard = userContext.CreateReviewCard(blockedCapture, NodalOsContextReviewCardStatus.BlockedRawPayload);
        var link = userContext.CreateEvidenceLink(safeCapture, safeCard, safeCapture.EvidenceRefs[0], NodalOsContextEvidenceLinkType.UserClaimReference);

        var summary = service.CreateValidationSummary(workspace, [safeCapture, blockedCapture], [safeCard, reviewCard, blockedCard], [link]);

        Assert.AreEqual(2, summary.TotalCaptures);
        Assert.AreEqual(1, summary.SafeCaptures);
        Assert.AreEqual(1, summary.BlockedCaptures);
        Assert.AreEqual(1, summary.RequiresReviewCaptures);
        Assert.IsTrue(summary.BlockedByReasonRedacted.ContainsKey(nameof(NodalOsContextReviewCardStatus.BlockedRawPayload)));
        Assert.IsTrue(summary.MissingInfoCount > 0);
        Assert.IsTrue(summary.QuestionsCount > 0);
        Assert.AreEqual(1, summary.EvidenceLinkedCount);
        Assert.AreEqual(1, summary.UnverifiedClaimsCount);
        Assert.AreEqual(1, summary.RawPayloadBlockedCount);
        Assert.AreEqual(0, summary.CredentialBlockedCount);
    }

    [TestMethod]
    public void ValidationSummary_ProducesHumanTechnicalWarningsAndRemainsNonAuthoritative()
    {
        var (_, summary, _) = NodalOsContextIntakePreviewFixtures.SafeContextIntakeSet();
        var json = serializer.SerializeValidationSummary(summary);

        Assert.IsFalse(string.IsNullOrWhiteSpace(summary.HumanReadableSummaryRedacted));
        Assert.IsFalse(string.IsNullOrWhiteSpace(summary.TechnicalSummaryRedacted));
        Assert.IsTrue(summary.WarningsRedacted.Count > 0);
        Assert.IsTrue(summary.RecommendationsRedacted.Count > 0);
        Assert.IsTrue(summary.NonAuthoritative);
        Assert.IsFalse(summary.CanAuthorizeExecution);
        Assert.IsFalse(summary.ConvertsClaimsToTruth);
        Assert.IsFalse(summary.CallsLlmProvider);
        Assert.IsFalse(summary.CreatesPrompt);
        Assert.IsFalse(summary.ReadsFiles);
        AssertSafeOutput(json);
    }

    [TestMethod]
    public void ReadinessReport_CreatesAllRequiredStates()
    {
        var states = Enum.GetValues<NodalOsProjectUnderstandingReadinessState>();

        foreach (var state in states)
        {
            var report = service.CreateReadinessReport(
                NodalOsWorkspaceFixtures.ActiveReadOnlyWorkspace(),
                NodalOsWorkspaceReadinessFixtures.ReadyForUserProvidedContextIntake(),
                NodalOsContextIntakePreviewFixtures.SafeContextIntakeSet().Summary,
                [],
                state);

            Assert.AreEqual(state, report.State);
            Assert.IsTrue(report.ReadinessOnly);
            Assert.IsFalse(report.StartsRealProjectUnderstanding);
        }
    }

    [TestMethod]
    public void ReadinessReport_IncludesBlockersWarningsStepsAndExplainsNoRealUnderstanding()
    {
        var (_, _, report) = NodalOsContextIntakePreviewFixtures.SafeContextIntakeSet();

        Assert.AreEqual(NodalOsProjectUnderstandingReadinessState.ReadyForUserProvidedContextReview, report.State);
        Assert.IsTrue(report.WarningsRedacted.Count > 0);
        Assert.IsTrue(report.NextSafeStepsRedacted.Count > 0);
        AssertContains(report.UserFacingExplanationRedacted, "no real project understanding");
        AssertContains(report.TechnicalExplanationRedacted, "without scanning");
        Assert.IsFalse(report.ScansFilesystem);
        Assert.IsFalse(report.ReadsFiles);
        Assert.IsFalse(report.UsesEmbeddings);
        Assert.IsFalse(report.CallsLlmProvider);
        Assert.IsFalse(report.CreatesPrompt);
        Assert.IsFalse(report.CanAuthorizeExecution);
    }

    [TestMethod]
    public void ReadinessReport_SerializationIsSafe()
    {
        var (_, _, report) = NodalOsContextIntakePreviewFixtures.SafeContextIntakeSet();
        var json = serializer.SerializeReadinessReport(report);

        AssertSafeOutput(json);
    }

    [TestMethod]
    public void Boundary_NewContextIntakePreviewFiles_DoNotReferenceForbiddenRuntimePrimitives()
    {
        var source = NewSource();

        AssertDoesNotContain(source, "OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(source, "HttpClient");
        AssertDoesNotContain(source, "ClientWebSocket");
        AssertDoesNotContain(source, "Process.Start");
        AssertDoesNotContain(source, "System.Diagnostics.Process");
        AssertDoesNotContain(source, "BackgroundService");
        AssertDoesNotContain(source, "Task.Run");
        AssertDoesNotContain(source, "IHostedService");
        AssertDoesNotContain(source, "QueueClient");
        AssertDoesNotContain(source, "RecorderRuntime");
        AssertDoesNotContain(source, "ReplayRuntime");
        AssertDoesNotContain(source, "DslParserRuntime");
        AssertDoesNotContain(source, "ProviderClient");
        AssertDoesNotContain(source, "CloudSync");
        AssertDoesNotContain(source, "File.Write");
        AssertDoesNotContain(source, "File.Read");
        AssertDoesNotContain(source, "Directory.");
    }

    [TestMethod]
    public void ExistingSafetyContinuity_PreviousLayersRemainSafe()
    {
        var userCapture = NodalOsUserContextFixtures.UserSummaryCapture();
        var readiness = NodalOsWorkspaceReadinessFixtures.ReadyForUserProvidedContextIntake();
        var shell = NodalOsMissionControlShellFixtures.ShellPreview();

        Assert.IsFalse(userCapture.CanAuthorizeExecution);
        Assert.IsFalse(userCapture.CallsLlmProvider);
        Assert.IsFalse(readiness.RuntimeExecutionAllowed);
        Assert.IsFalse(shell.CanAuthorizeExecution);
        Assert.IsTrue(shell.ReadOnlyUi);
    }

    [TestMethod]
    public void Artifacts_MarkContextIntakePreviewReadinessReady()
    {
        var artifact = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m509", "context-intake-preview-readiness-summary.json"));
        var html = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m509", "context-intake-ui-preview.html"));

        AssertContains(artifact, "\"contextIntakeUiPreview\": true");
        AssertContains(artifact, "\"contextValidationSummary\": true");
        AssertContains(artifact, "\"projectUnderstandingReadinessReport\": true");
        AssertContains(artifact, "\"promptCreationIntroduced\": false");
        AssertContains(html, "NODAL OS");
        AssertSafeOutput(html);
    }

    private static void AssertSafeOutput(string text)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(text, marker);
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(text, name);
    }

    private static string NewSource() =>
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsContextIntakePreviewContracts.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsContextIntakePreviewServices.cs"));

    private static string PathFor(params string[] parts) =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", Path.Combine(parts));

    private static void AssertContains(string text, string expected) =>
        StringAssert.Contains(text, expected);

    private static void AssertDoesNotContain(string text, string unexpected) =>
        Assert.IsFalse(text.Contains(unexpected, StringComparison.Ordinal), $"Unexpected marker found: {unexpected}");
}
