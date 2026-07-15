namespace OneBrain.Pilot;

public static class PilotRuntimeIntegrationEndpointMapper
{
    public static IEndpointRouteBuilder MapProductLedgerLocalDevRoutePreview(
        this WebApplication app,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(environment);

        ProductLedgerLocalDevRouteEndpointMapper.MapProductLedgerLocalDevRoutePreview(
            (IEndpointRouteBuilder)app,
            environment);
        SelectiveRuntimeInspectorEndpointMapper.MapSelectiveRuntimeInspector(app, environment);
        TestOwnedFileCreateEndpointMapper.MapTestOwnedFileCreateFixture(app, environment);
        BoundedWorkspaceUnderstandingEndpointMapper.MapBoundedWorkspaceUnderstanding(app, environment);
        return app;
    }
}
