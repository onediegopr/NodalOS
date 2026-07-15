using System.Security.Cryptography;
using System.Text;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.AgentOperations.Core.Runtime;

public sealed record NodalOsTestOwnedFileUpdateMissionSnapshot(
    bool LocalDevOnly,
    bool ReadOnlySurface,
    bool SecretsExcluded,
    string WorkspaceId,
    string MissionId,
    string MissionStatus,
    string RegistryState,
    string ApprovalStatus,
    string FileUpdateState,
    bool FileUpdateVerified,
    bool PreconditionMatched,
    bool SnapshotCreated,
    bool AtomicReplaceUsed,
    bool RollbackPlanCreated,
    bool FixtureCleanupRemovedRollbackSnapshot,
    string RelativePath,
    string? OriginalSha256,
    string? UpdatedSha256,
    long OriginalBytes,
    long UpdatedBytes,
    bool MissionAuthorizationReused,
    bool AdditionalStepApprovalRequested,
    bool TestOwnedFilesystemTouched,
    bool TestOwnedFixtureCleaned,
    bool UserWorkspaceFilesystemTouched,
    bool NetworkUsed,
    bool ProductAuthorityGranted,
    int TimelineCount,
    int EvidenceCount);

public sealed record NodalOsTestOwnedFileUpdateMissionResult(
    NodalOsWorkspaceLocalModel Workspace,
    NodalOsPathJailBinding UserWorkspacePathJail,
    NodalOsWorkspaceMissionBinding MissionBinding,
    MissionPlan Plan,
    MissionRuntimeState Mission,
    MissionResumeCard ResumeCard,
    NodalOsExecutionRegistryEntry RegistryEntry,
    NodalOsApprovalCard MissionAuthorizationCard,
    NodalOsApprovalDecision MissionAuthorizationDecision,
    NodalOsTestOwnedFileCreateResult SeedCreateAction,
    NodalOsTestOwnedFileUpdateResult FileUpdateAction,
    NodalOsTestOwnedFixtureCleanupResult FixtureCleanup,
    IReadOnlyList<NodalOsCoreTimelineProjection> Timeline,
    bool MissionAuthorizationReused,
    bool AdditionalStepApprovalRequested,
    bool TestOwnedFilesystemTouched,
    bool UserWorkspaceFilesystemTouched,
    bool NetworkUsed,
    bool ProductAuthorityGranted)
{
    public NodalOsTestOwnedFileUpdateMissionSnapshot ToSnapshot() => new(
        LocalDevOnly: true,
        ReadOnlySurface: true,
        SecretsExcluded: true,
        WorkspaceId: Workspace.WorkspaceId,
        MissionId: Mission.MissionId,
        MissionStatus: Mission.Status.ToString(),
        RegistryState: RegistryEntry.State.ToString(),
        ApprovalStatus: MissionAuthorizationDecision.DecisionKind.ToString(),
        FileUpdateState: FileUpdateAction.Decision.ToString(),
        FileUpdateVerified: FileUpdateAction.Verified,
        PreconditionMatched: FileUpdateAction.PreconditionMatched,
        SnapshotCreated: FileUpdateAction.SnapshotCreated,
        AtomicReplaceUsed: FileUpdateAction.AtomicReplaceUsed,
        RollbackPlanCreated: FileUpdateAction.RestorePlan is not null && FileUpdateAction.RollbackAvailable,
        FixtureCleanupRemovedRollbackSnapshot: FixtureCleanup.Success && FixtureCleanup.RootRemoved,
        RelativePath: FileUpdateAction.RelativePath,
        OriginalSha256: FileUpdateAction.OriginalSha256,
        UpdatedSha256: FileUpdateAction.UpdatedSha256,
        OriginalBytes: FileUpdateAction.OriginalBytes,
        UpdatedBytes: FileUpdateAction.UpdatedBytes,
        MissionAuthorizationReused: MissionAuthorizationReused,
        AdditionalStepApprovalRequested: AdditionalStepApprovalRequested,
        TestOwnedFilesystemTouched: TestOwnedFilesystemTouched,
        TestOwnedFixtureCleaned: FixtureCleanup.Success && FixtureCleanup.RootRemoved,
        UserWorkspaceFilesystemTouched: UserWorkspaceFilesystemTouched,
        NetworkUsed: NetworkUsed,
        ProductAuthorityGranted: ProductAuthorityGranted,
        TimelineCount: Timeline.Count,
        EvidenceCount: Timeline.SelectMany(item => item.EvidenceRefs)
            .Select(item => item.EvidenceId)
            .Distinct(StringComparer.Ordinal)
            .Count());
}

