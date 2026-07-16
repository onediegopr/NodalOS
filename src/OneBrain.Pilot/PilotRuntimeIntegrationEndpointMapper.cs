using OneBrain.AgentOperations.Core.Models;
using OneBrain.AgentOperations.Core.Workspace;

namespace OneBrain.Pilot;

public static class PilotRuntimeIntegrationEndpointMapper
{
    public static IEndpointRouteBuilder MapProductLedgerLocalDevRoutePreview(
        this WebApplication app,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(environment);

        Func<NodalOsWorkspaceSelectionService> workspaceSelectionServiceFactory =
            NodalOsWorkspaceSelectionRuntime.CreateDefault;
        Func<NodalOsWorkspaceMissionDraftService> missionDraftServiceFactory =
            NodalOsWorkspaceMissionDraftRuntime.CreateDefault;
        Func<NodalOsWorkspaceHandoffExecutionService> handoffExecutionServiceFactory =
            NodalOsWorkspaceHandoffExecutionRuntime.CreateDefault;
        Func<NodalOsByokModelConfigurationService> byokModelConfigurationServiceFactory =
            NodalOsByokModelConfigurationRuntime.CreateDefault;
        LocalWorkspaceSelectionEndpointMapper.MapLocalWorkspaceSelection(
            app,
            environment,
            workspaceSelectionServiceFactory);
        RealWorkspaceMissionDraftEndpointMapper.MapRealWorkspaceMissionDraft(
            app,
            environment,
            missionDraftServiceFactory);
        RealWorkspaceHandoffExecutionEndpointMapper.MapRealWorkspaceHandoffExecution(
            app,
            environment,
            handoffExecutionServiceFactory);
        NodalOsByokModelConfigurationEndpointMapper.MapNodalOsByokModelConfiguration(
            app,
            environment,
            byokModelConfigurationServiceFactory);
        MissionControlProductShellEndpointMapper.MapMissionControlProductShell(
            app,
            environment,
            workspaceSelectionServiceFactory,
            missionDraftServiceFactory,
            handoffExecutionServiceFactory,
            byokModelConfigurationServiceFactory);
        ProductLedgerLocalDevRouteEndpointMapper.MapProductLedgerLocalDevRoutePreview(
            (IEndpointRouteBuilder)app,
            environment);
        SelectiveRuntimeInspectorEndpointMapper.MapSelectiveRuntimeInspector(app, environment);
        TestOwnedFileCreateEndpointMapper.MapTestOwnedFileCreateFixture(app, environment);
        TestOwnedFileUpdateEndpointMapper.MapTestOwnedFileUpdateFixture(app, environment);
        BoundedWorkspaceUnderstandingEndpointMapper.MapBoundedWorkspaceUnderstanding(app, environment);
        BoundedWorkspaceHandoffExportEndpointMapper.MapBoundedWorkspaceHandoffExport(app, environment);
        TeachNodalLocalDevSurface.MapTeachNodalLocalDevSurface(app, environment);
        return app;
    }
}
