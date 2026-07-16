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

    [DataTestMethod]
    [DataRow("CandidateStale", "Revisión requerida", "Revisar y regenerar misión", "La precondición cambió")]
    [DataRow("ResultChanged", "Resultado modificado", "Inspeccionar resultado", "rollback automático está deshabilitado")]
    [DataRow("FailedClosed", "Ejecución detenida", "Revisar causa", "se detuvo de forma segura")]
    public async Task RecoveryGuidanceUsesExistingExecutionStateWithoutNewAuthority(
        string state,
        string expectedStage,
        string expectedAction,
        string expectedDetail)
    {
        var snapshot = await MissionControlProductShellEndpointMapper.BuildSnapshotAsync();
        var recovery = snapshot with
        {
            WorkspaceSelected = true,
            WorkspacePersisted = true,
            RealMissionDraft = true,
            MissionDraftPersisted = true,
            ActionCandidateKind = "ExactHashUpdate",
            ActionCandidateTarget = "NODAL_HANDOFF.md",
            ActionExecutionState = state,
            ActionApprovalAvailable = false,
            ActionExecuted = state == "ResultChanged",
            ActionVerified = false,
            ActionRollbackAvailable = false,
            ActionRolledBack = false,
            ProductAuthorityGranted = false
        };
        var html = System.Net.WebUtility.HtmlDecode(MissionControlProductShellHtmlRenderer.Render(recovery));

        StringAssert.Contains(html, expectedStage);
        StringAssert.Contains(html, expectedAction);
        StringAssert.Contains(html, expectedDetail);
        StringAssert.Contains(html, "data-onboarding-complete=\"false\"");
        Assert.IsFalse(html.Contains("P2c", StringComparison.Ordinal));
        Assert.IsFalse(html.Contains("Próximo gate productivo", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(recovery.ProductAuthorityGranted);
    }
}
