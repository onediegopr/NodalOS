using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("AiriSelectiveRuntime")]
[TestCategory("MvpVerticalSlice")]
[TestCategory("MissionControlProductShell")]
public sealed class MissionControlProductShellSelectiveRuntimeInspectorRouteTestsOnboarding
{
    [TestMethod]
    public async Task QuickStartIsDerivedFromExistingSnapshotAndKeepsAuthorityClosed()
    {
        var snapshot = await MissionControlProductShellEndpointMapper.BuildSnapshotAsync();
        var html = MissionControlProductShellHtmlRenderer.Render(snapshot);

        Assert.IsFalse(snapshot.ProductAuthorityGranted);
        StringAssert.Contains(html, "data-onboarding-complete=\"false\"");
        StringAssert.Contains(html, "data-section-id='quick-start'");
        StringAssert.Contains(html, "data-onboarding-step='workspace'");
        StringAssert.Contains(html, "Seleccionar workspace local");
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task CompletedLoopResumesAtNextMissionWithoutAnotherGate()
    {
        var snapshot = await MissionControlProductShellEndpointMapper.BuildSnapshotAsync();
        var completed = snapshot with
        {
            WorkspaceSelected = true,
            WorkspacePersisted = true,
            RealMissionDraft = true,
            MissionDraftPersisted = true,
            ActionExecutionState = "Completed",
            ActionExecuted = true,
            ActionVerified = true,
            ActionRollbackAvailable = true,
            ByokConfigured = true,
            ModelConnectionVerified = true,
            FixtureBacked = false
        };
        var html = MissionControlProductShellHtmlRenderer.Render(completed);

        StringAssert.Contains(html, "data-onboarding-complete=\"true\"");
        StringAssert.Contains(html, "data-section-id=\"resume\"");
        StringAssert.Contains(System.Net.WebUtility.HtmlDecode(html), "Crear siguiente misión");
        Assert.IsFalse(html.Contains("Próximo gate productivo", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(completed.ProductAuthorityGranted);
    }
}
