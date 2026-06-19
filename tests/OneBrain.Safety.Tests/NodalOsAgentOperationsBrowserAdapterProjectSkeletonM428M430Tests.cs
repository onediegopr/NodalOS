using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Adapters.Browser;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BrowserAdapterProjectSkeleton")]
[TestCategory("OrchestrationInProcessFacadeV1")]
[TestCategory("AgentOperationsCoreServicesExtraction")]
[TestCategory("BrowserAdapterBoundary")]
public sealed class NodalOsAgentOperationsBrowserAdapterProjectSkeletonM428M430Tests
{
    [TestMethod]
    public void BrowserAdapterProjectExists()
    {
        Assert.IsTrue(File.Exists(AdapterProjectPath()));
    }

    [TestMethod]
    public void BrowserAdapterProjectReferencesAgentOperationsContracts()
    {
        StringAssert.Contains(ReadAdapterProject(), "OneBrain.AgentOperations.Contracts");
    }

    [TestMethod]
    public void BrowserAdapterProjectReferencesAgentOperationsCore()
    {
        StringAssert.Contains(ReadAdapterProject(), "OneBrain.AgentOperations.Core");
    }

    [TestMethod]
    public void BrowserAdapterProjectDoesNotReferenceBrowserExecutorCdp()
    {
        Assert.IsFalse(ReadAdapterProject().Contains("OneBrain.BrowserExecutor.Cdp", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(ReadAdapterProject().Contains("BrowserExecutor.Cdp", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserAdapterProjectDoesNotReferenceChromeCdpPackages()
    {
        var project = ReadAdapterProject();

        Assert.IsFalse(project.Contains("Chrome", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(project.Contains("Cdp", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(project.Contains("Microsoft.ML.OnnxRuntime", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void AgentOperationsCoreStillDoesNotReferenceBrowserExecutorCdp()
    {
        var project = File.ReadAllText(Path.Combine(CoreDirectory(), "OneBrain.AgentOperations.Core.csproj"));

        Assert.IsFalse(project.Contains("OneBrain.BrowserExecutor.Cdp", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(project.Contains("BrowserExecutor.Cdp", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void AgentOperationsContractsStillDoesNotReferenceBrowserExecutorCdp()
    {
        var project = File.ReadAllText(Path.Combine(ContractsDirectory(), "OneBrain.AgentOperations.Contracts.csproj"));

        Assert.IsFalse(project.Contains("OneBrain.BrowserExecutor.Cdp", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(project.Contains("BrowserExecutor.Cdp", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserAdapterBoundaryMarkerExists()
    {
        Assert.IsTrue(File.Exists(Path.Combine(AdapterDirectory(), "NodalOsBrowserAgentOperationsAdapterBoundary.cs")));
        StringAssert.Contains(NodalOsBrowserAgentOperationsAdapterBoundary.BoundaryName, "NODAL OS");
    }

    [TestMethod]
    public void BoundaryMarkerRuntimeBehaviorImplementedFalse()
    {
        Assert.IsFalse(MarkerConstant(nameof(NodalOsBrowserAgentOperationsAdapterBoundary.RuntimeBehaviorImplemented)));
    }

    [TestMethod]
    public void BoundaryMarkerBrowserRuntimeMovedFalse()
    {
        Assert.IsFalse(MarkerConstant(nameof(NodalOsBrowserAgentOperationsAdapterBoundary.BrowserRuntimeMoved)));
    }

    [TestMethod]
    public void BoundaryMarkerAdapterProjectSkeletonTrue()
    {
        Assert.IsTrue(MarkerConstant(nameof(NodalOsBrowserAgentOperationsAdapterBoundary.AdapterProjectSkeleton)));
    }

    [TestMethod]
    public void ChromeCdpBrowserExecutorStillInBrowserExecutorCdp()
    {
        Assert.IsTrue(File.Exists(Path.Combine(BrowserExecutorCdpDirectory(), "ChromeCdpBrowserExecutor.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(AdapterDirectory(), "ChromeCdpBrowserExecutor.cs")));
    }

    [TestMethod]
    public void BrowserRuntimeSmokeStillInBrowserExecutorCdp()
    {
        Assert.IsTrue(File.Exists(Path.Combine(BrowserExecutorCdpDirectory(), "BrowserRuntimeSmoke.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(AdapterDirectory(), "BrowserRuntimeSmoke.cs")));
    }

    [TestMethod]
    public void BrowserPersistentAuditLedgerStillInBrowserExecutorCdp()
    {
        Assert.IsTrue(File.Exists(Path.Combine(BrowserExecutorCdpDirectory(), "BrowserPersistentAuditLedger.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(AdapterDirectory(), "BrowserPersistentAuditLedger.cs")));
    }

    [TestMethod]
    public void NoRuntimeBehaviorChangeArtifactFlag()
    {
        StringAssert.Contains(ReadArtifact(), "\"noRuntimeBehaviorChange\": true");
    }

    [TestMethod]
    public void NoUiOrOrchestrationArtifactFlag()
    {
        var artifact = ReadArtifact();

        StringAssert.Contains(artifact, "\"noUiImplemented\": true");
        StringAssert.Contains(artifact, "\"noOrchestrationApiImplemented\": true");
        StringAssert.Contains(artifact, "\"noExecutionImplemented\": true");
    }

    [TestMethod]
    public void RoadmapUpdatedWithM428M430()
    {
        var roadmap = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "roadmap", "nodal-os-roadmap-vnext.md"));

        StringAssert.Contains(roadmap, "Agent Operations Browser Adapter Project Skeleton M430");
        StringAssert.Contains(roadmap, "M431-M433 Scheduled Read-Only Runs Decision Record");
    }

    private static string ReadAdapterProject() =>
        File.ReadAllText(AdapterProjectPath());

    private static bool MarkerConstant(string name) =>
        (bool)(typeof(NodalOsBrowserAgentOperationsAdapterBoundary)
            .GetField(name)
            ?.GetRawConstantValue() ?? false);

    private static string ReadArtifact() =>
        File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "artifacts",
            "agent-operations",
            "m430",
            "agent-operations-browser-adapter-project-skeleton-summary.json"));

    private static string AdapterProjectPath() =>
        Path.Combine(AdapterDirectory(), "OneBrain.AgentOperations.Adapters.Browser.csproj");

    private static string AdapterDirectory() =>
        Path.Combine(FindRepoRoot(), "src", "OneBrain.AgentOperations.Adapters.Browser");

    private static string CoreDirectory() =>
        Path.Combine(FindRepoRoot(), "src", "OneBrain.AgentOperations.Core");

    private static string ContractsDirectory() =>
        Path.Combine(FindRepoRoot(), "src", "OneBrain.AgentOperations.Contracts");

    private static string BrowserExecutorCdpDirectory() =>
        Path.Combine(FindRepoRoot(), "src", "OneBrain.BrowserExecutor.Cdp");

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
