using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core.Runtime;
using OneBrain.Core.History;
using OneBrain.Core.Models;
using OneBrain.Core.Runtime;

namespace OneBrain.AgentOperations.Core.Workspace;

public enum NodalOsWorkspaceMissionDraftState
{
    NotConfigured,
    ReadyForReview,
    WorkspaceRequired,
    GoalRejected,
    CandidateStale,
    CorruptMetadata,
    FailedClosed
}

public enum NodalOsReviewedWorkspaceActionKind
{
    CreateTextFile,
    ExactHashUpdate
}

public enum NodalOsReviewedWorkspaceActionState
{
    ReadyForReview,
    StalePrecondition,
    Blocked
}

public sealed record NodalOsReviewedWorkspaceActionCandidate(
    string ActionId,
    NodalOsReviewedWorkspaceActionKind Kind,
    NodalOsReviewedWorkspaceActionState State,
    string RelativeTargetPath,
    string DescriptionRedacted,
    MissionRiskLevel RiskLevel,
    bool Reversible,
    bool ApprovalRequired,
    string RequiredApprovalScope,
    bool ExecutionEnabled,
    bool TargetExists,
    long ExistingBytes,
    string? ExistingSha256,
    string ProposedContentRedacted,
    int ProposedBytes,
    string ProposedSha256,
    IReadOnlyList<string> PreconditionsRedacted,
    string RollbackPlanRedacted,
    IReadOnlyList<MissionExpectedEvidence> ExpectedEvidence,
    IReadOnlyList<string> BlockersRedacted);

