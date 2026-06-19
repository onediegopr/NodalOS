using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Adapters.Browser;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("AgentOperationsNamespaceMigration")]
public sealed class NodalOsAgentOperationsNamespaceMigrationM443M445Tests
{
    private const string ContractsNamespace = "namespace OneBrain.AgentOperations.Contracts;";
    private const string CoreNamespace = "namespace OneBrain.AgentOperations.Core;";
    private const string BrowserContractsNamespace = "namespace OneBrain.BrowserExecutor.Contracts";
    private const string BrowserCdpNamespace = "namespace OneBrain.BrowserExecutor.Cdp";

    [TestMethod]
    public void AgentOperationsContractsFilesUseCanonicalNamespace()
    {
        foreach (var file in SourceFiles("src", "OneBrain.AgentOperations.Contracts"))
        {
            var text = File.ReadAllText(file);
            Assert.IsTrue(text.Contains(ContractsNamespace, StringComparison.Ordinal), file);
        }
    }

    [TestMethod]
    public void AgentOperationsCoreFilesUseCanonicalNamespace()
    {
        foreach (var file in SourceFiles("src", "OneBrain.AgentOperations.Core"))
        {
            var text = File.ReadAllText(file);
            Assert.IsTrue(text.Contains(CoreNamespace, StringComparison.Ordinal), file);
        }
    }

    [TestMethod]
    public void AgentOperationsAdapterBrowserUsesCanonicalNamespace()
    {
        var marker = Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.AgentOperations.Adapters.Browser",
            "NodalOsBrowserAgentOperationsAdapterBoundary.cs");

        var text = File.ReadAllText(marker);

        var markerValue = typeof(NodalOsBrowserAgentOperationsAdapterBoundary)
            .GetField(nameof(NodalOsBrowserAgentOperationsAdapterBoundary.AdapterProjectSkeleton))!
            .GetRawConstantValue();

