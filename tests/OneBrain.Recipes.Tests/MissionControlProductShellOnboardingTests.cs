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

    [TestMethod]
    public async Task RecoveryGuidanceUsesExistingExecutionStateWithoutNewAuthority()
    {
        var snapshot = await MissionControlProductShellEndpointMapper.BuildSnapshotAsync();
        var cases = new[]
        {
            (State: "CandidateStale", Stage: "Revisión requerida", Action: "Revisar y regenerar misión", Detail: "La precondición cambió"),
            (State: "ResultChanged", Stage: "Resultado modificado", Action: "Inspeccionar resultado", Detail: "rollback automático está deshabilitado"),
            (State: "FailedClosed", Stage: "Ejecución detenida", Action: "Revisar causa", Detail: "se detuvo de forma segura")
        };

        foreach (var item in cases)
        {
            var recovery = snapshot with
            {
                WorkspaceSelected = true,
                WorkspacePersisted = true,
                RealMissionDraft = true,
                MissionDraftPersisted = true,
                ActionCandidateKind = "ExactHashUpdate",
                ActionCandidateTarget = "NODAL_HANDOFF.md",
                ActionExecutionState = item.State,
                ActionApprovalAvailable = false,
                ActionExecuted = item.State == "ResultChanged",
                ActionVerified = false,
                ActionRollbackAvailable = false,
                ActionRolledBack = false,
                ProductAuthorityGranted = false
            };
            var html = System.Net.WebUtility.HtmlDecode(MissionControlProductShellHtmlRenderer.Render(recovery));

            StringAssert.Contains(html, item.Stage);
            StringAssert.Contains(html, item.Action);
            StringAssert.Contains(html, item.Detail);
            StringAssert.Contains(html, "data-onboarding-complete=\"false\"");
            Assert.IsFalse(html.Contains("P2c", StringComparison.Ordinal));
            Assert.IsFalse(html.Contains("Próximo gate productivo", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(html.Contains("<form", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(recovery.ProductAuthorityGranted);
        }
    }
}
