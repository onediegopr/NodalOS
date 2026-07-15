using System.Security.Cryptography;
using System.Text;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.AgentOperations.Core.Runtime;

public sealed record NodalOsTestOwnedFileCreateMissionSnapshot(
    bool LocalDevOnly,
    bool ReadOnlySurface,
    bool SecretsExcluded,
    string WorkspaceId,
    string MissionId,
    string MissionStatus,
    string RegistryState,
    string ApprovalStatus,
    string FileCreateState,
    bool FileCreateVerified,
    string RelativePath,
    string? ContentSha256,
    long BytesWritten,
    bool MissionAuthorizationReused,
    bool AdditionalStepApprovalRequested,
    bool TestOwnedFilesystemTouched,
    bool TestOwnedFixtureCleaned,
    bool UserWorkspaceFilesystemTouched,
    bool NetworkUsed,
    bool ProductAuthorityGranted,
    int TimelineCount,
    int EvidenceCount);

public sealed record NodalOsTestOwnedFileCreateMissionResult(
    NodalOsWorkspaceLocalModel Workspace,
    NodalOsPathJailBinding UserWorkspacePathJail,
    NodalOsWorkspaceMissionBinding MissionBinding,
    MissionPlan Plan,
    MissionRuntimeState Mission,
    MissionResumeCard ResumeCard,
    NodalOsExecutionRegistryEntry RegistryEntry,
    NodalOsApprovalCard MissionAuthorizationCard,
    NodalOsApprovalDecision MissionAuthorizationDecision,
    NodalOsTestOwnedFileCreateResult FileCreateAction,
    NodalOsTestOwnedFixtureCleanupResult FixtureCleanup,
    IReadOnlyList<NodalOsCoreTimelineProjection> Timeline,
    bool MissionAuthorizationReused,
    bool AdditionalStepApprovalRequested,
    bool TestOwnedFilesystemTouched,
    bool UserWorkspaceFilesystemTouched,
    bool NetworkUsed,
    bool ProductAuthorityGranted)
{
    public NodalOsTestOwnedFileCreateMissionSnapshot ToSnapshot() => new(
        LocalDevOnly: true,
        ReadOnlySurface: true,
        SecretsExcluded: true,
        WorkspaceId: Workspace.WorkspaceId,
        MissionId: Mission.MissionId,
        MissionStatus: Mission.Status.ToString(),
        RegistryState: RegistryEntry.State.ToString(),
        ApprovalStatus: MissionAuthorizationDecision.DecisionKind.ToString(),
        FileCreateState: FileCreateAction.Decision.ToString(),
        FileCreateVerified: FileCreateAction.Verified,
        RelativePath: FileCreateAction.RelativePath,
        ContentSha256: FileCreateAction.ContentSha256,
        BytesWritten: FileCreateAction.BytesWritten,
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

public sealed class NodalOsTestOwnedFileCreateMissionScenario
{
    public async ValueTask<NodalOsTestOwnedFileCreateMissionResult> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var fileAction = new NodalOsTestOwnedFileCreateAction();
        var root = Path.Combine(
            NodalOsTestOwnedFileCreateAction.AllowedBaseRoot,
            "run-" + Guid.NewGuid().ToString("N"));
        string? rootFingerprint = null;
        var cleanupCompleted = false;

        try
        {
            var plan = CreatePlan();
            var runtime = new LightweightMissionRuntime(
                plan,
                new MissionAuthorizationScope(
                    plan.MissionId,
                    new HashSet<string>(["filesystem.write.safe"], StringComparer.Ordinal),
                    new HashSet<MissionExecutionSurface>([MissionExecutionSurface.Filesystem]),
                    MissionRiskLevel.Medium),
                "fixture-file-create-run");
            runtime.Start("fixture-file-create-start");

            var eventBus = new NodalOsCoreEventBus();
            var registry = new NodalOsExecutionRegistry();
            var seedEvidence = Evidence(
                "evidence-file-create-mission-scope",
                "mission-scope-approval-record",
                NodalOsEvidenceBridgeUseKind.AuditTrail);
            var registryEntry = registry.Register(new NodalOsExecutionRequest
            {
                RequestId = "fixture-file-create-request",
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
                Summary = "Create one verified text file inside a unique test-owned fixture root.",
                EvidenceRefs = [seedEvidence],
                CreatedAt = DateTimeOffset.UtcNow
            });
            Publish(eventBus, NodalOsCoreEventKind.ExecutionRequestRegistered, registryEntry, plan, "Create-only fixture request registered.");

            registryEntry = registry.Transition(
                registryEntry.RegistryEntryId,
                NodalOsExecutionRegistryState.PolicyEvaluated,
                "fixture-file-policy",
                "The target is a unique test-owned root; operation is create-only, atomic and no-overwrite.",
                policyDecisionRef: "fixture-create-only-policy");
            Publish(eventBus, NodalOsCoreEventKind.PolicyGateEvaluated, registryEntry, plan, "Create-only test fixture policy passed.");

            registryEntry = registry.Transition(
                registryEntry.RegistryEntryId,
                NodalOsExecutionRegistryState.ApprovalRequired,
                "fixture-approval-center",
                "One mission-level approval is required before the bounded file action.");
            var approvalEvent = Publish(
                eventBus,
                NodalOsCoreEventKind.ApprovalRequired,
                registryEntry,
                plan,
                "Mission-level approval required once for the create-only fixture output.");

            var approvalService = new NodalOsApprovalCenterService();
            var approvalCard = approvalService.CreateApprovalCard(
                registryEntry,
                approvalEvent,
                NodalOsApprovalSeverity.Medium,
                NodalOsApprovalActionKind.ExternalMutationFuture,
                "Create one new Markdown report inside a unique test-owned temporary root.",
                "No overwrite, no user workspace access, no shell, no network and SHA-256 verification are enforced.",
                ["test-owned output/verified-handoff.md"]) with
            {
                RollbackPlanRedacted = "Remove only the unique test-owned fixture root after verification.",
                EvidencePlanRedacted = "Record the created file SHA-256 before mission completion."
            };
            EnsureValid(new NodalOsApprovalTimelineEvidenceValidator().ValidateApprovalCard(approvalCard));
            var approvalDecision = approvalService.CreateDecision(
                approvalCard,
                NodalOsApprovalDecisionKind.Approve,
                "fixture-operator-preauthorized",
                "Approved once for this mission and create-only test fixture scope.");
            EnsureValid(new NodalOsApprovalTimelineEvidenceValidator().ValidateApprovalDecision(approvalDecision));

            registryEntry = registry.Transition(
                registryEntry.RegistryEntryId,
                NodalOsExecutionRegistryState.Approved,
                "fixture-operator-preauthorized",
                "Mission-level create-only scope approved.",
                approvalRef: approvalDecision.DecisionId);
            Publish(
                eventBus,
                NodalOsCoreEventKind.ApprovalGranted,
                registryEntry,
                plan,
                "Mission-level approval granted; the bounded create-only action continues without another prompt.",
                new Dictionary<string, string>
                {
                    ["approvalCardId"] = approvalCard.ApprovalCardId,
                    ["approvalDecisionId"] = approvalDecision.DecisionId
                });

            registryEntry = registry.Transition(
                registryEntry.RegistryEntryId,
                NodalOsExecutionRegistryState.DryRunPlanned,
                "fixture-file-create",
                "Create-only atomic write planned inside the dedicated test fixture boundary.",
                dryRunRef: "fixture-create-only-plan");
            Publish(eventBus, NodalOsCoreEventKind.DryRunPlanCreated, registryEntry, plan, "Create-only atomic write prepared.");

            rootFingerprint = FingerprintPath(root);
            var stepId = plan.Steps.Single().Id;
            runtime.BeginStep(stepId, "fixture-file-create-step");
            runtime.RecordToolCallStarted(stepId, "filesystem.write.safe", "fixture-file-create-call-start");
            var fileResult = fileAction.Execute(
                new NodalOsTestOwnedFileCreateRequest(
                    OperationId: "fixture-mission-handoff",
                    ApprovalDecisionId: approvalDecision.DecisionId,
                    TestOwnedRootPath: root,
                    RelativePath: "output/verified-handoff.md",
                    Content: BuildContent(plan.MissionId)),
                cancellationToken);
            if (!fileResult.Success || fileResult.Evidence is null)
            {
                registryEntry = registry.Transition(
                    registryEntry.RegistryEntryId,
                    NodalOsExecutionRegistryState.Failed,
                    "fixture-file-create",
                    fileResult.SafeMessage);
                Publish(eventBus, NodalOsCoreEventKind.ExecutionFailed, registryEntry, plan, "Create-only fixture action failed closed.");
                runtime.FailStep(stepId, fileResult.SafeMessage, "fixture-file-create-failed");
                throw new InvalidOperationException(fileResult.SafeMessage);
            }

            runtime.RecordToolCallCompleted(
                stepId,
                "filesystem.write.safe",
                "fixture-file-create-call-complete",
                [fileResult.Evidence.EvidenceId]);
            registryEntry = registry.Transition(
                registryEntry.RegistryEntryId,
                NodalOsExecutionRegistryState.Completed,
                "fixture-file-create",
                "Create-only fixture file was created atomically and verified.",
                verificationReportRef: "fixture-file-sha256-verified",
                evidenceRefs: [fileResult.Evidence]);
            Publish(
                eventBus,
                NodalOsCoreEventKind.ExecutionCompleted,
                registryEntry,
                plan,
                "Create-only test-owned file completed and verified.",
                evidenceRefs: [fileResult.Evidence]);
            Publish(
                eventBus,
                NodalOsCoreEventKind.EvidenceAttached,
                registryEntry,
                plan,
                "Verified file SHA-256 attached before mission completion.",
                evidenceRefs: [fileResult.Evidence]);

            runtime.MarkReadyForVerification(
                stepId,
                "fixture-file-ready-for-verification",
                [fileResult.Evidence.EvidenceId]);
            runtime.VerifyStep(
                stepId,
                passed: true,
                "fixture-file-verified",
                [fileResult.Evidence.EvidenceId]);

            var cleanup = fileAction.CleanupOwnedRoot(root, fileResult.RootFingerprint);
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
                EvidenceRefs = [fileResult.Evidence],
                AllowedCapabilitiesRedacted = ["filesystem.write.safe test-owned fixture only"],
                DisabledCapabilitiesRedacted = ["filesystem.write user workspace", "overwrite", "shell", "network"],
                NextSafeStepsRedacted = ["Review the verified create-only evidence.", "Keep user workspace mutation disabled."],
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var userWorkspaceJail = workspaceService.CreatePathJailBinding(workspace.WorkspaceId);
            var missionBinding = new NodalOsWorkspaceMissionBindingService()
                .CreateBinding(workspace, plan.MissionId) with
                {
                    MissionTitleRedacted = plan.Goal,
                    MissionSummaryRedacted = "Bounded file-create validation uses only a unique test-owned temporary root.",
                    ActiveTimelineRefs = workspace.TimelineRefs,
                    ActiveEvidenceRefs = workspace.EvidenceRefs,
                    AllowedCapabilitiesRedacted = ["filesystem.write.safe test-owned fixture only"],
                    DisabledCapabilitiesRedacted = ["user workspace write", "overwrite", "network", "browser", "terminal", "production authority"]
                };
            EnsureValid(new NodalOsWorkspaceValidator().ValidateWorkspace(workspace));
            EnsureValid(new NodalOsWorkspaceValidator().ValidatePathJailBinding(userWorkspaceJail));
            EnsureValid(new NodalOsWorkspaceMissionValidator().ValidateMissionBinding(missionBinding));

            return new NodalOsTestOwnedFileCreateMissionResult(
                Workspace: workspace,
                UserWorkspacePathJail: userWorkspaceJail,
                MissionBinding: missionBinding,
                Plan: plan,
                Mission: runtime.State,
                ResumeCard: runtime.ResumeCard,
                RegistryEntry: registryEntry,
                MissionAuthorizationCard: approvalCard,
                MissionAuthorizationDecision: approvalDecision,
                FileCreateAction: fileResult,
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
                fileAction.CleanupOwnedRoot(root, rootFingerprint);
        }
    }

    private static MissionPlan CreatePlan() =>
        new(
            "fixture-file-create-mission",
            1,
            DateTimeOffset.UtcNow,
            "Create and verify one test-owned handoff file without touching the user workspace.",
            [
                new MissionStep(
                    "fixture-create-handoff",
                    null,
                    "Create one verified Markdown handoff in the dedicated test fixture root.",
                    MissionExecutionSurface.Filesystem,
                    ["filesystem.write.safe"],
                    [new MissionExpectedEvidence("file-sha256", "SHA-256 of the newly created test-owned file")],
                    MissionRiskLevel.Medium,
                    false,
                    [],
                    MissionStepStatus.Pending,
                    0,
                    null,
                    [])
            ],
            MissionStatus.Active);

    private static string FingerprintPath(string path) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(path))))
            .ToLowerInvariant();

    private static string BuildContent(string missionId) =>
        $"""
        # NODAL OS Verified Test-Owned Handoff

        Mission: {missionId}
        Operation: create-only
        Overwrite: rejected
        User workspace touched: no
        Verification: SHA-256 before mission completion
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
            EventId = $"fixture-file-{entry.RequestId}-{kind}-{entry.Transitions.Count}",
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
        NodalOsEvidenceBridgeUseKind useKind) =>
        new()
        {
            EvidenceId = evidenceId,
            Kind = kind,
            SourceKind = NodalOsEvidenceBridgeSourceKind.AgentOperation,
            UseKind = useKind,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            Provenance = "Bounded test-owned file create mission.",
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static void EnsureValid(NodalOsCoreRuntimeValidationResult result)
    {
        if (!result.IsValid)
            throw new InvalidOperationException(string.Join(" | ", result.Errors));
    }
}
