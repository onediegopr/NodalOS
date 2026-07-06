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

    public const string LocalApprovalExecutionRoute =
        "/internal/product-ledger/approval/execute";

    public const string LocalApprovalExecutionStateRoute =
        "/internal/product-ledger/approval/execution-state";

    public const string LocalBoundedApprovalExecutionRoute =
        "/internal/product-ledger/approval/execute-bounded";

    public const string LocalBoundedApprovalExecutionStateRoute =
        "/internal/product-ledger/approval/bounded-state";

    public const string LocalApprovedHandoffReportDraftRoute =
        "/internal/product-ledger/approval/create-local-handoff-draft";

    public const string LocalApprovedHandoffReportDraftStateRoute =
        "/internal/product-ledger/approval/local-handoff-draft-state";

    public const string LocalWorkspaceTestJailHandoffDraftRoute =
        "/internal/product-ledger/approval/create-workspace-test-jail-handoff-draft";

    public const string LocalWorkspaceTestJailHandoffDraftStateRoute =
        "/internal/product-ledger/approval/workspace-test-jail-handoff-draft-state";

    public const string LocalUserWorkspaceAllowlistedHandoffDraftRoute =
        "/internal/product-ledger/approval/create-user-workspace-allowlisted-handoff-draft";

    public const string LocalUserWorkspaceAllowlistedHandoffDraftStateRoute =
        "/internal/product-ledger/approval/user-workspace-allowlisted-handoff-draft-state";

    public const string LocalOperatorSurfaceLatestStateSnapshotRoute =
        "/internal/product-ledger/operator-surface/create-latest-state-snapshot";

    public const string LocalOperatorSurfaceLatestStateSnapshotStateRoute =
        "/internal/product-ledger/operator-surface/latest-state-snapshot-state";

    public const string LocalOperatorSurfaceLatestStateManifestRoute =
        "/internal/product-ledger/operator-surface/create-latest-state-manifest";

    public const string LocalOperatorSurfaceLatestStateManifestStateRoute =
        "/internal/product-ledger/operator-surface/latest-state-manifest-state";

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
            CreateDefaultDecisionStateStore(),
            CreateDefaultNoOpExecutor(),
            CreateDefaultBoundedActionExecutor(),
            CreateDefaultHandoffReportDraftExecutor(),
            CreateDefaultWorkspaceTestJailHandoffDraftExecutor());

    public static IEndpointRouteBuilder MapProductLedgerLocalDevRoutePreview(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionStateStore decisionStateStore)
        => endpoints.MapProductLedgerLocalDevRoutePreview(
            environment,
            readModelSource,
            decisionStateStore,
            CreateDefaultNoOpExecutor(),
            CreateDefaultBoundedActionExecutor(),
            CreateDefaultHandoffReportDraftExecutor(),
            CreateDefaultWorkspaceTestJailHandoffDraftExecutor());

    public static IEndpointRouteBuilder MapProductLedgerLocalDevRoutePreview(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionStateStore decisionStateStore,
        ProductLedgerLocalApprovedActionNoOpExecutor noOpExecutor)
        => endpoints.MapProductLedgerLocalDevRoutePreview(
            environment,
            readModelSource,
            decisionStateStore,
            noOpExecutor,
            CreateDefaultBoundedActionExecutor(),
            CreateDefaultHandoffReportDraftExecutor(),
            CreateDefaultWorkspaceTestJailHandoffDraftExecutor());

    public static IEndpointRouteBuilder MapProductLedgerLocalDevRoutePreview(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionStateStore decisionStateStore,
        ProductLedgerLocalApprovedActionNoOpExecutor noOpExecutor,
        ProductLedgerLocalBoundedApprovedActionExecutor boundedActionExecutor)
        => endpoints.MapProductLedgerLocalDevRoutePreview(
            environment,
            readModelSource,
            decisionStateStore,
            noOpExecutor,
            boundedActionExecutor,
            CreateDefaultHandoffReportDraftExecutor(),
            CreateDefaultWorkspaceTestJailHandoffDraftExecutor());

    public static IEndpointRouteBuilder MapProductLedgerLocalDevRoutePreview(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionStateStore decisionStateStore,
        ProductLedgerLocalApprovedActionNoOpExecutor noOpExecutor,
        ProductLedgerLocalBoundedApprovedActionExecutor boundedActionExecutor,
        ProductLedgerLocalApprovedHandoffReportDraftExecutor handoffReportDraftExecutor)
        => endpoints.MapProductLedgerLocalDevRoutePreview(
            environment,
            readModelSource,
            decisionStateStore,
            noOpExecutor,
            boundedActionExecutor,
            handoffReportDraftExecutor,
            CreateDefaultWorkspaceTestJailHandoffDraftExecutor());

    public static IEndpointRouteBuilder MapProductLedgerLocalDevRoutePreview(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionStateStore decisionStateStore,
        ProductLedgerLocalApprovedActionNoOpExecutor noOpExecutor,
        ProductLedgerLocalBoundedApprovedActionExecutor boundedActionExecutor,
        ProductLedgerLocalApprovedHandoffReportDraftExecutor handoffReportDraftExecutor,
        ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor workspaceTestJailHandoffDraftExecutor,
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor? userWorkspaceAllowlistedHandoffDraftExecutor = null,
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor? latestStateSnapshotExecutor = null,
        ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter? latestStateManifestWriter = null)
    {
        userWorkspaceAllowlistedHandoffDraftExecutor ??= CreateDefaultUserWorkspaceAllowlistedHandoffDraftExecutor();
        latestStateSnapshotExecutor ??= CreateDefaultLatestStateSnapshotExecutor();
        latestStateManifestWriter ??= CreateDefaultLatestStateManifestWriter();
        if (!environment.IsDevelopment())
        {
            return endpoints;
        }

        endpoints.MapGet(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            () => RenderProductLedgerLocalDevRoutePreview(readModelSource, decisionStateStore.Read(), noOpExecutor.Read(), boundedActionExecutor.Read(), handoffReportDraftExecutor.Read(), workspaceTestJailHandoffDraftExecutor.Read(), userWorkspaceAllowlistedHandoffDraftExecutor.Read(), latestStateSnapshotExecutor.Read(), latestStateManifestWriter.Read()));
        endpoints.MapGet(
            LocalApprovalDecisionStateRoute,
            () => Results.Json(decisionStateStore.Read(), RouteJsonOptions));
        endpoints.MapGet(
            LocalApprovalExecutionStateRoute,
            () => Results.Json(noOpExecutor.Read(), RouteJsonOptions));
        endpoints.MapGet(
            LocalBoundedApprovalExecutionStateRoute,
            () => Results.Json(boundedActionExecutor.Read(), RouteJsonOptions));
        endpoints.MapGet(
            LocalApprovedHandoffReportDraftStateRoute,
            () => Results.Json(handoffReportDraftExecutor.Read(), RouteJsonOptions));
        endpoints.MapGet(
            LocalWorkspaceTestJailHandoffDraftStateRoute,
            () => Results.Json(workspaceTestJailHandoffDraftExecutor.Read(), RouteJsonOptions));
        endpoints.MapGet(
            LocalUserWorkspaceAllowlistedHandoffDraftStateRoute,
            () => Results.Json(userWorkspaceAllowlistedHandoffDraftExecutor.Read(), RouteJsonOptions));
        endpoints.MapGet(
            LocalOperatorSurfaceLatestStateSnapshotStateRoute,
            () => Results.Json(latestStateSnapshotExecutor.Read(), RouteJsonOptions));
        endpoints.MapGet(
            LocalOperatorSurfaceLatestStateManifestStateRoute,
            () => Results.Json(latestStateManifestWriter.Read(), RouteJsonOptions));
        Func<HttpContext, Task<IResult>> persistDecisionHandler =
            context => PersistProductLedgerLocalApprovalDecisionAsync(
                context,
                readModelSource,
                decisionStateStore);
        endpoints.MapPost(LocalApprovalDecisionRoute, persistDecisionHandler);
        Func<HttpContext, Task<IResult>> executeNoOpHandler =
            context => ExecuteProductLedgerLocalApprovalNoOpAsync(
                context,
                readModelSource,
                decisionStateStore,
                noOpExecutor);
        endpoints.MapPost(LocalApprovalExecutionRoute, executeNoOpHandler);
        Func<HttpContext, Task<IResult>> executeBoundedHandler =
            context => ExecuteProductLedgerLocalBoundedApprovalActionAsync(
                context,
                readModelSource,
                decisionStateStore,
                noOpExecutor,
                boundedActionExecutor);
        endpoints.MapPost(LocalBoundedApprovalExecutionRoute, executeBoundedHandler);
        Func<HttpContext, Task<IResult>> createHandoffDraftHandler =
            context => CreateProductLedgerLocalApprovedHandoffReportDraftAsync(
                context,
                readModelSource,
                decisionStateStore,
                noOpExecutor,
                boundedActionExecutor,
                handoffReportDraftExecutor);
        endpoints.MapPost(LocalApprovedHandoffReportDraftRoute, createHandoffDraftHandler);
        Func<HttpContext, Task<IResult>> createWorkspaceTestJailHandoffDraftHandler =
            context => CreateProductLedgerLocalWorkspaceTestJailHandoffDraftAsync(
                context,
                readModelSource,
                decisionStateStore,
                noOpExecutor,
                boundedActionExecutor,
                handoffReportDraftExecutor,
                workspaceTestJailHandoffDraftExecutor);
        endpoints.MapPost(LocalWorkspaceTestJailHandoffDraftRoute, createWorkspaceTestJailHandoffDraftHandler);
        Func<HttpContext, Task<IResult>> createUserWorkspaceAllowlistedHandoffDraftHandler =
            context => CreateProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftAsync(
                context,
                readModelSource,
                decisionStateStore,
                noOpExecutor,
                boundedActionExecutor,
                handoffReportDraftExecutor,
                workspaceTestJailHandoffDraftExecutor,
                userWorkspaceAllowlistedHandoffDraftExecutor);
        endpoints.MapPost(LocalUserWorkspaceAllowlistedHandoffDraftRoute, createUserWorkspaceAllowlistedHandoffDraftHandler);
        Func<HttpContext, Task<IResult>> createLatestStateSnapshotHandler =
            context => CreateProductLedgerLocalOperatorSurfaceLatestStateSnapshotAsync(
                context,
                readModelSource,
                decisionStateStore,
                noOpExecutor,
                boundedActionExecutor,
                handoffReportDraftExecutor,
                workspaceTestJailHandoffDraftExecutor,
                userWorkspaceAllowlistedHandoffDraftExecutor,
                latestStateSnapshotExecutor);
        endpoints.MapPost(LocalOperatorSurfaceLatestStateSnapshotRoute, createLatestStateSnapshotHandler);
        Func<HttpContext, Task<IResult>> createLatestStateManifestHandler =
            context => CreateProductLedgerLocalOperatorSurfaceLatestStateManifestAsync(
                context,
                latestStateSnapshotExecutor,
                latestStateManifestWriter);
        endpoints.MapPost(LocalOperatorSurfaceLatestStateManifestRoute, createLatestStateManifestHandler);
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
        => RenderProductLedgerLocalDevRoutePreview(
            readModelSource,
            approvalDecisionState,
            ProductLedgerLocalApprovedActionExecutionSnapshot.Pending);

    public static IResult RenderProductLedgerLocalDevRoutePreview(
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionSnapshot approvalDecisionState,
        ProductLedgerLocalApprovedActionExecutionSnapshot approvedActionExecutionState)
        => RenderProductLedgerLocalDevRoutePreview(
            readModelSource,
            approvalDecisionState,
            approvedActionExecutionState,
            ProductLedgerLocalBoundedApprovedActionSnapshot.Pending);

    public static IResult RenderProductLedgerLocalDevRoutePreview(
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionSnapshot approvalDecisionState,
        ProductLedgerLocalApprovedActionExecutionSnapshot approvedActionExecutionState,
        ProductLedgerLocalBoundedApprovedActionSnapshot boundedApprovedActionState)
        => RenderProductLedgerLocalDevRoutePreview(
            readModelSource,
            approvalDecisionState,
            approvedActionExecutionState,
            boundedApprovedActionState,
            ProductLedgerLocalApprovedHandoffReportDraftSnapshot.Pending);

    public static IResult RenderProductLedgerLocalDevRoutePreview(
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionSnapshot approvalDecisionState,
        ProductLedgerLocalApprovedActionExecutionSnapshot approvedActionExecutionState,
        ProductLedgerLocalBoundedApprovedActionSnapshot boundedApprovedActionState,
        ProductLedgerLocalApprovedHandoffReportDraftSnapshot handoffReportDraftState)
        => RenderProductLedgerLocalDevRoutePreview(
            readModelSource,
            approvalDecisionState,
            approvedActionExecutionState,
            boundedApprovedActionState,
            handoffReportDraftState,
            ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot.Pending);

    public static IResult RenderProductLedgerLocalDevRoutePreview(
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionSnapshot approvalDecisionState,
        ProductLedgerLocalApprovedActionExecutionSnapshot approvedActionExecutionState,
        ProductLedgerLocalBoundedApprovedActionSnapshot boundedApprovedActionState,
        ProductLedgerLocalApprovedHandoffReportDraftSnapshot handoffReportDraftState,
        ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot workspaceTestJailHandoffDraftState,
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot? userWorkspaceAllowlistedHandoffDraftState = null,
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult? latestStateSnapshotState = null,
        ProductLedgerLocalOperatorSurfaceLatestStateManifestResult? latestStateManifestState = null)
    {
        var result = new ProductLedgerLocalDevRoutePreview().Render(
            ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest(),
            readModelSource,
            approvalDecisionState,
            approvedActionExecutionState,
            boundedApprovedActionState,
            handoffReportDraftState,
            workspaceTestJailHandoffDraftState,
            userWorkspaceAllowlistedHandoffDraftState,
            latestStateSnapshotState,
            latestStateManifestState);

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

    private static async Task<IResult> CreateProductLedgerLocalOperatorSurfaceLatestStateSnapshotAsync(
        HttpContext context,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionStateStore decisionStateStore,
        ProductLedgerLocalApprovedActionNoOpExecutor noOpExecutor,
        ProductLedgerLocalBoundedApprovedActionExecutor boundedActionExecutor,
        ProductLedgerLocalApprovedHandoffReportDraftExecutor handoffReportDraftExecutor,
        ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor workspaceTestJailHandoffDraftExecutor,
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor userWorkspaceAllowlistedHandoffDraftExecutor,
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor latestStateSnapshotExecutor)
    {
        if (context.Request.ContentLength is null or <= 0 or > 8192)
        {
            return Results.Json(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult.Pending with
            {
                Blockers = [ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingRequest],
                StatusText = ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(context.Request.ContentType, "application/json", StringComparison.OrdinalIgnoreCase)
            && context.Request.ContentType?.StartsWith("application/json;", StringComparison.OrdinalIgnoreCase) != true)
        {
            return Results.Json(ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult.Pending with
            {
                Blockers = [ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBlocker.MissingRequest],
                StatusText = ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBody? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBody>(
                context.Request.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                context.RequestAborted);
        }
        catch (JsonException)
        {
            body = null;
        }

        var surface = CurrentSurface(
            readModelSource,
            decisionStateStore,
            noOpExecutor,
            boundedActionExecutor,
            handoffReportDraftExecutor,
            workspaceTestJailHandoffDraftExecutor,
            userWorkspaceAllowlistedHandoffDraftExecutor,
            latestStateSnapshotExecutor);
        var snapshot = latestStateSnapshotExecutor.CreateSnapshot(ToLatestStateSnapshotRequest(body, surface));
        var statusCode = snapshot.Decision switch
        {
            ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.SnapshotCreatedLocalOnly => StatusCodes.Status200OK,
            ProductLedgerLocalOperatorSurfaceLatestStateSnapshotDecision.IdempotentReplay => StatusCodes.Status200OK,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Json(snapshot, RouteJsonOptions, statusCode: statusCode);
    }

    private static async Task<IResult> CreateProductLedgerLocalOperatorSurfaceLatestStateManifestAsync(
        HttpContext context,
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor latestStateSnapshotExecutor,
        ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter latestStateManifestWriter)
    {
        if (context.Request.ContentLength is null or <= 0 or > 8192)
        {
            return Results.Json(ProductLedgerLocalOperatorSurfaceLatestStateManifestResult.Pending with
            {
                Blockers = [ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.MissingRequest],
                StatusText = ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(context.Request.ContentType, "application/json", StringComparison.OrdinalIgnoreCase)
            && context.Request.ContentType?.StartsWith("application/json;", StringComparison.OrdinalIgnoreCase) != true)
        {
            return Results.Json(ProductLedgerLocalOperatorSurfaceLatestStateManifestResult.Pending with
            {
                Blockers = [ProductLedgerLocalOperatorSurfaceLatestStateManifestBlocker.MissingRequest],
                StatusText = ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        ProductLedgerLocalOperatorSurfaceLatestStateManifestBody? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<ProductLedgerLocalOperatorSurfaceLatestStateManifestBody>(
                context.Request.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                context.RequestAborted);
        }
        catch (JsonException)
        {
            body = null;
        }

        var manifest = latestStateManifestWriter.CreateManifest(ToLatestStateManifestRequest(body, latestStateSnapshotExecutor.Read()));
        var statusCode = manifest.Decision switch
        {
            ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision.ManifestCreatedLocalOnly => StatusCodes.Status200OK,
            ProductLedgerLocalOperatorSurfaceLatestStateManifestDecision.IdempotentReplay => StatusCodes.Status200OK,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Json(manifest, RouteJsonOptions, statusCode: statusCode);
    }

    private static async Task<IResult> ExecuteProductLedgerLocalApprovalNoOpAsync(
        HttpContext context,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionStateStore decisionStateStore,
        ProductLedgerLocalApprovedActionNoOpExecutor noOpExecutor)
    {
        if (context.Request.ContentLength is null or <= 0 or > 8192)
        {
            return Results.Json(ProductLedgerLocalApprovedActionExecutionSnapshot.Pending with
            {
                Blockers = [ProductLedgerLocalApprovedActionExecutionBlocker.MissingRequest],
                StatusText = ProductLedgerLocalApprovedActionNoOpExecutor.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(context.Request.ContentType, "application/json", StringComparison.OrdinalIgnoreCase)
            && context.Request.ContentType?.StartsWith("application/json;", StringComparison.OrdinalIgnoreCase) != true)
        {
            return Results.Json(ProductLedgerLocalApprovedActionExecutionSnapshot.Pending with
            {
                Blockers = [ProductLedgerLocalApprovedActionExecutionBlocker.MissingRequest],
                StatusText = ProductLedgerLocalApprovedActionNoOpExecutor.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        ProductLedgerLocalApprovalExecutionBody? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<ProductLedgerLocalApprovalExecutionBody>(
                context.Request.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                context.RequestAborted);
        }
        catch (JsonException)
        {
            body = null;
        }

        var snapshot = noOpExecutor.ExecuteNoOp(ToExecutionRequest(
            body,
            decisionStateStore.Read(),
            CurrentCandidate(readModelSource).CandidateActionKind));
        var statusCode = snapshot.Decision switch
        {
            ProductLedgerLocalApprovedActionExecutionDecision.NoOpExecutionCompletedLocalOnly => StatusCodes.Status200OK,
            ProductLedgerLocalApprovedActionExecutionDecision.IdempotentReplay => StatusCodes.Status200OK,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Json(snapshot, RouteJsonOptions, statusCode: statusCode);
    }

    private static async Task<IResult> ExecuteProductLedgerLocalBoundedApprovalActionAsync(
        HttpContext context,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionStateStore decisionStateStore,
        ProductLedgerLocalApprovedActionNoOpExecutor noOpExecutor,
        ProductLedgerLocalBoundedApprovedActionExecutor boundedActionExecutor)
    {
        if (context.Request.ContentLength is null or <= 0 or > 8192)
        {
            return Results.Json(ProductLedgerLocalBoundedApprovedActionSnapshot.Pending with
            {
                Blockers = [ProductLedgerLocalBoundedApprovedActionBlocker.MissingRequest],
                StatusText = ProductLedgerLocalBoundedApprovedActionExecutor.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(context.Request.ContentType, "application/json", StringComparison.OrdinalIgnoreCase)
            && context.Request.ContentType?.StartsWith("application/json;", StringComparison.OrdinalIgnoreCase) != true)
        {
            return Results.Json(ProductLedgerLocalBoundedApprovedActionSnapshot.Pending with
            {
                Blockers = [ProductLedgerLocalBoundedApprovedActionBlocker.MissingRequest],
                StatusText = ProductLedgerLocalBoundedApprovedActionExecutor.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        ProductLedgerLocalBoundedApprovalExecutionBody? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<ProductLedgerLocalBoundedApprovalExecutionBody>(
                context.Request.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                context.RequestAborted);
        }
        catch (JsonException)
        {
            body = null;
        }

        var snapshot = boundedActionExecutor.ExecuteBoundedCompletionMarker(ToBoundedActionRequest(
            body,
            decisionStateStore.Read(),
            noOpExecutor.Read(),
            CurrentCandidate(readModelSource).CandidateActionKind));
        var statusCode = snapshot.Decision switch
        {
            ProductLedgerLocalBoundedApprovedActionDecision.BoundedLocalCompletionRecorded => StatusCodes.Status200OK,
            ProductLedgerLocalBoundedApprovedActionDecision.IdempotentReplay => StatusCodes.Status200OK,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Json(snapshot, RouteJsonOptions, statusCode: statusCode);
    }

    private static async Task<IResult> CreateProductLedgerLocalApprovedHandoffReportDraftAsync(
        HttpContext context,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionStateStore decisionStateStore,
        ProductLedgerLocalApprovedActionNoOpExecutor noOpExecutor,
        ProductLedgerLocalBoundedApprovedActionExecutor boundedActionExecutor,
        ProductLedgerLocalApprovedHandoffReportDraftExecutor handoffReportDraftExecutor)
    {
        if (context.Request.ContentLength is null or <= 0 or > 8192)
        {
            return Results.Json(ProductLedgerLocalApprovedHandoffReportDraftSnapshot.Pending with
            {
                Blockers = [ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingRequest],
                StatusText = ProductLedgerLocalApprovedHandoffReportDraftExecutor.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(context.Request.ContentType, "application/json", StringComparison.OrdinalIgnoreCase)
            && context.Request.ContentType?.StartsWith("application/json;", StringComparison.OrdinalIgnoreCase) != true)
        {
            return Results.Json(ProductLedgerLocalApprovedHandoffReportDraftSnapshot.Pending with
            {
                Blockers = [ProductLedgerLocalApprovedHandoffReportDraftBlocker.MissingRequest],
                StatusText = ProductLedgerLocalApprovedHandoffReportDraftExecutor.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        ProductLedgerLocalApprovedHandoffReportDraftBody? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<ProductLedgerLocalApprovedHandoffReportDraftBody>(
                context.Request.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                context.RequestAborted);
        }
        catch (JsonException)
        {
            body = null;
        }

        var snapshot = handoffReportDraftExecutor.CreateDraft(ToHandoffReportDraftRequest(
            body,
            decisionStateStore.Read(),
            noOpExecutor.Read(),
            boundedActionExecutor.Read(),
            CurrentCandidate(readModelSource).CandidateActionKind));
        var statusCode = snapshot.Decision switch
        {
            ProductLedgerLocalApprovedHandoffReportDraftDecision.DraftCreatedLocalOnly => StatusCodes.Status200OK,
            ProductLedgerLocalApprovedHandoffReportDraftDecision.IdempotentReplay => StatusCodes.Status200OK,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Json(snapshot, RouteJsonOptions, statusCode: statusCode);
    }

    private static async Task<IResult> CreateProductLedgerLocalWorkspaceTestJailHandoffDraftAsync(
        HttpContext context,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionStateStore decisionStateStore,
        ProductLedgerLocalApprovedActionNoOpExecutor noOpExecutor,
        ProductLedgerLocalBoundedApprovedActionExecutor boundedActionExecutor,
        ProductLedgerLocalApprovedHandoffReportDraftExecutor handoffReportDraftExecutor,
        ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor workspaceTestJailHandoffDraftExecutor)
    {
        if (context.Request.ContentLength is null or <= 0 or > 8192)
        {
            return Results.Json(ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot.Pending with
            {
                Blockers = [ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingRequest],
                StatusText = ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(context.Request.ContentType, "application/json", StringComparison.OrdinalIgnoreCase)
            && context.Request.ContentType?.StartsWith("application/json;", StringComparison.OrdinalIgnoreCase) != true)
        {
            return Results.Json(ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot.Pending with
            {
                Blockers = [ProductLedgerLocalWorkspaceTestJailHandoffDraftBlocker.MissingRequest],
                StatusText = ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        ProductLedgerLocalWorkspaceTestJailHandoffDraftBody? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<ProductLedgerLocalWorkspaceTestJailHandoffDraftBody>(
                context.Request.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                context.RequestAborted);
        }
        catch (JsonException)
        {
            body = null;
        }

        var snapshot = workspaceTestJailHandoffDraftExecutor.CreateDraft(ToWorkspaceTestJailHandoffDraftRequest(
            body,
            decisionStateStore.Read(),
            noOpExecutor.Read(),
            boundedActionExecutor.Read(),
            handoffReportDraftExecutor.Read(),
            CurrentCandidate(readModelSource).CandidateActionKind));
        var statusCode = snapshot.Decision switch
        {
            ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision.DraftCreatedWorkspaceTestJailOnly => StatusCodes.Status200OK,
            ProductLedgerLocalWorkspaceTestJailHandoffDraftDecision.IdempotentReplay => StatusCodes.Status200OK,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Json(snapshot, RouteJsonOptions, statusCode: statusCode);
    }

    private static async Task<IResult> CreateProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftAsync(
        HttpContext context,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionStateStore decisionStateStore,
        ProductLedgerLocalApprovedActionNoOpExecutor noOpExecutor,
        ProductLedgerLocalBoundedApprovedActionExecutor boundedActionExecutor,
        ProductLedgerLocalApprovedHandoffReportDraftExecutor handoffReportDraftExecutor,
        ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor workspaceTestJailHandoffDraftExecutor,
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor userWorkspaceAllowlistedHandoffDraftExecutor)
    {
        if (context.Request.ContentLength is null or <= 0 or > 8192)
        {
            return Results.Json(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot.Pending with
            {
                Blockers = [ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingRequest],
                StatusText = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(context.Request.ContentType, "application/json", StringComparison.OrdinalIgnoreCase)
            && context.Request.ContentType?.StartsWith("application/json;", StringComparison.OrdinalIgnoreCase) != true)
        {
            return Results.Json(ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftSnapshot.Pending with
            {
                Blockers = [ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBlocker.MissingRequest],
                StatusText = ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.RejectedStatus
            }, RouteJsonOptions, statusCode: StatusCodes.Status400BadRequest);
        }

        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBody? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBody>(
                context.Request.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                context.RequestAborted);
        }
        catch (JsonException)
        {
            body = null;
        }

        var snapshot = userWorkspaceAllowlistedHandoffDraftExecutor.CreateDraft(ToUserWorkspaceAllowlistedHandoffDraftRequest(
            body,
            decisionStateStore.Read(),
            noOpExecutor.Read(),
            boundedActionExecutor.Read(),
            handoffReportDraftExecutor.Read(),
            workspaceTestJailHandoffDraftExecutor.Read(),
            CurrentCandidate(readModelSource).CandidateActionKind));
        var statusCode = snapshot.Decision switch
        {
            ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision.DraftCreatedUserWorkspaceAllowlistedOnly => StatusCodes.Status200OK,
            ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftDecision.IdempotentReplay => StatusCodes.Status200OK,
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

    private static ProductLedgerLocalApprovedHandoffReportDraftRequest? ToHandoffReportDraftRequest(
        ProductLedgerLocalApprovedHandoffReportDraftBody? body,
        ProductLedgerLocalApprovalDecisionSnapshot approval,
        ProductLedgerLocalApprovedActionExecutionSnapshot noOpExecution,
        ProductLedgerLocalBoundedApprovedActionSnapshot boundedExecution,
        ProductLedgerInternalCommandKind currentCandidateActionKind)
    {
        if (body is null)
        {
            return null;
        }

        return new ProductLedgerLocalApprovedHandoffReportDraftRequest(
            ExplicitLocalApprovedHandoffDraftScope: body.ExplicitLocalApprovedHandoffDraftScope == true,
            DevelopmentMode: body.DevelopmentMode == true,
            LocalMode: body.LocalMode == true,
            InternalMode: body.InternalMode == true,
            ActionId: body.ActionId,
            CandidateId: body.CandidateId,
            ActionKind: ParseHandoffReportDraftActionKind(body.ActionKind),
            ApprovalDecision: approval,
            NoOpExecution: noOpExecution,
            BoundedExecution: boundedExecution,
            CandidateActionKind: body.CandidateActionKind is null
                ? currentCandidateActionKind
                : ParseCommandKind(body.CandidateActionKind),
            CandidateEvidenceHash: body.CandidateEvidenceHash,
            CurrentEvidenceHash: body.CurrentEvidenceHash,
            DraftTitle: body.DraftTitle,
            RedactedDraftSummary: body.RedactedDraftSummary,
            EvidenceReferences: body.EvidenceReferences ?? [],
            ProposedPath: body.ProposedPath,
            ProposedCommand: body.ProposedCommand,
            ProposedUrl: body.ProposedUrl,
            ProposedProvider: body.ProposedProvider,
            ProposedDbMigration: body.ProposedDbMigration,
            ClaimsArbitraryPathInput: body.ClaimsArbitraryPathInput == true,
            ClaimsFilesystemScan: body.ClaimsFilesystemScan == true,
            RequestsOverwrite: body.RequestsOverwrite == true,
            RequestsUserFileWrite: body.RequestsUserFileWrite == true,
            RequestsPublicUiAction: body.RequestsPublicUiAction == true,
            RequestsProductCommandExecution: body.RequestsProductCommandExecution == true,
            RequestsProductCommandHandler: body.RequestsProductCommandHandler == true,
            RequestsProductiveServiceRegistration: body.RequestsProductiveServiceRegistration == true,
            RequestsShellOrSubprocess: body.RequestsShellOrSubprocess == true,
            ClaimsArbitraryCommandExecution: body.ClaimsArbitraryCommandExecution == true,
            ClaimsProviderCloudNetwork: body.ClaimsProviderCloudNetwork == true,
            ClaimsDbMigration: body.ClaimsDbMigration == true,
            ClaimsKmsWormExternalTrust: body.ClaimsKmsWormExternalTrust == true,
            ClaimsBrowserCdpWcuOcrRecipesLive: body.ClaimsBrowserCdpWcuOcrRecipesLive == true,
            ClaimsPilotRun: body.ClaimsPilotRun == true,
            ClaimsReleaseCommercial: body.ClaimsReleaseCommercial == true);
    }

    private static ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest? ToWorkspaceTestJailHandoffDraftRequest(
        ProductLedgerLocalWorkspaceTestJailHandoffDraftBody? body,
        ProductLedgerLocalApprovalDecisionSnapshot approval,
        ProductLedgerLocalApprovedActionExecutionSnapshot noOpExecution,
        ProductLedgerLocalBoundedApprovedActionSnapshot boundedExecution,
        ProductLedgerLocalApprovedHandoffReportDraftSnapshot predecessorDraft,
        ProductLedgerInternalCommandKind currentCandidateActionKind)
    {
        if (body is null)
        {
            return null;
        }

        return new ProductLedgerLocalWorkspaceTestJailHandoffDraftRequest(
            ExplicitWorkspaceTestJailScope: body.ExplicitWorkspaceTestJailScope == true,
            DevelopmentMode: body.DevelopmentMode == true,
            LocalMode: body.LocalMode == true,
            InternalMode: body.InternalMode == true,
            ActionId: body.ActionId,
            CandidateId: body.CandidateId,
            ActionKind: ParseWorkspaceTestJailHandoffDraftActionKind(body.ActionKind),
            ApprovalDecision: approval,
            NoOpExecution: noOpExecution,
            BoundedExecution: boundedExecution,
            PredecessorDraft: predecessorDraft,
            CandidateActionKind: body.CandidateActionKind is null
                ? currentCandidateActionKind
                : ParseCommandKind(body.CandidateActionKind),
            CandidateEvidenceHash: body.CandidateEvidenceHash,
            CurrentEvidenceHash: body.CurrentEvidenceHash,
            PredecessorDraftContentHash: body.PredecessorDraftContentHash,
            DraftTitle: body.DraftTitle,
            RedactedDraftSummary: body.RedactedDraftSummary,
            EvidenceReferences: body.EvidenceReferences ?? [],
            ProposedPath: body.ProposedPath,
            ProposedRoot: body.ProposedRoot,
            ProposedFilename: body.ProposedFilename,
            ProposedCommand: body.ProposedCommand,
            ProposedUrl: body.ProposedUrl,
            ProposedProvider: body.ProposedProvider,
            ProposedDbMigration: body.ProposedDbMigration,
            ClaimsArbitraryPathInput: body.ClaimsArbitraryPathInput == true,
            ClaimsFilesystemScan: body.ClaimsFilesystemScan == true,
            RequestsOverwrite: body.RequestsOverwrite == true,
            RequestsUserSelectedPath: body.RequestsUserSelectedPath == true || body.RequestsUserFileWrite == true,
            RequestsPublicUiAction: body.RequestsPublicUiAction == true,
            RequestsProductCommandExecution: body.RequestsProductCommandExecution == true,
            RequestsProductCommandHandler: body.RequestsProductCommandHandler == true,
            RequestsProductiveServiceRegistration: body.RequestsProductiveServiceRegistration == true,
            RequestsShellOrSubprocess: body.RequestsShellOrSubprocess == true,
            ClaimsArbitraryCommandExecution: body.ClaimsArbitraryCommandExecution == true,
            ClaimsProviderCloudNetwork: body.ClaimsProviderCloudNetwork == true,
            ClaimsDbMigration: body.ClaimsDbMigration == true,
            ClaimsKmsWormExternalTrust: body.ClaimsKmsWormExternalTrust == true,
            ClaimsBrowserCdpWcuOcrRecipesLive: body.ClaimsBrowserCdpWcuOcrRecipesLive == true,
            ClaimsPilotRun: body.ClaimsPilotRun == true,
            ClaimsReleaseCommercial: body.ClaimsReleaseCommercial == true);
    }

    private static ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest? ToUserWorkspaceAllowlistedHandoffDraftRequest(
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBody? body,
        ProductLedgerLocalApprovalDecisionSnapshot approval,
        ProductLedgerLocalApprovedActionExecutionSnapshot noOpExecution,
        ProductLedgerLocalBoundedApprovedActionSnapshot boundedExecution,
        ProductLedgerLocalApprovedHandoffReportDraftSnapshot localHandoffDraft,
        ProductLedgerLocalWorkspaceTestJailHandoffDraftSnapshot workspaceTestJailHandoffDraft,
        ProductLedgerInternalCommandKind currentCandidateActionKind)
    {
        if (body is null)
        {
            return null;
        }

        return new ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftRequest(
            ExplicitUserWorkspaceAllowlistedScope: body.ExplicitUserWorkspaceAllowlistedScope == true,
            DevelopmentMode: body.DevelopmentMode == true,
            LocalMode: body.LocalMode == true,
            InternalMode: body.InternalMode == true,
            ActionId: body.ActionId,
            CandidateId: body.CandidateId,
            ActionKind: ParseUserWorkspaceAllowlistedHandoffDraftActionKind(body.ActionKind),
            ApprovalDecision: approval,
            NoOpExecution: noOpExecution,
            BoundedExecution: boundedExecution,
            LocalApprovedHandoffDraft: localHandoffDraft,
            WorkspaceTestJailHandoffDraft: workspaceTestJailHandoffDraft,
            CandidateActionKind: body.CandidateActionKind is null
                ? currentCandidateActionKind
                : ParseCommandKind(body.CandidateActionKind),
            CandidateEvidenceHash: body.CandidateEvidenceHash,
            CurrentEvidenceHash: body.CurrentEvidenceHash,
            LocalApprovedHandoffDraftContentHash: body.LocalApprovedHandoffDraftContentHash,
            WorkspaceTestJailHandoffDraftContentHash: body.WorkspaceTestJailHandoffDraftContentHash,
            DraftTitle: body.DraftTitle,
            RedactedDraftSummary: body.RedactedDraftSummary,
            EvidenceReferences: body.EvidenceReferences ?? [],
            ProposedPath: body.ProposedPath,
            ProposedRoot: body.ProposedRoot,
            ProposedFilename: body.ProposedFilename,
            ProposedCommand: body.ProposedCommand,
            ProposedUrl: body.ProposedUrl,
            ProposedProvider: body.ProposedProvider,
            ProposedDbMigration: body.ProposedDbMigration,
            ClaimsArbitraryPathInput: body.ClaimsArbitraryPathInput == true,
            ClaimsFilesystemScan: body.ClaimsFilesystemScan == true,
            RequestsOverwrite: body.RequestsOverwrite == true,
            RequestsUserSelectedPath: body.RequestsUserSelectedPath == true || body.RequestsUserFileWrite == true,
            RequestsPublicUiAction: body.RequestsPublicUiAction == true,
            RequestsProductCommandExecution: body.RequestsProductCommandExecution == true,
            RequestsProductCommandHandler: body.RequestsProductCommandHandler == true,
            RequestsProductiveServiceRegistration: body.RequestsProductiveServiceRegistration == true,
            RequestsShellOrSubprocess: body.RequestsShellOrSubprocess == true,
            ClaimsArbitraryCommandExecution: body.ClaimsArbitraryCommandExecution == true,
            ClaimsProviderCloudNetwork: body.ClaimsProviderCloudNetwork == true,
            ClaimsDbMigration: body.ClaimsDbMigration == true,
            ClaimsKmsWormExternalTrust: body.ClaimsKmsWormExternalTrust == true,
            ClaimsBrowserCdpWcuOcrRecipesLive: body.ClaimsBrowserCdpWcuOcrRecipesLive == true,
            ClaimsPilotRun: body.ClaimsPilotRun == true,
            ClaimsReleaseCommercial: body.ClaimsReleaseCommercial == true);
    }

    private static ProductLedgerLocalApprovedActionExecutionRequest? ToExecutionRequest(
        ProductLedgerLocalApprovalExecutionBody? body,
        ProductLedgerLocalApprovalDecisionSnapshot approval,
        ProductLedgerInternalCommandKind currentCandidateActionKind)
    {
        if (body is null)
        {
            return null;
        }

        return new ProductLedgerLocalApprovedActionExecutionRequest(
            ExplicitLocalOnlyNoOpExecutionScope: body.ExplicitLocalOnlyNoOpExecutionScope == true,
            DevelopmentMode: body.DevelopmentMode == true,
            LocalMode: body.LocalMode == true,
            InternalMode: body.InternalMode == true,
            ExecutionId: body.ExecutionId,
            ApprovalDecision: approval,
            CandidateActionKind: body.CandidateActionKind is null
                ? currentCandidateActionKind
                : ParseCommandKind(body.CandidateActionKind),
            CandidateEvidenceHash: body.CandidateEvidenceHash,
            CurrentEvidenceHash: body.CurrentEvidenceHash,
            EvidenceReferences: body.EvidenceReferences ?? [],
            RequestsBoundedAction: body.RequestsBoundedAction == true,
            RequestsPublicUiAction: body.RequestsPublicUiAction == true,
            RequestsProductCommandExecution: body.RequestsProductCommandExecution == true,
            RequestsProductCommandHandler: body.RequestsProductCommandHandler == true,
            RequestsProductiveServiceRegistration: body.RequestsProductiveServiceRegistration == true,
            RequestsPhysicalExport: body.RequestsPhysicalExport == true,
            RequestsFileWriteOutsideExecutionStore: body.RequestsFileWriteOutsideExecutionStore == true,
            ClaimsArbitraryPathInput: body.ClaimsArbitraryPathInput == true,
            ClaimsFilesystemScan: body.ClaimsFilesystemScan == true,
            ClaimsProviderCloudNetwork: body.ClaimsProviderCloudNetwork == true,
            ClaimsDbMigration: body.ClaimsDbMigration == true,
            ClaimsKmsWormExternalTrust: body.ClaimsKmsWormExternalTrust == true,
            ClaimsBrowserCdpWcuOcrRecipesLive: body.ClaimsBrowserCdpWcuOcrRecipesLive == true,
            ClaimsPilotRun: body.ClaimsPilotRun == true,
            ClaimsReleaseCommercial: body.ClaimsReleaseCommercial == true);
    }

    private static ProductLedgerLocalBoundedApprovedActionRequest? ToBoundedActionRequest(
        ProductLedgerLocalBoundedApprovalExecutionBody? body,
        ProductLedgerLocalApprovalDecisionSnapshot approval,
        ProductLedgerLocalApprovedActionExecutionSnapshot noOpExecution,
        ProductLedgerInternalCommandKind currentCandidateActionKind)
    {
        if (body is null)
        {
            return null;
        }

        return new ProductLedgerLocalBoundedApprovedActionRequest(
            ExplicitLocalBoundedActionScope: body.ExplicitLocalBoundedActionScope == true,
            DevelopmentMode: body.DevelopmentMode == true,
            LocalMode: body.LocalMode == true,
            InternalMode: body.InternalMode == true,
            ExecutionId: body.ExecutionId,
            ActionId: body.ActionId,
            ActionKind: ParseBoundedActionKind(body.ActionKind),
            ApprovalDecision: approval,
            NoOpExecution: noOpExecution,
            CandidateActionKind: body.CandidateActionKind is null
                ? currentCandidateActionKind
                : ParseCommandKind(body.CandidateActionKind),
            CandidateEvidenceHash: body.CandidateEvidenceHash,
            CurrentEvidenceHash: body.CurrentEvidenceHash,
            EvidenceReferences: body.EvidenceReferences ?? [],
            ProposedPath: body.ProposedPath,
            ProposedCommand: body.ProposedCommand,
            ProposedUrl: body.ProposedUrl,
            RequestsPublicUiAction: body.RequestsPublicUiAction == true,
            RequestsProductCommandExecution: body.RequestsProductCommandExecution == true,
            RequestsProductCommandHandler: body.RequestsProductCommandHandler == true,
            RequestsProductiveServiceRegistration: body.RequestsProductiveServiceRegistration == true,
            RequestsPhysicalExport: body.RequestsPhysicalExport == true,
            RequestsFileWriteOutsideExecutionStore: body.RequestsFileWriteOutsideExecutionStore == true,
            RequestsUserFileWrite: body.RequestsUserFileWrite == true,
            RequestsShellOrSubprocess: body.RequestsShellOrSubprocess == true,
            ClaimsArbitraryCommandExecution: body.ClaimsArbitraryCommandExecution == true,
            ClaimsArbitraryPathInput: body.ClaimsArbitraryPathInput == true,
            ClaimsFilesystemScan: body.ClaimsFilesystemScan == true,
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

    private static ProductLedgerOperatorSurfaceModel CurrentSurface(
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionStateStore decisionStateStore,
        ProductLedgerLocalApprovedActionNoOpExecutor noOpExecutor,
        ProductLedgerLocalBoundedApprovedActionExecutor boundedActionExecutor,
        ProductLedgerLocalApprovedHandoffReportDraftExecutor handoffReportDraftExecutor,
        ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor workspaceTestJailHandoffDraftExecutor,
        ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor userWorkspaceAllowlistedHandoffDraftExecutor,
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor latestStateSnapshotExecutor) =>
        new ProductLedgerLocalDevRoutePreview()
            .Render(
                ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest(),
                readModelSource,
                decisionStateStore.Read(),
                noOpExecutor.Read(),
                boundedActionExecutor.Read(),
                handoffReportDraftExecutor.Read(),
                workspaceTestJailHandoffDraftExecutor.Read(),
                userWorkspaceAllowlistedHandoffDraftExecutor.Read(),
                latestStateSnapshotExecutor.Read())
            .CanonicalSurface;

    private static ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest? ToLatestStateSnapshotRequest(
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBody? body,
        ProductLedgerOperatorSurfaceModel surface)
    {
        if (body is null)
        {
            return null;
        }

        return new ProductLedgerLocalOperatorSurfaceLatestStateSnapshotRequest(
            ExplicitLatestStateSnapshotScope: body.ExplicitLatestStateSnapshotScope == true,
            DevelopmentMode: body.DevelopmentMode == true,
            LocalMode: body.LocalMode == true,
            InternalMode: body.InternalMode == true,
            SnapshotId: body.SnapshotId,
            ActionId: body.ActionId,
            ActionKind: ParseLatestStateSnapshotActionKind(body.ActionKind),
            OperatorSurface: surface,
            OperatorSurfaceModelHash: body.OperatorSurfaceModelHash,
            EvidenceReferences: body.EvidenceReferences ?? [],
            ProposedPath: body.ProposedPath,
            ProposedRoot: body.ProposedRoot,
            ProposedFilename: body.ProposedFilename,
            ProposedCommand: body.ProposedCommand,
            ProposedUrl: body.ProposedUrl,
            ProposedProvider: body.ProposedProvider,
            ProposedDbMigration: body.ProposedDbMigration,
            ClaimsArbitraryPathInput: body.ClaimsArbitraryPathInput == true,
            ClaimsFilesystemScan: body.ClaimsFilesystemScan == true,
            RequestsOverwrite: body.RequestsOverwrite == true,
            RequestsLatestPointerOverwrite: body.RequestsLatestPointerOverwrite == true,
            RequestsUserSelectedPath: body.RequestsUserSelectedPath == true,
            RequestsPublicUiAction: body.RequestsPublicUiAction == true,
            RequestsProductCommandExecution: body.RequestsProductCommandExecution == true,
            RequestsProductCommandHandler: body.RequestsProductCommandHandler == true,
            RequestsProductiveServiceRegistration: body.RequestsProductiveServiceRegistration == true,
            RequestsShellOrSubprocess: body.RequestsShellOrSubprocess == true,
            ClaimsArbitraryCommandExecution: body.ClaimsArbitraryCommandExecution == true,
            ClaimsProviderCloudNetwork: body.ClaimsProviderCloudNetwork == true,
            ClaimsDbMigration: body.ClaimsDbMigration == true,
            ClaimsKmsWormExternalTrust: body.ClaimsKmsWormExternalTrust == true,
            ClaimsBrowserCdpWcuOcrRecipesLive: body.ClaimsBrowserCdpWcuOcrRecipesLive == true,
            ClaimsPilotRun: body.ClaimsPilotRun == true,
            ClaimsReleaseCommercial: body.ClaimsReleaseCommercial == true);
    }

    private static ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest? ToLatestStateManifestRequest(
        ProductLedgerLocalOperatorSurfaceLatestStateManifestBody? body,
        ProductLedgerLocalOperatorSurfaceLatestStateSnapshotResult sourceSnapshot)
    {
        if (body is null)
        {
            return null;
        }

        return new ProductLedgerLocalOperatorSurfaceLatestStateManifestRequest(
            ExplicitLatestStateManifestScope: body.ExplicitLatestStateManifestScope == true,
            DevelopmentMode: body.DevelopmentMode == true,
            LocalMode: body.LocalMode == true,
            InternalMode: body.InternalMode == true,
            ManifestId: body.ManifestId,
            ActionId: body.ActionId,
            ActionKind: ParseLatestStateManifestActionKind(body.ActionKind),
            SourceSnapshot: sourceSnapshot,
            ExpectedSourceSnapshotContentHash: body.ExpectedSourceSnapshotContentHash,
            ExpectedSourceSnapshotCheckpointHash: body.ExpectedSourceSnapshotCheckpointHash,
            EvidenceReferences: body.EvidenceReferences ?? [],
            ProposedPath: body.ProposedPath,
            ProposedRoot: body.ProposedRoot,
            ProposedFilename: body.ProposedFilename,
            ProposedCommand: body.ProposedCommand,
            ProposedUrl: body.ProposedUrl,
            ProposedProvider: body.ProposedProvider,
            ProposedDbMigration: body.ProposedDbMigration,
            ClaimsArbitraryPathInput: body.ClaimsArbitraryPathInput == true,
            ClaimsFilesystemScan: body.ClaimsFilesystemScan == true,
            RequestsOverwrite: body.RequestsOverwrite == true,
            RequestsLatestPointer: body.RequestsLatestPointer == true,
            RequestsLatestPointerOverwrite: body.RequestsLatestPointerOverwrite == true,
            RequestsReadPrecedence: body.RequestsReadPrecedence == true,
            RequestsUserSelectedPath: body.RequestsUserSelectedPath == true,
            RequestsPublicUiAction: body.RequestsPublicUiAction == true,
            RequestsProductCommandExecution: body.RequestsProductCommandExecution == true,
            RequestsProductCommandHandler: body.RequestsProductCommandHandler == true,
            RequestsProductiveServiceRegistration: body.RequestsProductiveServiceRegistration == true,
            RequestsShellOrSubprocess: body.RequestsShellOrSubprocess == true,
            ClaimsArbitraryCommandExecution: body.ClaimsArbitraryCommandExecution == true,
            ClaimsProviderCloudNetwork: body.ClaimsProviderCloudNetwork == true,
            ClaimsDbMigration: body.ClaimsDbMigration == true,
            ClaimsKmsWormExternalTrust: body.ClaimsKmsWormExternalTrust == true,
            ClaimsBrowserCdpWcuOcrRecipesLive: body.ClaimsBrowserCdpWcuOcrRecipesLive == true,
            ClaimsPilotRun: body.ClaimsPilotRun == true,
            ClaimsReleaseCommercial: body.ClaimsReleaseCommercial == true,
            ClaimsLiveAuthority: body.ClaimsLiveAuthority == true,
            ClaimsProductAuthority: body.ClaimsProductAuthority == true,
            ClaimsComplianceCustody: body.ClaimsComplianceCustody == true,
            ClaimsCloudBackedDurability: body.ClaimsCloudBackedDurability == true);
    }

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

    private static ProductLedgerInternalCommandKind? ParseCommandKind(string? value)
    {
        if (Enum.TryParse<ProductLedgerInternalCommandKind>(
            value,
            ignoreCase: true,
            out var command))
        {
            return command;
        }

        return null;
    }

    private static ProductLedgerLocalBoundedApprovedActionKind? ParseBoundedActionKind(string? value)
    {
        if (Enum.TryParse<ProductLedgerLocalBoundedApprovedActionKind>(
            value,
            ignoreCase: true,
            out var action))
        {
            return action;
        }

        return null;
    }

    private static ProductLedgerLocalApprovedHandoffReportDraftActionKind? ParseHandoffReportDraftActionKind(string? value)
    {
        if (Enum.TryParse<ProductLedgerLocalApprovedHandoffReportDraftActionKind>(
            value,
            ignoreCase: true,
            out var action))
        {
            return action;
        }

        return null;
    }

    private static ProductLedgerLocalWorkspaceTestJailHandoffDraftActionKind? ParseWorkspaceTestJailHandoffDraftActionKind(string? value)
    {
        if (Enum.TryParse<ProductLedgerLocalWorkspaceTestJailHandoffDraftActionKind>(
            value,
            ignoreCase: true,
            out var action))
        {
            return action;
        }

        return null;
    }

    private static ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftActionKind? ParseUserWorkspaceAllowlistedHandoffDraftActionKind(string? value)
    {
        if (Enum.TryParse<ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftActionKind>(
            value,
            ignoreCase: true,
            out var action))
        {
            return action;
        }

        return null;
    }

    private static ProductLedgerLocalOperatorSurfaceLatestStateSnapshotActionKind? ParseLatestStateSnapshotActionKind(string? value)
    {
        if (Enum.TryParse<ProductLedgerLocalOperatorSurfaceLatestStateSnapshotActionKind>(
            value,
            ignoreCase: true,
            out var action))
        {
            return action;
        }

        return null;
    }

    private static ProductLedgerLocalOperatorSurfaceLatestStateManifestActionKind? ParseLatestStateManifestActionKind(string? value)
    {
        if (Enum.TryParse<ProductLedgerLocalOperatorSurfaceLatestStateManifestActionKind>(
            value,
            ignoreCase: true,
            out var action))
        {
            return action;
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

    private static ProductLedgerLocalApprovedActionNoOpExecutor CreateDefaultNoOpExecutor() =>
        new(new ProductLedgerLocalApprovedActionExecutionStoreOptions(
            StoreRootPath: Path.Combine(Path.GetTempPath(), "nodal-os-product-ledger-local-approved-no-op-execution"),
            ExplicitLocalOnlyExecutionStore: true,
            AllowsArbitraryPathInput: false,
            AllowsFilesystemScan: false,
            AllowsExport: false,
            AllowsNetwork: false,
            AllowsDb: false,
            AllowsReleaseCommercial: false));

    private static ProductLedgerLocalBoundedApprovedActionExecutor CreateDefaultBoundedActionExecutor() =>
        new(new ProductLedgerLocalApprovedActionExecutionStoreOptions(
            StoreRootPath: Path.Combine(Path.GetTempPath(), "nodal-os-product-ledger-local-bounded-approved-action"),
            ExplicitLocalOnlyExecutionStore: true,
            AllowsArbitraryPathInput: false,
            AllowsFilesystemScan: false,
            AllowsExport: false,
            AllowsNetwork: false,
            AllowsDb: false,
            AllowsReleaseCommercial: false));

    private static ProductLedgerLocalApprovedHandoffReportDraftExecutor CreateDefaultHandoffReportDraftExecutor() =>
        new(new ProductLedgerLocalApprovedHandoffReportDraftOptions(
            OutputRootPath: Path.Combine(FindRepoRoot(), "docs", "test-output", "product-ledger", "approved-local-handoff-drafts"),
            ExplicitLocalApprovedHandoffDraftBoundary: true,
            AllowsArbitraryPathInput: false,
            AllowsFilesystemScan: false,
            AllowsOverwrite: false,
            AllowsUserFileWrite: false,
            AllowsShellOrSubprocess: false,
            AllowsCommandExecution: false,
            AllowsNetwork: false,
            AllowsDb: false,
            AllowsKmsWormExternalTrust: false,
            AllowsReleaseCommercial: false));

    private static ProductLedgerLocalWorkspaceTestJailHandoffDraftExecutor CreateDefaultWorkspaceTestJailHandoffDraftExecutor() =>
        new(new ProductLedgerLocalWorkspaceTestJailHandoffDraftOptions(
            WorkspaceTestJailRootPath: Path.Combine(FindRepoRoot(), "docs", "test-output", "product-ledger", "workspace-test-jail"),
            ExplicitWorkspaceTestJailBoundary: true,
            AllowsArbitraryPathInput: false,
            AllowsFilesystemScan: false,
            AllowsOverwrite: false,
            AllowsUserSelectedPath: false,
            AllowsShellOrSubprocess: false,
            AllowsCommandExecution: false,
            AllowsNetwork: false,
            AllowsDb: false,
            AllowsKmsWormExternalTrust: false,
            AllowsReleaseCommercial: false));

    private static ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor CreateDefaultUserWorkspaceAllowlistedHandoffDraftExecutor() =>
        new(new ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftOptions(
            TrustedWorkspaceRootPath: FindRepoRoot(),
            WorkspaceClassification: ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftExecutor.WorkspaceClassification,
            ExplicitUserWorkspaceAllowlistedBoundary: true,
            AllowsArbitraryPathInput: false,
            AllowsFilesystemScan: false,
            AllowsOverwrite: false,
            AllowsUserSelectedPath: false,
            AllowsShellOrSubprocess: false,
            AllowsCommandExecution: false,
            AllowsNetwork: false,
            AllowsDb: false,
            AllowsKmsWormExternalTrust: false,
            AllowsReleaseCommercial: false));

    private static ProductLedgerLocalOperatorSurfaceLatestStateSnapshotExecutor CreateDefaultLatestStateSnapshotExecutor() =>
        new(new ProductLedgerLocalOperatorSurfaceLatestStateSnapshotOptions(
            WorkspaceRootPath: FindRepoRoot(),
            ExplicitLatestStateSnapshotBoundary: true,
            AllowsArbitraryPathInput: false,
            AllowsFilesystemScan: false,
            AllowsOverwrite: false,
            AllowsLatestPointerOverwrite: false,
            AllowsUserSelectedPath: false,
            AllowsShellOrSubprocess: false,
            AllowsCommandExecution: false,
            AllowsNetwork: false,
            AllowsDb: false,
            AllowsKmsWormExternalTrust: false,
            AllowsReleaseCommercial: false));

    private static ProductLedgerLocalOperatorSurfaceLatestStateManifestWriter CreateDefaultLatestStateManifestWriter() =>
        new(new ProductLedgerLocalOperatorSurfaceLatestStateManifestOptions(
            WorkspaceRootPath: FindRepoRoot(),
            ExplicitLatestStateManifestBoundary: true,
            AllowsArbitraryPathInput: false,
            AllowsFilesystemScan: false,
            AllowsOverwrite: false,
            AllowsLatestPointer: false,
            AllowsLatestPointerOverwrite: false,
            AllowsReadPrecedence: false,
            AllowsUserSelectedPath: false,
            AllowsShellOrSubprocess: false,
            AllowsCommandExecution: false,
            AllowsNetwork: false,
            AllowsDb: false,
            AllowsKmsWormExternalTrust: false,
            AllowsReleaseCommercial: false));

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

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

    private sealed record ProductLedgerLocalApprovalExecutionBody(
        bool? ExplicitLocalOnlyNoOpExecutionScope,
        bool? DevelopmentMode,
        bool? LocalMode,
        bool? InternalMode,
        string? ExecutionId,
        string? CandidateActionKind,
        string? CandidateEvidenceHash,
        string? CurrentEvidenceHash,
        IReadOnlyList<string>? EvidenceReferences,
        bool? RequestsBoundedAction,
        bool? RequestsPublicUiAction,
        bool? RequestsProductCommandExecution,
        bool? RequestsProductCommandHandler,
        bool? RequestsProductiveServiceRegistration,
        bool? RequestsPhysicalExport,
        bool? RequestsFileWriteOutsideExecutionStore,
        bool? ClaimsArbitraryPathInput,
        bool? ClaimsFilesystemScan,
        bool? ClaimsProviderCloudNetwork,
        bool? ClaimsDbMigration,
        bool? ClaimsKmsWormExternalTrust,
        bool? ClaimsBrowserCdpWcuOcrRecipesLive,
        bool? ClaimsPilotRun,
        bool? ClaimsReleaseCommercial);

    private sealed record ProductLedgerLocalBoundedApprovalExecutionBody(
        bool? ExplicitLocalBoundedActionScope,
        bool? DevelopmentMode,
        bool? LocalMode,
        bool? InternalMode,
        string? ExecutionId,
        string? ActionId,
        string? ActionKind,
        string? CandidateActionKind,
        string? CandidateEvidenceHash,
        string? CurrentEvidenceHash,
        IReadOnlyList<string>? EvidenceReferences,
        string? ProposedPath,
        string? ProposedCommand,
        string? ProposedUrl,
        bool? RequestsPublicUiAction,
        bool? RequestsProductCommandExecution,
        bool? RequestsProductCommandHandler,
        bool? RequestsProductiveServiceRegistration,
        bool? RequestsPhysicalExport,
        bool? RequestsFileWriteOutsideExecutionStore,
        bool? RequestsUserFileWrite,
        bool? RequestsShellOrSubprocess,
        bool? ClaimsArbitraryCommandExecution,
        bool? ClaimsArbitraryPathInput,
        bool? ClaimsFilesystemScan,
        bool? ClaimsProviderCloudNetwork,
        bool? ClaimsDbMigration,
        bool? ClaimsKmsWormExternalTrust,
        bool? ClaimsBrowserCdpWcuOcrRecipesLive,
        bool? ClaimsPilotRun,
        bool? ClaimsReleaseCommercial);

    private sealed record ProductLedgerLocalApprovedHandoffReportDraftBody(
        bool? ExplicitLocalApprovedHandoffDraftScope,
        bool? DevelopmentMode,
        bool? LocalMode,
        bool? InternalMode,
        string? ActionId,
        string? CandidateId,
        string? ActionKind,
        string? CandidateActionKind,
        string? CandidateEvidenceHash,
        string? CurrentEvidenceHash,
        string? DraftTitle,
        string? RedactedDraftSummary,
        IReadOnlyList<string>? EvidenceReferences,
        string? ProposedPath,
        string? ProposedCommand,
        string? ProposedUrl,
        string? ProposedProvider,
        string? ProposedDbMigration,
        bool? ClaimsArbitraryPathInput,
        bool? ClaimsFilesystemScan,
        bool? RequestsOverwrite,
        bool? RequestsUserFileWrite,
        bool? RequestsPublicUiAction,
        bool? RequestsProductCommandExecution,
        bool? RequestsProductCommandHandler,
        bool? RequestsProductiveServiceRegistration,
        bool? RequestsShellOrSubprocess,
        bool? ClaimsArbitraryCommandExecution,
        bool? ClaimsProviderCloudNetwork,
        bool? ClaimsDbMigration,
        bool? ClaimsKmsWormExternalTrust,
        bool? ClaimsBrowserCdpWcuOcrRecipesLive,
        bool? ClaimsPilotRun,
        bool? ClaimsReleaseCommercial);

    private sealed record ProductLedgerLocalWorkspaceTestJailHandoffDraftBody(
        bool? ExplicitWorkspaceTestJailScope,
        bool? DevelopmentMode,
        bool? LocalMode,
        bool? InternalMode,
        string? ActionId,
        string? CandidateId,
        string? ActionKind,
        string? CandidateActionKind,
        string? CandidateEvidenceHash,
        string? CurrentEvidenceHash,
        string? PredecessorDraftContentHash,
        string? DraftTitle,
        string? RedactedDraftSummary,
        IReadOnlyList<string>? EvidenceReferences,
        string? ProposedPath,
        string? ProposedRoot,
        string? ProposedFilename,
        string? ProposedCommand,
        string? ProposedUrl,
        string? ProposedProvider,
        string? ProposedDbMigration,
        bool? ClaimsArbitraryPathInput,
        bool? ClaimsFilesystemScan,
        bool? RequestsOverwrite,
        bool? RequestsUserSelectedPath,
        bool? RequestsUserFileWrite,
        bool? RequestsPublicUiAction,
        bool? RequestsProductCommandExecution,
        bool? RequestsProductCommandHandler,
        bool? RequestsProductiveServiceRegistration,
        bool? RequestsShellOrSubprocess,
        bool? ClaimsArbitraryCommandExecution,
        bool? ClaimsProviderCloudNetwork,
        bool? ClaimsDbMigration,
        bool? ClaimsKmsWormExternalTrust,
        bool? ClaimsBrowserCdpWcuOcrRecipesLive,
        bool? ClaimsPilotRun,
        bool? ClaimsReleaseCommercial);

    private sealed record ProductLedgerLocalUserWorkspaceAllowlistedHandoffDraftBody(
        bool? ExplicitUserWorkspaceAllowlistedScope,
        bool? DevelopmentMode,
        bool? LocalMode,
        bool? InternalMode,
        string? ActionId,
        string? CandidateId,
        string? ActionKind,
        string? CandidateActionKind,
        string? CandidateEvidenceHash,
        string? CurrentEvidenceHash,
        string? LocalApprovedHandoffDraftContentHash,
        string? WorkspaceTestJailHandoffDraftContentHash,
        string? DraftTitle,
        string? RedactedDraftSummary,
        IReadOnlyList<string>? EvidenceReferences,
        string? ProposedPath,
        string? ProposedRoot,
        string? ProposedFilename,
        string? ProposedCommand,
        string? ProposedUrl,
        string? ProposedProvider,
        string? ProposedDbMigration,
        bool? ClaimsArbitraryPathInput,
        bool? ClaimsFilesystemScan,
        bool? RequestsOverwrite,
        bool? RequestsUserSelectedPath,
        bool? RequestsUserFileWrite,
        bool? RequestsPublicUiAction,
        bool? RequestsProductCommandExecution,
        bool? RequestsProductCommandHandler,
        bool? RequestsProductiveServiceRegistration,
        bool? RequestsShellOrSubprocess,
        bool? ClaimsArbitraryCommandExecution,
        bool? ClaimsProviderCloudNetwork,
        bool? ClaimsDbMigration,
        bool? ClaimsKmsWormExternalTrust,
        bool? ClaimsBrowserCdpWcuOcrRecipesLive,
        bool? ClaimsPilotRun,
        bool? ClaimsReleaseCommercial);

    private sealed record ProductLedgerLocalOperatorSurfaceLatestStateSnapshotBody(
        bool? ExplicitLatestStateSnapshotScope,
        bool? DevelopmentMode,
        bool? LocalMode,
        bool? InternalMode,
        string? SnapshotId,
        string? ActionId,
        string? ActionKind,
        string? OperatorSurfaceModelHash,
        IReadOnlyList<string>? EvidenceReferences,
        string? ProposedPath,
        string? ProposedRoot,
        string? ProposedFilename,
        string? ProposedCommand,
        string? ProposedUrl,
        string? ProposedProvider,
        string? ProposedDbMigration,
        bool? ClaimsArbitraryPathInput,
        bool? ClaimsFilesystemScan,
        bool? RequestsOverwrite,
        bool? RequestsLatestPointerOverwrite,
        bool? RequestsUserSelectedPath,
        bool? RequestsPublicUiAction,
        bool? RequestsProductCommandExecution,
        bool? RequestsProductCommandHandler,
        bool? RequestsProductiveServiceRegistration,
        bool? RequestsShellOrSubprocess,
        bool? ClaimsArbitraryCommandExecution,
        bool? ClaimsProviderCloudNetwork,
        bool? ClaimsDbMigration,
        bool? ClaimsKmsWormExternalTrust,
        bool? ClaimsBrowserCdpWcuOcrRecipesLive,
        bool? ClaimsPilotRun,
        bool? ClaimsReleaseCommercial);

    private sealed record ProductLedgerLocalOperatorSurfaceLatestStateManifestBody(
        bool? ExplicitLatestStateManifestScope,
        bool? DevelopmentMode,
        bool? LocalMode,
        bool? InternalMode,
        string? ManifestId,
        string? ActionId,
        string? ActionKind,
        string? ExpectedSourceSnapshotContentHash,
        string? ExpectedSourceSnapshotCheckpointHash,
        IReadOnlyList<string>? EvidenceReferences,
        string? ProposedPath,
        string? ProposedRoot,
        string? ProposedFilename,
        string? ProposedCommand,
        string? ProposedUrl,
        string? ProposedProvider,
        string? ProposedDbMigration,
        bool? ClaimsArbitraryPathInput,
        bool? ClaimsFilesystemScan,
        bool? RequestsOverwrite,
        bool? RequestsLatestPointer,
        bool? RequestsLatestPointerOverwrite,
        bool? RequestsReadPrecedence,
        bool? RequestsUserSelectedPath,
        bool? RequestsPublicUiAction,
        bool? RequestsProductCommandExecution,
        bool? RequestsProductCommandHandler,
        bool? RequestsProductiveServiceRegistration,
        bool? RequestsShellOrSubprocess,
        bool? ClaimsArbitraryCommandExecution,
        bool? ClaimsProviderCloudNetwork,
        bool? ClaimsDbMigration,
        bool? ClaimsKmsWormExternalTrust,
        bool? ClaimsBrowserCdpWcuOcrRecipesLive,
        bool? ClaimsPilotRun,
        bool? ClaimsReleaseCommercial,
        bool? ClaimsLiveAuthority,
        bool? ClaimsProductAuthority,
        bool? ClaimsComplianceCustody,
        bool? ClaimsCloudBackedDurability);
}
