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
        LocalWorkspaceSelectionEndpointMapper.MapLocalWorkspaceSelection(
            app,
            environment,
            workspaceSelectionServiceFactory);
        MissionControlProductShellEndpointMapper.MapMissionControlProductShell(
            app,
            environment,
            workspaceSelectionServiceFactory);
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
