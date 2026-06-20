using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
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
public sealed class NodalOsPromptGovernanceM516M518Tests
{
    private static readonly string[] ForbiddenNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SensitiveMarkers = ["Bearer ", "Authorization:", "Cookie:", "password", "secret", "api_key", "access_token", "refresh_token", "id_token", "private key", "sk-", "connection string"];
    private readonly NodalOsPromptGovernanceService service = new();
    private readonly NodalOsPromptGovernanceJsonSerializer serializer = new();

    [TestMethod]
    public void PromptGovernance_CreateAllRequiredStates()
    {
        foreach (var state in Enum.GetValues<NodalOsPromptGovernanceState>())
        {
            var policy = service.CreatePromptGovernancePolicy(state);

            Assert.AreEqual(state, policy.PromptConstructionStatus);
            Assert.IsTrue(policy.RequiresSafeContextBoundary);
            Assert.IsTrue(policy.RequiresRedaction);
            Assert.IsTrue(policy.RequiresConsent);
            Assert.IsTrue(policy.RequiresProvenanceConfidenceFreshness);
            Assert.IsTrue(policy.RequiresBudgetGuardrails);
            Assert.IsTrue(policy.RequiresByokPolicy);
            Assert.IsFalse(policy.GeneratesFinalPromptText);
            Assert.IsFalse(policy.CallsProvider);
            Assert.IsFalse(policy.CallsLlmProvider);
            Assert.IsFalse(policy.CallsCloud);
            Assert.IsFalse(policy.CanAuthorizeExecution);
            AssertSafeOutput(serializer.SerializePromptGovernance(policy));
        }
    }

    [TestMethod]
    public void PromptGovernance_DeclaresRequiredDisclosuresAndPurposePolicy()
    {
        var policy = NodalOsPromptGovernanceFixtures.PromptPreviewPolicy();

        CollectionAssert.Contains(policy.DisclosuresRedacted.ToList(), "Prompt construction real is not implemented.");
        CollectionAssert.Contains(policy.DisclosuresRedacted.ToList(), "No context was sent to a model.");
        CollectionAssert.Contains(policy.DisclosuresRedacted.ToList(), "No provider call was made.");
        CollectionAssert.Contains(policy.DisclosuresRedacted.ToList(), "Future prompt requires BYOK, consent, policy and budget guardrails.");
        Assert.AreEqual(NodalOsPromptPurpose.ProjectUnderstandingFuture, policy.AllowedPromptPurpose);
        CollectionAssert.Contains(policy.DeniedPromptPurposes.ToList(), NodalOsPromptPurpose.Unknown);
        Assert.IsTrue(policy.AllowedContextRefsRedacted.Count > 0);
        Assert.IsTrue(policy.DeniedContextRefsRedacted.Count > 0);
        Assert.IsTrue(policy.EvidenceRefs.Count > 0);
        Assert.IsTrue(policy.TimelineRefs.Count > 0);
        Assert.IsTrue(policy.GuardrailRefs.Count > 0);
    }

    [TestMethod]
    public void BudgetGuardrails_CreateAllRequiredStatuses()
    {
        foreach (var status in Enum.GetValues<NodalOsBudgetGuardrailStatus>())
        {
            var draft = service.CreateBudgetGuardrailsDraft(status);

            Assert.AreEqual(status, draft.BudgetStatus);
            Assert.IsTrue(draft.DraftOnly);
            Assert.IsFalse(draft.PerformsTokenCounting);
            Assert.IsFalse(draft.PerformsLiveCostLookup);
            Assert.IsFalse(draft.CallsProvider);
            Assert.IsFalse(draft.PerformsBilling);
            Assert.IsFalse(draft.CallsCloud);
            Assert.IsFalse(draft.UsesProviderSdk);
            Assert.IsFalse(draft.SendsNetworkRequest);
            Assert.IsFalse(draft.CallsLlmProvider);
            Assert.IsFalse(draft.CanAuthorizeExecution);
            AssertSafeOutput(serializer.SerializeBudgetGuardrails(draft));
        }
    }

