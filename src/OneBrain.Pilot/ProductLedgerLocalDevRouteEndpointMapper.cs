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
            CreateDefaultHandoffReportDraftExecutor());

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
            CreateDefaultHandoffReportDraftExecutor());

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
            CreateDefaultHandoffReportDraftExecutor());

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
            CreateDefaultHandoffReportDraftExecutor());

    public static IEndpointRouteBuilder MapProductLedgerLocalDevRoutePreview(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment,
        ProductLedgerOperatorSurfaceReadModelSource readModelSource,
        ProductLedgerLocalApprovalDecisionStateStore decisionStateStore,
        ProductLedgerLocalApprovedActionNoOpExecutor noOpExecutor,
        ProductLedgerLocalBoundedApprovedActionExecutor boundedActionExecutor,
        ProductLedgerLocalApprovedHandoffReportDraftExecutor handoffReportDraftExecutor)
    {
        if (!environment.IsDevelopment())
        {
            return endpoints;
        }

        endpoints.MapGet(
            ProductLedgerLocalDevRoutePreview.RouteTemplatePreview,
            () => RenderProductLedgerLocalDevRoutePreview(readModelSource, decisionStateStore.Read(), noOpExecutor.Read(), boundedActionExecutor.Read(), handoffReportDraftExecutor.Read()));
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
    {
        var result = new ProductLedgerLocalDevRoutePreview().Render(
            ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest(),
            readModelSource,
            approvalDecisionState,
            approvedActionExecutionState,
            boundedApprovedActionState,
            handoffReportDraftState);

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
}
