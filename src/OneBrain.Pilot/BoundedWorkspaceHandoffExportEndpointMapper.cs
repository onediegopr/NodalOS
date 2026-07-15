using System.Net;
using OneBrain.AgentOperations.Core.Runtime;

namespace OneBrain.Pilot;

public sealed record BoundedWorkspaceHandoffExportSnapshot(
    string Decision,
    bool Accepted,
    bool LocalDevOnly,
    bool ReadOnly,
    bool SecretsExcluded,
    bool RootConfigured,
    string MissionStatus,
    string HandoffPackId,
    string EvidenceDigest,
    string FileName,
    string ContentType,
    bool Deterministic,
    bool ContainsRawPayload,
    bool ContainsExternalResource,
    bool IsAuthoritative,
    bool Executable,
    bool RealFilesystemRead,
    bool FilesystemMutationAllowed,
    bool NetworkUsed,
    bool ProductAuthorityGranted,
    IReadOnlyList<string> EvidenceRefs,
    string MarkdownRedacted);

public static class BoundedWorkspaceHandoffExportEndpointMapper
{
    public const string JsonRoute = "/api/workspace/understanding/handoff";
    public const string MarkdownRoute = "/workspace/understanding/handoff.md";
    public const string MarkdownContentType = "text/markdown; charset=utf-8";

