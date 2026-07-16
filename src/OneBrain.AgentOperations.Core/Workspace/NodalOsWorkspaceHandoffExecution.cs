using System.Security;
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
    string RegistryState,
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
    private readonly string metadataFilePath;
    private readonly string restoreRootPath;
    private readonly NodalOsWorkspaceSelectionService workspaceSelection;
    private readonly NodalOsWorkspaceMissionDraftService missionDraft;
    private readonly ISecretReferenceStore rootReferenceStore;
    private readonly NodalOsApprovalTimelineEvidenceValidator approvalValidator;
    private readonly NodalOsCoreRuntimeValidator coreValidator = new();

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

        this.metadataFilePath = Path.GetFullPath(metadataFilePath);
        this.restoreRootPath = Path.GetFullPath(restoreRootPath);
        this.workspaceSelection = workspaceSelection;
        this.missionDraft = missionDraft;
        this.rootReferenceStore = rootReferenceStore;
        this.approvalValidator = approvalValidator ?? new NodalOsApprovalTimelineEvidenceValidator();
    }

    public string MetadataFilePath => metadataFilePath;

    public async ValueTask<NodalOsWorkspaceHandoffExecutionSnapshot> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(metadataFilePath))
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

        var selected = await workspaceSelection.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!selected.Accepted || selected.Workspace is null ||
            !string.Equals(selected.WorkspaceId, document.WorkspaceId, StringComparison.Ordinal) ||
            !string.Equals(selected.RootPathFingerprint, document.WorkspaceFingerprint, StringComparison.Ordinal))
        {
            return FromDocument(
                document,
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_WORKSPACE_CHANGED",
                accepted: false,
                NodalOsWorkspaceHandoffExecutionState.CandidateStale,
                ["The selected workspace changed after approval or execution."],
                rehydrated: true,
                rollbackAvailableOverride: false);
        }

        if (document.State == NodalOsWorkspaceHandoffExecutionState.ApprovedPendingExecution)
        {
            return FromDocument(
                document,
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_INTERRUPTED",
                accepted: false,
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["Execution was interrupted after approval. Automatic resume is disabled; review and retry explicitly."],
                rehydrated: true,
                rollbackAvailableOverride: false);
        }

        var root = await OpenWorkspaceRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return FromDocument(
                document,
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_ROOT_UNAVAILABLE",
                accepted: false,
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["The protected workspace root could not be reopened."],
                rehydrated: true,
                rollbackAvailableOverride: false);
        }

        var target = ResolveTarget(root, document.Candidate.RelativeTargetPath);
        if (target is null)
        {
            return FromDocument(
                document,
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_TARGET_INVALID",
                accepted: false,
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["The persisted relative target no longer resolves inside the selected workspace."],
                rehydrated: true,
                rollbackAvailableOverride: false);
        }

        if (document.State == NodalOsWorkspaceHandoffExecutionState.Completed)
        {
            var currentHash = await HashFileAsync(target, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(currentHash, document.ResultSha256, StringComparison.Ordinal))
            {
                return FromDocument(
                    document,
                    "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_RESULT_CHANGED",
                    accepted: false,
                    NodalOsWorkspaceHandoffExecutionState.ResultChanged,
                    ["The verified handoff changed after execution. Rollback is disabled until a new review."],
                    rehydrated: true,
                    rollbackAvailableOverride: false);
            }
        }
        else if (document.State == NodalOsWorkspaceHandoffExecutionState.RolledBack)
        {
            var rollbackVerified = document.Candidate.Kind switch
            {
                NodalOsReviewedWorkspaceActionKind.CreateTextFile => !File.Exists(target),
                NodalOsReviewedWorkspaceActionKind.ExactHashUpdate => string.Equals(
                    await HashFileAsync(target, cancellationToken).ConfigureAwait(false),
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

        var accepted = document.State is NodalOsWorkspaceHandoffExecutionState.Completed or NodalOsWorkspaceHandoffExecutionState.RolledBack;
        var decision = document.State switch
        {
            NodalOsWorkspaceHandoffExecutionState.Completed => "GO_WORKSPACE_HANDOFF_EXECUTION_REHYDRATED",
            NodalOsWorkspaceHandoffExecutionState.RolledBack => "GO_WORKSPACE_HANDOFF_ROLLBACK_REHYDRATED",
            _ => "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_REHYDRATED"
        };
        var blockers = document.FailureReasonRedacted is null ? Array.Empty<string>() : [document.FailureReasonRedacted];
        return FromDocument(document, decision, accepted, document.State, blockers, rehydrated: true);
    }

    public async ValueTask<NodalOsWorkspaceHandoffExecutionSnapshot> ApproveAndExecuteAsync(
        string decidedBy = "local operator",
        string reason = "Approved after reviewing the mission-scoped reversible handoff action.",
        CancellationToken cancellationToken = default)
    {
        var existing = await TryReadDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (existing?.State == NodalOsWorkspaceHandoffExecutionState.Completed)
            return await GetCurrentAsync(cancellationToken).ConfigureAwait(false);

        var draft = await missionDraft.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!draft.Accepted || draft.Candidate is null || draft.Plan is null || draft.Binding is null)
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_DRAFT_NOT_READY",
                NodalOsWorkspaceHandoffExecutionState.CandidateStale,
                draft.ReviewBlockers.DefaultIfEmpty("A fresh reviewed mission draft is required before approval.").ToArray());
        }

        var missionDocument = await ReadMissionDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (missionDocument is null || !SameCandidate(missionDocument.Candidate, draft.Candidate) ||
            !string.Equals(missionDocument.Binding.MissionId, draft.MissionId, StringComparison.Ordinal) ||
            !string.Equals(missionDocument.WorkspaceFingerprint, draft.WorkspaceFingerprint, StringComparison.Ordinal))
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_DRAFT_METADATA_INVALID",
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["Mission draft metadata could not be bound to the reviewed candidate."]);
        }

        var selected = await workspaceSelection.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!selected.Accepted || selected.Workspace is null ||
            !string.Equals(selected.WorkspaceId, missionDocument.Binding.WorkspaceId, StringComparison.Ordinal) ||
            !string.Equals(selected.RootPathFingerprint, missionDocument.WorkspaceFingerprint, StringComparison.Ordinal))
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_WORKSPACE_NOT_READY",
                NodalOsWorkspaceHandoffExecutionState.CandidateStale,
                selected.ReviewBlockers.DefaultIfEmpty("The selected workspace is no longer ready.").ToArray());
        }

        var root = await OpenWorkspaceRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_ROOT_UNAVAILABLE",
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["The protected workspace root could not be opened."]);
        }

        var staleReason = await ValidateCandidateAsync(root, missionDocument.Candidate, cancellationToken).ConfigureAwait(false);
        if (staleReason is not null)
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_PRECONDITION_CHANGED",
                NodalOsWorkspaceHandoffExecutionState.CandidateStale,
                [staleReason]);
        }

        var approval = CreateApproval(missionDocument, selected.Workspace, decidedBy, reason);
        if (approval is null)
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_APPROVAL_INVALID",
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["The mission-scoped approval record failed validation."]);
        }

        var operationId = $"handoff-{Guid.NewGuid():N}";
        var approvalEvidence = approval.ApprovalCard.EvidenceRefs
            .First(value => value.Kind == "mission-scope-approval-binding");
        var preTimeline = new[]
        {
            Timeline(NodalOsCoreEventKind.ApprovalRequired, missionDocument, "Mission-scope approval required for the exact reviewed handoff action.", [approvalEvidence]),
            Timeline(NodalOsCoreEventKind.ApprovalGranted, missionDocument, "The operator approved the exact mission, workspace, action, capability, target and reviewed hashes.", [approvalEvidence]),
            Timeline(NodalOsCoreEventKind.DryRunPlanCreated, missionDocument, "Atomic write, exact precondition, post-write verification and guarded rollback were prepared before mutation.", [approvalEvidence])
        };
        var now = DateTimeOffset.UtcNow;
        var preExecution = new NodalOsPersistedWorkspaceHandoffExecution(
            CurrentSchemaVersion,
            NodalOsWorkspaceHandoffExecutionState.ApprovedPendingExecution,
            operationId,
            missionDocument.Binding.MissionId,
            missionDocument.Binding.WorkspaceId,
            missionDocument.WorkspaceFingerprint,
            missionDocument.Candidate,
            approval,
            RegistryState: "DryRunPlanned",
            ResultSha256: null,
            OriginalSha256: missionDocument.Candidate.ExistingSha256,
            BytesWritten: 0,
            Verified: false,
            RestorePlan: null,
            EvidenceRefs: approval.ApprovalCard.EvidenceRefs,
            Timeline: preTimeline,
            FailureReasonRedacted: null,
            CreatedAt: now,
            UpdatedAt: now,
            ExecutedAt: null,
            RolledBackAt: null);

        try
        {
            await WriteDocumentAsync(preExecution, root, cancellationToken).ConfigureAwait(false);
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

        WriteResult write;
        try
        {
            write = await ExecuteAsync(root, operationId, missionDocument.Candidate, approval.ApprovalDecision.DecisionId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            var cancelled = preExecution with
            {
                State = NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                RegistryState = "Cancelled",
                FailureReasonRedacted = "Execution was cancelled. Any committed write was restored inside the controlled writer.",
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await BestEffortWriteAsync(cancelled, root).ConfigureAwait(false);
            throw;
        }

        if (!write.Success || !write.Verified || write.Evidence is null || write.RestorePlan is null)
        {
            var failed = preExecution with
            {
                State = NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                RegistryState = "Failed",
                FailureReasonRedacted = SafeRuntimeText.Sanitize(write.Message, 240),
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await BestEffortWriteAsync(failed, root).ConfigureAwait(false);
            return FromDocument(
                failed,
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_FAILED_CLOSED",
                accepted: false,
                failed.State,
                [failed.FailureReasonRedacted],
                rehydrated: false);
        }

        var completedTimeline = preExecution.Timeline
            .Concat([
                Timeline(NodalOsCoreEventKind.ExecutionCompleted, missionDocument, "The approved handoff action completed and passed deterministic verification.", [write.Evidence]),
                Timeline(NodalOsCoreEventKind.EvidenceAttached, missionDocument, "Post-write SHA-256, approval scope and rollback readiness were attached before completion.", [write.Evidence])
            ])
            .ToArray();
        var completedEvidence = preExecution.EvidenceRefs
            .Concat([write.Evidence])
            .DistinctBy(value => value.EvidenceId)
            .ToArray();
        var completed = preExecution with
        {
            State = NodalOsWorkspaceHandoffExecutionState.Completed,
            RegistryState = "Completed",
            ResultSha256 = write.ResultSha256,
            OriginalSha256 = write.OriginalSha256,
            BytesWritten = write.BytesWritten,
            Verified = true,
            RestorePlan = write.RestorePlan,
            EvidenceRefs = completedEvidence,
            Timeline = completedTimeline,
            UpdatedAt = DateTimeOffset.UtcNow,
            ExecutedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await WriteDocumentAsync(completed, root, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            var rollback = await RollbackWriteAsync(root, write.RestorePlan, CancellationToken.None).ConfigureAwait(false);
            var rolledBack = completed with
            {
                State = rollback.Success ? NodalOsWorkspaceHandoffExecutionState.RolledBack : NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                RegistryState = rollback.Success ? "RolledBack" : "Failed",
                Verified = false,
                FailureReasonRedacted = "Final execution metadata persistence was cancelled; guarded rollback was attempted.",
                RolledBackAt = rollback.Success ? DateTimeOffset.UtcNow : null,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await BestEffortWriteAsync(rolledBack, root).ConfigureAwait(false);
            throw;
        }
        catch
        {
            var rollback = await RollbackWriteAsync(root, write.RestorePlan, CancellationToken.None).ConfigureAwait(false);
            var rolledBack = completed with
            {
                State = rollback.Success ? NodalOsWorkspaceHandoffExecutionState.RolledBack : NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                RegistryState = rollback.Success ? "RolledBack" : "Failed",
                Verified = false,
                FailureReasonRedacted = "Final execution metadata persistence failed; guarded rollback was attempted.",
                RolledBackAt = rollback.Success ? DateTimeOffset.UtcNow : null,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await BestEffortWriteAsync(rolledBack, root).ConfigureAwait(false);
            return FromDocument(
                rolledBack,
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_FINAL_PERSISTENCE_FAILED",
                accepted: false,
                rolledBack.State,
                [rolledBack.FailureReasonRedacted],
                rehydrated: false,
                rollbackAvailableOverride: false);
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
            !document.Verified || document.RestorePlan is null)
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_ROLLBACK_NOT_AVAILABLE",
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                ["A verified completed execution with a matching restore plan is required before rollback."],
                persisted: document is not null);
        }

        var root = await OpenWorkspaceRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
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

        var rollback = await RollbackWriteAsync(root, document.RestorePlan, cancellationToken).ConfigureAwait(false);
        if (!rollback.Success || rollback.Evidence is null)
        {
            return FromDocument(
                document,
                "BLOCKED_WORKSPACE_HANDOFF_ROLLBACK_FAILED_CLOSED",
                accepted: false,
                NodalOsWorkspaceHandoffExecutionState.FailedClosed,
                [rollback.Message],
                rehydrated: false,
                rollbackAvailableOverride: false);
        }

        var missionDocument = await ReadMissionDocumentAsync(cancellationToken).ConfigureAwait(false);
        var rollbackTimeline = missionDocument is null
            ? document.Timeline
            : document.Timeline.Concat([
                Timeline(NodalOsCoreEventKind.EvidenceAttached, missionDocument, "The controlled handoff rollback completed and passed exact-state verification.", [rollback.Evidence])
            ]).ToArray();
        var rolledBack = document with
        {
            State = NodalOsWorkspaceHandoffExecutionState.RolledBack,
            RegistryState = "RolledBack",
            EvidenceRefs = document.EvidenceRefs.Concat([rollback.Evidence]).DistinctBy(value => value.EvidenceId).ToArray(),
            Timeline = rollbackTimeline,
            UpdatedAt = DateTimeOffset.UtcNow,
            RolledBackAt = DateTimeOffset.UtcNow
        };
        await WriteDocumentAsync(rolledBack, root, cancellationToken).ConfigureAwait(false);
        DeleteSnapshot(document.RestorePlan);
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
                DeleteSnapshot(document.RestorePlan);
            if (File.Exists(metadataFilePath))
                File.Delete(metadataFilePath);
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
        return Empty(appConfigurationMutated: true);
    }

    private async ValueTask<NodalOsWorkspaceHandoffExecutionSnapshot> ReadyOrBlockedAsync(
        CancellationToken cancellationToken)
    {
        var draft = await missionDraft.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!draft.Accepted || draft.Candidate is null)
        {
            return Failure(
                "BLOCKED_WORKSPACE_HANDOFF_EXECUTION_DRAFT_NOT_READY",
                NodalOsWorkspaceHandoffExecutionState.NotConfigured,
                draft.ReviewBlockers.DefaultIfEmpty("Create and review a mission draft before execution.").ToArray());
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
            draft.Candidate.ProposedSha256,
            draft.Candidate.ExistingSha256,
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

    private NodalOsMissionScopeApprovalBinding? CreateApproval(
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
        var evidenceRefs = workspace.EvidenceRefs.Concat([approvalEvidence]).DistinctBy(value => value.EvidenceId).ToArray();
        var now = DateTimeOffset.UtcNow;
        var card = new NodalOsApprovalCard
        {
            ApprovalCardId = $"approval-{candidate.ActionId}",
            ApprovalRequestId = $"request-{candidate.ActionId}",
            ExecutionRegistryEntryId = $"registry-{candidate.ActionId}",
            MissionId = mission.Binding.MissionId,
            TaskId = "execute-controlled-action",
            Status = NodalOsApprovalStatus.PendingHumanDecision,
            Severity = candidate.RiskLevel == MissionRiskLevel.Low ? NodalOsApprovalSeverity.Low : NodalOsApprovalSeverity.Medium,
            RequestedAction = NodalOsApprovalActionKind.ExternalMutationFuture,
            HumanExplanationRedacted = $"{candidate.Kind} one reviewed handoff document in the selected local workspace.",
            PolicyGateReasonRedacted = "The target, workspace fingerprint, proposed hash and current target precondition are fixed; shell, network and other paths are denied.",
            AffectedResourcesRedacted = [candidate.RelativeTargetPath],
            RollbackPlanRedacted = candidate.RollbackPlanRedacted,
            EvidencePlanRedacted = "Record approval scope, precondition, post-write SHA-256, verification and rollback readiness before completion.",
            EvidenceRefs = evidenceRefs,
            UserOptions =
            [
                NodalOsApprovalUserOptionKind.Approve,
                NodalOsApprovalUserOptionKind.Reject,
                NodalOsApprovalUserOptionKind.RequestChanges,
                NodalOsApprovalUserOptionKind.RequestExplanation
            ],
            CanAuthorizeExecution = false,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            CreatedAt = now
        };
        if (!approvalValidator.ValidateApprovalCard(card).IsValid)
            return null;

        var decision = new NodalOsApprovalCenterService().CreateDecision(
            card,
            NodalOsApprovalDecisionKind.Approve,
            SafeRuntimeText.Sanitize(decidedBy, 80),
            SafeRuntimeText.Sanitize(reason, 240));
        if (!approvalValidator.ValidateApprovalDecision(decision).IsValid)
            return null;

        var approval = new NodalOsMissionScopeApprovalBinding(
            candidate.RequiredApprovalScope,
            mission.Binding.MissionId,
            mission.Binding.WorkspaceId,
            mission.WorkspaceFingerprint,
            candidate.ActionId,
            CapabilityId,
            candidate.RelativeTargetPath,
            candidate.ProposedSha256,
            candidate.ExistingSha256,
            card,
            decision,
            DateTimeOffset.UtcNow,
            OneShot: true,
            DecisionIsExecutionAuthority: false);
        return ValidateApproval(approval, mission) ? approval : null;
    }

    private static bool ValidateApproval(
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

    private async ValueTask<WriteResult> ExecuteAsync(
        string root,
        string operationId,
        NodalOsReviewedWorkspaceActionCandidate candidate,
        string approvalDecisionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var target = ResolveTarget(root, candidate.RelativeTargetPath);
        if (target is null || IsReparse(root) || IsReparse(Path.GetDirectoryName(target)!))
            return WriteResult.Failed("The reviewed target no longer resolves inside a non-reparse workspace boundary.");

        var proposed = Encoding.UTF8.GetBytes(candidate.ProposedContentRedacted);
        try
        {
            if (proposed.Length is <= 0 or > MaximumContentBytes ||
                !string.Equals(Sha256(proposed), candidate.ProposedSha256, StringComparison.Ordinal) ||
                HistorySanitizer.ContainsSecretLikeContent(candidate.ProposedContentRedacted))
            {
                return WriteResult.Failed("The proposed content failed size, hash or redaction validation.");
            }

            return candidate.Kind switch
            {
                NodalOsReviewedWorkspaceActionKind.CreateTextFile => await CreateAsync(root, target, operationId, approvalDecisionId, candidate, proposed, cancellationToken).ConfigureAwait(false),
                NodalOsReviewedWorkspaceActionKind.ExactHashUpdate => await UpdateAsync(root, target, operationId, approvalDecisionId, candidate, proposed, cancellationToken).ConfigureAwait(false),
                _ => WriteResult.Failed("The reviewed action kind is unsupported.")
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(proposed);
        }
    }

    private async ValueTask<WriteResult> CreateAsync(
        string root,
        string target,
        string operationId,
        string approvalDecisionId,
        NodalOsReviewedWorkspaceActionCandidate candidate,
        byte[] proposed,
        CancellationToken cancellationToken)
    {
        if (File.Exists(target) || Directory.Exists(target))
            return WriteResult.Failed("The create-only target now exists; the reviewed precondition is stale.");

        var temp = Path.Combine(root, $".{Path.GetFileName(target)}.{operationId}.tmp");
        var committed = false;
        try
        {
            await WriteNewAsync(temp, proposed, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temp, target, overwrite: false);
            committed = true;
            var persisted = await File.ReadAllBytesAsync(target, cancellationToken).ConfigureAwait(false);
            var resultHash = Sha256(persisted);
            if (!persisted.AsSpan().SequenceEqual(proposed) ||
                !string.Equals(resultHash, candidate.ProposedSha256, StringComparison.Ordinal))
            {
                await DeleteIfHashMatchesAsync(target, resultHash).ConfigureAwait(false);
                return WriteResult.Failed("The created handoff failed exact verification and was removed when possible.");
            }

            var plan = new NodalOsWorkspaceHandoffRestorePlan(
                $"restore-{operationId}",
                operationId,
                approvalDecisionId,
                Fingerprint(root),
                candidate.ActionId,
                candidate.Kind,
                candidate.RelativeTargetPath,
                resultHash,
                OriginalSha256: null,
                SnapshotId: null,
                DeleteCreatedFile: true,
                SnapshotStoredOutsideWorkspace: true,
                RequiresExactCurrentHash: true,
                DateTimeOffset.UtcNow);
            var evidence = Evidence(
                $"evidence:workspace-handoff-create:{operationId}",
                "real-workspace-handoff-create-verification",
                resultHash,
                $"workspace-handoff:{operationId}:create",
                "Approved create-only handoff write completed atomically and matched the reviewed SHA-256.");
            return new WriteResult(true, true, resultHash, null, persisted.LongLength, plan, evidence, "The handoff was created atomically and verified.");
        }
        catch (OperationCanceledException)
        {
            if (committed)
                await DeleteIfHashMatchesAsync(target, candidate.ProposedSha256).ConfigureAwait(false);
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            if (committed)
                await DeleteIfHashMatchesAsync(target, candidate.ProposedSha256).ConfigureAwait(false);
            return WriteResult.Failed("The create-only handoff write failed closed.");
        }
        finally
        {
            TryDelete(temp);
        }
    }

    private async ValueTask<WriteResult> UpdateAsync(
        string root,
        string target,
        string operationId,
        string approvalDecisionId,
        NodalOsReviewedWorkspaceActionCandidate candidate,
        byte[] proposed,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(target) || Directory.Exists(target) || IsReparse(target) ||
            string.IsNullOrWhiteSpace(candidate.ExistingSha256))
            return WriteResult.Failed("The exact-hash update target is missing or invalid.");

        var original = await File.ReadAllBytesAsync(target, cancellationToken).ConfigureAwait(false);
        var originalHash = Sha256(original);
        if (!string.Equals(originalHash, candidate.ExistingSha256, StringComparison.Ordinal))
            return WriteResult.Failed("The exact current SHA-256 precondition changed after review.");
        if (original.AsSpan().SequenceEqual(proposed))
            return WriteResult.Failed("The proposed handoff is identical to the current target.");

        var snapshotId = $"snapshot-{operationId}";
        var snapshot = ResolveSnapshot(snapshotId);
        if (snapshot is null)
            return WriteResult.Failed("The app-local restore snapshot boundary is unavailable.");
        Directory.CreateDirectory(Path.GetDirectoryName(snapshot)!);
        if (IsReparse(restoreRootPath) || IsReparse(Path.GetDirectoryName(snapshot)!))
            return WriteResult.Failed("The app-local restore snapshot boundary contains a reparse point.");

        await WriteNewAsync(snapshot, original, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(await HashFileAsync(snapshot, cancellationToken).ConfigureAwait(false), originalHash, StringComparison.Ordinal))
        {
            TryDelete(snapshot);
            return WriteResult.Failed("The app-local restore snapshot failed exact verification.");
        }

        var temp = Path.Combine(root, $".{Path.GetFileName(target)}.{operationId}.tmp");
        var committed = false;
        try
        {
            await WriteNewAsync(temp, proposed, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Replace(temp, target, destinationBackupFileName: null, ignoreMetadataErrors: true);
            committed = true;
            var persisted = await File.ReadAllBytesAsync(target, cancellationToken).ConfigureAwait(false);
            var resultHash = Sha256(persisted);
            if (!persisted.AsSpan().SequenceEqual(proposed) ||
                !string.Equals(resultHash, candidate.ProposedSha256, StringComparison.Ordinal))
            {
                var restored = await RestoreUpdateAsync(target, snapshot, originalHash, resultHash, CancellationToken.None).ConfigureAwait(false);
                if (restored)
                    TryDelete(snapshot);
                return WriteResult.Failed(restored
                    ? "The update failed verification and the original snapshot was restored."
                    : "The update failed verification and guarded rollback also failed.");
            }

            var plan = new NodalOsWorkspaceHandoffRestorePlan(
                $"restore-{operationId}",
                operationId,
                approvalDecisionId,
                Fingerprint(root),
                candidate.ActionId,
                candidate.Kind,
                candidate.RelativeTargetPath,
                resultHash,
                originalHash,
                snapshotId,
                DeleteCreatedFile: false,
                SnapshotStoredOutsideWorkspace: true,
                RequiresExactCurrentHash: true,
                DateTimeOffset.UtcNow);
            var evidence = Evidence(
                $"evidence:workspace-handoff-update:{operationId}",
                "real-workspace-handoff-update-verification",
                resultHash,
                $"workspace-handoff:{operationId}:update:before:{originalHash}",
                "Approved exact-hash handoff replacement used an app-local verified snapshot, atomic replace and post-write SHA-256 verification.");
            return new WriteResult(true, true, resultHash, originalHash, persisted.LongLength, plan, evidence, "The handoff was replaced atomically and verified.");
        }
        catch (OperationCanceledException)
        {
            if (committed)
            {
                var restored = await RestoreUpdateAsync(target, snapshot, originalHash, candidate.ProposedSha256, CancellationToken.None).ConfigureAwait(false);
                if (restored)
                    TryDelete(snapshot);
            }
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException or PlatformNotSupportedException)
        {
            if (committed)
            {
                var restored = await RestoreUpdateAsync(target, snapshot, originalHash, candidate.ProposedSha256, CancellationToken.None).ConfigureAwait(false);
                if (restored)
                    TryDelete(snapshot);
            }
            else
            {
                TryDelete(snapshot);
            }
            return WriteResult.Failed("The exact-hash replacement failed closed.");
        }
        finally
        {
            TryDelete(temp);
            CryptographicOperations.ZeroMemory(original);
        }
    }

    private async ValueTask<RollbackResult> RollbackWriteAsync(
        string root,
        NodalOsWorkspaceHandoffRestorePlan plan,
        CancellationToken cancellationToken)
    {
        var target = ResolveTarget(root, plan.RelativeTargetPath);
        if (target is null || !plan.RequiresExactCurrentHash ||
            !string.Equals(plan.WorkspaceFingerprint, Fingerprint(root), StringComparison.Ordinal) ||
            !string.Equals(plan.RelativeTargetPath, NodalOsWorkspaceMissionDraftService.RelativeTargetPath, StringComparison.Ordinal))
            return RollbackResult.Failed("The restore plan no longer matches the workspace or allowlisted target.");

        var currentHash = await HashFileAsync(target, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(currentHash, plan.ResultSha256, StringComparison.Ordinal))
            return RollbackResult.Failed("The exact current result hash changed; guarded rollback was not attempted.");

        if (plan.DeleteCreatedFile)
        {
            try
            {
                File.Delete(target);
                if (File.Exists(target))
                    return RollbackResult.Failed("The created handoff could not be removed during rollback.");
                return new RollbackResult(
                    true,
                    Evidence(
                        $"evidence:workspace-handoff-rollback:{plan.OperationId}",
                        "real-workspace-handoff-create-rollback-verification",
                        plan.ResultSha256,
                        $"workspace-handoff:{plan.OperationId}:rollback:create",
                        "The created handoff was removed only after its exact result hash and restore-plan identity matched."),
                    "The created handoff was removed and absence was verified.");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
            {
                return RollbackResult.Failed("The create-only rollback failed closed.");
            }
        }

        if (string.IsNullOrWhiteSpace(plan.SnapshotId) || string.IsNullOrWhiteSpace(plan.OriginalSha256))
            return RollbackResult.Failed("The exact-hash update restore plan is incomplete.");
        var snapshot = ResolveSnapshot(plan.SnapshotId);
        if (snapshot is null || !File.Exists(snapshot) || IsReparse(snapshot) ||
            !string.Equals(await HashFileAsync(snapshot, cancellationToken).ConfigureAwait(false), plan.OriginalSha256, StringComparison.Ordinal))
            return RollbackResult.Failed("The app-local restore snapshot is unavailable or invalid.");

        var restored = await RestoreUpdateAsync(target, snapshot, plan.OriginalSha256, plan.ResultSha256, cancellationToken).ConfigureAwait(false);
        if (!restored)
            return RollbackResult.Failed("The exact-hash update rollback failed deterministic verification.");
        return new RollbackResult(
            true,
            Evidence(
                $"evidence:workspace-handoff-rollback:{plan.OperationId}",
                "real-workspace-handoff-update-rollback-verification",
                plan.OriginalSha256,
                $"workspace-handoff:{plan.OperationId}:rollback:update",
                "The original snapshot was restored only after operation identity, result hash and snapshot hash matched."),
            "The original handoff snapshot was restored and verified.");
    }

    private async ValueTask<string?> ValidateCandidateAsync(
        string root,
        NodalOsReviewedWorkspaceActionCandidate candidate,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(candidate.RelativeTargetPath, NodalOsWorkspaceMissionDraftService.RelativeTargetPath, StringComparison.Ordinal) ||
            candidate.State != NodalOsReviewedWorkspaceActionState.ReadyForReview ||
            candidate.ExecutionEnabled || !candidate.ApprovalRequired || !candidate.Reversible ||
            !IsSha256(candidate.ProposedSha256))
            return "The reviewed candidate no longer matches the allowlisted executable shape.";

        var target = ResolveTarget(root, candidate.RelativeTargetPath);
        if (target is null || IsReparse(root) || IsReparse(Path.GetDirectoryName(target)!))
            return "The reviewed target no longer resolves inside a non-reparse workspace boundary.";
        if (candidate.Kind == NodalOsReviewedWorkspaceActionKind.CreateTextFile)
            return File.Exists(target) || Directory.Exists(target) ? "The create-only target now exists." : null;
        if (candidate.Kind != NodalOsReviewedWorkspaceActionKind.ExactHashUpdate ||
            !File.Exists(target) || Directory.Exists(target) || IsReparse(target) ||
            string.IsNullOrWhiteSpace(candidate.ExistingSha256))
            return "The exact-hash update target is missing or invalid.";

        return string.Equals(await HashFileAsync(target, cancellationToken).ConfigureAwait(false), candidate.ExistingSha256, StringComparison.Ordinal)
            ? null
            : "The exact current target SHA-256 changed after review.";
    }

    private NodalOsCoreTimelineProjection Timeline(
        NodalOsCoreEventKind kind,
        NodalOsPersistedWorkspaceMissionDraft mission,
        string summary,
        IReadOnlyList<NodalOsEvidenceBridgeRef> evidenceRefs)
    {
        var coreEvent = new NodalOsCoreEvent
        {
            EventId = $"event-{Guid.NewGuid():N}",
            Kind = kind,
            ExecutionRegistryEntryId = $"registry-{mission.Candidate.ActionId}",
            ExecutionRequestId = $"request-{mission.Candidate.ActionId}",
            MissionId = mission.Binding.MissionId,
            TaskId = kind switch
            {
                NodalOsCoreEventKind.ApprovalRequired or NodalOsCoreEventKind.ApprovalGranted => "resolve-mission-scope-approval",
                NodalOsCoreEventKind.ExecutionCompleted => "execute-controlled-action",
                NodalOsCoreEventKind.EvidenceAttached => "record-evidence-handoff",
                _ => "execute-controlled-action"
            },
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            MetadataRedacted = new Dictionary<string, string>
            {
                ["actionId"] = mission.Candidate.ActionId,
                ["target"] = mission.Candidate.RelativeTargetPath
            },
            EvidenceRefs = evidenceRefs,
            HumanSummaryRedacted = SafeRuntimeText.Sanitize(summary, 400),
            TechnicalSummaryRedacted = SafeRuntimeText.Sanitize(summary, 400),
            CreatedAt = DateTimeOffset.UtcNow
        };
        var validation = coreValidator.ValidateEvent(coreEvent);
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join(" | ", validation.Errors));
        return coreValidator.ToTimelineProjection(coreEvent);
    }

    private async ValueTask<string?> OpenWorkspaceRootAsync(CancellationToken cancellationToken)
    {
        var selection = await ReadSelectionDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (selection is null)
            return null;

        SecretLease? lease;
        try
        {
            lease = await rootReferenceStore.OpenAsync(selection.RootPathReference, cancellationToken).ConfigureAwait(false);
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
                var root = Path.GetFullPath(Encoding.UTF8.GetString(lease.Bytes.Span));
                return Directory.Exists(root) && !IsReparse(root) ? root : null;
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
            var info = new FileInfo(workspaceSelection.MetadataFilePath);
            if (!info.Exists || info.Length is <= 0 or > MaximumMetadataBytes)
                return null;
            var json = await File.ReadAllTextAsync(info.FullName, cancellationToken).ConfigureAwait(false);
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
            var info = new FileInfo(missionDraft.MetadataFilePath);
            if (!info.Exists || info.Length is <= 0 or > MaximumMetadataBytes)
                return null;
            var json = await File.ReadAllTextAsync(info.FullName, cancellationToken).ConfigureAwait(false);
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
        if (!File.Exists(metadataFilePath))
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
        var info = new FileInfo(metadataFilePath);
        if (!info.Exists || info.Length is <= 0 or > MaximumMetadataBytes)
            throw new InvalidDataException("Handoff execution metadata size is invalid.");
        var json = await File.ReadAllTextAsync(info.FullName, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<NodalOsPersistedWorkspaceHandoffExecution>(json, JsonOptions);
    }

    private async ValueTask WriteDocumentAsync(
        NodalOsPersistedWorkspaceHandoffExecution document,
        string root,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(document, JsonOptions);
        var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (json.Length > MaximumMetadataBytes || json.Contains(root, pathComparison) || HistorySanitizer.ContainsSecretLikeContent(json))
            throw new InvalidDataException("Handoff execution metadata crossed a path or secret boundary.");

        var directory = Path.GetDirectoryName(metadataFilePath)
            ?? throw new InvalidOperationException("Handoff execution metadata directory is unavailable.");
        Directory.CreateDirectory(directory);
        var temp = $"{metadataFilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(temp, json, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
            File.Move(temp, metadataFilePath, overwrite: true);
            TryHide(metadataFilePath);
        }
        finally
        {
            TryDelete(temp);
        }
    }

    private async ValueTask BestEffortWriteAsync(NodalOsPersistedWorkspaceHandoffExecution document, string root)
    {
        try
        {
            await WriteDocumentAsync(document, root, CancellationToken.None).ConfigureAwait(false);
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
        string.Equals(document.Candidate.RelativeTargetPath, NodalOsWorkspaceMissionDraftService.RelativeTargetPath, StringComparison.Ordinal) &&
        IsSha256(document.Candidate.ProposedSha256) &&
        document.Approval.OneShot &&
        !document.Approval.DecisionIsExecutionAuthority &&
        document.Approval.ApprovalDecision.DecisionKind == NodalOsApprovalDecisionKind.Approve &&
        !document.Approval.ApprovalDecision.RuntimeExecutionAllowed &&
        !document.Approval.ApprovalDecision.CanAuthorizeExecution &&
        string.Equals(document.Approval.MissionId, document.MissionId, StringComparison.Ordinal) &&
        string.Equals(document.Approval.WorkspaceId, document.WorkspaceId, StringComparison.Ordinal) &&
        string.Equals(document.Approval.WorkspaceFingerprint, document.WorkspaceFingerprint, StringComparison.Ordinal) &&
        string.Equals(document.Approval.ActionId, document.Candidate.ActionId, StringComparison.Ordinal) &&
        string.Equals(document.Approval.CapabilityId, CapabilityId, StringComparison.Ordinal) &&
        string.Equals(document.Approval.RelativeTargetPath, document.Candidate.RelativeTargetPath, StringComparison.Ordinal) &&
        string.Equals(document.Approval.ProposedSha256, document.Candidate.ProposedSha256, StringComparison.Ordinal) &&
        document.CreatedAt != default && document.UpdatedAt != default;

    private static bool SameCandidate(
        NodalOsReviewedWorkspaceActionCandidate left,
        NodalOsReviewedWorkspaceActionCandidate right) =>
        string.Equals(left.ActionId, right.ActionId, StringComparison.Ordinal) &&
        left.Kind == right.Kind && left.State == right.State &&
        string.Equals(left.RelativeTargetPath, right.RelativeTargetPath, StringComparison.Ordinal) &&
        string.Equals(left.ProposedSha256, right.ProposedSha256, StringComparison.Ordinal) &&
        string.Equals(left.ExistingSha256, right.ExistingSha256, StringComparison.Ordinal) &&
        left.ApprovalRequired == right.ApprovalRequired && left.Reversible == right.Reversible && !right.ExecutionEnabled;

    private static async ValueTask<bool> RestoreUpdateAsync(
        string target,
        string snapshot,
        string originalHash,
        string expectedCurrentHash,
        CancellationToken cancellationToken)
    {
        var currentHash = await HashFileAsync(target, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(currentHash, expectedCurrentHash, StringComparison.Ordinal))
            return false;
        var original = await File.ReadAllBytesAsync(snapshot, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(Sha256(original), originalHash, StringComparison.Ordinal))
            return false;
        var temp = Path.Combine(Path.GetDirectoryName(target)!, $".{Path.GetFileName(target)}.{Guid.NewGuid():N}.restore.tmp");
        try
        {
            await WriteNewAsync(temp, original, cancellationToken).ConfigureAwait(false);
            File.Replace(temp, target, destinationBackupFileName: null, ignoreMetadataErrors: true);
            var restored = await File.ReadAllBytesAsync(target, cancellationToken).ConfigureAwait(false);
            return restored.AsSpan().SequenceEqual(original) && string.Equals(Sha256(restored), originalHash, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(original);
            TryDelete(temp);
        }
    }

    private static async ValueTask WriteNewAsync(string path, byte[] bytes, CancellationToken cancellationToken)
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

    private static async ValueTask<string?> HashFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(path) || Directory.Exists(path) || IsReparse(path))
                return null;
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
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

    private static async ValueTask DeleteIfHashMatchesAsync(string path, string expectedHash)
    {
        try
        {
            if (string.Equals(await HashFileAsync(path, CancellationToken.None).ConfigureAwait(false), expectedHash, StringComparison.Ordinal))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private string? ResolveSnapshot(string snapshotId)
    {
        if (!SafeIdentifier(snapshotId))
            return null;
        var root = restoreRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(root, snapshotId + ".bak"));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return full.StartsWith(root + Path.DirectorySeparatorChar, comparison) ? full : null;
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
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return target.StartsWith(root + Path.DirectorySeparatorChar, comparison) ? target : null;
        }
        catch
        {
            return null;
        }
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

    private void DeleteSnapshot(NodalOsWorkspaceHandoffRestorePlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.SnapshotId))
            return;
        var snapshot = ResolveSnapshot(plan.SnapshotId);
        if (snapshot is not null)
            TryDelete(snapshot);
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
            (state == NodalOsWorkspaceHandoffExecutionState.Completed && document.RestorePlan is not null && document.Verified);
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
            document.RegistryState,
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

    private static NodalOsWorkspaceHandoffExecutionSnapshot Empty(bool appConfigurationMutated) => new(
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
        AppConfigurationMutated: appConfigurationMutated,
        NetworkUsed: false,
        ExternalProcessUsed: false,
        SecretsExcluded: true,
        ProductAuthorityGranted: false);

    private static string Sha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string Fingerprint(string root) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            "nodal-workspace-v1|" + Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant())))
            .ToLowerInvariant();

    private static bool IsSha256(string? value) => value is { Length: 64 } && value.All(Uri.IsHexDigit);

    private static bool SafeIdentifier(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 160 && value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.');

    private static bool IsReparse(string path)
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

    private sealed record WriteResult(
        bool Success,
        bool Verified,
        string? ResultSha256,
        string? OriginalSha256,
        long BytesWritten,
        NodalOsWorkspaceHandoffRestorePlan? RestorePlan,
        NodalOsEvidenceBridgeRef? Evidence,
        string Message)
    {
        public static WriteResult Failed(string message) =>
            new(false, false, null, null, 0, null, null, SafeRuntimeText.Sanitize(message, 240));
    }

    private sealed record RollbackResult(bool Success, NodalOsEvidenceBridgeRef? Evidence, string Message)
    {
        public static RollbackResult Failed(string message) =>
            new(false, null, SafeRuntimeText.Sanitize(message, 240));
    }
}