    [TestMethod]
    public void BudgetGuardrails_IncludePlaceholdersConfirmationStopCancelAndEvidenceTimeline()
    {
        var draft = NodalOsPromptGovernanceFixtures.BudgetDraft();

        Assert.IsFalse(string.IsNullOrWhiteSpace(draft.MaxSpendPlaceholderRedacted));
        Assert.IsFalse(string.IsNullOrWhiteSpace(draft.MaxTokensPlaceholderRedacted));
        Assert.IsFalse(string.IsNullOrWhiteSpace(draft.MaxCallsPlaceholderRedacted));
        Assert.IsFalse(string.IsNullOrWhiteSpace(draft.MaxRetriesPlaceholderRedacted));
        Assert.IsFalse(string.IsNullOrWhiteSpace(draft.MaxConcurrentRequestsPlaceholderRedacted));
        Assert.IsTrue(draft.RequiresUserConfirmationAboveThreshold);
        AssertContains(draft.StopCancelRequirementRedacted, "stop/cancel");
        Assert.IsTrue(draft.EvidenceTimelineRequirementsRedacted.Count > 0);
        CollectionAssert.Contains(draft.DisclosuresRedacted.ToList(), "BYOK does not mean cost free.");
        CollectionAssert.Contains(draft.DisclosuresRedacted.ToList(), "Managed AI future requires separate policy.");
        CollectionAssert.Contains(draft.DisclosuresRedacted.ToList(), "No LLM call can occur without budget guardrails.");
    }

    [TestMethod]
    public void ModelCapabilityMatrix_CreateRequiredProviderProfiles()
    {
        var providerKinds = new[]
        {
            NodalOsByokProviderKind.OpenAiFuture,
            NodalOsByokProviderKind.AnthropicFuture,
            NodalOsByokProviderKind.GeminiFuture,
            NodalOsByokProviderKind.LocalModelFuture,
            NodalOsByokProviderKind.CustomOpenAiCompatibleFuture
        };

        foreach (var providerKind in providerKinds)
        {
            var profile = service.CreateModelCapabilityProfile(providerKind);

            Assert.AreEqual(providerKind, profile.ProviderKind);
            Assert.IsTrue(profile.PromptGovernanceRequired);
            Assert.IsTrue(profile.BudgetGuardrailsRequired);
            Assert.IsTrue(profile.HumanReviewRequired);
            Assert.IsFalse(profile.BrowserAutomationFutureEnabledByDefault);
            Assert.IsFalse(profile.ToolUseFutureEnabledByDefault);
            Assert.IsFalse(profile.EmbeddingsFutureEnabledBeforePolicy);
            Assert.IsFalse(profile.ExpertAdvisorCanExecute);
            Assert.IsFalse(profile.PerformsLiveModelCheck);
            Assert.IsFalse(profile.PerformsLiveCostLookup);
            Assert.IsFalse(profile.CreatesRoutingDecision);
            Assert.IsFalse(profile.CallsProvider);
            Assert.IsFalse(profile.CanAuthorizeExecution);
            AssertSafeOutput(serializer.SerializeModelCapability(profile));
        }
    }

