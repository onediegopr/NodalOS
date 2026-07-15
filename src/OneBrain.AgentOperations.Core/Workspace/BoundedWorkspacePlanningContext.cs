using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core.Runtime;
using OneBrain.Core.Runtime;

namespace OneBrain.AgentOperations.Core.Workspace;

public sealed record BoundedWorkspacePlanningContext(
    string ContextId,
    string WorkspaceId,
    string MissionId,
    string RootFingerprint,
    string EvidenceDigest,
    int FilesRead,
    int FilesSkipped,
    long TotalBytesRead,
    bool ScanTruncated,
    IReadOnlyDictionary<string, int> ExtensionCounts,
    IReadOnlyList<string> SafeFileRefs,
    IReadOnlyList<string> FindingsRedacted,
    bool Redacted,
    bool ReadOnly,
    bool Authoritative,
    bool CallsModelProvider,
    bool CreatesPrompt,
    bool FilesystemMutationAllowed,
    bool NetworkUsed,
    bool ProductAuthorityGranted);

public sealed record BoundedWorkspacePlanningProjectionResult(
    string Decision,
    bool Accepted,
    BoundedWorkspacePlanningContext? Context,
    NodalOsTaskGraphDraft? TaskGraph,
    MissionPlan? MissionPlan,
    IReadOnlyList<string> Blockers,
    bool RequiresHumanReview,
    bool CanExecute,
    bool CallsModelProvider,
    bool CreatesPrompt,
    bool FilesystemMutationAllowed,
    bool NetworkUsed,
    bool ProductAuthorityGranted);

public sealed class BoundedWorkspacePlanningContextService
{
    public BoundedWorkspacePlanningProjectionResult Build(
        BoundedWorkspaceScanResult scan,
        string workspaceId,
        string missionId)
    {
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(missionId);

        var blockers = Validate(scan).ToArray();
        if (blockers.Length > 0)
            return Blocked(blockers);

        var safeWorkspaceId = SafeRuntimeText.Sanitize(workspaceId, 120);
        var safeMissionId = SafeRuntimeText.Sanitize(missionId, 120);
        var safeFileRefs = scan.Files
            .Select(file => SafeRuntimeText.Sanitize(file.RelativePathRedacted, 240))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Take(200)
            .ToArray();
        var findings = scan.Findings
            .Select(value => SafeRuntimeText.Sanitize(value, 240))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var extensionCounts = scan.ExtensionCounts
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var context = new BoundedWorkspacePlanningContext(
            ContextId: $"workspace-context-{scan.EvidenceDigest[..16]}",
            WorkspaceId: safeWorkspaceId,
            MissionId: safeMissionId,
            RootFingerprint: scan.RootFingerprint,
            EvidenceDigest: scan.EvidenceDigest,
            FilesRead: scan.FilesRead,
            FilesSkipped: scan.FilesSkipped,
            TotalBytesRead: scan.TotalBytesRead,
            ScanTruncated: scan.Truncated,
            ExtensionCounts: extensionCounts,
            SafeFileRefs: safeFileRefs,
            FindingsRedacted: findings,
            Redacted: true,
            ReadOnly: true,
            Authoritative: false,
            CallsModelProvider: false,
            CreatesPrompt: false,
            FilesystemMutationAllowed: false,
            NetworkUsed: false,
            ProductAuthorityGranted: false);
        var graph = BuildTaskGraph(context);
        var plan = NodalOsMissionPlanProjector.Project(
            graph,
            $"Review the bounded local workspace context for mission {safeMissionId}.");
        var reviewBlockers = scan.Truncated
            ? new[] { "The scan reached its explicit budget; a larger context requires a new operator-authorized mission." }
            : Array.Empty<string>();

        return new BoundedWorkspacePlanningProjectionResult(
            Decision: "GO_BOUNDED_WORKSPACE_CONTEXT_READY_FOR_REVIEWED_PLAN",
            Accepted: true,
            Context: context,
            TaskGraph: graph,
            MissionPlan: plan,
            Blockers: reviewBlockers,
            RequiresHumanReview: true,
            CanExecute: false,
            CallsModelProvider: false,
            CreatesPrompt: false,
            FilesystemMutationAllowed: false,
            NetworkUsed: false,
            ProductAuthorityGranted: false);
    }

    private static IEnumerable<string> Validate(BoundedWorkspaceScanResult scan)
    {
        if (scan.Decision != BoundedWorkspaceScanDecision.Accepted)
            yield return "Workspace scan was not accepted.";
        if (scan.Cancelled)
            yield return "Workspace scan was cancelled.";
        if (!scan.RealFilesystemRead || scan.FilesRead <= 0)
            yield return "Workspace scan did not produce bounded file evidence.";
        if (!scan.SecretsExcluded)
            yield return "Workspace evidence is not marked as secret-excluded.";
        if (scan.FilesystemMutationAllowed)
            yield return "Workspace evidence cannot allow filesystem mutation.";
        if (scan.NetworkUsed)
            yield return "Workspace evidence cannot use network access.";
        if (scan.ProductAuthorityGranted)
            yield return "Workspace evidence cannot grant product authority.";
        if (!IsSha256(scan.RootFingerprint))
            yield return "Workspace root fingerprint is invalid.";
        if (!IsSha256(scan.EvidenceDigest))
            yield return "Workspace evidence digest is invalid.";
        if (scan.Files.Any(file => string.IsNullOrWhiteSpace(file.RelativePathRedacted) ||
                                   Path.IsPathRooted(file.RelativePathRedacted) ||
                                   file.RelativePathRedacted.Contains("..", StringComparison.Ordinal)))
        {
            yield return "Workspace file references are not safe relative paths.";
        }
    }