    public static IEndpointRouteBuilder MapBoundedWorkspaceHandoffExport(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment,
        Func<string?>? rootProvider = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(environment);
        rootProvider ??= () => Environment.GetEnvironmentVariable(
            BoundedWorkspaceUnderstandingEndpointMapper.RootEnvironmentVariable);

        endpoints.MapGet(JsonRoute, async (HttpContext context) =>
        {
            if (!IsRequestAllowed(environment, context.Connection.RemoteIpAddress))
                return Results.NotFound(new { decision = "BOUNDED_WORKSPACE_HANDOFF_LOCAL_DEV_ONLY" });

            ApplyExportHeaders(context.Response);
            var export = await BuildExportAsync(rootProvider(), context.RequestAborted).ConfigureAwait(false);
            return Results.Json(
                export,
                statusCode: export.Accepted ? StatusCodes.Status200OK : StatusCodes.Status409Conflict);
        });

        endpoints.MapGet(MarkdownRoute, async (HttpContext context) =>
        {
            if (!IsRequestAllowed(environment, context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyExportHeaders(context.Response);
            var export = await BuildExportAsync(rootProvider(), context.RequestAborted).ConfigureAwait(false);
            if (!export.Accepted)
            {
                return Results.Content(
                    BlockedMarkdown(export.Decision),
                    MarkdownContentType,
                    statusCode: StatusCodes.Status409Conflict);
            }

            context.Response.Headers["Content-Disposition"] =
                $"attachment; filename=\"{export.FileName}\"";
            return Results.Content(
                export.MarkdownRedacted,
                MarkdownContentType,
                statusCode: StatusCodes.Status200OK);
        });

        return endpoints;
    }

    public static bool IsRequestAllowed(IHostEnvironment environment, IPAddress? remoteAddress) =>
        BoundedWorkspaceUnderstandingEndpointMapper.IsRequestAllowed(environment, remoteAddress);

    public static async ValueTask<BoundedWorkspaceHandoffExportSnapshot> BuildExportAsync(
        string? configuredRoot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(configuredRoot))
            return Blocked("BLOCKED_BOUNDED_WORKSPACE_ROOT_NOT_CONFIGURED", rootConfigured: false);

        var mission = await new NodalOsBoundedWorkspaceMissionScenario()
            .RunAsync(configuredRoot, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var markdown = mission.HandoffRender.MarkdownRedacted ?? string.Empty;
        var accepted = mission.Completed &&
                       mission.VerificationPassed &&
                       mission.Scan.SecretsExcluded &&
                       mission.Handoff.VerifiesEvidenceContent &&
                       !mission.Handoff.IsAuthoritative &&
                       !mission.Handoff.Executable &&
                       !mission.Handoff.RuntimeExecutionAllowed &&
                       !mission.Handoff.CallsLlmProvider &&
                       !mission.Handoff.CreatesPrompt &&
                       mission.HandoffRender.Deterministic &&
                       !mission.HandoffRender.ContainsRawPayload &&
                       !mission.HandoffRender.ContainsExternalResource &&
                       !mission.FilesystemMutationAllowed &&
                       !mission.NetworkUsed &&
                       !mission.ProductAuthorityGranted &&
                       IsSafeMarkdown(markdown, configuredRoot);
        if (!accepted)
        {
            return Blocked(
                mission.Decision,
                rootConfigured: true,
                missionStatus: mission.Mission.Status.ToString(),
                secretsExcluded: mission.Scan.SecretsExcluded,
                realFilesystemRead: mission.Scan.RealFilesystemRead,
                evidenceDigest: mission.Scan.EvidenceDigest);
        }

        var fileName = $"nodal-os-workspace-handoff-{mission.Scan.EvidenceDigest[..12]}.md";
        return new BoundedWorkspaceHandoffExportSnapshot(
            Decision: "GO_BOUNDED_WORKSPACE_HANDOFF_EXPORT_READY",
            Accepted: true,
            LocalDevOnly: true,
            ReadOnly: true,
            SecretsExcluded: true,
            RootConfigured: true,
            MissionStatus: mission.Mission.Status.ToString(),
            HandoffPackId: mission.Handoff.HandoffPackId,
            EvidenceDigest: mission.Scan.EvidenceDigest,
            FileName: fileName,
            ContentType: MarkdownContentType,
            Deterministic: mission.HandoffRender.Deterministic,
            ContainsRawPayload: false,
            ContainsExternalResource: false,
            IsAuthoritative: false,
            Executable: false,
            RealFilesystemRead: mission.Scan.RealFilesystemRead,
            FilesystemMutationAllowed: false,
            NetworkUsed: false,
            ProductAuthorityGranted: false,
            EvidenceRefs: mission.Handoff.EvidenceRefs
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .Take(200)
                .ToArray(),
            MarkdownRedacted: markdown);
    }

    private static bool IsSafeMarkdown(string markdown, string configuredRoot)
    {
        if (string.IsNullOrWhiteSpace(markdown) || markdown.Length > 64 * 1024)
            return false;
        if (markdown.Contains(configuredRoot.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        return !markdown.Contains("<script", StringComparison.OrdinalIgnoreCase) &&
               !markdown.Contains("<form", StringComparison.OrdinalIgnoreCase) &&
               !markdown.Contains("http://", StringComparison.OrdinalIgnoreCase) &&
               !markdown.Contains("https://", StringComparison.OrdinalIgnoreCase);
    }

    private static BoundedWorkspaceHandoffExportSnapshot Blocked(
        string decision,
        bool rootConfigured,
        string missionStatus = "Blocked",
        bool secretsExcluded = true,
        bool realFilesystemRead = false,
        string evidenceDigest = "") => new(
            Decision: decision,
            Accepted: false,
            LocalDevOnly: true,
            ReadOnly: true,
            SecretsExcluded: secretsExcluded,
            RootConfigured: rootConfigured,
            MissionStatus: missionStatus,
            HandoffPackId: string.Empty,
            EvidenceDigest: evidenceDigest,
            FileName: string.Empty,
            ContentType: MarkdownContentType,
            Deterministic: false,
            ContainsRawPayload: false,
            ContainsExternalResource: false,
            IsAuthoritative: false,
            Executable: false,
            RealFilesystemRead: realFilesystemRead,
            FilesystemMutationAllowed: false,
            NetworkUsed: false,
            ProductAuthorityGranted: false,
            EvidenceRefs: Array.Empty<string>(),
            MarkdownRedacted: string.Empty);

    private static string BlockedMarkdown(string decision) => $"""
        # NODAL OS Workspace Handoff

        Export unavailable.

        Decision: {decision}
        """;

    private static void ApplyExportHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
        response.Headers["Referrer-Policy"] = "no-referrer";
    }
}
