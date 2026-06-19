using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("BrowserAdapterBoundary")]
[TestCategory("AgentOperationsCoreServicesExtraction")]
[TestCategory("AgentOperationsContractsExtraction")]
[TestCategory("BrowserRuntimeSmoke")]
[TestCategory("PackageRegistryWorkerIntegration")]
public sealed class NodalOsAgentOperationsBrowserAdapterBoundaryM413M415Tests
{
    [TestMethod]
    public void BrowserAdapterBoundaryAuditReportExists()
    {
        Assert.IsTrue(File.Exists(AuditReportPath()));
    }

    [TestMethod]
    public void BrowserAdapterBoundaryReportExists()
    {
        Assert.IsTrue(File.Exists(BoundaryReportPath()));
    }

    [TestMethod]
    public void ArtifactExists()
    {
        Assert.IsTrue(File.Exists(ArtifactPath()));
    }

    [TestMethod]
    public void AgentOperationsCoreDoesNotReferenceBrowserExecutorCdp()
    {
        var project = File.ReadAllText(Path.Combine(CoreDirectory(), "OneBrain.AgentOperations.Core.csproj"));

        Assert.IsFalse(project.Contains("OneBrain.BrowserExecutor.Cdp", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(project.Contains("BrowserExecutor.Cdp", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void AgentOperationsContractsDoesNotReferenceBrowserExecutorCdp()
    {
        var project = File.ReadAllText(Path.Combine(ContractsDirectory(), "OneBrain.AgentOperations.Contracts.csproj"));

        Assert.IsFalse(project.Contains("OneBrain.BrowserExecutor.Cdp", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(project.Contains("BrowserExecutor.Cdp", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserExecutorCdpReferencesAgentOperationsCore()
    {
        var project = File.ReadAllText(Path.Combine(BrowserExecutorCdpDirectory(), "OneBrain.BrowserExecutor.Cdp.csproj"));

        StringAssert.Contains(project, "OneBrain.AgentOperations.Core");
    }

    [TestMethod]
    public void ChromeCdpBrowserExecutorStayedInBrowserExecutorCdp()
    {
        Assert.IsTrue(File.Exists(Path.Combine(BrowserExecutorCdpDirectory(), "ChromeCdpBrowserExecutor.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(CoreDirectory(), "ChromeCdpBrowserExecutor.cs")));
    }

    [TestMethod]
    public void BrowserRuntimeSmokeStayedInBrowserExecutorCdp()
    {
        Assert.IsTrue(File.Exists(Path.Combine(BrowserExecutorCdpDirectory(), "BrowserRuntimeSmoke.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(CoreDirectory(), "BrowserRuntimeSmoke.cs")));
    }

    [TestMethod]
    public void ArtifactMarksNoRuntimeBehaviorChange()
    {
        StringAssert.Contains(ReadArtifact(), "\"noRuntimeBehaviorChange\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoUiImplemented()
    {
        StringAssert.Contains(ReadArtifact(), "\"noUiImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoOrchestrationApi()
    {
        StringAssert.Contains(ReadArtifact(), "\"noOrchestrationApiImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoExecution()
    {
        StringAssert.Contains(ReadArtifact(), "\"noExecutionImplemented\": true");
    }

    [TestMethod]
    public void ArtifactClassifiesBrowserPersistentAuditLedger()
    {
        StringAssert.Contains(ReadArtifact(), "\"browserPersistentAuditLedgerClassified\": true");
        StringAssert.Contains(ReadAuditReport(), "AuditLedgerBrowserSpecific");
    }

    [TestMethod]
    public void RoadmapUpdatedWithM413M415()
    {
        var roadmap = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "roadmap", "nodal-os-roadmap-vnext.md"));

        StringAssert.Contains(roadmap, "Agent Operations Browser Adapter Boundary M415");
        StringAssert.Contains(roadmap, "M416-M418 Orchestration API Decision Record");
    }

    [TestMethod]
    public void ReportsUseNodalOsName_NotNexaProjectName()
    {
        var audit = ReadAuditReport();
        var boundary = File.ReadAllText(BoundaryReportPath());

        StringAssert.Contains(audit, "Project: NODAL OS");
        StringAssert.Contains(boundary, "Project: NODAL OS");
        Assert.IsFalse(audit.Contains("Project: NEXA", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(boundary.Contains("Project: NEXA", StringComparison.OrdinalIgnoreCase));
    }

    private static string ReadArtifact() =>
        File.ReadAllText(ArtifactPath());

    private static string ReadAuditReport() =>
        File.ReadAllText(AuditReportPath());

    private static string AuditReportPath() =>
        Path.Combine(FindRepoRoot(), "docs", "reports", "agent-operations-browser-adapter-boundary-audit-m413.md");

    private static string BoundaryReportPath() =>
        Path.Combine(FindRepoRoot(), "docs", "reports", "agent-operations-browser-adapter-boundary-m415.md");

    private static string ArtifactPath() =>
        Path.Combine(FindRepoRoot(), "artifacts", "agent-operations", "m415", "agent-operations-browser-adapter-boundary-summary.json");

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