    [TestMethod]
    public void ModelCapabilityMatrix_ModelsRequiredCapabilitiesAndGates()
    {
        var profile = NodalOsPromptGovernanceFixtures.OpenAiFutureProfile();
        var capabilities = profile.CapabilityFlags.ToList();

        CollectionAssert.Contains(capabilities, NodalOsModelCapabilityKind.ChatFuture);
        CollectionAssert.Contains(capabilities, NodalOsModelCapabilityKind.ReasoningFuture);
        CollectionAssert.Contains(capabilities, NodalOsModelCapabilityKind.SummarizationFuture);
        CollectionAssert.Contains(capabilities, NodalOsModelCapabilityKind.ProjectUnderstandingFuture);
        CollectionAssert.Contains(capabilities, NodalOsModelCapabilityKind.AssignmentPlanningFuture);
        CollectionAssert.Contains(capabilities, NodalOsModelCapabilityKind.ExpertAdvisorFuture);
        CollectionAssert.Contains(capabilities, NodalOsModelCapabilityKind.CodeAssistanceFuture);
        CollectionAssert.Contains(capabilities, NodalOsModelCapabilityKind.VisionFuture);
        CollectionAssert.Contains(capabilities, NodalOsModelCapabilityKind.EmbeddingsFuture);
        CollectionAssert.Contains(capabilities, NodalOsModelCapabilityKind.ToolUseFuture);
        CollectionAssert.Contains(capabilities, NodalOsModelCapabilityKind.BrowserAutomationFuture);
        Assert.IsTrue(profile.RiskNotesRedacted.Any(note => note.Contains("ProjectUnderstandingFuture gated", StringComparison.Ordinal)));
        Assert.IsTrue(profile.RiskNotesRedacted.Any(note => note.Contains("AssignmentPlanningFuture gated", StringComparison.Ordinal)));
        Assert.IsTrue(profile.RiskNotesRedacted.Any(note => note.Contains("ExpertAdvisorFuture is non-executor", StringComparison.Ordinal)));
        Assert.IsTrue(profile.DeniedUseCasesRedacted.Any(useCase => useCase.Contains("browser automation future disabled", StringComparison.Ordinal)));
        Assert.IsTrue(profile.DeniedUseCasesRedacted.Any(useCase => useCase.Contains("tool use future disabled", StringComparison.Ordinal)));
        Assert.IsTrue(profile.DeniedUseCasesRedacted.Any(useCase => useCase.Contains("embeddings future disabled", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void PromptGovernanceBudgetAndModelMatrix_SanitizeAdversarialInputs()
    {
        var unsafeValues = new[]
        {
            "Bearer abcdefghijklmnop",
            "Authorization: value",
            "Cookie: session=value",
            "password=value",
            "secret=value",
            "api_key=value",
            "access_token=value",
            "refresh_token=value",
            "id_token=value",
            "private key",
            "sk-test-value",
            "connection string"
        };

        foreach (var unsafeValue in unsafeValues)
        {
            var policy = service.CreatePromptGovernancePolicy(NodalOsPromptGovernanceState.AllowedForPreviewOnly, safeContextBoundaryRef: unsafeValue);
            var budget = service.CreateBudgetGuardrailsDraft(maxSpendPlaceholder: unsafeValue);
            var profile = service.CreateModelCapabilityProfile(NodalOsByokProviderKind.CustomOpenAiCompatibleFuture, unsafeValue);

            AssertSafeOutput(serializer.SerializePromptGovernance(policy));
            AssertSafeOutput(serializer.SerializeBudgetGuardrails(budget));
            AssertSafeOutput(serializer.SerializeModelCapability(profile));
        }
    }

    [TestMethod]
    public void Boundary_NewPromptGovernanceFiles_DoNotReferenceForbiddenRuntimeOrProviderPrimitives()
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
        AssertDoesNotContain(source, "ProviderSdkClient");
        AssertDoesNotContain(source, "CloudSync");
        AssertDoesNotContain(source, "Environment.GetEnvironmentVariable");
        AssertDoesNotContain(source, "FinalPromptTextBuilder");
        AssertDoesNotContain(source, "TokenCounter");
        AssertDoesNotContain(source, "PricingLookup");
        AssertDoesNotContain(source, "ModelAvailabilityLookup");
        AssertDoesNotContain(source, "RoutingEngine");
        AssertDoesNotContain(source, "File.Write");
        AssertDoesNotContain(source, "File.Read");
        AssertDoesNotContain(source, "Directory.");
        AssertDoesNotContain(source, "git ");
        AssertDoesNotContain(source, "embeddings client");
    }

    [TestMethod]
    public void ExistingSafetyContinuity_ByokAndProjectUnderstandingRemainNoAuthority()
    {
        var byok = NodalOsByokProviderFixtures.ReferenceOnlySettings();
        var providerTest = NodalOsByokProviderFixtures.MockOnlyTestConnection();
        var governance = NodalOsProjectUnderstandingPolicyFixtures.FutureLlmGovernance();
        var shell = NodalOsMissionControlShellFixtures.ShellPreview();

        Assert.IsFalse(byok.CallsProvider);
        Assert.IsFalse(byok.SendsNetworkRequest);
        Assert.IsFalse(byok.CanAuthorizeExecution);
        Assert.IsTrue(providerTest.ActionDisabled);
        Assert.IsFalse(providerTest.CallsLlmProvider);
        Assert.IsFalse(providerTest.PerformsNetworkRequest);
        Assert.IsFalse(governance.CreatesPrompt);
        Assert.IsFalse(governance.CallsLlmProvider);
        Assert.IsFalse(shell.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void ArtifactMarksPromptGovernanceBudgetModelMatrixReady()
    {
        var artifact = System.IO.File.ReadAllText(PathFor("artifacts", "agent-operations", "m518", "prompt-governance-budget-models-summary.json"));

        AssertContains(artifact, "\"promptGovernanceContract\": true");
        AssertContains(artifact, "\"budgetGuardrailsDraft\": true");
        AssertContains(artifact, "\"modelCapabilityMatrix\": true");
        AssertContains(artifact, "\"promptCreationIntroduced\": false");
        AssertContains(artifact, "\"finalPromptTextGenerated\": false");
        AssertContains(artifact, "\"tokenCountingIntroduced\": false");
        AssertContains(artifact, "\"pricingLookupIntroduced\": false");
        AssertContains(artifact, "\"routingIntroduced\": false");
    }

    private static void AssertSafeOutput(string text)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(text, marker);
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(text, name);
    }

    private static string NewSource() =>
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsPromptGovernanceContracts.cs")) +
        System.IO.File.ReadAllText(PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsPromptGovernanceServices.cs"));

    private static string PathFor(params string[] parts) =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", Path.Combine(parts));

    private static void AssertContains(string text, string expected) =>
        StringAssert.Contains(text, expected);

    private static void AssertDoesNotContain(string text, string unexpected) =>
        Assert.IsFalse(text.Contains(unexpected, StringComparison.Ordinal), $"Unexpected marker found: {unexpected}");
}
