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

        var packaged = NodalOsDesktopLaunchRuntime.IsPackaged();
        var localDiagnostics = NodalOsLocalDiagnostics.CreateDefault();
        var teachNodalProductService = NodalOsTeachNodalProductRuntime.CreateDefault();
        Func<NodalOsWorkspaceSelectionService> workspaceSelectionServiceFactory =
            NodalOsWorkspaceSelectionRuntime.CreateDefault;
        Func<NodalOsWorkspaceMissionDraftService> missionDraftServiceFactory =
            NodalOsWorkspaceMissionDraftRuntime.CreateDefault;
        Func<NodalOsWorkspaceHandoffExecutionService> handoffExecutionServiceFactory =
            NodalOsWorkspaceHandoffExecutionRuntime.CreateDefault;
        Func<NodalOsByokModelConfigurationService> byokModelConfigurationServiceFactory =
            NodalOsByokModelConfigurationRuntime.CreateDefault;

        localDiagnostics.Attach(
            app,
            packaged,
            missionDraftServiceFactory,
            handoffExecutionServiceFactory);

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
        MissionControlProductHandoffExportEndpointMapper.MapMissionControlProductHandoffExport(
            app,
            cancellationToken => MissionControlProductShellEndpointMapper.BuildSnapshotAsync(
                cancellationToken,
                workspaceSelectionServiceFactory(),
                missionDraftServiceFactory(),
                handoffExecutionServiceFactory(),
                byokModelConfigurationServiceFactory()));
        NodalOsLocalDiagnosticsEndpointMapper.MapNodalOsLocalDiagnostics(
            app,
            () => localDiagnostics,
            () => packaged);
        NodalOsTeachNodalProductEndpointMapper.MapNodalOsTeachNodalProductSurface(
            app,
            () => teachNodalProductService);
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