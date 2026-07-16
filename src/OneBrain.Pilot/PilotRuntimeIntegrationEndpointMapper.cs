namespace OneBrain.Pilot;

public static class PilotRuntimeIntegrationEndpointMapper
{
    public static IEndpointRouteBuilder MapProductLedgerLocalDevRoutePreview(
        this WebApplication app,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(environment);

        MissionControlProductShellEndpointMapper.MapMissionControlProductShell(app, environment);
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