public sealed class NodalOsTestOwnedFileUpdateMissionScenario
{
    public async ValueTask<NodalOsTestOwnedFileUpdateMissionResult> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var createAction = new NodalOsTestOwnedFileCreateAction();
        var updateAction = new NodalOsTestOwnedFileUpdateAction();
        var root = Path.Combine(
            NodalOsTestOwnedFileCreateAction.AllowedBaseRoot,
            "run-" + Guid.NewGuid().ToString("N"));
        string? rootFingerprint = null;
        var cleanupCompleted = false;

        try
        {
            var seed = createAction.Execute(
                new NodalOsTestOwnedFileCreateRequest(
                    OperationId: "fixture-update-seed",
                    ApprovalDecisionId: "fixture-seed-internal",
                    TestOwnedRootPath: root,
                    RelativePath: "output/reviewed-handoff.md",
                    Content: BuildOriginalContent()),
                cancellationToken);
            if (!seed.Success || seed.ContentSha256 is null)
                throw new InvalidOperationException(seed.SafeMessage);
            rootFingerprint = seed.RootFingerprint;

            var plan = CreatePlan();
            var runtime = new LightweightMissionRuntime(
                plan,
                new MissionAuthorizationScope(
                    plan.MissionId,
                    new HashSet<string>(["filesystem.write.safe"], StringComparer.Ordinal),
                    new HashSet<MissionExecutionSurface>([MissionExecutionSurface.Filesystem]),
                    MissionRiskLevel.Medium),
                "fixture-file-update-run");
            runtime.Start("fixture-file-update-start");

            var eventBus = new NodalOsCoreEventBus();
            var registry = new NodalOsExecutionRegistry();
            var seedEvidence = Evidence(
                "evidence-file-update-mission-scope",
                "mission-scope-approval-record",
                NodalOsEvidenceBridgeUseKind.AuditTrail);
            var registryEntry = registry.Register(new NodalOsExecutionRequest
            {
                RequestId = "fixture-file-update-request",
                MissionId = plan.MissionId,
                TaskId = plan.Steps.Single().Id,
                RunId = runtime.State.RunId,
                RequestedBy = "fixture-operator-preauthorized",
                ActorKind = NodalOsExecutionActorKind.HumanOperator,
                SourceKind = NodalOsExecutionSourceKind.MissionControl,
                RuntimeExecutionAllowed = false,
                RuntimeExecutionDeferred = true,
                RequiresGlobalPolicyEvaluation = true,
                RequiresEvidenceRedaction = true,
                Summary = "Replace one existing test-owned text file under an exact SHA-256 precondition.",
                EvidenceRefs = [seedEvidence],
                CreatedAt = DateTimeOffset.UtcNow
            });
            Publish(eventBus, NodalOsCoreEventKind.ExecutionRequestRegistered, registryEntry, plan, "Exact-hash update fixture request registered.");

            registryEntry = registry.Transition(
                registryEntry.RegistryEntryId,
                NodalOsExecutionRegistryState.PolicyEvaluated,
                "fixture-file-update-policy",
                "The target is test-owned, exact-hash preconditioned, snapshotted, atomically replaced and verified.",
                policyDecisionRef: "fixture-exact-hash-update-policy");
            Publish(eventBus, NodalOsCoreEventKind.PolicyGateEvaluated, registryEntry, plan, "Exact-hash test fixture update policy passed.");

            registryEntry = registry.Transition(
                registryEntry.RegistryEntryId,
                NodalOsExecutionRegistryState.ApprovalRequired,
                "fixture-approval-center",
                "One mission-level approval is required before the bounded update action.");
            var approvalEvent = Publish(
                eventBus,
                NodalOsCoreEventKind.ApprovalRequired,
                registryEntry,
                plan,
                "Mission-level approval required once for the exact-hash fixture update.");

            var approvalService = new NodalOsApprovalCenterService();
            var approvalCard = approvalService.CreateApprovalCard(
                registryEntry,
                approvalEvent,
                NodalOsApprovalSeverity.Medium,
                NodalOsApprovalActionKind.ExternalMutationFuture,
                "Replace one existing Markdown file inside a unique test-owned temporary root.",
                "Exact current SHA-256, verified restore snapshot, atomic replacement and post-write SHA-256 are enforced.",
                ["test-owned output/reviewed-handoff.md"]) with
            {
                RollbackPlanRedacted = "Restore only when root, target, current updated hash and original snapshot hash match the approved restore plan.",
                EvidencePlanRedacted = "Record original and updated SHA-256 plus the verified restore plan before mission completion."
            };
            EnsureValid(new NodalOsApprovalTimelineEvidenceValidator().ValidateApprovalCard(approvalCard));
            var approvalDecision = approvalService.CreateDecision(
                approvalCard,
                NodalOsApprovalDecisionKind.Approve,
                "fixture-operator-preauthorized",
                "Approved once for this mission and exact-hash test-owned update scope.");
            EnsureValid(new NodalOsApprovalTimelineEvidenceValidator().ValidateApprovalDecision(approvalDecision));

            registryEntry = registry.Transition(
                registryEntry.RegistryEntryId,
                NodalOsExecutionRegistryState.Approved,
                "fixture-operator-preauthorized",
                "Mission-level exact-hash update scope approved.",
                approvalRef: approvalDecision.DecisionId);
            Publish(
                eventBus,
                NodalOsCoreEventKind.ApprovalGranted,
                registryEntry,
                plan,
                "Mission-level approval granted; the bounded update continues without another prompt.",
                new Dictionary<string, string>
                {
                    ["approvalCardId"] = approvalCard.ApprovalCardId,
                    ["approvalDecisionId"] = approvalDecision.DecisionId
                });

            registryEntry = registry.Transition(
                registryEntry.RegistryEntryId,
                NodalOsExecutionRegistryState.DryRunPlanned,
                "fixture-file-update",
                "Exact-hash precondition, restore snapshot, atomic replacement and verification planned.",
                dryRunRef: "fixture-exact-hash-update-plan");
            Publish(eventBus, NodalOsCoreEventKind.DryRunPlanCreated, registryEntry, plan, "Exact-hash atomic update prepared.");

            var stepId = plan.Steps.Single().Id;
            runtime.BeginStep(stepId, "fixture-file-update-step");
            runtime.RecordToolCallStarted(stepId, "filesystem.write.safe", "fixture-file-update-call-start");
            var update = updateAction.Execute(
                new NodalOsTestOwnedFileUpdateRequest(
                    OperationId: "fixture-mission-update-handoff",
                    ApprovalDecisionId: approvalDecision.DecisionId,
                    TestOwnedRootPath: root,
                    RelativePath: seed.RelativePath,
                    ExpectedCurrentSha256: seed.ContentSha256,
                    ReplacementContent: BuildUpdatedContent(plan.MissionId, seed.ContentSha256)),
                cancellationToken);
            if (!update.Success || update.Evidence is null || update.RestorePlan is null)
            {
                registryEntry = registry.Transition(
                    registryEntry.RegistryEntryId,
                    NodalOsExecutionRegistryState.Failed,
                    "fixture-file-update",
                    update.SafeMessage);
                Publish(eventBus, NodalOsCoreEventKind.ExecutionFailed, registryEntry, plan, "Exact-hash fixture update failed closed.");
                runtime.FailStep(stepId, update.SafeMessage, "fixture-file-update-failed");
                throw new InvalidOperationException(update.SafeMessage);
            }

            runtime.RecordToolCallCompleted(
                stepId,
                "filesystem.write.safe",
                "fixture-file-update-call-complete",
                [update.Evidence.EvidenceId]);
            registryEntry = registry.Transition(
                registryEntry.RegistryEntryId,
                NodalOsExecutionRegistryState.Completed,
                "fixture-file-update",
                "Exact-hash fixture update was snapshotted, replaced atomically and verified.",
                verificationReportRef: "fixture-file-update-sha256-verified",
                evidenceRefs: [update.Evidence]);
            Publish(
                eventBus,
                NodalOsCoreEventKind.ExecutionCompleted,
                registryEntry,
                plan,
                "Exact-hash test-owned file update completed and verified.",
                evidenceRefs: [update.Evidence]);
            Publish(
                eventBus,
                NodalOsCoreEventKind.EvidenceAttached,
                registryEntry,
                plan,
                "Original and updated SHA-256 plus guarded restore plan were attached before mission completion.",
                new Dictionary<string, string>
                {
                    ["restorePlanId"] = update.RestorePlan.RestorePlanId,
                    ["rollbackRequiresExactCurrentHash"] = update.RestorePlan.RequiresExactCurrentHash.ToString()
                },
                [update.Evidence]);

            runtime.MarkReadyForVerification(
                stepId,
                "fixture-file-update-ready-for-verification",
                [update.Evidence.EvidenceId]);
            runtime.VerifyStep(
                stepId,
                passed: true,
                "fixture-file-update-verified",
                [update.Evidence.EvidenceId]);

            var cleanup = createAction.CleanupOwnedRoot(root, update.RootFingerprint);
            if (!cleanup.Success || !cleanup.RootRemoved)
                throw new InvalidOperationException(cleanup.SafeMessage);
            cleanupCompleted = true;

            var runtimeTimeline = new NodalOsMissionEventProjectionService()
                .Project(runtime.Events, new NodalOsCoreEventBus());
            var timeline = eventBus.ToTimelineProjections()
                .Concat(runtimeTimeline)
                .GroupBy(item => item.EventId, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(item => item.CreatedAt)
                .ToArray();

            var workspaceService = new NodalOsWorkspaceService();
            var workspace = workspaceService.CreateActiveReadOnlyWorkspace() with
            {
                ActiveMissionRefs = [plan.MissionId],
                TimelineRefs = timeline.Select(item => item.ProjectionId).ToArray(),
                EvidenceRefs = [update.Evidence],
                AllowedCapabilitiesRedacted = ["filesystem.write.safe exact-hash test-owned fixture only"],
                DisabledCapabilitiesRedacted = ["filesystem.write user workspace", "unconditional overwrite", "general patch", "shell", "network"],
                NextSafeStepsRedacted = ["Review exact-hash update evidence and restore plan.", "Keep user workspace update disabled."],
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var userWorkspaceJail = workspaceService.CreatePathJailBinding(workspace.WorkspaceId);
            var missionBinding = new NodalOsWorkspaceMissionBindingService()
                .CreateBinding(workspace, plan.MissionId) with
                {
                    MissionTitleRedacted = plan.Goal,
                    MissionSummaryRedacted = "Bounded file-update validation uses only a unique test-owned temporary root.",
                    ActiveTimelineRefs = workspace.TimelineRefs,
                    ActiveEvidenceRefs = workspace.EvidenceRefs,
                    AllowedCapabilitiesRedacted = ["filesystem.write.safe exact-hash test-owned fixture only"],
                    DisabledCapabilitiesRedacted = ["user workspace write", "unconditional overwrite", "general patch", "network", "browser", "terminal", "production authority"]
                };
            EnsureValid(new NodalOsWorkspaceValidator().ValidateWorkspace(workspace));
            EnsureValid(new NodalOsWorkspaceValidator().ValidatePathJailBinding(userWorkspaceJail));
            EnsureValid(new NodalOsWorkspaceMissionValidator().ValidateMissionBinding(missionBinding));

            return new NodalOsTestOwnedFileUpdateMissionResult(
                Workspace: workspace,
                UserWorkspacePathJail: userWorkspaceJail,
                MissionBinding: missionBinding,
                Plan: plan,
                Mission: runtime.State,
                ResumeCard: runtime.ResumeCard,
                RegistryEntry: registryEntry,
                MissionAuthorizationCard: approvalCard,
                MissionAuthorizationDecision: approvalDecision,
                SeedCreateAction: seed,
                FileUpdateAction: update,
                FixtureCleanup: cleanup,
                Timeline: timeline,
                MissionAuthorizationReused: true,
                AdditionalStepApprovalRequested: runtime.Events.Any(item => item.Kind == MissionEventKind.ApprovalRequired),
                TestOwnedFilesystemTouched: true,
                UserWorkspaceFilesystemTouched: false,
                NetworkUsed: false,
                ProductAuthorityGranted: false);
        }
        finally
        {
            if (!cleanupCompleted && rootFingerprint is not null)
                createAction.CleanupOwnedRoot(root, rootFingerprint);
        }
    }

    private static MissionPlan CreatePlan() => new(
        "fixture-file-update-mission",
        1,
        DateTimeOffset.UtcNow,
        "Update and verify one existing test-owned handoff file under an exact SHA-256 precondition without touching the user workspace.",
        [
            new MissionStep(
                "fixture-update-handoff",
                null,
                "Snapshot, replace atomically and verify one test-owned Markdown handoff.",
                MissionExecutionSurface.Filesystem,
                ["filesystem.write.safe"],
                [
                    new MissionExpectedEvidence("file-before-sha256", "SHA-256 of the exact pre-update file"),
                    new MissionExpectedEvidence("file-after-sha256", "SHA-256 of the verified updated file"),
                    new MissionExpectedEvidence("restore-plan", "Guarded restore plan bound to root, target and hashes")
                ],
                MissionRiskLevel.Medium,
                false,
                [],
                MissionStepStatus.Pending,
                0,
                null,
                [])
        ],
        MissionStatus.Active);

    private static string BuildOriginalContent() => """
        # NODAL OS Test-Owned Handoff

        Status: draft
        Evidence: pending
        """;

    private static string BuildUpdatedContent(string missionId, string originalSha256) => $"""
        # NODAL OS Verified Test-Owned Handoff

        Mission: {missionId}
        Operation: exact-hash update
        Original SHA-256: {originalSha256}
        Snapshot: verified before replacement
        Replacement: atomic
        User workspace touched: no
        Verification: updated SHA-256 before mission completion
        """;

    private static NodalOsCoreEvent Publish(
        NodalOsCoreEventBus eventBus,
        NodalOsCoreEventKind kind,
        NodalOsExecutionRegistryEntry entry,
        MissionPlan plan,
        string summary,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyList<NodalOsEvidenceBridgeRef>? evidenceRefs = null)
    {
        var coreEvent = new NodalOsCoreEvent
        {
            EventId = $"fixture-update-{entry.RequestId}-{kind}-{entry.Transitions.Count}",
            Kind = kind,
            ExecutionRegistryEntryId = entry.RegistryEntryId,
            ExecutionRequestId = entry.RequestId,
            MissionId = plan.MissionId,
            TaskId = plan.Steps.Single().Id,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            MetadataRedacted = metadata ?? new Dictionary<string, string>(),
            EvidenceRefs = evidenceRefs ?? entry.EvidenceRefs,
            HumanSummaryRedacted = summary,
            TechnicalSummaryRedacted = kind.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        EnsureValid(eventBus.Publish(coreEvent));
        return coreEvent;
    }

    private static NodalOsEvidenceBridgeRef Evidence(
        string evidenceId,
        string kind,
        NodalOsEvidenceBridgeUseKind useKind) => new()
        {
            EvidenceId = evidenceId,
            Kind = kind,
            SourceKind = NodalOsEvidenceBridgeSourceKind.AgentOperation,
            UseKind = useKind,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            Provenance = "Bounded exact-hash test-owned file update mission.",
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static void EnsureValid(NodalOsCoreRuntimeValidationResult result)
    {
        if (!result.IsValid)
            throw new InvalidOperationException(string.Join(" | ", result.Errors));
    }
}