    private static NodalOsTaskGraphDraft BuildTaskGraph(BoundedWorkspacePlanningContext context)
    {
        var evidence = Evidence(context.EvidenceDigest);
        var truncatedNote = context.ScanTruncated
            ? "Context is intentionally incomplete because the bounded scan reached its configured budget."
            : "Context covers every eligible file found inside the configured bounded scan.";
        return new NodalOsTaskGraphDraft
        {
            TaskGraphId = $"taskgraph-{context.ContextId}",
            AssignmentRequestId = $"assignment-{context.ContextId}",
            WorkspaceId = context.WorkspaceId,
            MissionId = context.MissionId,
            GraphStatus = NodalOsAssignmentTaskGraphStatus.ReadyForManualReview,
            Tasks =
            [
                new NodalOsAssignmentTaskDraft
                {
                    TaskId = "review-bounded-workspace-context",
                    TitleRedacted = "Review bounded workspace context",
                    SummaryRedacted = $"Review {context.FilesRead} redacted file references and extension counts. {truncatedNote}",
                    TaskKind = NodalOsAssignmentTaskKind.AnalysisDraft,
                    Status = NodalOsAssignmentTaskStatus.ReadyForManualReview,
                    DependencyIds = [],
                    BlockedByIds = [],
                    RiskLevel = NodalOsAssignmentRiskLevel.Low,
                    AllowedCapabilitiesRedacted = ["filesystem.read"],
                    DisabledCapabilitiesRedacted =
                    [
                        "filesystem.write",
                        "terminal.execute",
                        "browser.action.execute",
                        "network",
                        "model.provider",
                        "product authority"
                    ],
                    SuggestedAssigneeType = NodalOsSuggestedAssigneeType.FutureAssignmentPlanner,
                    EvidenceRefs = [evidence],
                    TimelineRefs = [$"timeline:{context.ContextId}"],
                    RequiresApproval = false,
                    RequiresLlmFuture = false,
                    RequiresRuntimeFuture = false,
                    RequiresFilesystemFuture = false,
                    CanExecute = false
                }
            ],
            DependenciesRedacted = [],
            RiskNotesRedacted =
            [
                "Read-only evidence projection.",
                "No raw root path or secret-bearing file content is included.",
                truncatedNote
            ],
            EvidenceRefs = [evidence],
            TimelineRefs = [$"timeline:{context.ContextId}"],
            ApprovalRefs = [],
            ContextRefsRedacted =
            [
                context.ContextId,
                $"root-fingerprint:{context.RootFingerprint}",
                $"files-read:{context.FilesRead}",
                $"bytes-read:{context.TotalBytesRead}"
            ],
            GuardrailRefs =
            [
                "bounded-workspace-evidence-only",
                "redaction-before-plan",
                "human-review-required",
                "no-model-call",
                "no-prompt",
                "no-execution",
                "no-authority"
            ],
            HumanReviewRequirementRedacted = "The operator reviews this context before any later plan is promoted or executed.",
            ReadinessGateResultRedacted = "Verified bounded workspace evidence is ready for manual plan review.",
            DraftOnly = true,
            Executable = false,
            ResolvesDependenciesProductively = false,
            CallsLlmProvider = false,
            CallsRuntime = false,
            TouchesFilesystem = false,
            CreatesAuthoritativeApproval = false,
            CanAuthorizeExecution = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static NodalOsEvidenceBridgeRef Evidence(string digest) => new()
    {
        EvidenceId = $"evidence:bounded-workspace:{digest}",
        Kind = "bounded-workspace-understanding-digest",
        Ref = null,
        Hash = digest,
        SourceKind = NodalOsEvidenceBridgeSourceKind.AgentOperation,
        UseKind = NodalOsEvidenceBridgeUseKind.VerificationSupport,
        Authority = NodalOsEvidenceBridgeAuthority.SupportsVerificationOnly,
        Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
        RedactionState = NodalOsEvidenceRedactionState.NotRequired,
        LedgerRef = $"workspace-understanding:{digest}",
        Provenance = "Verified bounded local workspace scan projected into reviewed planning context.",
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static bool IsSha256(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);

    private static BoundedWorkspacePlanningProjectionResult Blocked(IReadOnlyList<string> blockers) => new(
        Decision: "BLOCKED_BOUNDED_WORKSPACE_CONTEXT_FAIL_CLOSED",
        Accepted: false,
        Context: null,
        TaskGraph: null,
        MissionPlan: null,
        Blockers: blockers,
        RequiresHumanReview: true,
        CanExecute: false,
        CallsModelProvider: false,
        CreatesPrompt: false,
        FilesystemMutationAllowed: false,
        NetworkUsed: false,
        ProductAuthorityGranted: false);
}
