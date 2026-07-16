using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core.Runtime;
using OneBrain.Core.History;
using OneBrain.Core.Models;

namespace OneBrain.AgentOperations.Core.Workspace;

public enum NodalOsWorkspaceHandoffExecutionState
{
    NotConfigured,
    ReadyForApproval,
    ApprovedPendingExecution,
    Completed,
    RolledBack,
    CandidateStale,
    ResultChanged,
    FailedClosed
}

public sealed record NodalOsMissionScopeApprovalBinding(
    string ScopeId,
    string MissionId,
    string WorkspaceId,
    string WorkspaceFingerprint,
    string ActionId,
    string CapabilityId,
    string RelativeTargetPath,
    string ProposedSha256,
    string? ExpectedCurrentSha256,
    NodalOsApprovalCard ApprovalCard,
    NodalOsApprovalDecision ApprovalDecision,
    DateTimeOffset ApprovedAt,
    bool OneShot,
    bool DecisionIsExecutionAuthority);

public sealed record NodalOsWorkspaceHandoffRestorePlan(
    string RestorePlanId,
    string OperationId,
    string ApprovalDecisionId,
    string WorkspaceFingerprint,
    string ActionId,
    NodalOsReviewedWorkspaceActionKind ActionKind,
    string RelativeTargetPath,
    string ResultSha256,
    string? OriginalSha256,
    string? SnapshotId,
    bool DeleteCreatedFile,
    bool SnapshotStoredOutsideWorkspace,
    bool RequiresExactCurrentHash,
    DateTimeOffset CreatedAt);

public sealed record NodalOsPersistedWorkspaceHandoffExecution(
    int SchemaVersion,
    NodalOsWorkspaceHandoffExecutionState State,
    string OperationId,
    string MissionId,
    string WorkspaceId,
    string WorkspaceFingerprint,
    NodalOsReviewedWorkspaceActionCandidate Candidate,
    NodalOsMissionScopeApprovalBinding Approval,
    NodalOsExecutionRegistryEntry RegistryEntry,
    string? ResultSha256,
    string? OriginalSha256,
    long BytesWritten,
    bool Verified,
    NodalOsWorkspaceHandoffRestorePlan? RestorePlan,
    IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs,
    IReadOnlyList<NodalOsCoreTimelineProjection> Timeline,
    string? FailureReasonRedacted,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ExecutedAt,
    DateTimeOffset? RolledBackAt);

public sealed record NodalOsWorkspaceHandoffExecutionSnapshot(
    string Decision,
    bool Accepted,
    NodalOsWorkspaceHandoffExecutionState State,
    string? OperationId,
    string? MissionId,
    string? WorkspaceId,
    string? WorkspaceFingerprint,
    string? ActionId,
    string? ActionKind,
    string RelativeTargetPath,
    string? ProposedSha256,
    string? ExpectedCurrentSha256,
    string? ResultSha256,
    string? OriginalSha256,
    long BytesWritten,
    string ApprovalStatus,
    string? ApprovalDecisionId,
    string? ApprovalScope,
    string RegistryState,
    bool Persisted,
    bool Rehydrated,
    bool Executed,
    bool Verified,
    bool RollbackAvailable,
    bool RolledBack,
    string? RestorePlanId,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<NodalOsCoreTimelineProjection> Timeline,
    IReadOnlyList<string> Blockers,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? ExecutedAt,
    DateTimeOffset? RolledBackAt,
    bool RealFilesystemRead,
    bool WorkspaceFilesystemMutated,
    bool AppConfigurationMutated,
    bool NetworkUsed,
    bool ExternalProcessUsed,
    bool SecretsExcluded,
    bool ProductAuthorityGranted);

public sealed class NodalOsWorkspaceHandoffExecutionService
{
    public const int CurrentSchemaVersion = 1;
    public const string CapabilityId = "filesystem.write.safe";

    private const int MaximumMetadataBytes = 2 * 1024 * 1024;
    private const int MaximumContentBytes = 32 * 1024;
    private readonly string _metadataFilePath;
    private readonly string _restoreRootPath;
    private readonly NodalOsWorkspaceSelectionService _workspaceSelection;
    private readonly NodalOsWorkspaceMissionDraftService _missionDraft;
    private readonly ISecretReferenceStore _rootReferenceStore;
    private readonly NodalOsApprovalTimelineEvidenceValidator _approvalValidator;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public NodalOsWorkspaceHandoffExecutionService(
        string metadataFilePath,
        string restoreRootPath,
        NodalOsWorkspaceSelectionService workspaceSelection,
        NodalOsWorkspaceMissionDraftService missionDraft,
        ISecretReferenceStore rootReferenceStore,
        NodalOsApprovalTimelineEvidenceValidator? approvalValidator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(restoreRootPath);
        ArgumentNullException.ThrowIfNull(workspaceSelection);
        ArgumentNullException.ThrowIfNull(missionDraft);
        ArgumentNullException.ThrowIfNull(rootReferenceStore);

        _metadataFilePath = Path.GetFullPath(metadataFilePath);
        _restoreRootPath = Path.GetFullPath(restoreRootPath);
        _workspaceSelection = workspaceSelection;
        _missionDraft = missionDraft;
        _rootReferenceStore = rootReferenceStore;
        _approvalValidator = approvalValidator ?? new NodalOsApprovalTimelineEvidenceValidator();
    }

    public string MetadataFilePath => _metadataFilePath;

