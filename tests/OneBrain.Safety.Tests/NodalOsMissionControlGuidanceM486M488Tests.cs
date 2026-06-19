using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("MissionControlGuidance")]
[TestCategory("MissionControlInteractionNoOp")]
[TestCategory("MissionControlShellReadOnly")]
[TestCategory("AuditAPreUiBoundaryNaming")]
[TestCategory("ApprovalUxHandoffObservability")]
[TestCategory("ApprovalTimelineEvidence")]
[TestCategory("CoreRuntimeRegistryEventBusRedaction")]
[TestCategory("NewTopicsIntake")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsMissionControlGuidanceM486M488Tests
{
    private static readonly string[] ForbiddenOperationalNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SecretMarkers =
    [
        "Bearer ",
        "Authorization:",
        "Cookie:",
        "Set-Cookie:",
        "password=",
        "api_key",
        "access_token",
        "refresh_token",
        "id_token",
        "raw-body",
        "data:image/"
    ];

    private readonly NodalOsMissionControlGuidanceService service = new();
    private readonly NodalOsMissionControlGuidanceValidator validator = new();
    private readonly NodalOsMissionControlGuidanceJsonSerializer serializer = new();

    [TestMethod]
    public void EmptyState_NoMissionSelected_IsReadOnly()
    {
        var state = EmptyState(NodalOsMissionControlEmptyStateKind.NoMissionSelected);

        AssertEmptyStateValid(state);
        AssertContains(state.RecommendedNextSafeStepRedacted, "Select");
    }

    [TestMethod]
    public void EmptyState_NoTimelineEvents_ExplainsNextSafeStep()
    {
        var state = EmptyState(NodalOsMissionControlEmptyStateKind.NoTimelineEvents);

        AssertEmptyStateValid(state);
        AssertContains(state.UserFriendlyExplanationRedacted, "timeline");
    }

    [TestMethod]
    public void EmptyState_NoApprovalsPending_IsMissionControlFirst()
    {
        var state = EmptyState(NodalOsMissionControlEmptyStateKind.NoApprovalsPending);

        AssertEmptyStateValid(state);
        AssertContains(state.TitleRedacted, "approvals");
    }

    [TestMethod]
    public void EmptyState_NoEvidence_IsRefOnlyGuidance()
    {
        var state = EmptyState(NodalOsMissionControlEmptyStateKind.NoEvidenceAvailable);

        AssertEmptyStateValid(state);
        AssertContains(state.UserFriendlyExplanationRedacted, "ref-only");
    }

    [TestMethod]
    public void EmptyState_NoObservabilityReport_IsReadOnly()
    {
        var state = EmptyState(NodalOsMissionControlEmptyStateKind.NoObservabilityReportYet);

        AssertEmptyStateValid(state);
        AssertContains(state.DisabledReasonRedacted!, "contract-only");
    }

    [TestMethod]
    public void EmptyState_NoWorkspaceSelected_BlocksFilesystemMutation()
    {
        var state = EmptyState(NodalOsMissionControlEmptyStateKind.NoWorkspaceSelected);

        AssertEmptyStateValid(state);
        AssertContains(state.DisabledReasonRedacted!, "Filesystem mutation");
    }

    [TestMethod]
    public void EmptyState_RuntimeUnavailableByDesign_Blocked()
    {
        var state = EmptyState(NodalOsMissionControlEmptyStateKind.RuntimeUnavailableByDesign);

        AssertEmptyStateValid(state);
        Assert.AreEqual(NodalOsMissionControlGuidanceSeverity.Blocked, state.Severity);
        Assert.IsTrue(state.RequiresAttention);
        AssertContains(state.DisabledReasonRedacted!, "Runtime execution");
    }

    [TestMethod]
    public void EmptyState_LlmNotConfiguredByDesign_BlocksProviderCalls()
    {
        var state = EmptyState(NodalOsMissionControlEmptyStateKind.LlmNotConfiguredByDesign);

        AssertEmptyStateValid(state);
        AssertContains(state.DisabledReasonRedacted!, "Provider calls");
    }

    [TestMethod]
    public void EmptyState_CloudSyncDisabledByDesign_IsLocalFirst()
    {
        var state = EmptyState(NodalOsMissionControlEmptyStateKind.CloudSyncDisabledByDesign);

        AssertEmptyStateValid(state);
        AssertContains(state.RecommendedNextSafeStepRedacted, "local-first");
    }

    [TestMethod]
    public void EmptyState_BrowserAutomationDeferredByDesign_Blocked()
    {
        var state = EmptyState(NodalOsMissionControlEmptyStateKind.BrowserAutomationDeferredByDesign);

        AssertEmptyStateValid(state);
        Assert.AreEqual(NodalOsMissionControlGuidanceSeverity.Blocked, state.Severity);
        AssertContains(state.DisabledReasonRedacted!, "Browser automation");
    }

    [TestMethod]
    public void EmptyStates_AllCannotExecuteAndAreReadOnly()
    {
        foreach (var state in service.CreateDefaultEmptyStates())
        {
            Assert.IsFalse(state.CanExecuteAction, state.Kind.ToString());
            Assert.IsTrue(state.IsReadOnly, state.Kind.ToString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(state.RecommendedNextSafeStepRedacted), state.Kind.ToString());
            AssertEmptyStateValid(state);
        }
    }

    [TestMethod]
    public void EmptyStateSerialization_IsRedactedAndUsesNodalOsOperationalNaming()
    {
        var state = EmptyState(NodalOsMissionControlEmptyStateKind.NoMissionSelected) with
        {
            UserFriendlyExplanationRedacted = "User pasted Authorization: Bearer secret-token and api_key=123."
        };

        var json = serializer.SerializeEmptyState(state);

        AssertDoesNotContainSecretsOrForbiddenNames(json);
        AssertContains(json, "mission");
    }

    [TestMethod]
    public void Onboarding_GeneratesMissionControlStep()
    {
        var step = Onboarding(NodalOsMissionControlOnboardingTarget.MissionControl);

        AssertOnboardingValid(step);
        AssertContains(step.ExplanationRedacted, "mission");
    }

    [TestMethod]
    public void Onboarding_GeneratesTimelineStep()
    {
        var step = Onboarding(NodalOsMissionControlOnboardingTarget.Timeline);

        AssertOnboardingValid(step);
        AssertContains(step.ExplanationRedacted, "events");
    }

    [TestMethod]
    public void Onboarding_GeneratesApprovalsStep()
    {
        var steps = service.CreateDefaultOnboarding();

        Assert.IsTrue(steps.Any(step => step.Target == NodalOsMissionControlOnboardingTarget.Approvals));
        Assert.IsTrue(steps.Any(step => step.StepId == "approval-no-authority"));
    }

    [TestMethod]
    public void Onboarding_GeneratesEvidenceStep()
    {
        var step = Onboarding(NodalOsMissionControlOnboardingTarget.Evidence);

        AssertOnboardingValid(step);
        AssertContains(step.ExplanationRedacted, "Evidence");
    }

    [TestMethod]
    public void Onboarding_GeneratesObservabilityLogStep()
    {
        var step = Onboarding(NodalOsMissionControlOnboardingTarget.ObservabilityLog);

        AssertOnboardingValid(step);
        AssertContains(step.ExplanationRedacted, "LOG");
    }

    [TestMethod]
    public void Onboarding_GeneratesGuardrailsStep()
    {
        var step = Onboarding(NodalOsMissionControlOnboardingTarget.Guardrails);

        AssertOnboardingValid(step);
        AssertContains(step.ExplanationRedacted, "Cloud");
    }

    [TestMethod]
    public void Onboarding_ExplainsApprovalNoAuthorityEvidenceRuntimeLlmByok()
    {
        var text = string.Join(" ", service.CreateDefaultOnboarding().Select(step => $"{step.TitleRedacted} {step.ExplanationRedacted} {step.DisabledFutureWorkExplanationRedacted}"));

        AssertContains(text, "Approval");
        AssertContains(text, "Evidence");
        AssertContains(text, "Runtime");
        AssertContains(text, "LLM/BYOK");
        AssertContains(text, "Cloud");
    }

    [TestMethod]
    public void Onboarding_DismissAndReopen_AreMockSafeNoOp()
    {
        var step = Onboarding(NodalOsMissionControlOnboardingTarget.MissionControl);
        var dismissed = service.DismissOnboardingStep(step);
        var reopened = service.ReopenOnboardingStep(dismissed);

        Assert.AreEqual(NodalOsMissionControlOnboardingState.DismissedMock, dismissed.State);
        Assert.AreEqual(NodalOsMissionControlOnboardingState.ReopenedMock, reopened.State);
        AssertOnboardingValid(dismissed);
        AssertOnboardingValid(reopened);
        Assert.IsFalse(reopened.ProductivePersistenceAllowed);
        Assert.IsFalse(reopened.TelemetryAllowed);
        Assert.IsFalse(reopened.CloudCallAllowed);
        Assert.IsFalse(reopened.LlmProviderCallAllowed);
    }

    [TestMethod]
    public void OnboardingSerialization_IsRedactedAndUsesNodalOsOperationalNaming()
    {
        var step = Onboarding(NodalOsMissionControlOnboardingTarget.MissionControl) with
        {
            ExplanationRedacted = "Bearer secret-token with password=123 should not survive."
        };

        var json = serializer.SerializeOnboardingStep(step);

        AssertDoesNotContainSecretsOrForbiddenNames(json);
    }

    [TestMethod]
    public void Guardrail_ReadOnlyMode_Exists()
    {
        var explainer = Explainer("read-only-mode");

        AssertExplainerValid(explainer);
        Assert.AreEqual(NodalOsMissionControlGuardrailBlockingCategory.Informational, explainer.BlockingCategory);
    }

    [TestMethod]
    public void Guardrail_NoRuntimeExecution_BlocksRuntime()
    {
        var explainer = Explainer("no-runtime-execution");

        AssertExplainerValid(explainer);
        Assert.AreEqual(NodalOsMissionControlGuardrailBlockingCategory.BlocksRuntime, explainer.BlockingCategory);
    }

    [TestMethod]
    public void Guardrail_NoBrowserAutomation_BlocksBrowserAutomation()
    {
        var explainer = Explainer("no-browser-automation");

        AssertExplainerValid(explainer);
        Assert.AreEqual(NodalOsMissionControlGuardrailBlockingCategory.BlocksBrowserAutomation, explainer.BlockingCategory);
    }

    [TestMethod]
    public void Guardrail_NoCloudSync_BlocksCloud()
    {
        var explainer = Explainer("no-cloud-sync");

        AssertExplainerValid(explainer);
        Assert.AreEqual(NodalOsMissionControlGuardrailBlockingCategory.BlocksCloud, explainer.BlockingCategory);
    }

    [TestMethod]
    public void Guardrail_NoLlmProviderCalls_BlocksLlmByok()
    {
        var explainer = Explainer("no-llm-provider-calls");

        AssertExplainerValid(explainer);
        Assert.AreEqual(NodalOsMissionControlGuardrailBlockingCategory.BlocksLlmByok, explainer.BlockingCategory);
    }

    [TestMethod]
    public void Guardrail_NoFilesystemMutation_NoShellSubprocess_Exist()
    {
        AssertExplainerValid(Explainer("no-filesystem-mutation"));
        AssertExplainerValid(Explainer("no-shell-subprocess"));
    }

    [TestMethod]
    public void Guardrail_ApprovalEvidenceRedactionPositiveGate_Exist()
    {
        AssertExplainerValid(Explainer("approval-no-authority"));
        AssertExplainerValid(Explainer("evidence-ref-only"));
        AssertExplainerValid(Explainer("redaction-applied"));
        AssertExplainerValid(Explainer("positive-gate-missing"));
    }

    [TestMethod]
    public void Guardrail_RecipeRiskHardeningAndLegacyQuarantine_BlockersExist()
    {
        var recipe = Explainer("recipe-risk-hardening-required");
        var legacy = Explainer("legacy-sensitive-quarantine");

        AssertExplainerValid(recipe);
        AssertExplainerValid(legacy);
        Assert.AreEqual(NodalOsMissionControlGuidanceSeverity.Critical, recipe.Severity);
        Assert.AreEqual(NodalOsMissionControlGuidanceSeverity.Critical, legacy.Severity);
    }

    [TestMethod]
    public void Guardrail_BlockedActionDisabledButton_ExplainsDisabledControl()
    {
        var explainer = Explainer("blocked-action-disabled-button");

        AssertExplainerValid(explainer);
        AssertContains(explainer.PlainLanguageExplanationRedacted, "Disabled");
    }

    [TestMethod]
    public void Guardrails_DoNotUnlockOrMutatePolicy()
    {
        foreach (var explainer in service.CreateDefaultGuardrailExplainers())
        {
            Assert.IsFalse(explainer.CanUnlockExecution, explainer.GuardrailId);
            Assert.IsFalse(explainer.CanChangePolicy, explainer.GuardrailId);
            Assert.IsFalse(explainer.CanMutateRegistry, explainer.GuardrailId);
            Assert.IsFalse(explainer.CanCreateException, explainer.GuardrailId);
            AssertExplainerValid(explainer);
        }
    }

    [TestMethod]
    public void GuardrailSerialization_IsRedacted()
    {
        var explainer = Explainer("redaction-applied") with
        {
            TechnicalReasonRedacted = "Authorization: Bearer secret-token should not survive."
        };

        var json = serializer.SerializeGuardrailExplainer(explainer);

        AssertDoesNotContainSecretsOrForbiddenNames(json);
    }

    [TestMethod]
    public void Boundary_NewGuidanceFiles_DoNotReferenceBrowserExecutorOrRuntimePrimitives()
    {
        var text = NewGuidanceSourceText();

        AssertDoesNotContain(text, "OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(text, "using OneBrain.BrowserExecutor.Cdp");
        AssertDoesNotContain(text, "HttpClient");
        AssertDoesNotContain(text, "ClientWebSocket");
        AssertDoesNotContain(text, "Process.Start");
        AssertDoesNotContain(text, "System.Diagnostics.Process");
        AssertDoesNotContain(text, "BackgroundService");
        AssertDoesNotContain(text, "Task.Run");
        AssertDoesNotContain(text, "new Timer(");
        AssertDoesNotContain(text, "new Thread(");
    }

    [TestMethod]
    public void Boundary_NoExecutionCloudLlmTelemetryOrPersistenceWiringIntroduced()
    {
        var text = NewGuidanceSourceText();

        AssertDoesNotContain(text, "ExecuteAsync");
        AssertDoesNotContain(text, "RunAsync");
        AssertDoesNotContain(text, "SendAsync");
        AssertDoesNotContain(text, "OpenAI");
        AssertDoesNotContain(text, "CloudClient");
        AssertDoesNotContain(text, "CloudSyncService");
        AssertDoesNotContain(text, "TelemetryClient");
        AssertDoesNotContain(text, "AnalyticsClient");
        AssertDoesNotContain(text, "DslParser");
        AssertDoesNotContain(text, "File.Write");
        AssertDoesNotContain(text, "Directory.CreateDirectory");
    }

    [TestMethod]
    public void ExistingSafety_ShellReadOnlyStillPasses()
    {
        var shell = NodalOsMissionControlShellFixtures.ShellPreview();
        var html = new NodalOsMissionControlShellService().RenderReadOnlyHtml(shell);

        Assert.IsTrue(shell.ReadOnlyUi);
        Assert.IsFalse(shell.CanAuthorizeExecution);
        Assert.IsFalse(shell.RuntimeExecutionAllowed);
        AssertDoesNotContainSecretsOrForbiddenNames(html);
    }

    [TestMethod]
    public void ExistingSafety_NoOpInteractionStillPasses()
    {
        var intent = NodalOsMissionControlInteractionFixtures.SelectTimelineIntent();
        var state = NodalOsMissionControlInteractionFixtures.UiState();

        Assert.IsTrue(intent.IsNoOp);
        Assert.IsFalse(intent.CanAuthorizeExecution);
        Assert.IsFalse(intent.RuntimeExecutionAllowed);
        Assert.IsTrue(state.MockPersistenceOnly);
        Assert.IsFalse(state.CloudPersistenceAllowed);
        Assert.IsFalse(state.ProductiveDatabasePersistenceAllowed);
    }

    [TestMethod]
    public void ArtifactMarksGuidanceAndNoRuntime()
    {
        var artifact = File.ReadAllText(ArtifactPath());

        AssertContains(artifact, "\"missionControlEmptyStates\": true");
        AssertContains(artifact, "\"contextualOnboarding\": true");
        AssertContains(artifact, "\"guardrailExplainers\": true");
        AssertContains(artifact, "\"guardrailsCanUnlockExecution\": false");
        AssertContains(artifact, "\"runtimeExecutionAllowed\": false");
        AssertContains(artifact, "\"telemetryOrAnalyticsIntroduced\": false");
    }

    private NodalOsMissionControlEmptyState EmptyState(NodalOsMissionControlEmptyStateKind kind) =>
        service.CreateDefaultEmptyStates().Single(state => state.Kind == kind);

    private NodalOsMissionControlOnboardingStep Onboarding(NodalOsMissionControlOnboardingTarget target) =>
        service.CreateDefaultOnboarding().First(step => step.Target == target);

    private NodalOsMissionControlGuardrailExplainer Explainer(string guardrailId) =>
        service.CreateDefaultGuardrailExplainers().Single(explainer => explainer.GuardrailId == guardrailId);

    private void AssertEmptyStateValid(NodalOsMissionControlEmptyState state)
    {
        var result = validator.ValidateEmptyState(state);
        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.IsFalse(state.CanExecuteAction);
        Assert.IsTrue(state.IsReadOnly);
        Assert.IsFalse(string.IsNullOrWhiteSpace(state.RecommendedNextSafeStepRedacted));
    }

    private void AssertOnboardingValid(NodalOsMissionControlOnboardingStep step)
    {
        var result = validator.ValidateOnboardingStep(step);
        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.IsTrue(step.IsNoOp);
        Assert.IsTrue(step.DismissReopenMockSafe);
        Assert.IsFalse(step.ProductivePersistenceAllowed);
        Assert.IsFalse(step.TelemetryAllowed);
        Assert.IsFalse(step.CloudCallAllowed);
        Assert.IsFalse(step.LlmProviderCallAllowed);
    }

    private void AssertExplainerValid(NodalOsMissionControlGuardrailExplainer explainer)
    {
        var result = validator.ValidateGuardrailExplainer(explainer);
        Assert.IsTrue(result.IsValid, string.Join(" | ", result.Errors));
        Assert.IsFalse(explainer.CanUnlockExecution);
        Assert.IsFalse(explainer.CanChangePolicy);
        Assert.IsFalse(explainer.CanMutateRegistry);
        Assert.IsFalse(explainer.CanCreateException);
    }

    private static string NewGuidanceSourceText() =>
        string.Join(Environment.NewLine,
            new[]
            {
                PathFor("src", "OneBrain.AgentOperations.Contracts", "NodalOsMissionControlGuidanceContracts.cs"),
                PathFor("src", "OneBrain.AgentOperations.Core", "NodalOsMissionControlGuidanceServices.cs")
            }.Select(File.ReadAllText));

    private static string ArtifactPath() =>
        PathFor("artifacts", "agent-operations", "m488", "mission-control-guidance-summary.json");

    private static void AssertDoesNotContainSecretsOrForbiddenNames(string text)
    {
        foreach (var marker in SecretMarkers)
            AssertDoesNotContain(text, marker);
        foreach (var forbidden in ForbiddenOperationalNames)
            AssertDoesNotContain(text, forbidden);
    }

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
