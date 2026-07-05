using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using OneBrain.Core.Approval;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneBrain.Pilot;

public static class ProductLedgerLocalDevRouteEndpointMapper
{
    public const string LocalApprovalDecisionRoute =
        "/internal/product-ledger/approval/decision";

    public const string LocalApprovalDecisionStateRoute =
        "/internal/product-ledger/approval/state";

    public const string LocalOnlyRouteResponseEvidenceMode =
        "LOCAL_ONLY_DEVELOPMENT_ONLY_HTTP_RESPONSE_PREVIEW_NO_EXECUTION";

    private static readonly JsonSerializerOptions RouteJsonOptions = CreateRouteJsonOptions();

    public static IEndpointRouteBuilder MapProductLedgerLocalDevRoutePreview(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment)
        => endpoints.MapProductLedgerLocalDevRoutePreview(environment, ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe);

    public static IEndpointRouteBuilder MapProductLedgerLocalDevRoutePreview(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource)
        => endpoints.MapProductLedgerLocalDevRoutePreview(
            environment,
            readModelSource,
            CreateDefaultDecisionStateStore());

    public static IEndpointRouteBuilder MapProductLedgerLocalDevRoutePreview(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionStateStore decisionStateStore)
    {
        if (!environment.IsDevelopment())
        {
            return endpoints;
        }

        endpoints.MapGet(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            () => RenderProductLedgerLocalDevRoutePreview(readModelSource, decisionStateStore.Read()));
        endpoints.MapGet(
            LocalApprovalDecisionStateRoute,
            () => Results.Json(decisionStateStore.Read(), RouteJsonOptions));
        Func<HttpContext, Task<IResult>> persistDecisionHandler =
            context => PersistProductLedgerLocalApprovalDecisionAsync(
                context,
                readModelSource,
                decisionStateStore);
        endpoints.MapPost(LocalApprovalDecisionRoute, persistDecisionHandler);
        return endpoints;
    }

    public static IResult RenderProductLedgerLocalDevRoutePreview()
        => RenderProductLedgerLocalDevRoutePreview(ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe);

    public static IResult RenderProductLedgerLocalDevRoutePreview(
        ProductLedgerOperatorSurfaceReadModelSource readModelSource)
        => RenderProductLedgerLocalDevRoutePreview(
            readModelSource,
            ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly);

    public static IResult RenderProductLedgerLocalDevRoutePreview(
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionSnapshot approvalDecisionState)
    {
        var result = new ProductLedgerLocalDevRoutePreview().Render(
            ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest(),
            readModelSource,
            approvalDecisionState);

        return result.Decision == ProductLedgerLocalDevRoutePreviewDecision.RenderedLocalDevInternalPreview
            ? Results.Content(result.HtmlSnapshot, result.ContentType)
            : Results.NotFound();
    }