        Assert.IsTrue(text.Contains("namespace OneBrain.AgentOperations.Adapters.Browser;", StringComparison.Ordinal));
        Assert.IsTrue((bool)markerValue!);
    }

    [TestMethod]
    public void NoNonCompatibilityAgentOperationsContractsFileUsesBrowserExecutorNamespace()
    {
        foreach (var file in SourceFiles("src", "OneBrain.AgentOperations.Contracts"))
        {
            if (IsCompatibilityFile(file))
                continue;

            var text = File.ReadAllText(file);
            Assert.IsFalse(text.Contains(BrowserContractsNamespace, StringComparison.Ordinal), file);
            Assert.IsFalse(text.Contains(BrowserCdpNamespace, StringComparison.Ordinal), file);
        }
    }

    [TestMethod]
    public void NoNonCompatibilityAgentOperationsCoreFileUsesBrowserExecutorNamespace()
    {
        foreach (var file in SourceFiles("src", "OneBrain.AgentOperations.Core"))
        {
            if (IsCompatibilityFile(file))
                continue;

            var text = File.ReadAllText(file);
            Assert.IsFalse(text.Contains(BrowserContractsNamespace, StringComparison.Ordinal), file);
            Assert.IsFalse(text.Contains(BrowserCdpNamespace, StringComparison.Ordinal), file);
        }
    }

    [TestMethod]
    public void CompatibilityShimsAreMarkedObsolete_IfPresent()
    {
        var shimFiles = SourceFiles("src", "OneBrain.AgentOperations.Contracts")
            .Concat(SourceFiles("src", "OneBrain.AgentOperations.Core"))
            .Where(IsCompatibilityFile)
            .ToArray();

        foreach (var file in shimFiles)
        {
            var text = File.ReadAllText(file);
            Assert.IsTrue(text.Contains("[Obsolete(", StringComparison.Ordinal), file);
        }
    }

    [TestMethod]
    public void InternalTestsUseAgentOperationsCanonicalNamespaces()
    {
        var globalUsings = Path.Combine(
            RepoRoot(),
            "tests",
            "OneBrain.Safety.Tests",
            "NodalOsAgentOperationsNamespaceGlobalUsings.cs");

        var text = File.ReadAllText(globalUsings);

        Assert.IsTrue(text.Contains("global using OneBrain.AgentOperations.Contracts;", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("global using OneBrain.AgentOperations.Core;", StringComparison.Ordinal));
    }

    [TestMethod]
    public void AgentOperationsContractsStillBrowserFree()
    {
        var project = ProjectText("src", "OneBrain.AgentOperations.Contracts", "OneBrain.AgentOperations.Contracts.csproj");

        AssertDoesNotReferenceBrowserOrCdp(project);
    }

    [TestMethod]
    public void AgentOperationsCoreStillBrowserFree()
    {
        var project = ProjectText("src", "OneBrain.AgentOperations.Core", "OneBrain.AgentOperations.Core.csproj");

        AssertDoesNotReferenceBrowserOrCdp(project);
        Assert.IsTrue(project.Contains("OneBrain.AgentOperations.Contracts", StringComparison.Ordinal));
    }

    [TestMethod]
    public void AgentOperationsAdaptersBrowserStillDoesNotReferenceCdp()
    {
        var project = ProjectText(
            "src",
            "OneBrain.AgentOperations.Adapters.Browser",
            "OneBrain.AgentOperations.Adapters.Browser.csproj");

        Assert.IsFalse(project.Contains("OneBrain.BrowserExecutor.Cdp", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserExecutorCdpStillTemporaryHost()
    {
        var project = ProjectText("src", "OneBrain.BrowserExecutor.Cdp", "OneBrain.BrowserExecutor.Cdp.csproj");

        Assert.IsTrue(project.Contains("OneBrain.AgentOperations.Core", StringComparison.Ordinal));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot(), "src", "OneBrain.BrowserExecutor.Cdp", "ChromeCdpBrowserExecutor.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot(), "src", "OneBrain.BrowserExecutor.Cdp", "BrowserRuntimeSmoke.cs")));
    }

    [TestMethod]
    public void OrchestrationFacadeStillNoExecution()
    {
        var result = new NodalOsOrchestrationInProcessFacade().Dispatch(new NodalOsOrchestrationCommandEnvelope
        {
            CommandId = "cmd-namespace-migration",
            Kind = NodalOsOrchestrationCommandKind.QuerySkillRegistry,
            RiskLevel = NodalOsOrchestrationCommandRiskLevel.Low,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresHumanApproval = false,
            CreatedAt = DateTimeOffset.UtcNow
        });

        Assert.IsTrue(result.Accepted);
        Assert.IsFalse(result.Executed);
        Assert.IsTrue(result.RuntimeExecutionDeferred);
    }

    [TestMethod]
    public void ScheduledReadOnlyStillNoSchedulerNoExecution()
    {
        var validator = new NodalOsScheduledReadOnlyRunValidator();
        var schedule = NodalOsScheduledReadOnlyRunFixtures.ValidManualOnlySchedule();
        var request = NodalOsScheduledReadOnlyRunFixtures.RequestFromSchedule(schedule);
        var preview = NodalOsScheduledReadOnlyRunFixtures.ValidDryRunPreview();

        Assert.IsTrue(validator.ValidateSchedule(schedule).IsValid);
        Assert.IsTrue(validator.ValidateRunRequest(request).IsValid);
        Assert.IsTrue(validator.ValidatePreview(preview).IsValid);
        Assert.IsTrue(request.ManualTriggerRequired);
        Assert.IsFalse(request.RuntimeExecutionAllowed);
        Assert.IsTrue(request.RuntimeExecutionDeferred);
        Assert.IsTrue(preview.DryRunOnly);
        Assert.IsFalse(preview.Executed);
    }

    [TestMethod]
    public void NoBroadNexaRenamePerformed()
    {
        var browserContracts = Path.Combine(RepoRoot(), "src", "OneBrain.BrowserExecutor.Contracts");
        var browserCdp = Path.Combine(RepoRoot(), "src", "OneBrain.BrowserExecutor.Cdp");

        Assert.IsTrue(Directory.GetFiles(browserContracts, "Nexa*.cs").Length > 0);
        Assert.IsTrue(Directory.GetFiles(browserCdp, "Nexa*.cs").Length > 0);
    }

    [TestMethod]
    public void NoRuntimeApiUiSchedulerWorkerIntroduced()
    {
        var artifact = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "artifacts",
            "agent-operations",
            "m445",
            "agent-operations-namespace-migration-summary.json"));

        AssertContains(artifact, "\"noRuntimeBehaviorChange\": true");
        AssertContains(artifact, "\"noSchedulerImplemented\": true");
        AssertContains(artifact, "\"noApiImplemented\": true");
        AssertContains(artifact, "\"noUiImplemented\": true");
        AssertContains(artifact, "\"noExecutionImplemented\": true");
    }

    private static void AssertDoesNotReferenceBrowserOrCdp(string project)
    {
        Assert.IsFalse(project.Contains("OneBrain.BrowserExecutor.Cdp", StringComparison.Ordinal));
        Assert.IsFalse(project.Contains("Chrome", StringComparison.Ordinal));
        Assert.IsFalse(project.Contains("Cdp", StringComparison.Ordinal));
        Assert.IsFalse(project.Contains("Playwright", StringComparison.Ordinal));
        Assert.IsFalse(project.Contains("Puppeteer", StringComparison.Ordinal));
        Assert.IsFalse(project.Contains("Selenium", StringComparison.Ordinal));
    }

    private static string ProjectText(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepoRoot(), .. parts]));

    private static IReadOnlyList<string> SourceFiles(params string[] parts)
    {
        var path = Path.Combine([RepoRoot(), .. parts]);

        return Directory
            .GetFiles(path, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static bool IsCompatibilityFile(string file) =>
        file.Contains($"{Path.DirectorySeparatorChar}Compatibility{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

    private static void AssertContains(string text, string expected) =>
        Assert.IsTrue(text.Contains(expected, StringComparison.Ordinal), expected);

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
}
