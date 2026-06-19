using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AutomationLayerIntegrationNoDivergence")]
[TestCategory("RecipeRiskClassifier")]
[TestCategory("SelectorSafetyHumanHandoff")]
[TestCategory("AutomationEventEvidence")]
public sealed class NodalOsAutomationLayerIntegrationNoDivergenceM460M462Tests
{
    [TestMethod]
    public void AutomationLayer_CrossLayer_NoDivergence_RuntimeExecutionAllowedFalse()
    {
        var context = BuildContext();

        Assert.IsFalse(context.AutomationEvent.RuntimeExecutionAllowed);
        Assert.IsFalse(context.SelectorPolicy.RuntimeExecutionAllowed);
        Assert.IsFalse(context.SelectorEvaluation.RuntimeExecutionAllowed);
        Assert.IsFalse(context.Handoff.RuntimeExecutionAllowed);
        Assert.IsFalse(context.StepClassification.RuntimeExecutionAllowed);
        Assert.IsFalse(context.Profile.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void AutomationLayer_CrossLayer_NoDivergence_RuntimeExecutionDeferredTrue()
    {
        var context = BuildContext();

        Assert.IsTrue(context.AutomationEvent.RuntimeExecutionDeferred);
        Assert.IsTrue(context.SelectorPolicy.RuntimeExecutionDeferred);
        Assert.IsTrue(context.SelectorEvaluation.RuntimeExecutionDeferred);
        Assert.IsTrue(context.Handoff.RuntimeExecutionDeferred);
        Assert.IsTrue(context.StepClassification.RuntimeExecutionDeferred);
        Assert.IsTrue(context.Profile.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void AutomationLayer_CrossLayer_NoDivergence_RequiresEvidenceRedaction()
    {
        var context = BuildContext();

        Assert.IsTrue(context.AutomationEvent.RequiresEvidenceRedaction);
        Assert.IsTrue(context.AutomationEvidence.Redacted);
        Assert.IsTrue(context.SelectorPolicy.RequiresEvidenceRedaction);
        Assert.IsTrue(context.StepClassification.RequiresEvidenceRedaction);
        Assert.IsTrue(context.Profile.RequiresEvidenceRedaction);
    }

    [TestMethod]
    public void AutomationLayer_CrossLayer_NoDivergence_RequiresGlobalPolicyEvaluation()
    {
        var context = BuildContext();

        Assert.IsTrue(context.AutomationEvent.RequiresGlobalPolicyEvaluation);
        Assert.IsTrue(context.SelectorPolicy.RequiresGlobalPolicyEvaluation);
        Assert.IsTrue(context.StepClassification.RequiresGlobalPolicyEvaluation);
        Assert.IsTrue(context.Profile.RequiresGlobalPolicyEvaluation);
    }

    [TestMethod]
    public void AutomationLayer_CrossLayer_NoDivergence_CanAuthorizeActionFalse()
    {
        var context = BuildContext();

        Assert.IsFalse(context.SelectorEvaluation.CanAuthorizeAction);
        Assert.IsFalse(context.Handoff.CanAuthorizeAction);
        Assert.IsFalse(context.StepClassification.CanAuthorizeAction);
        Assert.IsFalse(context.Profile.CanAuthorizeAction);
    }

    [TestMethod]
    public void AutomationLayer_CrossLayer_EvidenceRefsValidateViaBridge()
    {
        var context = BuildContext();
        var bridge = new NodalOsEvidenceRefBridge();

        Assert.IsTrue(bridge.ValidateBridgeRef(context.EvidenceRef).Accepted);
        Assert.IsTrue(context.EventValidator.ValidateEvent(context.AutomationEvent).IsValid);
        Assert.IsTrue(context.EventValidator.ValidateEvidence(context.AutomationEvidence).IsValid);
        Assert.IsTrue(context.SelectorValidator.ValidateSelectorCandidate(context.SelectorCandidate).IsValid);
        Assert.IsTrue(context.SelectorValidator.ValidateSelectorEvaluation(context.SelectorEvaluation).IsValid);
        Assert.IsTrue(context.SelectorValidator.ValidateHumanHandoff(context.Handoff).IsValid);
        Assert.IsTrue(context.RecipeClassifier.ValidateClassification(context.StepClassification).IsValid);
        Assert.IsTrue(context.RecipeClassifier.ValidateRiskProfile(context.Profile).IsValid);
    }

    [TestMethod]
    public void AutomationLayer_CrossLayer_CommonRedactionPreserved()
    {
        const string rawSecret = "Bearer abcdefghijklmnopqrstuvwxyz";
        var invalidEvent = NodalOsAutomationEventEvidenceFixtures.StepStartedEvent() with
        {
            HumanSummary = $"Observed {rawSecret}"
        };

        var result = new NodalOsAutomationEventEvidenceValidator().ValidateEvent(invalidEvent);
        var errors = string.Join(" | ", result.Errors);

        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(errors.Contains(rawSecret, StringComparison.Ordinal));
        AssertContains(errors, "HumanSummary contains sensitive content");
    }

    [TestMethod]
    public void AutomationLayer_CrossLayer_HandoffDoesNotAuthorizeRecipe()
    {
        var context = BuildContext();

        Assert.IsTrue(context.Handoff.RequiresHumanAction);
        Assert.IsFalse(context.Handoff.CanAuthorizeAction);
        Assert.IsTrue(context.StepClassification.RequiresHumanHandoff);
        Assert.IsTrue(context.Profile.RequiresHumanHandoff);
        Assert.IsFalse(context.StepClassification.CanAuthorizeAction);
        Assert.IsFalse(context.Profile.CanAuthorizeAction);
    }

    [TestMethod]
    public void AutomationLayer_CrossLayer_SelectorEvaluationDoesNotAuthorizeRecipeRisk()
    {
        var context = BuildContext();

        Assert.AreEqual(NodalOsSelectorSafetyDecision.AllowedForObservationOnly, context.SelectorEvaluation.Decision);
        Assert.IsFalse(context.SelectorEvaluation.CanAuthorizeAction);
        Assert.IsFalse(context.StepClassification.CanAuthorizeAction);
        Assert.IsFalse(context.Profile.CanAuthorizeAction);
    }

    [TestMethod]
    public void AutomationLayer_CrossLayer_DslDecisionDoesNotEnableRuntime()
    {
        var context = BuildContext();

        Assert.IsFalse(context.DslDecision.DslIsRuntime);
        Assert.IsFalse(context.DslDecision.ParserImplemented);
        Assert.IsFalse(context.DslDecision.DirectExecutionAllowed);
        Assert.IsTrue(context.DslDecision.ImportRequiresValidation);
        Assert.IsTrue(context.DslDecision.JsonCanonicalModelRequired);
        Assert.IsTrue(context.RecipeClassifier.ValidateDslDecision(context.DslDecision).IsValid);
    }

    [TestMethod]
    public void AgentOperationsContracts_DoesNotReferenceBrowserExecutorCdp() =>
        AssertProjectDoesNotReference(ProjectPath("OneBrain.AgentOperations.Contracts"), "BrowserExecutor.Cdp");

    [TestMethod]
    public void AgentOperationsCore_DoesNotReferenceBrowserExecutorCdp() =>
        AssertProjectDoesNotReference(ProjectPath("OneBrain.AgentOperations.Core"), "BrowserExecutor.Cdp");

    [TestMethod]
    public void AgentOperationsAdaptersBrowser_DoesNotReferenceBrowserExecutorCdp() =>
        AssertProjectDoesNotReference(ProjectPath("OneBrain.AgentOperations.Adapters.Browser"), "BrowserExecutor.Cdp");

    [TestMethod]
    public void AgentOperationsProjects_DoNotReferenceChromeCdpPackages()
    {
        foreach (var project in AgentOperationsProjectFiles())
        {
            var text = File.ReadAllText(project);

            Assert.IsFalse(text.Contains("Chrome", StringComparison.OrdinalIgnoreCase), project);
            Assert.IsFalse(text.Contains("ChromeDevTools", StringComparison.OrdinalIgnoreCase), project);
            Assert.IsFalse(text.Contains("Cdp", StringComparison.OrdinalIgnoreCase), project);
        }
    }

    [TestMethod]
    public void AgentOperationsProjects_DoNotReferencePlaywrightPuppeteerSeleniumCefWebView()
    {
        var forbidden = new[] { "Playwright", "Puppeteer", "Selenium", "Cef", "WebView" };

        foreach (var project in AgentOperationsProjectFiles())
        {
            var text = File.ReadAllText(project);
            foreach (var dependency in forbidden)
                Assert.IsFalse(text.Contains(dependency, StringComparison.OrdinalIgnoreCase), $"{dependency} found in {project}");
        }
    }

    [TestMethod]
    public void BrowserExecutorCdp_RemainsTemporaryHostOnly()
    {
        var project = ProjectPath("OneBrain.BrowserExecutor.Cdp");

        Assert.IsTrue(File.Exists(project));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot(), "src", "OneBrain.BrowserExecutor.Cdp", "ChromeCdpBrowserExecutor.cs")));
        AssertContains(File.ReadAllText(project), "OneBrain.AgentOperations.Core");
    }

    [TestMethod]
    public void NoRecorderReplayQueueSchedulerBrowserAutomationUiExecutionIntroduced()
    {
        var artifact = ArtifactText();

        AssertContains(artifact, "\"noDslParserImplemented\": true");
        AssertContains(artifact, "\"noRecorderImplemented\": true");
        AssertContains(artifact, "\"noReplayImplemented\": true");
        AssertContains(artifact, "\"noQueueImplemented\": true");
        AssertContains(artifact, "\"noSchedulerImplemented\": true");
        AssertContains(artifact, "\"noBrowserAutomationImplemented\": true");
        AssertContains(artifact, "\"noUiImplemented\": true");
        AssertContains(artifact, "\"noExecutionImplemented\": true");
    }

    [TestMethod]
    public void RuntimeGatedClassifierHardeningBacklogExists() =>
        Assert.IsTrue(File.Exists(BacklogPath()));

    [TestMethod]
    public void RuntimeGatedClassifierHardeningBacklogMentionsDropPurgeWipeTruncate()
    {
        var backlog = File.ReadAllText(BacklogPath());

        AssertContains(backlog, "drop table after reading status");
        AssertContains(backlog, "drop");
        AssertContains(backlog, "purge");
        AssertContains(backlog, "wipe");
        AssertContains(backlog, "truncate");
    }

    [TestMethod]
    public void RuntimeGatedClassifierHardeningBacklogBlocksExecutionBeforeClassifierUsedForApprovalGates()
    {
        var backlog = File.ReadAllText(BacklogPath());

        AssertContains(backlog, "runtime-gated blocker");
        AssertContains(backlog, "approval gates");
        AssertContains(backlog, "recipe/step execution");
    }

    [TestMethod]
    public void NoDslParserImplemented() =>
        AssertNoSourceContains("DslParser", "ParseDsl", "RecipeDslParser");

    [TestMethod]
    public void NoRecorderImplemented() =>
        AssertNoSourceContains("RecorderService", "StartRecording", "StopRecording");

    [TestMethod]
    public void NoReplayImplemented() =>
        AssertNoSourceContains("ReplayService", "StartReplay", "ReplayEngine");

    [TestMethod]
    public void NoQueueImplemented() =>
        AssertNoSourceContains("QueueService", "WorkQueueRuntime", "BackgroundQueue");

    [TestMethod]
    public void NoSchedulerImplemented() =>
        AssertNoSourceContains("IHostedService", "BackgroundService", "PeriodicTimer", "System.Threading.Timer");

    [TestMethod]
    public void NoBrowserAutomationImplemented() =>
        AssertNoSourceContains("Playwright", "Puppeteer", "Selenium", "ChromeCdpBrowserExecutor");

    [TestMethod]
    public void NoUiImplemented() =>
        AssertNoSourceContains("Controller", "RazorPage", "Blazor", "React");

    [TestMethod]
    public void NoExecutionImplemented() =>
        AssertContains(ArtifactText(), "\"noExecutionImplemented\": true");

    private static CrossLayerContext BuildContext()
    {
        var eventValidator = new NodalOsAutomationEventEvidenceValidator();
        var selectorValidator = new NodalOsSelectorSafetyHumanHandoffValidator();
        var recipeClassifier = new NodalOsRecipeRiskClassifier();
        var evidenceRef = NodalOsSelectorSafetyHumanHandoffFixtures.ValidEvidenceRef();

        var automationEvent = NodalOsAutomationEventEvidenceFixtures.StepStartedEvent() with
        {
            EvidenceRefs = [evidenceRef]
        };
        var automationEvidence = NodalOsAutomationEventEvidenceFixtures.StepLogEvidence() with
        {
            EvidenceRefs = [evidenceRef]
        };
        var selectorPolicy = NodalOsSelectorSafetyHumanHandoffFixtures.DefaultObservationOnlyPolicy();
        var selectorCandidate = NodalOsSelectorSafetyHumanHandoffFixtures.SafeSemanticSelector() with
        {
            EvidenceRefs = [evidenceRef]
        };
        var selectorEvaluation = selectorValidator.EvaluateSelector(selectorPolicy, selectorCandidate);
        var handoff = NodalOsSelectorSafetyHumanHandoffFixtures.LoginRequiredHandoff() with
        {
            EvidenceRefs = [evidenceRef]
        };
        var stepInput = NodalOsRecipeRiskClassifierFixtures.CredentialLoginStep() with
        {
            EvidenceRefs = [evidenceRef]
        };
        var stepClassification = recipeClassifier.ClassifyStep(stepInput);
        var profile = recipeClassifier.BuildRiskProfile("recipe-automation-cross-layer", [stepClassification]);
        var dslDecision = NodalOsRecipeRiskClassifierFixtures.DslRepresentationOnlyDecision();

        return new CrossLayerContext(
            eventValidator,
            selectorValidator,
            recipeClassifier,
            evidenceRef,
            automationEvent,
            automationEvidence,
            selectorPolicy,
            selectorCandidate,
            selectorEvaluation,
            handoff,
            stepClassification,
            profile,
            dslDecision);
    }

    private static void AssertProjectDoesNotReference(string projectPath, string forbidden)
    {
        Assert.IsTrue(File.Exists(projectPath), projectPath);
        Assert.IsFalse(
            File.ReadAllText(projectPath).Contains(forbidden, StringComparison.OrdinalIgnoreCase),
            $"{forbidden} found in {projectPath}");
    }

    private static void AssertNoSourceContains(params string[] forbidden)
    {
        foreach (var file in AgentOperationsSourceFiles())
        {
            if (IsBuildOutput(file))
                continue;

            var text = File.ReadAllText(file);
            foreach (var token in forbidden)
                Assert.IsFalse(text.Contains(token, StringComparison.OrdinalIgnoreCase), $"{token} found in {file}");
        }
    }

    private static IEnumerable<string> AgentOperationsProjectFiles() =>
        Directory.GetFiles(Path.Combine(RepoRoot(), "src"), "OneBrain.AgentOperations.*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path));

    private static IEnumerable<string> AgentOperationsSourceFiles() =>
        Directory.GetFiles(Path.Combine(RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}OneBrain.AgentOperations.", StringComparison.OrdinalIgnoreCase))
            .Where(path => !IsBuildOutput(path));

    private static string ProjectPath(string projectName) =>
        Path.Combine(RepoRoot(), "src", projectName, $"{projectName}.csproj");

    private static string ArtifactText() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "artifacts", "agent-operations", "m462", "automation-layer-integration-no-divergence-summary.json"));

    private static string BacklogPath() =>
        Path.Combine(RepoRoot(), "docs", "backlog", "runtime-gated-recipe-risk-classifier-hardening.md");

    private static void AssertContains(string text, string expected) =>
        Assert.IsTrue(text.Contains(expected, StringComparison.Ordinal), expected);

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

    private static string RepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "OneBrain.slnx")))
                return current;

            current = Directory.GetParent(current)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private sealed record CrossLayerContext(
        NodalOsAutomationEventEvidenceValidator EventValidator,
        NodalOsSelectorSafetyHumanHandoffValidator SelectorValidator,
        NodalOsRecipeRiskClassifier RecipeClassifier,
        NodalOsEvidenceBridgeRef EvidenceRef,
        NodalOsAutomationEvent AutomationEvent,
        NodalOsAutomationEvidence AutomationEvidence,
        NodalOsSelectorSafetyPolicy SelectorPolicy,
        NodalOsSelectorCandidate SelectorCandidate,
        NodalOsSelectorSafetyEvaluation SelectorEvaluation,
        NodalOsHumanHandoffContract Handoff,
        NodalOsRecipeStepRiskClassification StepClassification,
        NodalOsRecipeRiskProfile Profile,
        NodalOsRecipeDslDecisionRecord DslDecision);
}