    private static async Task<IResult> PersistProductLedgerLocalApprovalDecisionAsync(
        HttpContext context,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionStateStore decisionStateStore)
    {
        if (context.Request.ContentLength is null or <= 0 or > 8192)
        {
            return Results.Json(ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly with
            {
                Blockers = [ProductLedgerLocalApprovalDecisionBlocker.MissingRequest],
                StatusText = ProductLedgerLocalApprovalDecisionStateStore.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(context.Request.ContentType, "application/json", StringComparison.OrdinalIgnoreCase)
            && context.Request.ContentType?.StartsWith("application/json;", StringComparison.OrdinalIgnoreCase) != true)
        {
            return Results.Json(ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly with
            {
                Blockers = [ProductLedgerLocalApprovalDecisionBlocker.MissingRequest],
                StatusText = ProductLedgerLocalApprovalDecisionStateStore.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        ProductLedgerLocalApprovalDecisionBody? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<ProductLedgerLocalApprovalDecisionBody>(
                context.Request.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                context.RequestAborted);
        }
        catch (JsonException)
        {
            body = null;
        }

        var snapshot = decisionStateStore.Persist(ToStoreRequest(body, CurrentCandidate(readModelSource)));
        var statusCode = snapshot.Decision switch
        {
            ProductLedgerLocalApprovalDecisionStoreDecision.PersistedLocalOnly => StatusCodes.Status200OK,
            ProductLedgerLocalApprovalDecisionStoreDecision.IdempotentReplay => StatusCodes.Status200OK,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Json(snapshot, RouteJsonOptions, statusCode: statusCode);
    }

    private static ProductLedgerLocalApprovalDecisionStateRequest? ToStoreRequest(
        ProductLedgerLocalApprovalDecisionBody? body,
        ProductLedgerLocalApprovalExecutionResult candidate)
    {
        if (body is null)
        {
            return null;
        }

        return new ProductLedgerLocalApprovalDecisionStateRequest(
            ExplicitLocalOnlyStatePersistenceScope: body.ExplicitLocalOnlyStatePersistenceScope == true,
            ApprovalId: body.ApprovalId,
            CandidateResult: candidate,
            CandidateEvidenceHash: body.CandidateEvidenceHash,
            CurrentEvidenceHash: body.CurrentEvidenceHash,
            OperatorDecision: ParseDecision(body.OperatorDecision),
            DecidedAtUtc: body.DecidedAtUtc,
            OperatorClassification: body.OperatorClassification,
            OperatorNote: body.OperatorNote,
            EvidenceReferences: body.EvidenceReferences ?? [],
            RequestsPublicUiAction: body.RequestsPublicUiAction == true,
            RequestsProductCommandExecution: body.RequestsProductCommandExecution == true,
            RequestsProductCommandHandler: body.RequestsProductCommandHandler == true,
            RequestsProductiveServiceRegistration: body.RequestsProductiveServiceRegistration == true,
            RequestsPhysicalExport: body.RequestsPhysicalExport == true,
            RequestsFileWriteOutsideApprovalStateStore: body.RequestsFileWriteOutsideApprovalStateStore == true,
            ClaimsArbitraryPathInput: body.ClaimsArbitraryPathInput == true,
            ClaimsProviderCloudNetwork: body.ClaimsProviderCloudNetwork == true,
            ClaimsDbMigration: body.ClaimsDbMigration == true,
            ClaimsKmsWormExternalTrust: body.ClaimsKmsWormExternalTrust == true,
            ClaimsBrowserCdpWcuOcrRecipesLive: body.ClaimsBrowserCdpWcuOcrRecipesLive == true,
            ClaimsPilotRun: body.ClaimsPilotRun == true,
            ClaimsReleaseCommercial: body.ClaimsReleaseCommercial == true);
    }

    private static ProductLedgerLocalApprovalExecutionResult CurrentCandidate(
        ProductLedgerOperatorSurfaceReadModelSource readModelSource) =>
        new ProductLedgerLocalDevRoutePreview()
            .Render(
                ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest(),
                readModelSource)
            .CanonicalSurface
            .ApprovalExecutionCandidatePreview;

    private static ProductLedgerLocalApprovalOperatorDecisionKind? ParseDecision(string? value)
    {
        if (Enum.TryParse<ProductLedgerLocalApprovalOperatorDecisionKind>(
            value,
            ignoreCase: true,
            out var decision))
        {
            return decision;
        }

        return null;
    }

    private static ProductLedgerLocalApprovalDecisionStateStore CreateDefaultDecisionStateStore() =>
        new(new ProductLedgerLocalApprovalDecisionStateStoreOptions(
            StoreRootPath: Path.Combine(Path.GetTempPath(), "nodal-os-product-ledger-local-approval-state"),
            ExplicitLocalOnlyStateStore: true,
            AllowsArbitraryPathInput: false,
            AllowsExport: false,
            AllowsNetwork: false,
            AllowsDb: false,
            AllowsReleaseCommercial: false));

    private static JsonSerializerOptions CreateRouteJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private sealed record ProductLedgerLocalApprovalDecisionBody(
        bool? ExplicitLocalOnlyStatePersistenceScope,
        string? ApprovalId,
        string? CandidateEvidenceHash,
        string? CurrentEvidenceHash,
        string? OperatorDecision,
        DateTimeOffset? DecidedAtUtc,
        string? OperatorClassification,
        string? OperatorNote,
        IReadOnlyList<string>? EvidenceReferences,
        bool? RequestsPublicUiAction,
        bool? RequestsProductCommandExecution,
        bool? RequestsProductCommandHandler,
        bool? RequestsProductiveServiceRegistration,
        bool? RequestsPhysicalExport,
        bool? RequestsFileWriteOutsideApprovalStateStore,
        bool? ClaimsArbitraryPathInput,
        bool? ClaimsProviderCloudNetwork,
        bool? ClaimsDbMigration,
        bool? ClaimsKmsWormExternalTrust,
        bool? ClaimsBrowserCdpWcuOcrRecipesLive,
        bool? ClaimsPilotRun,
        bool? ClaimsReleaseCommercial);
}