public sealed record NodalOsPersistedWorkspaceMissionDraft(
    int SchemaVersion,
    string WorkspaceFingerprint,
    string GoalRedacted,
    bool GoalWasRedacted,
    NodalOsWorkspaceMissionBinding Binding,
    MissionPlan Plan,
    NodalOsReviewedWorkspaceActionCandidate Candidate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record NodalOsWorkspaceMissionDraftSnapshot(
    string Decision,
    bool Accepted,
    NodalOsWorkspaceMissionDraftState State,
    string? WorkspaceId,
    string? WorkspaceDisplayNameRedacted,
    string? WorkspaceFingerprint,
    string? MissionId,
    string? GoalRedacted,
    bool GoalWasRedacted,
    NodalOsWorkspaceMissionBinding? Binding,
    MissionPlan? Plan,
    NodalOsReviewedWorkspaceActionCandidate? Candidate,
    bool Persisted,
    bool Rehydrated,
    int ProgressPercent,
    string CurrentStep,
    string ApprovalState,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ReviewBlockers,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? LastValidatedAt,
    bool RealFilesystemRead,
    bool WorkspaceFilesystemMutated,
    bool AppConfigurationMutated,
    bool NetworkUsed,
    bool SecretsExcluded,
    bool ProductAuthorityGranted);

public sealed class NodalOsWorkspaceMissionDraftService
{
    public const int CurrentSchemaVersion = 1;
    public const string RelativeTargetPath = "NODAL_HANDOFF.md";

    private const int MaximumMetadataBytes = 1024 * 1024;
    private const long MaximumExistingTargetBytes = 1024 * 1024;
    private const int MaximumProposedContentBytes = 32 * 1024;

    private readonly string _metadataFilePath;
    private readonly NodalOsWorkspaceSelectionService _workspaceSelection;
    private readonly ISecretReferenceStore _rootReferenceStore;
    private readonly NodalOsWorkspaceMissionBindingService _bindingService;
    private readonly NodalOsRedactionService _redaction;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public NodalOsWorkspaceMissionDraftService(
        string metadataFilePath,
        NodalOsWorkspaceSelectionService workspaceSelection,
        ISecretReferenceStore rootReferenceStore,
        NodalOsWorkspaceMissionBindingService? bindingService = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataFilePath);
        ArgumentNullException.ThrowIfNull(workspaceSelection);
        ArgumentNullException.ThrowIfNull(rootReferenceStore);

        _metadataFilePath = Path.GetFullPath(metadataFilePath);
        _workspaceSelection = workspaceSelection;
        _rootReferenceStore = rootReferenceStore;
        _bindingService = bindingService ?? new NodalOsWorkspaceMissionBindingService();
        _redaction = new NodalOsRedactionService();
    }

    public string MetadataFilePath => _metadataFilePath;

    public async ValueTask<NodalOsWorkspaceMissionDraftSnapshot> CreateAsync(
        string? goal,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspaceSelection.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!workspace.Accepted || workspace.Workspace is null || string.IsNullOrWhiteSpace(workspace.RootPathFingerprint))
            return WorkspaceRequired(workspace.ReviewBlockers);

        var normalizedGoal = NormalizeGoal(goal, out var goalWasRedacted, out var goalBlocker);
        if (normalizedGoal is null)
            return GoalRejected(goalBlocker ?? "Mission goal is invalid.", workspace);

        var selectionDocument = await ReadWorkspaceSelectionDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (selectionDocument is null ||
            selectionDocument.SchemaVersion != NodalOsWorkspaceSelectionService.CurrentSchemaVersion ||
            !string.Equals(selectionDocument.Workspace.WorkspaceId, workspace.WorkspaceId, StringComparison.Ordinal) ||
            !string.Equals(selectionDocument.Workspace.RootPathFingerprint, workspace.RootPathFingerprint, StringComparison.Ordinal))
        {
            return Failure(
                "BLOCKED_REAL_WORKSPACE_MISSION_SELECTION_METADATA_INVALID",
                NodalOsWorkspaceMissionDraftState.FailedClosed,
                ["Workspace selection metadata could not be bound safely."],
                workspace);
        }

        using var lease = await OpenRootLeaseAsync(selectionDocument.RootPathReference, cancellationToken).ConfigureAwait(false);
        if (lease is null)
        {
            return Failure(
                "BLOCKED_REAL_WORKSPACE_MISSION_ROOT_UNAVAILABLE",
                NodalOsWorkspaceMissionDraftState.FailedClosed,
                ["The protected workspace root could not be opened."],
                workspace);
        }

        var rootPath = DecodeRoot(lease);
        if (rootPath is null)
        {
            return Failure(
                "BLOCKED_REAL_WORKSPACE_MISSION_ROOT_INVALID",
                NodalOsWorkspaceMissionDraftState.FailedClosed,
                ["The protected workspace root is invalid."],
                workspace);
        }

        var missionId = MissionId(workspace.WorkspaceId!, normalizedGoal);
        var title = Title(normalizedGoal);
        var proposedContent = ProposedContent(
            normalizedGoal,
            workspace.DisplayNameRedacted ?? "Selected Local Workspace",
            missionId);
        var candidate = await InspectCandidateAsync(
                rootPath,
                missionId,
                proposedContent,
                cancellationToken)
            .ConfigureAwait(false);
        if (candidate.State == NodalOsReviewedWorkspaceActionState.Blocked)
        {
            return Failure(
                "BLOCKED_REAL_WORKSPACE_MISSION_ACTION_CANDIDATE",
                NodalOsWorkspaceMissionDraftState.FailedClosed,
                candidate.BlockersRedacted,
                workspace,
                candidate: candidate);
        }

        var evidenceRefs = workspace.Workspace.EvidenceRefs
            .Select(reference => reference.EvidenceId)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Take(12)
            .ToArray();
        var plan = BuildPlan(missionId, normalizedGoal, candidate, evidenceRefs);
        var baseBinding = _bindingService.CreateBinding(workspace.Workspace, missionId);
        var binding = baseBinding with
        {
            Status = NodalOsWorkspaceMissionBindingStatus.BoundReadOnly,
            MissionTitleRedacted = title,
            MissionSummaryRedacted = normalizedGoal,
            ActiveTimelineRefs = workspace.Workspace.TimelineRefs
                .Append($"timeline:mission-draft:{missionId}")
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            ActiveApprovalRefs = [$"approval:mission-scope:{missionId}:pending"],
            ActiveEvidenceRefs = workspace.Workspace.EvidenceRefs,
            ObservabilityReportRefs = [$"observability:mission-draft:{missionId}"],
            AllowedCapabilitiesRedacted = ["workspace.read", "planning.preview", "filesystem.write.safe after approval"],
            DisabledCapabilitiesRedacted = ["runtime execution before approval", "terminal.execute", "network", "cloud sync", "product authority"],
            NextSafeStepsRedacted = plan.Steps.Select(step => step.Intent).Take(8).ToArray(),
            GuardrailSummaryRedacted = workspace.Workspace.GuardrailSummaryRedacted
                .Append("The reviewed action remains disabled until mission scope approval and execution-time precondition validation.")
                .ToArray(),
            ReadOnlyPreview = true,
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            CloudSyncAllowed = false,
            LlmProviderCallsAllowed = false,
            TaskGraphCreated = true,
            TouchesFilesystem = false,
            MutatesExecutionRegistryRuntime = false,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var now = DateTimeOffset.UtcNow;
        var document = new NodalOsPersistedWorkspaceMissionDraft(
            CurrentSchemaVersion,
            workspace.RootPathFingerprint!,
            normalizedGoal,
            goalWasRedacted,
            binding,
            plan,
            candidate,
            now,
            now);

        try
        {
            await WriteDocumentAsync(document, rootPath, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Failure(
                "BLOCKED_REAL_WORKSPACE_MISSION_PERSISTENCE_FAILED",
                NodalOsWorkspaceMissionDraftState.FailedClosed,
                ["Mission draft persistence failed closed."],
                workspace,
                binding,
                plan,
                candidate);
        }

        return Success(document, workspace, rehydrated: false, appConfigurationMutated: true, DateTimeOffset.UtcNow);
    }

    public async ValueTask<NodalOsWorkspaceMissionDraftSnapshot> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_metadataFilePath))
            return NotConfigured();

        NodalOsPersistedWorkspaceMissionDraft? document;
        try
        {
            document = await ReadDocumentAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Failure(
                "BLOCKED_REAL_WORKSPACE_MISSION_METADATA_CORRUPT",
                NodalOsWorkspaceMissionDraftState.CorruptMetadata,
                ["Persisted mission draft metadata is unavailable or invalid."]);
        }

        if (document is null || !ValidateDocument(document))
        {
            return Failure(
                "BLOCKED_REAL_WORKSPACE_MISSION_METADATA_CORRUPT",
                NodalOsWorkspaceMissionDraftState.CorruptMetadata,
                ["Persisted mission draft metadata failed the canonical validation boundary."]);
        }

        var workspace = await _workspaceSelection.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!workspace.Accepted || workspace.Workspace is null)
            return WorkspaceRequired(workspace.ReviewBlockers, persisted: true);
        if (!string.Equals(document.Binding.WorkspaceId, workspace.WorkspaceId, StringComparison.Ordinal) ||
            !string.Equals(document.WorkspaceFingerprint, workspace.RootPathFingerprint, StringComparison.Ordinal))
        {
            return Failure(
                "BLOCKED_REAL_WORKSPACE_MISSION_WORKSPACE_CHANGED",
                NodalOsWorkspaceMissionDraftState.CandidateStale,
                ["The selected workspace changed after this mission draft was created."],
                workspace,
                document.Binding,
                document.Plan,
                document.Candidate,
                persisted: true);
        }

        var selectionDocument = await ReadWorkspaceSelectionDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (selectionDocument is null)
        {
            return Failure(
                "BLOCKED_REAL_WORKSPACE_MISSION_SELECTION_METADATA_INVALID",
                NodalOsWorkspaceMissionDraftState.FailedClosed,
                ["Workspace selection metadata could not be reopened."],
                workspace,
                document.Binding,
                document.Plan,
                document.Candidate,
                persisted: true);
        }

        using var lease = await OpenRootLeaseAsync(selectionDocument.RootPathReference, cancellationToken).ConfigureAwait(false);
        var rootPath = lease is null ? null : DecodeRoot(lease);
        if (rootPath is null)
        {
            return Failure(
                "BLOCKED_REAL_WORKSPACE_MISSION_ROOT_UNAVAILABLE",
                NodalOsWorkspaceMissionDraftState.FailedClosed,
                ["The protected workspace root could not be reopened."],
                workspace,
                document.Binding,
                document.Plan,
                document.Candidate,
                persisted: true);
        }

        var currentCandidate = await InspectCandidateAsync(
                rootPath,
                document.Binding.MissionId,
                document.Candidate.ProposedContentRedacted,
                cancellationToken)
            .ConfigureAwait(false);
        if (!SamePrecondition(document.Candidate, currentCandidate))
        {
            var stale = document.Candidate with
            {
                State = NodalOsReviewedWorkspaceActionState.StalePrecondition,
                ExecutionEnabled = false,
                BlockersRedacted = ["The target changed after review. Recreate the mission draft before approval or execution."]
            };
            return Failure(
                "BLOCKED_REAL_WORKSPACE_MISSION_ACTION_PRECONDITION_CHANGED",
                NodalOsWorkspaceMissionDraftState.CandidateStale,
                stale.BlockersRedacted,
                workspace,
                document.Binding,
                document.Plan,
                stale,
                persisted: true);
        }

        return Success(document, workspace, rehydrated: true, appConfigurationMutated: false, DateTimeOffset.UtcNow);
    }

    public async ValueTask<NodalOsWorkspaceMissionDraftSnapshot> ClearAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(_metadataFilePath))
                File.Delete(_metadataFilePath);
        }
        catch
        {
            return Failure(
                "BLOCKED_REAL_WORKSPACE_MISSION_CLEAR_FAILED",
                NodalOsWorkspaceMissionDraftState.FailedClosed,
                ["Mission draft could not be cleared safely."],
                persisted: true);
        }

        await Task.CompletedTask.ConfigureAwait(false);
        return NotConfigured(appConfigurationMutated: true);
    }

    private string? NormalizeGoal(string? goal, out bool wasRedacted, out string? blocker)
    {
        wasRedacted = false;
        blocker = null;
        if (string.IsNullOrWhiteSpace(goal))
        {
            blocker = "Mission goal is required.";
            return null;
        }

        var normalized = SafeRuntimeText.Sanitize(goal, 600).Trim();
        if (normalized.Length < 8)
        {
            blocker = "Mission goal must contain at least 8 safe characters.";
            return null;
        }
        if (ContainsLocalPath(goal))
        {
            blocker = "Mission goal must not contain an absolute local path.";
            return null;
        }

        var redacted = _redaction.RedactValue(
            normalized,
            new NodalOsRedactionOptions { RedactWholeValue = false });
        wasRedacted = redacted.WasRedacted;
        normalized = SafeRuntimeText.Sanitize(redacted.Value, 600).Trim();
        if (normalized.Length < 8 || HistorySanitizer.ContainsSecretLikeContent(normalized))
        {
            blocker = "Mission goal still contains credential-like content after redaction.";
            return null;
        }
        return normalized;
    }

    private async ValueTask<NodalOsReviewedWorkspaceActionCandidate> InspectCandidateAsync(
        string rootPath,
        string missionId,
        string proposedContent,
        CancellationToken cancellationToken)
    {
        string canonicalRoot;
        string targetPath;
        try
        {
            canonicalRoot = Path.GetFullPath(rootPath);
            targetPath = Path.GetFullPath(Path.Combine(canonicalRoot, RelativeTargetPath));
        }
        catch
        {
            return BlockedCandidate(missionId, proposedContent, "The candidate target path is invalid.");
        }

        var rootPrefix = canonicalRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!targetPath.StartsWith(rootPrefix, comparison) || Path.IsPathRooted(RelativeTargetPath) || RelativeTargetPath.Contains("..", StringComparison.Ordinal))
            return BlockedCandidate(missionId, proposedContent, "The candidate target escaped the selected workspace boundary.");
        if (Directory.Exists(targetPath))
            return BlockedCandidate(missionId, proposedContent, "The candidate target is an existing directory.");

        var proposedBytes = Encoding.UTF8.GetBytes(proposedContent);
        try
        {
            if (proposedBytes.Length is <= 0 or > MaximumProposedContentBytes)
                return BlockedCandidate(missionId, proposedContent, "The proposed handoff content exceeds the bounded size.");

            var proposedSha = Convert.ToHexString(SHA256.HashData(proposedBytes)).ToLowerInvariant();
            if (!File.Exists(targetPath))
            {
                return Candidate(
                    missionId,
                    NodalOsReviewedWorkspaceActionKind.CreateTextFile,
                    targetExists: false,
                    existingBytes: 0,
                    existingSha: null,
                    proposedContent,
                    proposedBytes.Length,
                    proposedSha);
            }

            var info = new FileInfo(targetPath);
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
                return BlockedCandidate(missionId, proposedContent, "The candidate target is a reparse point and cannot be reviewed safely.");
            if (info.Length > MaximumExistingTargetBytes)
                return BlockedCandidate(missionId, proposedContent, "The existing target exceeds the bounded exact-hash update size.");

            await using var stream = new FileStream(
                targetPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var existingSha = Convert.ToHexString(
                    await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false))
                .ToLowerInvariant();
            return Candidate(
                missionId,
                NodalOsReviewedWorkspaceActionKind.ExactHashUpdate,
                targetExists: true,
                existingBytes: info.Length,
                existingSha,
                proposedContent,
                proposedBytes.Length,
                proposedSha);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(proposedBytes);
        }
    }

    private static NodalOsReviewedWorkspaceActionCandidate Candidate(
        string missionId,
        NodalOsReviewedWorkspaceActionKind kind,
        bool targetExists,
        long existingBytes,
        string? existingSha,
        string proposedContent,
        int proposedBytes,
        string proposedSha)
    {
        var create = kind == NodalOsReviewedWorkspaceActionKind.CreateTextFile;
        return new(
            ActionId: $"action-{missionId}-handoff",
            Kind: kind,
            State: NodalOsReviewedWorkspaceActionState.ReadyForReview,
            RelativeTargetPath,
            DescriptionRedacted: create
                ? "Create a deterministic mission handoff document in the selected workspace."
                : "Replace the existing mission handoff only when its exact current hash still matches.",
            RiskLevel: create ? MissionRiskLevel.Low : MissionRiskLevel.Medium,
            Reversible: true,
            ApprovalRequired: true,
            RequiredApprovalScope: $"mission:{missionId}:filesystem.write.safe:{RelativeTargetPath}",
            ExecutionEnabled: false,
            TargetExists: targetExists,
            ExistingBytes: existingBytes,
            ExistingSha256: existingSha,
            ProposedContentRedacted: proposedContent,
            ProposedBytes: proposedBytes,
            ProposedSha256: proposedSha,
            PreconditionsRedacted: create
                ? ["Selected workspace fingerprint must still match.", "Target must not exist at execution time."]
                : ["Selected workspace fingerprint must still match.", "Target must exist and match the reviewed SHA-256 at execution time."],
            RollbackPlanRedacted: create
                ? "Delete only the created file when the exact post-write hash and operation identity match."
                : "Create a bounded pre-write snapshot and restore it only when operation identity and post-write hash match.",
            ExpectedEvidence:
            [
                new MissionExpectedEvidence("precondition", "Workspace fingerprint and target state validated at execution time."),
                new MissionExpectedEvidence("rollback-plan", "Create cleanup or exact-hash restore plan prepared before write."),
                new MissionExpectedEvidence("post-write-hash", "Resulting file hash recorded after atomic write."),
                new MissionExpectedEvidence("verification", "Deterministic content and file-state verification completed."),
                new MissionExpectedEvidence("timeline", "Redacted evidence and mission timeline refs attached before completion.")
            ],
            BlockersRedacted: []);
    }

    private static NodalOsReviewedWorkspaceActionCandidate BlockedCandidate(
        string missionId,
        string proposedContent,
        string blocker)
    {
        var bytes = Encoding.UTF8.GetByteCount(proposedContent);
        var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(proposedContent))).ToLowerInvariant();
        return new(
            ActionId: $"action-{missionId}-handoff",
            Kind: NodalOsReviewedWorkspaceActionKind.CreateTextFile,
            State: NodalOsReviewedWorkspaceActionState.Blocked,
            RelativeTargetPath,
            DescriptionRedacted: "Reviewed workspace handoff action is blocked.",
            RiskLevel: MissionRiskLevel.Medium,
            Reversible: false,
            ApprovalRequired: true,
            RequiredApprovalScope: $"mission:{missionId}:filesystem.write.safe:{RelativeTargetPath}",
            ExecutionEnabled: false,
            TargetExists: false,
            ExistingBytes: 0,
            ExistingSha256: null,
            ProposedContentRedacted: proposedContent,
            ProposedBytes: bytes,
            ProposedSha256: sha,
            PreconditionsRedacted: [],
            RollbackPlanRedacted: "No rollback plan is available while the candidate is blocked.",
            ExpectedEvidence: [],
            BlockersRedacted: [SafeRuntimeText.Sanitize(blocker, 240)]);
    }

    private static MissionPlan BuildPlan(
        string missionId,
        string goal,
        NodalOsReviewedWorkspaceActionCandidate candidate,
        IReadOnlyList<string> workspaceEvidence)
    {
        var now = DateTimeOffset.UtcNow;
        return new MissionPlan(
            missionId,
            Version: 1,
            CreatedAt: now,
            Goal: goal,
            Steps:
            [
                new MissionStep(
                    "workspace-context-reviewed",
                    ParentId: null,
                    Intent: "Use the revalidated selected workspace context and evidence.",
                    MissionExecutionSurface.Reasoning,
                    ["workspace.read", "planning.preview"],
                    [new MissionExpectedEvidence("workspace-selection", "Protected workspace identity and bounded scan evidence." )],
                    MissionRiskLevel.Low,
                    ApprovalRequired: false,
                    Dependencies: [],
                    MissionStepStatus.Verified,
                    Attempts: 1,
                    LastFailure: null,
                    EvidenceRefs: workspaceEvidence),
                new MissionStep(
                    "review-action-candidate",
                    ParentId: null,
                    Intent: $"Review the proposed {candidate.Kind} for {candidate.RelativeTargetPath}.",
                    MissionExecutionSurface.Filesystem,
                    ["filesystem.write.safe"],
                    candidate.ExpectedEvidence,
                    candidate.RiskLevel,
                    ApprovalRequired: true,
                    Dependencies: ["workspace-context-reviewed"],
                    MissionStepStatus.InProgress,
                    Attempts: 0,
                    LastFailure: null,
                    EvidenceRefs: []),
                new MissionStep(
                    "resolve-mission-scope-approval",
                    ParentId: null,
                    Intent: "Resolve explicit mission-scope approval for the reviewed relative target and capability.",
                    MissionExecutionSurface.Human,
                    ["approval.resolve"],
                    [new MissionExpectedEvidence("approval", "Approval decision bound to mission, workspace fingerprint, action id and relative target.")],
                    candidate.RiskLevel,
                    ApprovalRequired: false,
                    Dependencies: ["review-action-candidate"],
                    MissionStepStatus.Pending,
                    Attempts: 0,
                    LastFailure: null,
                    EvidenceRefs: []),
                new MissionStep(
                    "execute-controlled-action",
                    ParentId: null,
                    Intent: "Execute only through the existing controlled file-operation boundary after approval and fresh precondition checks.",
                    MissionExecutionSurface.Filesystem,
                    ["filesystem.write.safe"],
                    candidate.ExpectedEvidence,
                    candidate.RiskLevel,
                    ApprovalRequired: true,
                    Dependencies: ["resolve-mission-scope-approval"],
                    MissionStepStatus.Pending,
                    Attempts: 0,
                    LastFailure: null,
                    EvidenceRefs: []),
                new MissionStep(
                    "verify-result",
                    ParentId: null,
                    Intent: "Verify resulting content, file state, rollback readiness and absence of unintended side effects.",
                    MissionExecutionSurface.Filesystem,
                    ["verification.run"],
                    [new MissionExpectedEvidence("verification", "Deterministic post-write verification report.")],
                    MissionRiskLevel.Low,
                    ApprovalRequired: false,
                    Dependencies: ["execute-controlled-action"],
                    MissionStepStatus.Pending,
                    Attempts: 0,
                    LastFailure: null,
                    EvidenceRefs: []),
                new MissionStep(
                    "record-evidence-handoff",
                    ParentId: null,
                    Intent: "Attach redacted evidence and update the mission handoff only after successful verification.",
                    MissionExecutionSurface.Reasoning,
                    ["evidence.append", "evidence.export"],
                    [new MissionExpectedEvidence("handoff", "Redacted deterministic handoff and timeline refs.")],
                    MissionRiskLevel.Low,
                    ApprovalRequired: false,
                    Dependencies: ["verify-result"],
                    MissionStepStatus.Pending,
                    Attempts: 0,
                    LastFailure: null,
                    EvidenceRefs: [])
            ],
            Status: MissionStatus.Active);
    }

    private async ValueTask<NodalOsPersistedWorkspaceSelection?> ReadWorkspaceSelectionDocumentAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var path = _workspaceSelection.MetadataFilePath;
            var info = new FileInfo(path);
            if (!info.Exists || info.Length is <= 0 or > MaximumMetadataBytes)
                return null;
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<NodalOsPersistedWorkspaceSelection>(json, JsonOptions);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private async ValueTask<SecretLease?> OpenRootLeaseAsync(
        SecretReference reference,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _rootReferenceStore.OpenAsync(reference, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static string? DecodeRoot(SecretLease lease)
    {
        try
        {
            var root = Encoding.UTF8.GetString(lease.Bytes.Span);
            return string.IsNullOrWhiteSpace(root) ? null : Path.GetFullPath(root);
        }
        catch
        {
            return null;
        }
    }

    private async ValueTask<NodalOsPersistedWorkspaceMissionDraft?> ReadDocumentAsync(
        CancellationToken cancellationToken)
    {
        var info = new FileInfo(_metadataFilePath);
        if (!info.Exists || info.Length is <= 0 or > MaximumMetadataBytes)
            throw new InvalidDataException("Mission draft metadata size is invalid.");
        var json = await File.ReadAllTextAsync(_metadataFilePath, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<NodalOsPersistedWorkspaceMissionDraft>(json, JsonOptions);
    }

    private async ValueTask WriteDocumentAsync(
        NodalOsPersistedWorkspaceMissionDraft document,
        string rootPath,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(document, JsonOptions);
        if (json.Length > MaximumMetadataBytes ||
            json.Contains(rootPath, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) ||
            HistorySanitizer.ContainsSecretLikeContent(json))
        {
            throw new InvalidDataException("Mission draft metadata crossed a path or secret boundary.");
        }

        var directory = Path.GetDirectoryName(_metadataFilePath)
            ?? throw new InvalidOperationException("Mission draft metadata directory is unavailable.");
        Directory.CreateDirectory(directory);
        var tempPath = $"{_metadataFilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(
                    tempPath,
                    json,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    cancellationToken)
                .ConfigureAwait(false);
            File.Move(tempPath, _metadataFilePath, overwrite: true);
            TryHide(_metadataFilePath);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static bool ValidateDocument(NodalOsPersistedWorkspaceMissionDraft document)
    {
        if (document.SchemaVersion != CurrentSchemaVersion ||
            string.IsNullOrWhiteSpace(document.WorkspaceFingerprint) || document.WorkspaceFingerprint.Length != 64 ||
            string.IsNullOrWhiteSpace(document.GoalRedacted) ||
            string.IsNullOrWhiteSpace(document.Binding.MissionId) ||
            document.Plan.MissionId != document.Binding.MissionId ||
            document.Candidate.ActionId.Length > 180 ||
            document.Candidate.ProposedSha256.Length != 64 ||
            document.Candidate.ProposedBytes is <= 0 or > MaximumProposedContentBytes ||
            Path.IsPathRooted(document.Candidate.RelativeTargetPath) ||
            !string.Equals(document.Candidate.RelativeTargetPath, RelativeTargetPath, StringComparison.Ordinal) ||
            document.Candidate.ExecutionEnabled ||
            document.Binding.RuntimeExecutionAllowed ||
            document.Binding.CanAuthorizeExecution ||
            document.Binding.TouchesFilesystem ||
            document.Binding.MutatesExecutionRegistryRuntime)
        {
            return false;
        }

        return !ContainsLocalPath(document.GoalRedacted) &&
               !HistorySanitizer.ContainsSecretLikeContent(document.GoalRedacted) &&
               !HistorySanitizer.ContainsSecretLikeContent(document.Candidate.ProposedContentRedacted);
    }

    private static bool SamePrecondition(
        NodalOsReviewedWorkspaceActionCandidate expected,
        NodalOsReviewedWorkspaceActionCandidate current) =>
        expected.Kind == current.Kind &&
        expected.TargetExists == current.TargetExists &&
        expected.ExistingBytes == current.ExistingBytes &&
        string.Equals(expected.ExistingSha256, current.ExistingSha256, StringComparison.Ordinal) &&
        string.Equals(expected.ProposedSha256, current.ProposedSha256, StringComparison.Ordinal);

    private static string ProposedContent(string goal, string workspaceDisplayName, string missionId) => $"""
# NODAL OS Mission Handoff Draft

Mission: {goal}
Workspace: {workspaceDisplayName}
Mission ID: {missionId}
Status: reviewed action candidate — not executed

## Planned outcome
- Preserve the user-approved mission goal.
- Keep the action inside the selected workspace and relative target.
- Verify the result before evidence or completion promotion.

## Safety boundary
- Mission-scope approval is required before write.
- Execution must use the controlled file-operation boundary.
- Rollback and evidence must be prepared before completion.
""";

    private static string MissionId(string workspaceId, string goal)
    {
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{workspaceId}\n{goal}"))).ToLowerInvariant();
        return $"mission-{digest[..16]}";
    }

    private static string Title(string goal) =>
        goal.Length <= 88 ? goal : $"{goal[..85]}...";

    private static bool ContainsLocalPath(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        HistorySanitizer.SanitizeText(value).Contains("[LOCAL_PATH]", StringComparison.Ordinal);

    private static NodalOsWorkspaceMissionDraftSnapshot Success(
        NodalOsPersistedWorkspaceMissionDraft document,
        NodalOsWorkspaceSelectionSnapshot workspace,
        bool rehydrated,
        bool appConfigurationMutated,
        DateTimeOffset validatedAt)
    {
        var verified = document.Plan.Steps.Count(step => step.Status == MissionStepStatus.Verified);
        var progress = document.Plan.Steps.Count == 0
            ? 0
            : (int)Math.Round((double)verified / document.Plan.Steps.Count * 100, MidpointRounding.AwayFromZero);
        return new(
            Decision: rehydrated
                ? "GO_REAL_WORKSPACE_MISSION_DRAFT_REHYDRATED"
                : "GO_REAL_WORKSPACE_MISSION_DRAFT_READY",
            Accepted: true,
            State: NodalOsWorkspaceMissionDraftState.ReadyForReview,
            WorkspaceId: workspace.WorkspaceId,
            WorkspaceDisplayNameRedacted: workspace.DisplayNameRedacted,
            WorkspaceFingerprint: workspace.RootPathFingerprint,
            MissionId: document.Binding.MissionId,
            GoalRedacted: document.GoalRedacted,
            GoalWasRedacted: document.GoalWasRedacted,
            Binding: document.Binding,
            Plan: document.Plan,
            Candidate: document.Candidate,
            Persisted: true,
            Rehydrated: rehydrated,
            ProgressPercent: progress,
            CurrentStep: "review-action-candidate",
            ApprovalState: "Mission-scope approval required before execution.",
            EvidenceRefs: document.Binding.ActiveEvidenceRefs.Select(reference => reference.EvidenceId).Distinct(StringComparer.Ordinal).ToArray(),
            ReviewBlockers: [],
            CreatedAt: document.CreatedAt,
            LastValidatedAt: validatedAt,
            RealFilesystemRead: true,
            WorkspaceFilesystemMutated: false,
            AppConfigurationMutated: appConfigurationMutated,
            NetworkUsed: false,
            SecretsExcluded: true,
            ProductAuthorityGranted: false);
    }

    private static NodalOsWorkspaceMissionDraftSnapshot NotConfigured(bool appConfigurationMutated = false) => new(
        Decision: "REAL_WORKSPACE_MISSION_DRAFT_REQUIRED",
        Accepted: false,
        State: NodalOsWorkspaceMissionDraftState.NotConfigured,
        WorkspaceId: null,
        WorkspaceDisplayNameRedacted: null,
        WorkspaceFingerprint: null,
        MissionId: null,
        GoalRedacted: null,
        GoalWasRedacted: false,
        Binding: null,
        Plan: null,
        Candidate: null,
        Persisted: false,
        Rehydrated: false,
        ProgressPercent: 0,
        CurrentStep: "create-mission-draft",
        ApprovalState: "No mission scope exists.",
        EvidenceRefs: [],
        ReviewBlockers: ["Create a mission draft after selecting a real local workspace."],
        CreatedAt: null,
        LastValidatedAt: null,
        RealFilesystemRead: false,
        WorkspaceFilesystemMutated: false,
        AppConfigurationMutated: appConfigurationMutated,
        NetworkUsed: false,
        SecretsExcluded: true,
        ProductAuthorityGranted: false);

    private static NodalOsWorkspaceMissionDraftSnapshot WorkspaceRequired(
        IReadOnlyList<string> blockers,
        bool persisted = false) => new(
        Decision: "BLOCKED_REAL_WORKSPACE_MISSION_WORKSPACE_REQUIRED",
        Accepted: false,
        State: NodalOsWorkspaceMissionDraftState.WorkspaceRequired,
        WorkspaceId: null,
        WorkspaceDisplayNameRedacted: null,
        WorkspaceFingerprint: null,
        MissionId: null,
        GoalRedacted: null,
        GoalWasRedacted: false,
        Binding: null,
        Plan: null,
        Candidate: null,
        Persisted: persisted,
        Rehydrated: false,
        ProgressPercent: 0,
        CurrentStep: "select-workspace",
        ApprovalState: "No mission scope exists.",
        EvidenceRefs: [],
        ReviewBlockers: blockers.DefaultIfEmpty("Select a real local workspace first.").Take(12).ToArray(),
        CreatedAt: null,
        LastValidatedAt: null,
        RealFilesystemRead: false,
        WorkspaceFilesystemMutated: false,
        AppConfigurationMutated: false,
        NetworkUsed: false,
        SecretsExcluded: true,
        ProductAuthorityGranted: false);

    private static NodalOsWorkspaceMissionDraftSnapshot GoalRejected(
        string blocker,
        NodalOsWorkspaceSelectionSnapshot workspace) => new(
        Decision: "BLOCKED_REAL_WORKSPACE_MISSION_GOAL_REJECTED",
        Accepted: false,
        State: NodalOsWorkspaceMissionDraftState.GoalRejected,
        WorkspaceId: workspace.WorkspaceId,
        WorkspaceDisplayNameRedacted: workspace.DisplayNameRedacted,
        WorkspaceFingerprint: workspace.RootPathFingerprint,
        MissionId: null,
        GoalRedacted: null,
        GoalWasRedacted: false,
        Binding: null,
        Plan: null,
        Candidate: null,
        Persisted: false,
        Rehydrated: false,
        ProgressPercent: 0,
        CurrentStep: "enter-safe-mission-goal",
        ApprovalState: "No mission scope exists.",
        EvidenceRefs: [],
        ReviewBlockers: [SafeRuntimeText.Sanitize(blocker, 240)],
        CreatedAt: null,
        LastValidatedAt: null,
        RealFilesystemRead: workspace.RealFilesystemRead,
        WorkspaceFilesystemMutated: false,
        AppConfigurationMutated: false,
        NetworkUsed: false,
        SecretsExcluded: true,
        ProductAuthorityGranted: false);

    private static NodalOsWorkspaceMissionDraftSnapshot Failure(
        string decision,
        NodalOsWorkspaceMissionDraftState state,
        IReadOnlyList<string> blockers,
        NodalOsWorkspaceSelectionSnapshot? workspace = null,
        NodalOsWorkspaceMissionBinding? binding = null,
        MissionPlan? plan = null,
        NodalOsReviewedWorkspaceActionCandidate? candidate = null,
        bool persisted = false) => new(
        Decision: decision,
        Accepted: false,
        State: state,
        WorkspaceId: workspace?.WorkspaceId,
        WorkspaceDisplayNameRedacted: workspace?.DisplayNameRedacted,
        WorkspaceFingerprint: workspace?.RootPathFingerprint,
        MissionId: binding?.MissionId,
        GoalRedacted: binding?.MissionSummaryRedacted,
        GoalWasRedacted: false,
        Binding: binding,
        Plan: plan,
        Candidate: candidate,
        Persisted: persisted,
        Rehydrated: false,
        ProgressPercent: plan is null || plan.Steps.Count == 0
            ? 0
            : (int)Math.Round((double)plan.Steps.Count(step => step.Status == MissionStepStatus.Verified) / plan.Steps.Count * 100, MidpointRounding.AwayFromZero),
        CurrentStep: candidate?.State == NodalOsReviewedWorkspaceActionState.StalePrecondition
            ? "refresh-action-candidate"
            : "mission-draft-blocked",
        ApprovalState: "Approval and execution remain disabled.",
        EvidenceRefs: binding?.ActiveEvidenceRefs.Select(reference => reference.EvidenceId).Distinct(StringComparer.Ordinal).ToArray() ?? [],
        ReviewBlockers: blockers.Select(value => SafeRuntimeText.Sanitize(value, 240)).Distinct(StringComparer.Ordinal).Take(12).ToArray(),
        CreatedAt: binding?.CreatedAt,
        LastValidatedAt: DateTimeOffset.UtcNow,
        RealFilesystemRead: workspace?.RealFilesystemRead ?? false,
        WorkspaceFilesystemMutated: false,
        AppConfigurationMutated: false,
        NetworkUsed: false,
        SecretsExcluded: true,
        ProductAuthorityGranted: false);

    private static void TryHide(string path)
    {
        try
        {
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
