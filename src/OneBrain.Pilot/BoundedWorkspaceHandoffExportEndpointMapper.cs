using System.Net;
using System.Text;
using OneBrain.AgentOperations.Core.Runtime;
using OneBrain.AgentOperations.Core.Workspace;

namespace OneBrain.Pilot;

public sealed record BoundedWorkspaceAdvisorExportFinding(
    string SuggestionId,
    string Category,
    string Severity,
    string Title,
    string MessageRedacted,
    IReadOnlyList<string> EvidenceRefs,
    bool RequiresHumanAttention,
    bool NonExecutable,
    bool CanAuthorizeExecution);

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
    string AdvisorDecision,
    string AdvisorProfile,
    int AdvisorInterventionLevel,
    int AdvisorSuggestionCount,
    bool AdvisorFindingsIncluded,
    bool AdvisorNonExecutor,
    IReadOnlyList<BoundedWorkspaceAdvisorExportFinding> AdvisorFindings,
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
        Func<string?>? rootProvider = null,
        Func<BoundedWorkspaceAdvisorSettings>? advisorSettingsProvider = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(environment);
        rootProvider ??= () => Environment.GetEnvironmentVariable(
            BoundedWorkspaceUnderstandingEndpointMapper.RootEnvironmentVariable);
        advisorSettingsProvider ??= () => BoundedWorkspaceUnderstandingEndpointMapper.ResolveAdvisorSettings(
            Environment.GetEnvironmentVariable(BoundedWorkspaceUnderstandingEndpointMapper.AdvisorProfileEnvironmentVariable),
            Environment.GetEnvironmentVariable(BoundedWorkspaceUnderstandingEndpointMapper.AdvisorInterventionEnvironmentVariable));

        endpoints.MapGet(JsonRoute, async (HttpContext context) =>
        {
            if (!IsRequestAllowed(environment, context.Connection.RemoteIpAddress))
                return Results.NotFound(new { decision = "BOUNDED_WORKSPACE_HANDOFF_LOCAL_DEV_ONLY" });

            ApplyExportHeaders(context.Response);
            var export = await BuildExportAsync(
                    rootProvider(),
                    context.RequestAborted,
                    advisorSettingsProvider())
                .ConfigureAwait(false);
            return Results.Json(
                export,
                statusCode: export.Accepted ? StatusCodes.Status200OK : StatusCodes.Status409Conflict);
        });

        endpoints.MapGet(MarkdownRoute, async (HttpContext context) =>
        {
            if (!IsRequestAllowed(environment, context.Connection.RemoteIpAddress))
                return Results.NotFound();

            ApplyExportHeaders(context.Response);
            var export = await BuildExportAsync(
                    rootProvider(),
                    context.RequestAborted,
                    advisorSettingsProvider())
                .ConfigureAwait(false);
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
        CancellationToken cancellationToken = default,
        BoundedWorkspaceAdvisorSettings? advisorSettings = null)
    {
        advisorSettings ??= new BoundedWorkspaceAdvisorSettings();
        advisorSettings.Validate();
        if (string.IsNullOrWhiteSpace(configuredRoot))
            return Blocked(
                "BLOCKED_BOUNDED_WORKSPACE_ROOT_NOT_CONFIGURED",
                rootConfigured: false,
                advisorSettings: advisorSettings);

        var mission = await new NodalOsBoundedWorkspaceMissionScenario()
            .RunAsync(configuredRoot, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var planning = mission.Completed && mission.VerificationPassed
            ? new BoundedWorkspacePlanningContextService().Build(
                mission.Scan,
                workspaceId: "workspace-local-configured",
                missionId: mission.Plan.MissionId)
            : null;
        var advisor = planning is null
            ? null
            : new BoundedWorkspaceAdvisorService().Evaluate(
                mission.Scan,
                planning,
                advisorSettings);
        var baseMarkdown = mission.HandoffRender.MarkdownRedacted ?? string.Empty;
        var markdown = advisor is null
            ? baseMarkdown
            : AppendAdvisorFindings(baseMarkdown, advisor);
        var advisorSafe = advisor is not null &&
                          advisor.Accepted &&
                          advisor.NonExecutor &&
                          !advisor.CallsModelProvider &&
                          !advisor.CreatesPrompt &&
                          !advisor.FilesystemMutationAllowed &&
                          !advisor.NetworkUsed &&
                          !advisor.ProductAuthorityGranted &&
                          advisor.Suggestions.All(value =>
                              value.NonExecutable &&
                              !value.CanAuthorizeExecution &&
                              !value.CallsModelProvider &&
                              !value.CreatesPrompt &&
                              !value.FilesystemMutationAllowed &&
                              !value.NetworkUsed &&
                              !value.ProductAuthorityGranted);
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
                       advisorSafe &&
                       !mission.FilesystemMutationAllowed &&
                       !mission.NetworkUsed &&
                       !mission.ProductAuthorityGranted &&
                       IsSafeMarkdown(markdown, configuredRoot);
        if (!accepted)
        {
            return Blocked(
                advisor?.Decision ?? mission.Decision,
                rootConfigured: true,
                missionStatus: mission.Mission.Status.ToString(),
                secretsExcluded: mission.Scan.SecretsExcluded,
                realFilesystemRead: mission.Scan.RealFilesystemRead,
                evidenceDigest: mission.Scan.EvidenceDigest,
                advisorSettings: advisorSettings,
                advisorDecision: advisor?.Decision ?? "BLOCKED_EXPERT_ADVISOR_INPUT_FAIL_CLOSED");
        }

        var advisorFindings = advisor!.Suggestions
            .Select(ToExportFinding)
            .ToArray();
        var evidenceRefs = mission.Handoff.EvidenceRefs
            .Concat(advisorFindings.SelectMany(value => value.EvidenceRefs))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Take(200)
            .ToArray();
        var fileName = $"nodal-os-workspace-handoff-{mission.Scan.EvidenceDigest[..12]}.md";
        return new BoundedWorkspaceHandoffExportSnapshot(
            Decision: "GO_BOUNDED_WORKSPACE_ADVISOR_HANDOFF_EXPORT_READY",
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
            AdvisorDecision: advisor.Decision,
            AdvisorProfile: advisor.Profile.ToString(),
            AdvisorInterventionLevel: advisor.InterventionLevel,
            AdvisorSuggestionCount: advisorFindings.Length,
            AdvisorFindingsIncluded: true,
            AdvisorNonExecutor: true,
            AdvisorFindings: advisorFindings,
            RealFilesystemRead: mission.Scan.RealFilesystemRead,
            FilesystemMutationAllowed: false,
            NetworkUsed: false,
            ProductAuthorityGranted: false,
            EvidenceRefs: evidenceRefs,
            MarkdownRedacted: markdown);
    }

    private static string AppendAdvisorFindings(
        string markdown,
        BoundedWorkspaceAdvisorResult advisor)
    {
        var section = new StringBuilder();
        section.AppendLine("## Expert Advisor findings");
        section.AppendLine($"Profile: {Clean(advisor.Profile.ToString())} ({advisor.InterventionLevel}/100)");
        section.AppendLine($"Decision: {Clean(advisor.Decision)}");
        section.AppendLine("The advisor is read-only, non-executing and cannot authorize work.");
        section.AppendLine();
        if (advisor.Suggestions.Count == 0)
        {
            section.AppendLine("No material deterministic finding at the configured profile and intervention level.");
            section.AppendLine();
        }
        else
        {
            foreach (var suggestion in advisor.Suggestions)
            {
                section.AppendLine($"### {Clean(suggestion.Severity.ToString())} · {Clean(suggestion.Category.ToString())} — {Clean(suggestion.Title)}");
                section.AppendLine(Clean(suggestion.MessageRedacted));
                if (suggestion.EvidenceRefs.Count > 0)
                {
                    section.AppendLine($"Evidence refs: {string.Join(", ", suggestion.EvidenceRefs.Select(Clean))}");
                }
                section.AppendLine();
            }
        }

        const string marker = "## Next safe step";
        var index = markdown.IndexOf(marker, StringComparison.Ordinal);
        if (index >= 0)
            return markdown.Insert(index, section.ToString());
        return markdown.TrimEnd() + Environment.NewLine + Environment.NewLine + section;
    }

    private static BoundedWorkspaceAdvisorExportFinding ToExportFinding(
        BoundedWorkspaceAdvisorSuggestion suggestion) => new(
            SuggestionId: suggestion.SuggestionId,
            Category: suggestion.Category.ToString(),
            Severity: suggestion.Severity.ToString(),
            Title: suggestion.Title,
            MessageRedacted: suggestion.MessageRedacted,
            EvidenceRefs: suggestion.EvidenceRefs,
            RequiresHumanAttention: suggestion.RequiresHumanAttention,
            NonExecutable: suggestion.NonExecutable,
            CanAuthorizeExecution: suggestion.CanAuthorizeExecution);

    private static string Clean(string? value)
    {
        var normalized = (value ?? string.Empty)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
        return normalized.Length <= 600 ? normalized : normalized[..599] + "…";
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
        string evidenceDigest = "",
        BoundedWorkspaceAdvisorSettings? advisorSettings = null,
        string advisorDecision = "BLOCKED_EXPERT_ADVISOR_INPUT_FAIL_CLOSED")
    {
        advisorSettings ??= new BoundedWorkspaceAdvisorSettings();
        return new BoundedWorkspaceHandoffExportSnapshot(
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
            AdvisorDecision: advisorDecision,
            AdvisorProfile: advisorSettings.Profile.ToString(),
            AdvisorInterventionLevel: advisorSettings.InterventionLevel,
            AdvisorSuggestionCount: 0,
            AdvisorFindingsIncluded: false,
            AdvisorNonExecutor: true,
            AdvisorFindings: Array.Empty<BoundedWorkspaceAdvisorExportFinding>(),
            RealFilesystemRead: realFilesystemRead,
            FilesystemMutationAllowed: false,
            NetworkUsed: false,
            ProductAuthorityGranted: false,
            EvidenceRefs: Array.Empty<string>(),
            MarkdownRedacted: string.Empty);
    }

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