    public async ValueTask<NodalOsWorkspaceHandoffExecutionSnapshot> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_metadataFilePath))
            return await ReadyOrBlockedAsync(cancellationToken).ConfigureAwait(false);

        NodalOsPersistedWorkspaceHandoffExecution? document;
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
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_METADATA_CORRUPT",
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["Persisted handoff execution metadata is unavailable or invalid."],
                persisted: true);
        }

        if (document is null || !ValidateDocument(document))
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_METADATA_CORRUPT",
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["Persisted handoff execution metadata failed the canonical validation boundary."],
                persisted: true);
        }

        var workspace = await _workspaceSelection.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!workspace.Accepted || workspace.Workspace is null ||
            !string.Equals(workspace.WorkspaceId, document.WorkspaceId, StringComparison.Ordinal) ||
            !string.Equals(workspace.RootPathFingerprint, document.WorkspaceFingerprint, StringComparison.Ordinal))
        {
            return FromDocument(
                document,
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_WORKSPACE_CHANGED",
                accepted: false,
                NodalOsWorkspaceHandoffExecutionState.CandidateStale,
                ["The selected workspace changed after approval or execution."],
                rehydrated: true);
        }

        if (document.State == NodalOsWorkspaceHandoffExecutionState.ApprovedPendingExecution)
        {
            return FromDocument(
                document,
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_INTERRUPTED",
                accepted: false,
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["Execution was interrupted after approval. The operator must review and retry; automatic resume is disabled."],
                rehydrated: true);
        }

        var rootPath = await OpenWorkspaceRootAsync(cancellationToken).ConfigureAwait(false);
        if (rootPath is null)
        {
            return FromDocument(
                document,
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_ROOT_UNAVAILABLE",
                accepted: false,
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["The protected workspace root could not be reopened."],
                rehydrated: true);
        }

        var target = ResolveTarget(rootPath, document.Candidate.RelativeTargetPath);
        if (target is null)
        {
            return FromDocument(
                document,
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_TARGET_INVALID",
                accepted: false,
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["The persisted relative target no longer resolves inside the selected workspace."],
                rehydrated: true);
        }

        if (document.State == NodalOsWorkspaceHandoffExecutionState.Completed)
        {
            var currentHash = await HashRegularFileAsync(target, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(currentHash, document.ResultSha256, StringComparison.Ordinal))
            {
                return FromDocument(
                    document,
                    "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_RESULT_CHANGED",
                    accepted: false,
                    NodalOsWorkspaceHandoffExecutionState.ResultChanged,
                    ["The verified handoff result changed after execution. Rollback is disabled until a new review."],
                    rehydrated: true,
                    rollbackAvailableOverride: false);
            }
        }
        else if (document.State == NodalOsWorkspaceHandoffExecutionState.RolledBack)
        {
            var rollbackVerified = document.Candidate.Kind switch
            {
                NodalOsReviewedWorkspaceActionKind.CreateTextFile => !File.Exists(target),
                NodalOsReviewedWorkspaceActionKind.ExactHashUpdate =>
                    string.Equals(
                        await HashRegularFileAsync(target, cancellationToken).ConfigureAwait(false),
                        document.OriginalSha256,
                        StringComparison.Ordinal),
                _ => false
            };
            if (!rollbackVerified)
            {
                return FromDocument(
                    document,
                    "BLOCKED_WORKSPACE_HANDOFF_ROLLBACK_RESULT_CHANGED",
                    accepted: false,
                    NodalOsWorkspaceHandoffExecutionState.ResultChanged,
                    ["The workspace no longer matches the verified rollback state."],
                    rehydrated: true,
                    rollbackAvailableOverride: false);
            }
        }

        return FromDocument(
            document,
            document.State switch
            {
                NodalOsWorkspaceHandoffExecutionState.Completed => "GO_WORKSPACE_HANDOFF_EXECUTION_REHYDRATED",
                NodalOsWorkspaceHandoffExecutionState.RolledBack => "GO_WORKSPACE_HANDOFF_ROLLBACK_REHYDRATED",
                _ => "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_REHYDRATED"
            },
            accepted: document.State is NodalOsWorkspaceHandoffExecutionState.Completed or NodalOsWorkspaceHandoffExecutionState.RolledBack,
            document.State,
            document.FailureReasonRedacted is null ? [] : [document.FailureReasonRedacted],
            rehydrated: true);
    }

    public async ValueTask<NodalOsWorkspaceHandoffExecutionSnapshot> ApproveAndExecuteAsync(
        string decidedBy = "local operator",
        string reason = "Approved after reviewing the mission-scoped reversible handoff action.",
        CancellationToken cancellationToken = default)
    {
        var existing = File.Exists(_metadataFilePath)
            ? await TryReadDocumentAsync(cancellationToken).ConfigureAwait(false)
            : null;
        if (existing?.State == NodalOsWorkspaceHandoffExecutionState.Completed)
            return await GetCurrentAsync(cancellationToken).ConfigureAwait(false);

        var draft = await _missionDraft.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!draft.Accepted || draft.Candidate is null || draft.Plan is null || draft.Binding is null ||
            string.IsNullOrWhiteSpace(draft.MissionId) || string.IsNullOrWhiteSpace(draft.WorkspaceId) ||
            string.IsNullOrWhiteSpace(draft.WorkspaceFingerprint))
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_DRAFT_NOT_READY",
                NodalOsWorkspaceHandoffExecutionState.CandidateStale,
                draft.ReviewBlockers.DefaultIfEmpty("A fresh reviewed mission draft is required before approval." ).ToArray());
        }

        var missionDocument = await ReadMissionDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (missionDocument is null ||
            !string.Equals(missionDocument.Binding.MissionId, draft.MissionId, StringComparison.Ordinal) ||
            !string.Equals(missionDocument.WorkspaceFingerprint, draft.WorkspaceFingerprint, StringComparison.Ordinal) ||
            !SameCandidate(missionDocument.Candidate, draft.Candidate))
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_DRAFT_METADATA_INVALID",
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["Mission draft metadata could not be bound to the reviewed candidate."]);
        }

        var workspace = await _workspaceSelection.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!workspace.Accepted || workspace.Workspace is null ||
            !string.Equals(workspace.WorkspaceId, draft.WorkspaceId, StringComparison.Ordinal) ||
            !string.Equals(workspace.RootPathFingerprint, draft.WorkspaceFingerprint, StringComparison.Ordinal))
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_WORKSPACE_NOT_READY",
                NodalOsWorkspaceHandoffExecutionState.CandidateStale,
                workspace.ReviewBlockers.DefaultIfEmpty("The selected workspace is no longer ready." ).ToArray());
        }

        var rootPath = await OpenWorkspaceRootAsync(cancellationToken).ConfigureAwait(false);
        if (rootPath is null)
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_ROOT_UNAVAILABLE",
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["The protected workspace root could not be opened."]);
        }

        var candidate = missionDocument.Candidate;
        var freshPrecondition = await ValidateCandidatePreconditionAsync(rootPath, candidate, cancellationToken).ConfigureAwait(false);
        if (freshPrecondition is not null)
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_PRECONDITION_CHANGED",
                NodalOsWorkspaceHandoffExecutionState.CandidateStale,
                [freshPrecondition]);
        }

        var approvalFlow = CreateApprovalFlow(missionDocument, workspace.Workspace, decidedBy, reason);
        if (approvalFlow is null)
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_APPROVAL_INVALID",
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["The mission-scoped approval record failed validation."]);
        }

        var now = DateTimeOffset.UtcNow;
        var operationId = $"handoff-{Guid.NewGuid():N}";
        var preExecution = new NodalOsPersistedWorkspaceHandoffExecution(
            CurrentSchemaVersion,
            NodalOsWorkspaceHandoffExecutionState.ApprovedPendingExecution,
            operationId,
            missionDocument.Binding.MissionId,
            missionDocument.Binding.WorkspaceId,
            missionDocument.WorkspaceFingerprint,
            candidate,
            approvalFlow.Value.Approval,
            approvalFlow.Value.RegistryEntry,
            ResultSha256: null,
            OriginalSha256: candidate.ExistingSha256,
            BytesWritten: 0,
            Verified: false,
            RestorePlan: null,
            EvidenceRefs: approvalFlow.Value.EvidenceRefs,
            Timeline: approvalFlow.Value.Timeline,
            FailureReasonRedacted: null,
            CreatedAt: now,
            UpdatedAt: now,
            ExecutedAt: null,
            RolledBackAt: null);

        try
        {
            await WriteDocumentAsync(preExecution, rootPath, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_PREWRITE_PERSISTENCE_FAILED",
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["Approval was not activated because the pre-write execution record could not be persisted."]);
        }

        ControlledWriteResult write;
        try
        {
            write = await ExecuteControlledWriteAsync(
                    rootPath,
                    operationId,
                    candidate,
                    approvalFlow.Value.Approval.ApprovalDecision.DecisionId,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            var cancelled = await FailAndRollbackAsync(
                    preExecution,
                    rootPath,
                    "Execution was cancelled and any committed write was rolled back when possible.",
                    cancellationToken: CancellationToken.None)
                .ConfigureAwait(false);
            await BestEffortWriteDocumentAsync(cancelled, rootPath).ConfigureAwait(false);
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException or PlatformNotSupportedException)
        {
            write = ControlledWriteResult.Failed("The controlled handoff write failed closed at an IO boundary.");
        }

        if (!write.Success || !write.Verified || write.Evidence is null)
        {
            var failed = await FailAndRollbackAsync(
                    preExecution,
                    rootPath,
                    write.SafeMessage,
                    write,
                    CancellationToken.None)
                .ConfigureAwait(false);
            await BestEffortWriteDocumentAsync(failed, rootPath).ConfigureAwait(false);
            return FromDocument(
                failed,
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_FAILED_CLOSED",
                accepted: false,
                failed.State,
                [failed.FailureReasonRedacted ?? "Controlled handoff execution failed closed."],
                rehydrated: false);
        }

        var completedFlow = CompleteApprovalFlow(
            approvalFlow.Value,
            missionDocument.Plan,
            write.Evidence,
            write.RestorePlan);
        var completed = preExecution with
        {
            State = NodalOsWorkspaceHandoffExecutionState.Completed,
            RegistryEntry = completedFlow.RegistryEntry,
            ResultSha256 = write.ResultSha256,
            OriginalSha256 = write.OriginalSha256,
            BytesWritten = write.BytesWritten,
            Verified = true,
            RestorePlan = write.RestorePlan,
            EvidenceRefs = completedFlow.EvidenceRefs,
            Timeline = completedFlow.Timeline,
            UpdatedAt = DateTimeOffset.UtcNow,
            ExecutedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await WriteDocumentAsync(completed, rootPath, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            var rolledBack = await FailAndRollbackAsync(
                    completed,
                    rootPath,
                    "Final execution metadata persistence was cancelled; the write was rolled back.",
                    write,
                    CancellationToken.None)
                .ConfigureAwait(false);
            await BestEffortWriteDocumentAsync(rolledBack, rootPath).ConfigureAwait(false);
            throw;
        }
        catch
        {
            var rolledBack = await FailAndRollbackAsync(
                    completed,
                    rootPath,
                    "Final execution metadata persistence failed; the write was rolled back.",
                    write,
                    CancellationToken.None)
                .ConfigureAwait(false);
            await BestEffortWriteDocumentAsync(rolledBack, rootPath).ConfigureAwait(false);
            return FromDocument(
                rolledBack,
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_FINAL_PERSISTENCE_FAILED",
                accepted: false,
                rolledBack.State,
                [rolledBack.FailureReasonRedacted ?? "Final execution metadata persistence failed closed."],
                rehydrated: false);
        }

        return FromDocument(
            completed,
            "GO_WORKSPACE_HANDOFF_EXECUTED_AND_VERIFIED",
            accepted: true,
            NodalOsWorkspaceHandoffExecutionState.Completed,
            [],
            rehydrated: false,
            appConfigurationMutated: true);
    }

    public async ValueTask<NodalOsWorkspaceHandoffExecutionSnapshot> RollbackAsync(
        CancellationToken cancellationToken = default)
    {
        var document = await TryReadDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (document is null || document.State != NodalOsWorkspaceHandoffExecutionState.Completed ||
            document.RestorePlan is null || !document.Verified)
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_ROLLBACK_NOT_AVAILABLE",
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["A verified completed execution with a matching restore plan is required before rollback."],
                persisted: document is not null);
        }

        var rootPath = await OpenWorkspaceRootAsync(cancellationToken).ConfigureAwait(false);
        if (rootPath is null)
        {
            return FromDocument(
                document,
                "BLOCKED_WORKSPACE_HANDOFF_ROLLBACK_ROOT_UNAVAILABLE",
                accepted: false,
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["The protected workspace root could not be opened for rollback."],
                rehydrated: true,
                rollbackAvailableOverride: false);
        }

        var rollback = await PerformRollbackAsync(rootPath, document.RestorePlan, cancellationToken).ConfigureAwait(false);
        if (!rollback.Success || rollback.Evidence is null)
        {
            return FromDocument(
                document,
                "BLOCKED_WORKSPACE_HANDOFF_ROLLBACK_FAILED_CLOSED",
                accepted: false,
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                [rollback.SafeMessage],
                rehydrated: false,
                rollbackAvailableOverride: false);
        }

        var evidence = document.EvidenceRefs
            .Concat([rollback.Evidence])
            .DistinctBy(value => value.EvidenceId)
            .ToArray();
        var eventBus = new NodalOsCoreEventBus();
        foreach (var projection in document.Timeline)
            _ = projection;
        Publish(
            eventBus,
            NodalOsCoreEventKind.EvidenceAttached,
            document.RegistryEntry,
            document.MissionId,
            "verify-result",
            "The controlled handoff rollback completed and passed exact-state verification.",
            [rollback.Evidence],
            new Dictionary<string, string>
            {
                ["operationId"] = document.OperationId,
                ["restorePlanId"] = document.RestorePlan.RestorePlanId
            });
        var timeline = document.Timeline
            .Concat(eventBus.ToTimelineProjections())
            .GroupBy(value => value.EventId, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(value => value.CreatedAt)
            .ToArray();
        var rolledBack = document with
        {
            State = NodalOsWorkspaceHandoffExecutionState.RolledBack,
            EvidenceRefs = evidence,
            Timeline = timeline,
            UpdatedAt = DateTimeOffset.UtcNow,
            RolledBackAt = DateTimeOffset.UtcNow
        };

        await WriteDocumentAsync(rolledBack, rootPath, cancellationToken).ConfigureAwait(false);
        TryDeleteSnapshot(document.RestorePlan);
        return FromDocument(
            rolledBack,
            "GO_WORKSPACE_HANDOFF_ROLLED_BACK_AND_VERIFIED",
            accepted: true,
            NodalOsWorkspaceHandoffExecutionState.RolledBack,
            [],
            rehydrated: false,
            appConfigurationMutated: true,
            rollbackAvailableOverride: false);
    }

    public async ValueTask<NodalOsWorkspaceHandoffExecutionSnapshot> ClearAsync(
        CancellationToken cancellationToken = default)
    {
        var document = await TryReadDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (document?.State == NodalOsWorkspaceHandoffExecutionState.Completed)
        {
            return FromDocument(
                document,
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_CLEAR_REQUIRES_ROLLBACK",
                accepted: false,
                document.State,
                ["Rollback or retain the verified execution record before clearing it."],
                rehydrated: true);
        }

        try
        {
            if (document?.RestorePlan is not null)
                TryDeleteSnapshot(document.RestorePlan);
            if (File.Exists(_metadataFilePath))
                File.Delete(_metadataFilePath);
        }
        catch
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_CLEAR_FAILED",
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["Execution state could not be cleared safely."],
                persisted: true);
        }

        await Task.CompletedTask.ConfigureAwait(false);
        return new NodalOsWorkspaceHandoffExecutionSnapshot(
            "WORKSPACE_HANDOFF_EXECUTION_NOT_CONFIGURED",
            Accepted: true,
            NodalOsWorkspaceHandoffExecutionState.NotConfigured,
            OperationId: null,
            MissionId: null,
            WorkspaceId: null,
            WorkspaceFingerprint: null,
            ActionId: null,
            ActionKind: null,
            NodalOsWorkspaceMissionDraftService.RelativeTargetPath,
            ProposedSha256: null,
            ExpectedCurrentSha256: null,
            ResultSha256: null,
            OriginalSha256: null,
            BytesWritten: 0,
            ApprovalStatus: "not configured",
            ApprovalDecisionId: null,
            ApprovalScope: null,
            RegistryState: "not configured",
            Persisted: false,
            Rehydrated: false,
            Executed: false,
            Verified: false,
            RollbackAvailable: false,
            RolledBack: false,
            RestorePlanId: null,
            EvidenceRefs: [],
            Timeline: [],
            Blockers: [],
            ApprovedAt: null,
            ExecutedAt: null,
            RolledBackAt: null,
            RealFilesystemRead: false,
            WorkspaceFilesystemMutated: false,
            AppConfigurationMutated: true,
            NetworkUsed: false,
            ExternalProcessUsed: false,
            SecretsExcluded: true,
            ProductAuthorityGranted: false);
    }

    private async ValueTask<NodalOsWorkspaceHandoffExecutionSnapshot> ReadyOrBlockedAsync(
        CancellationToken cancellationToken)
    {
        var draft = await _missionDraft.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!draft.Accepted || draft.Candidate is null)
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_DRAFT_NOT_READY",
                NodalOsWorkspaceHandoffExecutionState.NotConfigured,
                draft.ReviewBlockers.DefaultIfEmpty("Create and review a mission draft before execution." ).ToArray());
        }

        return new NodalOsWorkspaceHandoffExecutionSnapshot(
            "GO_WORKSPACE_HANDOFF_READY_FOR_MISSION_SCOPE_APPROVAL",
            Accepted: true,
            NodalOsWorkspaceHandoffExecutionState.ReadyForApproval,
            OperationId: null,
            MissionId: draft.MissionId,
            WorkspaceId: draft.WorkspaceId,
            WorkspaceFingerprint: draft.WorkspaceFingerprint,
            ActionId: draft.Candidate.ActionId,
            ActionKind: draft.Candidate.Kind.ToString(),
            draft.Candidate.RelativeTargetPath,
            ProposedSha256: draft.Candidate.ProposedSha256,
            ExpectedCurrentSha256: draft.Candidate.ExistingSha256,
            ResultSha256: null,
            OriginalSha256: draft.Candidate.ExistingSha256,
            BytesWritten: 0,
            ApprovalStatus: "pending mission-scope decision",
            ApprovalDecisionId: null,
            ApprovalScope: draft.Candidate.RequiredApprovalScope,
            RegistryState: "not registered",
            Persisted: false,
            Rehydrated: false,
            Executed: false,
            Verified: false,
            RollbackAvailable: false,
            RolledBack: false,
            RestorePlanId: null,
            EvidenceRefs: draft.EvidenceRefs,
            Timeline: [],
            Blockers: draft.Candidate.BlockersRedacted,
            ApprovedAt: null,
            ExecutedAt: null,
            RolledBackAt: null,
            RealFilesystemRead: draft.RealFilesystemRead,
            WorkspaceFilesystemMutated: false,
            AppConfigurationMutated: false,
            NetworkUsed: false,
            ExternalProcessUsed: false,
            SecretsExcluded: true,
            ProductAuthorityGranted: false);
    }

    private ApprovalFlow? CreateApprovalFlow(
        NodalOsPersistedWorkspaceMissionDraft mission,
        NodalOsWorkspaceLocalModel workspace,
        string decidedBy,
        string reason)
    {
        var candidate = mission.Candidate;
        var scopeCanonical = string.Join(
            "|",
            mission.Binding.MissionId,
            mission.Binding.WorkspaceId,
            mission.WorkspaceFingerprint,
            candidate.ActionId,
            CapabilityId,
            candidate.RelativeTargetPath,
            candidate.ProposedSha256,
            candidate.ExistingSha256 ?? "target-absent");
        var scopeHash = Sha256(Encoding.UTF8.GetBytes(scopeCanonical));
        var approvalEvidence = Evidence(
            $"evidence:mission-scope-approval:{scopeHash}",
            "mission-scope-approval-binding",
            scopeHash,
            $"mission-scope:{mission.Binding.MissionId}:{candidate.ActionId}",
            "Explicit operator approval bound to mission, workspace fingerprint, action, capability, target and reviewed hashes.");

        var registry = new NodalOsExecutionRegistry();
        var eventBus = new NodalOsCoreEventBus();
        var request = new NodalOsExecutionRequest
        {
            RequestId = $"request-{candidate.ActionId}",
            MissionId = mission.Binding.MissionId,
            TaskId = "execute-controlled-action",
            RunId = $"run-{candidate.ActionId}",
            RequestedBy = SafeRuntimeText.Sanitize(decidedBy, 80),
            ActorKind = NodalOsExecutionActorKind.HumanOperator,
            SourceKind = NodalOsExecutionSourceKind.MissionControl,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            Summary = "Execute one reviewed reversible handoff action inside the selected workspace.",
            EvidenceRefs = workspace.EvidenceRefs.Concat([approvalEvidence]).DistinctBy(value => value.EvidenceId).ToArray(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        var entry = registry.Register(request);
        Publish(eventBus, NodalOsCoreEventKind.ExecutionRequestRegistered, entry, mission.Binding.MissionId, "execute-controlled-action", "Mission-scoped handoff execution request registered.");

        entry = registry.Transition(
            entry.RegistryEntryId,
            NodalOsExecutionRegistryState.PolicyEvaluated,
            "workspace-handoff-policy",
            "The operation is restricted to one reviewed relative target, exact hashes, an atomic safe writer and no shell or network.",
            policyDecisionRef: $"policy:workspace-handoff:{scopeHash}");
        Publish(eventBus, NodalOsCoreEventKind.PolicyGateEvaluated, entry, mission.Binding.MissionId, "execute-controlled-action", "Controlled handoff policy and candidate scope passed fresh validation.");

        entry = registry.Transition(
            entry.RegistryEntryId,
            NodalOsExecutionRegistryState.ApprovalRequired,
            "approval-center",
            "One mission-scope operator decision is required for the reviewed reversible action.");
        var approvalEvent = Publish(
            eventBus,
            NodalOsCoreEventKind.ApprovalRequired,
            entry,
            mission.Binding.MissionId,
            "resolve-mission-scope-approval",
            "Mission-scope approval is required once for the reviewed handoff target.",
            [approvalEvidence]);

        var approvalService = new NodalOsApprovalCenterService();
        var card = approvalService.CreateApprovalCard(
            entry,
            approvalEvent,
            candidate.RiskLevel == MissionRiskLevel.Low ? NodalOsApprovalSeverity.Low : NodalOsApprovalSeverity.Medium,
            NodalOsApprovalActionKind.ExternalMutationFuture,
            $"{candidate.Kind} one reviewed handoff document in the selected local workspace.",
            "The target, workspace fingerprint, proposed hash and current target precondition are fixed; shell, network and other paths are denied.",
            [candidate.RelativeTargetPath]) with
        {
            RollbackPlanRedacted = candidate.RollbackPlanRedacted,
            EvidencePlanRedacted = "Record approval scope, precondition, post-write SHA-256, verification and rollback readiness before completion.",
            EvidenceRefs = entry.EvidenceRefs.Concat([approvalEvidence]).DistinctBy(value => value.EvidenceId).ToArray()
        };
        if (!_approvalValidator.ValidateApprovalCard(card).IsValid)
            return null;

        var decision = approvalService.CreateDecision(
            card,
            NodalOsApprovalDecisionKind.Approve,
            SafeRuntimeText.Sanitize(decidedBy, 80),
            SafeRuntimeText.Sanitize(reason, 240));
        if (!_approvalValidator.ValidateApprovalDecision(decision).IsValid)
            return null;

        var approval = new NodalOsMissionScopeApprovalBinding(
            ScopeId: candidate.RequiredApprovalScope,
            MissionId: mission.Binding.MissionId,
            WorkspaceId: mission.Binding.WorkspaceId,
            WorkspaceFingerprint: mission.WorkspaceFingerprint,
            ActionId: candidate.ActionId,
            CapabilityId,
            candidate.RelativeTargetPath,
            candidate.ProposedSha256,
            candidate.ExistingSha256,
            card,
            decision,
            ApprovedAt: DateTimeOffset.UtcNow,
            OneShot: true,
            DecisionIsExecutionAuthority: false);
        if (!ValidateApprovalBinding(approval, mission))
            return null;

        entry = registry.Transition(
            entry.RegistryEntryId,
            NodalOsExecutionRegistryState.Approved,
            SafeRuntimeText.Sanitize(decidedBy, 80),
            "The operator approved the exact mission, workspace, action, capability, target and reviewed hashes.",
            approvalRef: decision.DecisionId,
            evidenceRefs: [approvalEvidence]);
        Publish(
            eventBus,
            NodalOsCoreEventKind.ApprovalGranted,
            entry,
            mission.Binding.MissionId,
            "resolve-mission-scope-approval",
            "Mission-scope approval granted for the exact reviewed handoff action.",
            [approvalEvidence],
            new Dictionary<string, string>
            {
                ["approvalCardId"] = card.ApprovalCardId,
                ["approvalDecisionId"] = decision.DecisionId,
                ["scopeHash"] = scopeHash
            });

        entry = registry.Transition(
            entry.RegistryEntryId,
            NodalOsExecutionRegistryState.DryRunPlanned,
            "workspace-handoff-executor",
            "Atomic write, exact precondition, post-write verification and guarded rollback were prepared.",
            dryRunRef: $"dry-run:{candidate.ActionId}:{candidate.ProposedSha256}");
        Publish(eventBus, NodalOsCoreEventKind.DryRunPlanCreated, entry, mission.Binding.MissionId, "execute-controlled-action", "Controlled handoff write and rollback plan prepared before mutation.");

        return new ApprovalFlow(
            approval,
            entry,
            eventBus.ToTimelineProjections(),
            entry.EvidenceRefs.Concat([approvalEvidence]).DistinctBy(value => value.EvidenceId).ToArray());
    }

    private static bool ValidateApprovalBinding(
        NodalOsMissionScopeApprovalBinding approval,
        NodalOsPersistedWorkspaceMissionDraft mission) =>
        approval.OneShot &&
        !approval.DecisionIsExecutionAuthority &&
        approval.ApprovalDecision.DecisionKind == NodalOsApprovalDecisionKind.Approve &&
        !approval.ApprovalDecision.RuntimeExecutionAllowed &&
        !approval.ApprovalDecision.CanAuthorizeExecution &&
        string.Equals(approval.ApprovalCard.ApprovalCardId, approval.ApprovalDecision.ApprovalCardId, StringComparison.Ordinal) &&
        string.Equals(approval.ScopeId, mission.Candidate.RequiredApprovalScope, StringComparison.Ordinal) &&
        string.Equals(approval.MissionId, mission.Binding.MissionId, StringComparison.Ordinal) &&
        string.Equals(approval.WorkspaceId, mission.Binding.WorkspaceId, StringComparison.Ordinal) &&
        string.Equals(approval.WorkspaceFingerprint, mission.WorkspaceFingerprint, StringComparison.Ordinal) &&
        string.Equals(approval.ActionId, mission.Candidate.ActionId, StringComparison.Ordinal) &&
        string.Equals(approval.CapabilityId, CapabilityId, StringComparison.Ordinal) &&
        string.Equals(approval.RelativeTargetPath, mission.Candidate.RelativeTargetPath, StringComparison.Ordinal) &&
        string.Equals(approval.ProposedSha256, mission.Candidate.ProposedSha256, StringComparison.Ordinal) &&
        string.Equals(approval.ExpectedCurrentSha256, mission.Candidate.ExistingSha256, StringComparison.Ordinal);

    private static ApprovalFlow CompleteApprovalFlow(
        ApprovalFlow flow,
        MissionPlan plan,
        NodalOsEvidenceBridgeRef writeEvidence,
        NodalOsWorkspaceHandoffRestorePlan? restorePlan)
    {
        var registry = new NodalOsExecutionRegistry();
        var request = new NodalOsExecutionRequest
        {
            RequestId = flow.RegistryEntry.RequestId,
            MissionId = flow.Approval.MissionId,
            TaskId = "execute-controlled-action",
            RunId = $"run-{flow.Approval.ActionId}",
            RequestedBy = "local operator",
            ActorKind = NodalOsExecutionActorKind.HumanOperator,
            SourceKind = NodalOsExecutionSourceKind.MissionControl,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            Summary = "Execute one reviewed reversible handoff action inside the selected workspace.",
            EvidenceRefs = flow.EvidenceRefs,
            CreatedAt = flow.RegistryEntry.CreatedAt
        };
        var entry = registry.Register(request);
        entry = registry.Transition(entry.RegistryEntryId, NodalOsExecutionRegistryState.PolicyEvaluated, "workspace-handoff-policy", "Policy passed.", policyDecisionRef: flow.RegistryEntry.PolicyDecisionRef);
        entry = registry.Transition(entry.RegistryEntryId, NodalOsExecutionRegistryState.ApprovalRequired, "approval-center", "Approval required.");
        entry = registry.Transition(entry.RegistryEntryId, NodalOsExecutionRegistryState.Approved, "local operator", "Exact scope approved.", approvalRef: flow.Approval.ApprovalDecision.DecisionId);
        entry = registry.Transition(entry.RegistryEntryId, NodalOsExecutionRegistryState.DryRunPlanned, "workspace-handoff-executor", "Controlled write prepared.", dryRunRef: flow.RegistryEntry.DryRunRef);
        entry = registry.Transition(
            entry.RegistryEntryId,
            NodalOsExecutionRegistryState.Completed,
            "workspace-handoff-executor",
            "The handoff action completed and passed exact post-write verification.",
            verificationReportRef: $"verification:{writeEvidence.Hash}",
            snapshotRefs: restorePlan?.SnapshotId is null ? [] : [$"snapshot:{restorePlan.SnapshotId}"],
            evidenceRefs: [writeEvidence]);

        var eventBus = new NodalOsCoreEventBus();
        Publish(eventBus, NodalOsCoreEventKind.ExecutionCompleted, entry, plan.MissionId, "execute-controlled-action", "The approved handoff action completed and passed deterministic verification.", [writeEvidence]);
        Publish(eventBus, NodalOsCoreEventKind.EvidenceAttached, entry, plan.MissionId, "record-evidence-handoff", "Post-write SHA-256, approval scope and rollback readiness were attached before completion.", [writeEvidence]);
        var timeline = flow.Timeline
            .Concat(eventBus.ToTimelineProjections())
            .GroupBy(value => value.EventId, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(value => value.CreatedAt)
            .ToArray();
        var evidence = flow.EvidenceRefs
            .Concat([writeEvidence])
            .DistinctBy(value => value.EvidenceId)
            .ToArray();
        return flow with { RegistryEntry = entry, Timeline = timeline, EvidenceRefs = evidence };
    }

    private async ValueTask<ControlledWriteResult> ExecuteControlledWriteAsync(
        string rootPath,
        string operationId,
        NodalOsReviewedWorkspaceActionCandidate candidate,
        string approvalDecisionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var target = ResolveTarget(rootPath, candidate.RelativeTargetPath);
        if (target is null)
            return ControlledWriteResult.Failed("The reviewed relative target no longer resolves inside the workspace.");
        if (IsReparsePoint(rootPath) || IsReparsePoint(Path.GetDirectoryName(target)!))
            return ControlledWriteResult.Failed("The workspace or target directory contains a reparse point.");

        var proposedBytes = Encoding.UTF8.GetBytes(candidate.ProposedContentRedacted);
        try
        {
            if (proposedBytes.Length is <= 0 or > MaximumContentBytes ||
                !string.Equals(Sha256(proposedBytes), candidate.ProposedSha256, StringComparison.Ordinal) ||
                HistorySanitizer.ContainsSecretLikeContent(candidate.ProposedContentRedacted))
            {
                return ControlledWriteResult.Failed("The proposed content failed size, hash or redaction validation.");
            }

            return candidate.Kind switch
            {
                NodalOsReviewedWorkspaceActionKind.CreateTextFile => await CreateAsync(
                    rootPath,
                    target,
                    operationId,
                    approvalDecisionId,
                    candidate,
                    proposedBytes,
                    cancellationToken).ConfigureAwait(false),
                NodalOsReviewedWorkspaceActionKind.ExactHashUpdate => await UpdateAsync(
                    rootPath,
                    target,
                    operationId,
                    approvalDecisionId,
                    candidate,
                    proposedBytes,
                    cancellationToken).ConfigureAwait(false),
                _ => ControlledWriteResult.Failed("The reviewed action kind is unsupported.")
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(proposedBytes);
        }
    }

    private async ValueTask<ControlledWriteResult> CreateAsync(
        string rootPath,
        string target,
        string operationId,
        string approvalDecisionId,
        NodalOsReviewedWorkspaceActionCandidate candidate,
        byte[] proposedBytes,
        CancellationToken cancellationToken)
    {
        if (File.Exists(target) || Directory.Exists(target))
            return ControlledWriteResult.Failed("The create-only target now exists; the reviewed precondition is stale.");

        var temp = Path.Combine(rootPath, $".{Path.GetFileName(target)}.{operationId}.tmp");
        if (File.Exists(temp) || Directory.Exists(temp))
            return ControlledWriteResult.Failed("A temporary path for this operation already exists.");
        var committed = false;
        try
        {
            await WriteNewFileAsync(temp, proposedBytes, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temp, target, overwrite: false);
            committed = true;
            var persisted = await File.ReadAllBytesAsync(target, cancellationToken).ConfigureAwait(false);
            var hash = Sha256(persisted);
            if (!persisted.AsSpan().SequenceEqual(proposedBytes) ||
                !string.Equals(hash, candidate.ProposedSha256, StringComparison.Ordinal))
            {
                TryDeleteExact(target, hash);
                return ControlledWriteResult.Failed("The created handoff did not match the reviewed bytes and was removed when possible.");
            }

            var plan = new NodalOsWorkspaceHandoffRestorePlan(
                RestorePlanId: $"restore-{operationId}",
                OperationId: operationId,
                ApprovalDecisionId: approvalDecisionId,
                WorkspaceFingerprint: Fingerprint(rootPath),
                ActionId: candidate.ActionId,
                candidate.Kind,
                candidate.RelativeTargetPath,
                ResultSha256: hash,
                OriginalSha256: null,
                SnapshotId: null,
                DeleteCreatedFile: true,
                SnapshotStoredOutsideWorkspace: true,
                RequiresExactCurrentHash: true,
                CreatedAt: DateTimeOffset.UtcNow);
            var evidence = Evidence(
                $"evidence:workspace-handoff-create:{operationId}",
                "real-workspace-handoff-create-verification",
                hash,
                $"workspace-handoff:{operationId}:create",
                "Approved create-only handoff write completed atomically and matched the reviewed SHA-256.");
            return new ControlledWriteResult(true, true, hash, null, persisted.LongLength, plan, evidence, "The handoff was created atomically and verified.", committed);
        }
        catch (OperationCanceledException)
        {
            if (committed)
                TryDeleteExact(target, candidate.ProposedSha256);
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            if (committed)
                TryDeleteExact(target, candidate.ProposedSha256);
            return ControlledWriteResult.Failed("The create-only handoff write failed closed.", committed);
        }
        finally
        {
            if (File.Exists(temp))
                TryDelete(temp);
        }
    }

    private async ValueTask<ControlledWriteResult> UpdateAsync(
        string rootPath,
        string target,
        string operationId,
        string approvalDecisionId,
        NodalOsReviewedWorkspaceActionCandidate candidate,
        byte[] proposedBytes,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(target) || Directory.Exists(target) || IsReparsePoint(target) ||
            string.IsNullOrWhiteSpace(candidate.ExistingSha256))
        {
            return ControlledWriteResult.Failed("The exact-hash update target is missing, invalid or no longer a regular file.");
        }

        var original = await File.ReadAllBytesAsync(target, cancellationToken).ConfigureAwait(false);
        var originalHash = Sha256(original);
        if (!string.Equals(originalHash, candidate.ExistingSha256, StringComparison.Ordinal))
            return ControlledWriteResult.Failed("The exact current SHA-256 precondition changed after review.");
        if (original.AsSpan().SequenceEqual(proposedBytes))
            return ControlledWriteResult.Failed("The proposed handoff is identical to the existing target.");

        var snapshotId = $"snapshot-{operationId}";
        var snapshotPath = ResolveSnapshotPath(snapshotId);
        if (snapshotPath is null)
            return ControlledWriteResult.Failed("The app-local restore snapshot boundary is unavailable.");
        var snapshotDirectory = Path.GetDirectoryName(snapshotPath)!;
        Directory.CreateDirectory(snapshotDirectory);
        if (IsReparsePoint(_restoreRootPath) || IsReparsePoint(snapshotDirectory))
            return ControlledWriteResult.Failed("The app-local restore snapshot boundary contains a reparse point.");
        if (File.Exists(snapshotPath) || Directory.Exists(snapshotPath))
            return ControlledWriteResult.Failed("A restore snapshot for this operation already exists.");

        await WriteNewFileAsync(snapshotPath, original, cancellationToken).ConfigureAwait(false);
        var snapshotBytes = await File.ReadAllBytesAsync(snapshotPath, cancellationToken).ConfigureAwait(false);
        if (!snapshotBytes.AsSpan().SequenceEqual(original) ||
            !string.Equals(Sha256(snapshotBytes), originalHash, StringComparison.Ordinal))
        {
            TryDelete(snapshotPath);
            return ControlledWriteResult.Failed("The pre-write restore snapshot failed verification.");
        }

        var temp = Path.Combine(rootPath, $".{Path.GetFileName(target)}.{operationId}.tmp");
        var committed = false;
        try
        {
            await WriteNewFileAsync(temp, proposedBytes, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Replace(temp, target, destinationBackupFileName: null, ignoreMetadataErrors: true);
            committed = true;
            var persisted = await File.ReadAllBytesAsync(target, cancellationToken).ConfigureAwait(false);
            var resultHash = Sha256(persisted);
            if (!persisted.AsSpan().SequenceEqual(proposedBytes) ||
                !string.Equals(resultHash, candidate.ProposedSha256, StringComparison.Ordinal))
            {
                var emergency = await RestoreUpdateAsync(target, snapshotPath, originalHash, resultHash, cancellationToken).ConfigureAwait(false);
                if (emergency)
                    TryDelete(snapshotPath);
                return ControlledWriteResult.Failed(
                    emergency
                        ? "The update failed verification and the original snapshot was restored."
                        : "The update failed verification and guarded rollback also failed.",
                    committed);
            }

            var plan = new NodalOsWorkspaceHandoffRestorePlan(
                RestorePlanId: $"restore-{operationId}",
                OperationId: operationId,
                ApprovalDecisionId: approvalDecisionId,
                WorkspaceFingerprint: Fingerprint(rootPath),
                ActionId: candidate.ActionId,
                candidate.Kind,
                candidate.RelativeTargetPath,
                ResultSha256: resultHash,
                OriginalSha256: originalHash,
                SnapshotId: snapshotId,
                DeleteCreatedFile: false,
                SnapshotStoredOutsideWorkspace: true,
                RequiresExactCurrentHash: true,
                CreatedAt: DateTimeOffset.UtcNow);
            var evidence = Evidence(
                $"evidence:workspace-handoff-update:{operationId}",
                "real-workspace-handoff-update-verification",
                resultHash,
                $"workspace-handoff:{operationId}:update:before:{originalHash}",
                "Approved exact-hash handoff replacement used an app-local verified snapshot, atomic replace and post-write SHA-256 verification.");
            return new ControlledWriteResult(true, true, resultHash, originalHash, persisted.LongLength, plan, evidence, "The handoff was replaced atomically and verified.", committed);
        }
        catch (OperationCanceledException)
        {
            if (committed)
                await RestoreUpdateAsync(target, snapshotPath, originalHash, candidate.ProposedSha256, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException or PlatformNotSupportedException)
        {
            if (committed)
                await RestoreUpdateAsync(target, snapshotPath, originalHash, candidate.ProposedSha256, CancellationToken.None).ConfigureAwait(false);
            return ControlledWriteResult.Failed("The exact-hash replacement failed closed.", committed);
        }
        finally
        {
            if (File.Exists(temp))
                TryDelete(temp);
        }
    }

    private async ValueTask<string?> ValidateCandidatePreconditionAsync(
        string rootPath,
        NodalOsReviewedWorkspaceActionCandidate candidate,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(candidate.RelativeTargetPath, NodalOsWorkspaceMissionDraftService.RelativeTargetPath, StringComparison.Ordinal) ||
            candidate.State != NodalOsReviewedWorkspaceActionState.ReadyForReview ||
            candidate.ExecutionEnabled ||
            !candidate.ApprovalRequired ||
            !candidate.Reversible ||
            !IsSha256(candidate.ProposedSha256))
        {
            return "The reviewed candidate no longer matches the allowlisted executable shape.";
        }

        var target = ResolveTarget(rootPath, candidate.RelativeTargetPath);
        if (target is null || IsReparsePoint(rootPath) || IsReparsePoint(Path.GetDirectoryName(target)!))
            return "The reviewed target no longer resolves inside a non-reparse workspace boundary.";

        if (candidate.Kind == NodalOsReviewedWorkspaceActionKind.CreateTextFile)
            return File.Exists(target) || Directory.Exists(target) ? "The create-only target now exists." : null;
        if (candidate.Kind != NodalOsReviewedWorkspaceActionKind.ExactHashUpdate ||
            !File.Exists(target) || Directory.Exists(target) || IsReparsePoint(target) ||
            string.IsNullOrWhiteSpace(candidate.ExistingSha256))
        {
            return "The exact-hash update target is missing or invalid.";
        }

        var hash = await HashRegularFileAsync(target, cancellationToken).ConfigureAwait(false);
        return string.Equals(hash, candidate.ExistingSha256, StringComparison.Ordinal)
            ? null
            : "The exact current target SHA-256 changed after review.";
    }

    private async ValueTask<NodalOsPersistedWorkspaceHandoffExecution> FailAndRollbackAsync(
        NodalOsPersistedWorkspaceHandoffExecution document,
        string rootPath,
        string reason,
        ControlledWriteResult? write = null,
        CancellationToken cancellationToken = default)
    {
        var safeReason = SafeRuntimeText.Sanitize(reason, 240);
        var restorePlan = write?.RestorePlan ?? document.RestorePlan;
        var rollback = restorePlan is null
            ? null
            : await PerformRollbackAsync(rootPath, restorePlan, cancellationToken).ConfigureAwait(false);
        var state = rollback?.Success == true
            ? NodalOsWorkspaceHandoffExecutionState.RolledBack
            : NodalOsWorkspaceHandoffExecutionState.FailedClosed;
        var evidence = document.EvidenceRefs
            .Concat(write?.Evidence is null ? [] : [write.Evidence])
            .Concat(rollback?.Evidence is null ? [] : [rollback.Evidence])
            .DistinctBy(value => value.EvidenceId)
            .ToArray();
        return document with
        {
            State = state,
            ResultSha256 = write?.ResultSha256 ?? document.ResultSha256,
            OriginalSha256 = write?.OriginalSha256 ?? document.OriginalSha256,
            BytesWritten = write?.BytesWritten ?? document.BytesWritten,
            Verified = false,
            RestorePlan = restorePlan,
            EvidenceRefs = evidence,
            FailureReasonRedacted = safeReason,
            UpdatedAt = DateTimeOffset.UtcNow,
            RolledBackAt = rollback?.Success == true ? DateTimeOffset.UtcNow : null
        };
    }

    private async ValueTask<RollbackResult> PerformRollbackAsync(
        string rootPath,
        NodalOsWorkspaceHandoffRestorePlan plan,
        CancellationToken cancellationToken)
    {
        var target = ResolveTarget(rootPath, plan.RelativeTargetPath);
        if (target is null ||
            !string.Equals(plan.WorkspaceFingerprint, Fingerprint(rootPath), StringComparison.Ordinal) ||
            !plan.RequiresExactCurrentHash ||
            !string.Equals(plan.RelativeTargetPath, NodalOsWorkspaceMissionDraftService.RelativeTargetPath, StringComparison.Ordinal))
        {
            return RollbackResult.Failed("The restore plan no longer matches the workspace or allowlisted target.");
        }

        var currentHash = await HashRegularFileAsync(target, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(currentHash, plan.ResultSha256, StringComparison.Ordinal))
            return RollbackResult.Failed("The exact current result hash changed; guarded rollback was not attempted.");

        if (plan.DeleteCreatedFile)
        {
            try
            {
                File.Delete(target);
                if (File.Exists(target))
                    return RollbackResult.Failed("The created handoff could not be removed during rollback.");
                var evidence = Evidence(
                    $"evidence:workspace-handoff-rollback:{plan.OperationId}",
                    "real-workspace-handoff-create-rollback-verification",
                    plan.ResultSha256,
                    $"workspace-handoff:{plan.OperationId}:rollback:create",
                    "The created handoff was removed only after its exact result hash and restore-plan identity matched.");
                return new RollbackResult(true, evidence, "The create-only handoff was removed and absence was verified.");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
            {
                return RollbackResult.Failed("The create-only rollback failed closed.");
            }
        }

        if (string.IsNullOrWhiteSpace(plan.SnapshotId) || string.IsNullOrWhiteSpace(plan.OriginalSha256))
            return RollbackResult.Failed("The exact-hash update restore plan is incomplete.");
        var snapshot = ResolveSnapshotPath(plan.SnapshotId);
        if (snapshot is null || !File.Exists(snapshot) || IsReparsePoint(snapshot))
            return RollbackResult.Failed("The app-local restore snapshot is unavailable.");
        var snapshotHash = await HashRegularFileAsync(snapshot, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(snapshotHash, plan.OriginalSha256, StringComparison.Ordinal))
            return RollbackResult.Failed("The app-local restore snapshot failed exact-hash validation.");

        var restored = await RestoreUpdateAsync(target, snapshot, plan.OriginalSha256, plan.ResultSha256, cancellationToken).ConfigureAwait(false);
        if (!restored)
            return RollbackResult.Failed("The exact-hash update rollback failed deterministic verification.");
        var rollbackEvidence = Evidence(
            $"evidence:workspace-handoff-rollback:{plan.OperationId}",
            "real-workspace-handoff-update-rollback-verification",
            plan.OriginalSha256,
            $"workspace-handoff:{plan.OperationId}:rollback:update",
            "The original handoff snapshot was restored only after operation identity, current result hash and snapshot hash matched.");
        return new RollbackResult(true, rollbackEvidence, "The original handoff snapshot was restored and verified.");
    }

    private static async ValueTask<bool> RestoreUpdateAsync(
        string target,
        string snapshot,
        string originalHash,
        string expectedCurrentHash,
        CancellationToken cancellationToken)
    {
        var currentHash = await HashRegularFileAsync(target, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(currentHash, expectedCurrentHash, StringComparison.Ordinal))
            return false;
        var snapshotBytes = await File.ReadAllBytesAsync(snapshot, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(Sha256(snapshotBytes), originalHash, StringComparison.Ordinal))
            return false;
        var temp = Path.Combine(Path.GetDirectoryName(target)!, $".{Path.GetFileName(target)}.{Guid.NewGuid():N}.restore.tmp");
        try
        {
            await WriteNewFileAsync(temp, snapshotBytes, cancellationToken).ConfigureAwait(false);
            File.Replace(temp, target, destinationBackupFileName: null, ignoreMetadataErrors: true);
            var restored = await File.ReadAllBytesAsync(target, cancellationToken).ConfigureAwait(false);
            return restored.AsSpan().SequenceEqual(snapshotBytes) &&
                   string.Equals(Sha256(restored), originalHash, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(snapshotBytes);
            if (File.Exists(temp))
                TryDelete(temp);
        }
    }

    private async ValueTask<string?> OpenWorkspaceRootAsync(CancellationToken cancellationToken)
    {
        var selection = await ReadSelectionDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (selection is null)
            return null;
        SecretLease? lease;
        try
        {
            lease = await _rootReferenceStore.OpenAsync(selection.RootPathReference, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
        using (lease)
        {
            if (lease is null)
                return null;
            try
            {
                var path = Encoding.UTF8.GetString(lease.Bytes.Span);
                var root = Path.GetFullPath(path);
                return Directory.Exists(root) && !IsReparsePoint(root) ? root : null;
            }
            catch
            {
                return null;
            }
        }
    }

    private async ValueTask<NodalOsPersistedWorkspaceSelection?> ReadSelectionDocumentAsync(CancellationToken cancellationToken)
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

    private async ValueTask<NodalOsPersistedWorkspaceMissionDraft?> ReadMissionDocumentAsync(CancellationToken cancellationToken)
    {
        try
        {
            var path = _missionDraft.MetadataFilePath;
            var info = new FileInfo(path);
            if (!info.Exists || info.Length is <= 0 or > MaximumMetadataBytes)
                return null;
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<NodalOsPersistedWorkspaceMissionDraft>(json, JsonOptions);
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

    private async ValueTask<NodalOsPersistedWorkspaceHandoffExecution?> TryReadDocumentAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_metadataFilePath))
            return null;
        try
        {
            return await ReadDocumentAsync(cancellationToken).ConfigureAwait(false);
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

    private async ValueTask<NodalOsPersistedWorkspaceHandoffExecution?> ReadDocumentAsync(CancellationToken cancellationToken)
    {
        var info = new FileInfo(_metadataFilePath);
        if (!info.Exists || info.Length is <= 0 or > MaximumMetadataBytes)
            throw new InvalidDataException("Handoff execution metadata size is invalid.");
        var json = await File.ReadAllTextAsync(_metadataFilePath, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<NodalOsPersistedWorkspaceHandoffExecution>(json, JsonOptions);
    }

    private async ValueTask WriteDocumentAsync(
        NodalOsPersistedWorkspaceHandoffExecution document,
        string rootPath,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(document, JsonOptions);
        if (json.Length > MaximumMetadataBytes ||
            json.Contains(rootPath, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) ||
            HistorySanitizer.ContainsSecretLikeContent(json))
        {
            throw new InvalidDataException("Handoff execution metadata crossed a path or secret boundary.");
        }

        var directory = Path.GetDirectoryName(_metadataFilePath)
            ?? throw new InvalidOperationException("Handoff execution metadata directory is unavailable.");
        Directory.CreateDirectory(directory);
        var temp = $"{_metadataFilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(temp, json, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
            File.Move(temp, _metadataFilePath, overwrite: true);
            TryHide(_metadataFilePath);
        }
        finally
        {
            if (File.Exists(temp))
                TryDelete(temp);
        }
    }

    private async ValueTask BestEffortWriteDocumentAsync(
        NodalOsPersistedWorkspaceHandoffExecution document,
        string rootPath)
    {
        try
        {
            await WriteDocumentAsync(document, rootPath, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private static bool ValidateDocument(NodalOsPersistedWorkspaceHandoffExecution document) =>
        document.SchemaVersion == CurrentSchemaVersion &&
        !string.IsNullOrWhiteSpace(document.OperationId) &&
        !string.IsNullOrWhiteSpace(document.MissionId) &&
        !string.IsNullOrWhiteSpace(document.WorkspaceId) &&
        IsSha256(document.WorkspaceFingerprint) &&
        !string.IsNullOrWhiteSpace(document.Candidate.ActionId) &&
        string.Equals(document.Candidate.RelativeTargetPath, NodalOsWorkspaceMissionDraftService.RelativeTargetPath, StringComparison.Ordinal) &&
        IsSha256(document.Candidate.ProposedSha256) &&
        string.Equals(document.Approval.MissionId, document.MissionId, StringComparison.Ordinal) &&
        string.Equals(document.Approval.WorkspaceId, document.WorkspaceId, StringComparison.Ordinal) &&
        string.Equals(document.Approval.WorkspaceFingerprint, document.WorkspaceFingerprint, StringComparison.Ordinal) &&
        string.Equals(document.Approval.ActionId, document.Candidate.ActionId, StringComparison.Ordinal) &&
        string.Equals(document.Approval.ProposedSha256, document.Candidate.ProposedSha256, StringComparison.Ordinal) &&
        document.Approval.ApprovalDecision.DecisionKind == NodalOsApprovalDecisionKind.Approve &&
        !document.Approval.DecisionIsExecutionAuthority &&
        !document.Approval.ApprovalDecision.RuntimeExecutionAllowed &&
        !document.Approval.ApprovalDecision.CanAuthorizeExecution &&
        document.CreatedAt != default &&
        document.UpdatedAt != default;

    private static bool SameCandidate(
        NodalOsReviewedWorkspaceActionCandidate left,
        NodalOsReviewedWorkspaceActionCandidate right) =>
        string.Equals(left.ActionId, right.ActionId, StringComparison.Ordinal) &&
        left.Kind == right.Kind &&
        left.State == right.State &&
        string.Equals(left.RelativeTargetPath, right.RelativeTargetPath, StringComparison.Ordinal) &&
        string.Equals(left.ProposedSha256, right.ProposedSha256, StringComparison.Ordinal) &&
        string.Equals(left.ExistingSha256, right.ExistingSha256, StringComparison.Ordinal) &&
        left.ApprovalRequired == right.ApprovalRequired &&
        left.Reversible == right.Reversible &&
        !right.ExecutionEnabled;

    private static ApprovalFlow PublishCompletionFailure(
        ApprovalFlow flow,
        string reason)
    {
        var eventBus = new NodalOsCoreEventBus();
        Publish(eventBus, NodalOsCoreEventKind.ExecutionFailed, flow.RegistryEntry, flow.Approval.MissionId, "execute-controlled-action", reason);
        return flow with
        {
            Timeline = flow.Timeline.Concat(eventBus.ToTimelineProjections()).OrderBy(value => value.CreatedAt).ToArray()
        };
    }

    private static NodalOsCoreEvent Publish(
        NodalOsCoreEventBus eventBus,
        NodalOsCoreEventKind kind,
        NodalOsExecutionRegistryEntry entry,
        string missionId,
        string taskId,
        string summary,
        IReadOnlyList<NodalOsEvidenceBridgeRef>? evidenceRefs = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var coreEvent = new NodalOsCoreEvent
        {
            EventId = $"event-{Guid.NewGuid():N}",
            Kind = kind,
            ExecutionRegistryEntryId = entry.RegistryEntryId,
            ExecutionRequestId = entry.RequestId,
            MissionId = missionId,
            TaskId = taskId,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            MetadataRedacted = metadata ?? new Dictionary<string, string>(),
            EvidenceRefs = evidenceRefs ?? entry.EvidenceRefs,
            HumanSummaryRedacted = SafeRuntimeText.Sanitize(summary, 400),
            TechnicalSummaryRedacted = SafeRuntimeText.Sanitize(summary, 400),
            CreatedAt = DateTimeOffset.UtcNow
        };
        var validation = eventBus.Publish(coreEvent);
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join(" | ", validation.Errors));
        return coreEvent;
    }

    private static NodalOsEvidenceBridgeRef Evidence(
        string id,
        string kind,
        string hash,
        string ledgerRef,
        string provenance) => new()
    {
        EvidenceId = id,
        Kind = kind,
        Ref = null,
        Hash = hash,
        SourceKind = NodalOsEvidenceBridgeSourceKind.VerificationGate,
        UseKind = NodalOsEvidenceBridgeUseKind.VerificationSupport,
        Authority = NodalOsEvidenceBridgeAuthority.SupportsVerificationOnly,
        Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
        RedactionState = NodalOsEvidenceRedactionState.NotRequired,
        LedgerRef = ledgerRef,
        Provenance = provenance,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private string? ResolveSnapshotPath(string snapshotId)
    {
        if (!IsSafeIdentifier(snapshotId))
            return null;
        var root = _restoreRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(root, snapshotId + ".bak"));
        var prefix = root + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return full.StartsWith(prefix, comparison) ? full : null;
    }

    private static string? ResolveTarget(string rootPath, string relativePath)
    {
        if (!string.Equals(relativePath, NodalOsWorkspaceMissionDraftService.RelativeTargetPath, StringComparison.Ordinal) ||
            Path.IsPathRooted(relativePath) || relativePath.Contains("..", StringComparison.Ordinal))
            return null;
        try
        {
            var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var target = Path.GetFullPath(Path.Combine(root, relativePath));
            var prefix = root + Path.DirectorySeparatorChar;
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return target.StartsWith(prefix, comparison) ? target : null;
        }
        catch
        {
            return null;
        }
    }

    private static async ValueTask WriteNewFileAsync(
        string path,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        stream.Flush(flushToDisk: true);
    }

    private static async ValueTask<string?> HashRegularFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(path) || Directory.Exists(path) || IsReparsePoint(path))
                return null;
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false)).ToLowerInvariant();
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

    private static string Sha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string Fingerprint(string path) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            "nodal-workspace-v1|" + Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant())))
            .ToLowerInvariant();

    private static bool IsSha256(string? value) =>
        value is { Length: 64 } && value.All(Uri.IsHexDigit);

    private static bool IsSafeIdentifier(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 160 &&
        value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.');

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return File.Exists(path) || Directory.Exists(path)
                ? (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0
                : false;
        }
        catch
        {
            return true;
        }
    }

    private void TryDeleteSnapshot(NodalOsWorkspaceHandoffRestorePlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.SnapshotId))
            return;
        var path = ResolveSnapshotPath(plan.SnapshotId);
        if (path is not null)
            TryDelete(path);
    }

    private static void TryDeleteExact(string path, string expectedHash)
    {
        try
        {
            if (string.Equals(HashRegularFileAsync(path, CancellationToken.None).AsTask().GetAwaiter().GetResult(), expectedHash, StringComparison.Ordinal))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

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

    private NodalOsWorkspaceHandoffExecutionSnapshot FromDocument(
        NodalOsPersistedWorkspaceHandoffExecution document,
        string decision,
        bool accepted,
        NodalOsWorkspaceHandoffExecutionState state,
        IReadOnlyList<string> blockers,
        bool rehydrated,
        bool appConfigurationMutated = false,
        bool? rollbackAvailableOverride = null)
    {
        var rollbackAvailable = rollbackAvailableOverride ??
            (state == NodalOsWorkspaceHandoffExecutionState.Completed &&
             document.RestorePlan is not null &&
             document.Verified);
        return new NodalOsWorkspaceHandoffExecutionSnapshot(
            decision,
            accepted,
            state,
            document.OperationId,
            document.MissionId,
            document.WorkspaceId,
            document.WorkspaceFingerprint,
            document.Candidate.ActionId,
            document.Candidate.Kind.ToString(),
            document.Candidate.RelativeTargetPath,
            document.Candidate.ProposedSha256,
            document.Candidate.ExistingSha256,
            document.ResultSha256,
            document.OriginalSha256,
            document.BytesWritten,
            document.Approval.ApprovalDecision.DecisionKind.ToString(),
            document.Approval.ApprovalDecision.DecisionId,
            document.Approval.ScopeId,
            document.RegistryEntry.State.ToString(),
            Persisted: true,
            Rehydrated: rehydrated,
            Executed: document.ExecutedAt is not null,
            Verified: document.Verified,
            RollbackAvailable: rollbackAvailable,
            RolledBack: state == NodalOsWorkspaceHandoffExecutionState.RolledBack,
            RestorePlanId: document.RestorePlan?.RestorePlanId,
            EvidenceRefs: document.EvidenceRefs.Select(value => value.EvidenceId).Distinct(StringComparer.Ordinal).ToArray(),
            Timeline: document.Timeline,
            Blockers: blockers,
            ApprovedAt: document.Approval.ApprovedAt,
            ExecutedAt: document.ExecutedAt,
            RolledBackAt: document.RolledBackAt,
            RealFilesystemRead: true,
            WorkspaceFilesystemMutated: document.ExecutedAt is not null,
            AppConfigurationMutated: appConfigurationMutated,
            NetworkUsed: false,
            ExternalProcessUsed: false,
            SecretsExcluded: true,
            ProductAuthorityGranted: false);
    }

    private static NodalOsWorkspaceHandoffExecutionSnapshot Failure(
        string decision,
        NodalOsWorkspaceHandoffExecutionState state,
        IReadOnlyList<string> blockers,
        bool persisted = false) => new(
        decision,
        Accepted: false,
        state,
        OperationId: null,
        MissionId: null,
        WorkspaceId: null,
        WorkspaceFingerprint: null,
        ActionId: null,
        ActionKind: null,
        NodalOsWorkspaceMissionDraftService.RelativeTargetPath,
        ProposedSha256: null,
        ExpectedCurrentSha256: null,
        ResultSha256: null,
        OriginalSha256: null,
        BytesWritten: 0,
        ApprovalStatus: "not approved",
        ApprovalDecisionId: null,
        ApprovalScope: null,
        RegistryState: "not registered",
        Persisted: persisted,
        Rehydrated: false,
        Executed: false,
        Verified: false,
        RollbackAvailable: false,
        RolledBack: false,
        RestorePlanId: null,
        EvidenceRefs: [],
        Timeline: [],
        Blockers: blockers.Select(value => SafeRuntimeText.Sanitize(value, 240)).ToArray(),
        ApprovedAt: null,
        ExecutedAt: null,
        RolledBackAt: null,
        RealFilesystemRead: false,
        WorkspaceFilesystemMutated: false,
        AppConfigurationMutated: false,
        NetworkUsed: false,
        ExternalProcessUsed: false,
        SecretsExcluded: true,
        ProductAuthorityGranted: false);

    private sealed record ApprovalFlow(
        NodalOsMissionScopeApprovalBinding Approval,
        NodalOsExecutionRegistryEntry RegistryEntry,
        IReadOnlyList<NodalOsCoreTimelineProjection> Timeline,
        IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs);

    private sealed record ControlledWriteResult(
        bool Success,
        bool Verified,
        string? ResultSha256,
        string? OriginalSha256,
        long BytesWritten,
        NodalOsWorkspaceHandoffRestorePlan? RestorePlan,
        NodalOsEvidenceBridgeRef? Evidence,
        string SafeMessage,
        bool WriteCommitted)
    {
        public static ControlledWriteResult Failed(string message, bool writeCommitted = false) =>
            new(false, false, null, null, 0, null, null, SafeRuntimeText.Sanitize(message, 240), writeCommitted);
    }

    private sealed record RollbackResult(
        bool Success,
        NodalOsEvidenceBridgeRef? Evidence,
        string SafeMessage)
    {
        public static RollbackResult Failed(string message) =>
            new(false, null, SafeRuntimeText.Sanitize(message, 240));
    }
}