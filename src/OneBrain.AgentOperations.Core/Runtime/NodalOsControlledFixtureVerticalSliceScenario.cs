using System.Net;
using System.Security.Cryptography;
using System.Text;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Execution;
using OneBrain.Core.Identity;
using OneBrain.Core.Models;
using OneBrain.Core.Selectors;

namespace OneBrain.AgentOperations.Core.Runtime;

public sealed record NodalOsMvpVerticalSliceInspectorSnapshot(
    bool LocalDevOnly,
    bool ReadOnly,
    bool SecretsExcluded,
    string WorkspaceId,
    string MissionId,
    string MissionStatus,
    string RegistryState,
    string ApprovalStatus,
    string ControlledActionState,
    bool ControlledActionVerified,
    int TimelineCount,
    int EvidenceCount,
    string HandoffPackId,
    bool MissionAuthorizationReused,
    bool AdditionalStepApprovalRequested,
    bool RealFilesystemTouched,
    bool NetworkUsed,
    bool ProductAuthorityGranted,
    RuntimeInspectorSnapshot Runtime);

public sealed record NodalOsControlledFixtureVerticalSliceResult(
    NodalOsWorkspaceLocalModel Workspace,
    NodalOsPathJailBinding PathJail,
    NodalOsWorkspaceMissionBinding MissionBinding,
    NodalOsSelectiveRuntimeFixtureResult Runtime,
    NodalOsExecutionRegistryEntry RegistryEntry,
    NodalOsApprovalCard MissionAuthorizationCard,
    NodalOsApprovalDecision MissionAuthorizationDecision,
    SafeExecutionResult ControlledAction,
    NodalOsEvidenceBridgeRef ControlledActionEvidence,
    IReadOnlyList<NodalOsCoreTimelineProjection> Timeline,
    NodalOsPlannerHandoffPack Handoff,
    NodalOsPlannerHandoffRender HandoffRender,
    bool MissionAuthorizationReused,
    bool AdditionalStepApprovalRequested,
    bool RealFilesystemTouched,
    bool NetworkUsed,
    bool ProductAuthorityGranted)
{
    public NodalOsMvpVerticalSliceInspectorSnapshot ToInspectorSnapshot() => new(
        LocalDevOnly: true,
        ReadOnly: true,
        SecretsExcluded: true,
        WorkspaceId: Workspace.WorkspaceId,
        MissionId: Runtime.Plan.MissionId,
        MissionStatus: Runtime.Mission.Status.ToString(),
        RegistryState: RegistryEntry.State.ToString(),
        ApprovalStatus: MissionAuthorizationDecision.DecisionKind.ToString(),
        ControlledActionState: ControlledAction.FinalState.ToString(),
        ControlledActionVerified: ControlledAction.VerificationResult?.Success == true,
        TimelineCount: Timeline.Count,
        EvidenceCount: Timeline.SelectMany(item => item.EvidenceRefs).Select(item => item.EvidenceId).Distinct(StringComparer.Ordinal).Count(),
        HandoffPackId: Handoff.HandoffPackId,
        MissionAuthorizationReused: MissionAuthorizationReused,
        AdditionalStepApprovalRequested: AdditionalStepApprovalRequested,
        RealFilesystemTouched: RealFilesystemTouched,
        NetworkUsed: NetworkUsed,
        ProductAuthorityGranted: ProductAuthorityGranted,
        Runtime: Runtime.Inspector);
}

public sealed class NodalOsControlledFixtureVerticalSliceScenario
{
    public async ValueTask<NodalOsControlledFixtureVerticalSliceResult> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var eventBus = new NodalOsCoreEventBus();
        var registry = new NodalOsExecutionRegistry();
        NodalOsExecutionRegistryEntry? registryEntry = null;
        NodalOsApprovalCard? approvalCard = null;
        NodalOsApprovalDecision? approvalDecision = null;
        SafeExecutionResult? controlledAction = null;
        NodalOsEvidenceBridgeRef? controlledEvidence = null;

        ValueTask<IReadOnlyList<string>> ExecuteControlledStep(
            LightweightMissionRuntime missionRuntime,
            string stepId,
            CancellationToken stepCancellationToken)
        {
            var missionId = missionRuntime.Plan.MissionId;
            var seedEvidence = CreateEvidenceRef(
                "evidence-mission-scope-fixture",
                "mission-scope-approval-record",
                NodalOsEvidenceBridgeUseKind.AuditTrail);
            registryEntry = registry.Register(new NodalOsExecutionRequest
            {
                RequestId = "fixture-controlled-observation",
                MissionId = missionId,
                TaskId = stepId,
                RunId = missionRuntime.State.RunId,
                RequestedBy = "fixture-operator-preauthorized",
                ActorKind = NodalOsExecutionActorKind.HumanOperator,
                SourceKind = NodalOsExecutionSourceKind.MissionControl,
                RuntimeExecutionAllowed = false,
                RuntimeExecutionDeferred = true,
                RequiresGlobalPolicyEvaluation = true,
                RequiresEvidenceRedaction = true,
                Summary = "Observe one fixture-owned value through SafeExecutionFsm.",
                EvidenceRefs = [seedEvidence],
                CreatedAt = DateTimeOffset.UtcNow
            });
            Publish(
                eventBus,
                NodalOsCoreEventKind.ExecutionRequestRegistered,
                registryEntry,
                missionId,
                stepId,
                "Controlled fixture observation registered.");

            registryEntry = registry.Transition(
                registryEntry.RegistryEntryId,
                NodalOsExecutionRegistryState.PolicyEvaluated,
                "fixture-policy-gate",
                "Read-only fixture observation is inside the authorized mission scope.",
                policyDecisionRef: "fixture-policy-read-only");
            Publish(
                eventBus,
                NodalOsCoreEventKind.PolicyGateEvaluated,
                registryEntry,
                missionId,
                stepId,
                "Policy allowed one read-only fixture observation within the current mission scope.");

            registryEntry = registry.Transition(
                registryEntry.RegistryEntryId,
                NodalOsExecutionRegistryState.ApprovalRequired,
                "fixture-approval-center",
                "Mission-level authorization is required once before the controlled action.");
            var approvalRequiredEvent = Publish(
                eventBus,
                NodalOsCoreEventKind.ApprovalRequired,
                registryEntry,
                missionId,
                stepId,
                "Mission-level authorization required for the controlled fixture observation.");

            var approvalService = new NodalOsApprovalCenterService();
            approvalCard = approvalService.CreateApprovalCard(
                registryEntry,
                approvalRequiredEvent,
                NodalOsApprovalSeverity.Low,
                NodalOsApprovalActionKind.Observation,
                "Read one fixture-owned value without mutation or external IO.",
                "The current mission scope permits one read-only observation through the controlled boundary.",
                ["fixture-owned observation target"]);
            EnsureValid(new NodalOsApprovalTimelineEvidenceValidator().ValidateApprovalCard(approvalCard));
            approvalDecision = approvalService.CreateDecision(
                approvalCard,
                NodalOsApprovalDecisionKind.Approve,
                "fixture-operator-preauthorized",
                "Approved once for this mission, workspace and read-only capability scope.");
            EnsureValid(new NodalOsApprovalTimelineEvidenceValidator().ValidateApprovalDecision(approvalDecision));

            registryEntry = registry.Transition(
                registryEntry.RegistryEntryId,
                NodalOsExecutionRegistryState.Approved,
                "fixture-operator-preauthorized",
                "Mission-level authorization approved.",
                approvalRef: approvalDecision.DecisionId);
            Publish(
                eventBus,
                NodalOsCoreEventKind.ApprovalGranted,
                registryEntry,
                missionId,
                stepId,
                "Mission-level authorization approved; ordinary read-only step continues without another prompt.",
                new Dictionary<string, string>
                {
                    ["approvalCardId"] = approvalCard.ApprovalCardId,
                    ["approvalDecisionId"] = approvalDecision.DecisionId
                });

            registryEntry = registry.Transition(
                registryEntry.RegistryEntryId,
                NodalOsExecutionRegistryState.DryRunPlanned,
                "fixture-safe-execution",
                "Fixture executor performs no external IO.",
                dryRunRef: "fixture-safe-read-plan");
            Publish(
                eventBus,
                NodalOsCoreEventKind.DryRunPlanCreated,
                registryEntry,
                missionId,
                stepId,
                "Controlled read-only fixture action prepared.");

            missionRuntime.RecordToolCallStarted(
                stepId,
                "filesystem.read",
                "fixture-controlled-read-start");
            controlledAction = ExecuteControlledObservation(approvalDecision, stepCancellationToken);
            controlledEvidence = CreateControlledActionEvidence(controlledAction);
            if (!controlledAction.Success)
            {
                registryEntry = registry.Transition(
                    registryEntry.RegistryEntryId,
                    NodalOsExecutionRegistryState.Failed,
                    "safe-execution-fsm",
                    controlledAction.BlockReason,
                    evidenceRefs: [controlledEvidence]);
                Publish(
                    eventBus,
                    NodalOsCoreEventKind.ExecutionFailed,
                    registryEntry,
                    missionId,
                    stepId,
                    "Controlled fixture observation failed closed.",
                    evidenceRefs: [controlledEvidence]);
                missionRuntime.FailStep(stepId, controlledAction.BlockReason, "fixture-controlled-read-failed");
                throw new InvalidOperationException(controlledAction.BlockReason);
            }

            registryEntry = registry.Transition(
                registryEntry.RegistryEntryId,
                NodalOsExecutionRegistryState.Completed,
                "safe-execution-fsm",
                "Controlled fixture observation verified.",
                verificationReportRef: "fixture-verification-passed",
                evidenceRefs: [controlledEvidence]);
            Publish(
                eventBus,
                NodalOsCoreEventKind.ExecutionCompleted,
                registryEntry,
                missionId,
                stepId,
                "Controlled fixture observation completed and verified.",
                evidenceRefs: [controlledEvidence]);
            Publish(
                eventBus,
                NodalOsCoreEventKind.EvidenceAttached,
                registryEntry,
                missionId,
                stepId,
                "SafeExecutionFsm transition digest attached as verification evidence.",
                evidenceRefs: [controlledEvidence]);
            missionRuntime.RecordToolCallCompleted(
                stepId,
                "filesystem.read",
                "fixture-controlled-read-complete",
                [controlledEvidence.EvidenceId]);

            return ValueTask.FromResult<IReadOnlyList<string>>([controlledEvidence.EvidenceId]);
        }

        var runtime = await new NodalOsSelectiveRuntimeFixtureScenario()
            .RunAsync(ExecuteControlledStep, cancellationToken)
            .ConfigureAwait(false);
        var finalRegistryEntry = registryEntry
            ?? throw new InvalidOperationException("Controlled fixture registry entry was not created.");
        var finalApprovalCard = approvalCard
            ?? throw new InvalidOperationException("Controlled fixture approval card was not created.");
        var finalApprovalDecision = approvalDecision
            ?? throw new InvalidOperationException("Controlled fixture approval decision was not created.");
        var finalControlledAction = controlledAction
            ?? throw new InvalidOperationException("Controlled fixture action was not executed.");
        var finalControlledEvidence = controlledEvidence
            ?? throw new InvalidOperationException("Controlled fixture evidence was not created.");

        var controlledTimeline = eventBus.ToTimelineProjections();
        var timeline = runtime.Timeline
            .Concat(controlledTimeline)
            .GroupBy(item => item.EventId, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(item => item.CreatedAt)
            .ToArray();

        var workspaceService = new NodalOsWorkspaceService();
        var workspace = workspaceService.CreateActiveReadOnlyWorkspace() with
        {
            ActiveMissionRefs = [runtime.Plan.MissionId],
            TimelineRefs = timeline.Select(item => item.ProjectionId).ToArray(),
            EvidenceRefs = timeline.SelectMany(item => item.EvidenceRefs)
                .DistinctBy(item => item.EvidenceId)
                .DefaultIfEmpty(CreateEvidenceRef(
                    "evidence-workspace-runtime-fixture",
                    "workspace-runtime-fixture",
                    NodalOsEvidenceBridgeUseKind.AuditTrail))
                .ToArray(),
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var pathJail = workspaceService.CreatePathJailBinding(workspace.WorkspaceId);
        var missionBinding = new NodalOsWorkspaceMissionBindingService()
            .CreateBinding(workspace, runtime.Plan.MissionId) with
            {
                MissionTitleRedacted = runtime.Plan.Goal,
                MissionSummaryRedacted = "Fixture-safe mission bound to one local read-only workspace.",
                ActiveTimelineRefs = workspace.TimelineRefs,
                ActiveEvidenceRefs = workspace.EvidenceRefs,
                AllowedCapabilitiesRedacted = ["filesystem.read fixture", "model.chat fixture", "verification.run fixture"],
                DisabledCapabilitiesRedacted = ["filesystem.write", "network", "browser.action.execute", "terminal.execute", "production authority"],
                NextSafeStepsRedacted = ["Review verified handoff.", "Provision pinned CloakBrowser binary before live CDP smoke."]
            };

        EnsureValid(new NodalOsWorkspaceValidator().ValidateWorkspace(workspace));
        EnsureValid(new NodalOsWorkspaceValidator().ValidatePathJailBinding(pathJail));
        EnsureValid(new NodalOsWorkspaceMissionValidator().ValidateMissionBinding(missionBinding));

        var handoff = BuildHandoff(
            runtime,
            finalRegistryEntry,
            finalApprovalDecision,
            finalControlledAction,
            finalControlledEvidence,
            timeline);
        var handoffRender = RenderHandoff(handoff);

        return new NodalOsControlledFixtureVerticalSliceResult(
            Workspace: workspace,
            PathJail: pathJail,
            MissionBinding: missionBinding,
            Runtime: runtime,
            RegistryEntry: finalRegistryEntry,
            MissionAuthorizationCard: finalApprovalCard,
            MissionAuthorizationDecision: finalApprovalDecision,
            ControlledAction: finalControlledAction,
            ControlledActionEvidence: finalControlledEvidence,
            Timeline: timeline,
            Handoff: handoff,
            HandoffRender: handoffRender,
            MissionAuthorizationReused: true,
            AdditionalStepApprovalRequested: runtime.ApprovalRequested,
            RealFilesystemTouched: false,
            NetworkUsed: false,
            ProductAuthorityGranted: false);
    }

    private static SafeExecutionResult ExecuteControlledObservation(
        NodalOsApprovalDecision decision,
        CancellationToken cancellationToken)
    {
        var identity = new ElementIdentity(
            "fixture-read-runtime",
            "Text",
            "Fixture evidence value",
            "nodal-fixture-read-target")
        {
            Role = "Text",
            ControlType = "Text",
            AncestorPath = "Window:NODAL OS Fixture > Group:ControlledObservation",
            ClassName = "TextBlock",
            Provenance = Provenance.Uia
        };
        var selector = SelectorEngine.GenerateSelector(identity);
        var digest = ElementFingerprintBuilder.Build(identity);
        var binding = new ApprovalBinding(
            ApprovalDecisionId: decision.DecisionId,
            ApprovedIdentityDigest: digest,
            Selector: selector,
            ActionKind: "read",
            Mode: "mission_scope",
            PolicyVersion: "mvp-fixture-v1",
            EvidenceHash: digest);
        var contract = new RecipeSafetyContract(
            SchemaVersion: 1,
            ContractId: "fixture-controlled-read-contract",
            ActionKind: "read",
            ExpectedIdentity: identity,
            Selector: selector,
            WindowConstraints: new ExecutionWindowConstraints(true, true),
            Reversible: true,
            MaxActions: 1,
            ActionCeiling: ActionCeiling.ReadOnly,
            Provenance: Provenance.Uia,
            TrustLevel: TrustLevel.ProfileVerified,
            ApprovalRef: binding);
        var dispatch = new PatternExecutionRequest(
            ActionKind: "read",
            TargetRef: "fixture:controlled-observation",
            ExpectedTargetName: identity.Name,
            ProcessName: "NODAL OS Fixture",
            WindowTitleContains: "NODAL OS Fixture",
            Selector: selector,
            ExpectedIdentity: identity);
        var fsm = new SafeExecutionFsm(
            new ContractValidator(),
            new ApprovalBindingValidator(),
            new FixtureOwnershipMonitor(),
            new FixtureReadExecutor(identity),
            new FixtureReadVerifier(identity));

        return fsm.Execute(new SafeExecutionRequest(contract, [identity], dispatch, cancellationToken));
    }

    private static NodalOsEvidenceBridgeRef CreateControlledActionEvidence(SafeExecutionResult action)
    {
        var canonical = string.Join("|", action.Ledger.Entries.Select(entry =>
            $"{entry.Sequence}:{entry.Event}:{entry.FromState}:{entry.ToState}:{entry.FailureKind}:{entry.MatchVerdict}"));
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        return CreateEvidenceRef(
            "evidence-safe-execution-fixture",
            "safe-execution-fsm-transition-digest",
            NodalOsEvidenceBridgeUseKind.VerificationSupport,
            digest,
            "fsm:fixture-controlled-read-contract");
    }

    private static NodalOsPlannerHandoffPack BuildHandoff(
        NodalOsSelectiveRuntimeFixtureResult runtime,
        NodalOsExecutionRegistryEntry registryEntry,
        NodalOsApprovalDecision approvalDecision,
        SafeExecutionResult controlledAction,
        NodalOsEvidenceBridgeRef controlledEvidence,
        IReadOnlyList<NodalOsCoreTimelineProjection> timeline) =>
        new()
        {
            HandoffPackId = $"verified-handoff-{runtime.Plan.MissionId}",
            MissionRef = runtime.Plan.MissionId,
            AssignmentRef = runtime.Plan.Steps.Single().Id,
            TaskGraphDraftRefs = [runtime.Plan.MissionId, runtime.Plan.Version.ToString()],
            ReviewSessionRefs = [approvalDecision.DecisionId],
            SelectedBlockersRedacted = ["Pinned CloakBrowser binary remains unavailable for live CDP smoke."],
            OpenQuestionsRedacted = ["When will the audited CloakBrowser binary be provisioned?"],
            MissingReadinessGatesRedacted = ["BLOCKED_EXTERNAL_CLOAKBROWSER_BINARY"],
            EvidenceRefs = timeline.SelectMany(item => item.EvidenceRefs)
                .Select(item => item.EvidenceId)
                .Append(controlledEvidence.EvidenceId)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            TimelineRefs = timeline.Select(item => item.ProjectionId).Distinct(StringComparer.Ordinal).ToArray(),
            ContextRefsRedacted = ["fixture-owned workspace context", "mission-level authorization scope"],
            GuardrailRefs = ["local-first", "read-only-action", "verification-before-completion", "no-network", "no-product-authority"],
            DisclaimersRedacted =
            [
                "Fixture-safe local validation only.",
                "No real filesystem, browser, network, provider or customer data was used.",
                "The handoff is a verified report, not execution authority."
            ],
            WhatWasReviewedRedacted = "Workspace binding, mission plan, one mission-level approval, controlled read-only action, verification, evidence and timeline were reviewed.",
            WhatIsBlockedRedacted = "Live CloakBrowser/CDP smoke remains blocked by the missing pinned binary.",
            WhatNeedsUserDecisionRedacted = "No decision is required for the completed fixture mission. Binary provisioning is the next external dependency.",
            EvidenceRefsOnlyRedacted = "Evidence is included as redacted references and a SafeExecutionFsm transition digest; no raw payload is embedded.",
            WhatIsNotVerifiedRedacted = "Live browser behavior and production runtime remain unverified.",
            WhatCannotExecuteRedacted = "The handoff cannot execute actions or grant production, browser, filesystem, network or release authority.",
            RecommendedNextSafeStepRedacted = "Use this verified fixture as the local product-loop baseline and provision the pinned CloakBrowser binary for live CDP validation.",
            DraftOnly = false,
            IsAuthoritative = false,
            Executable = false,
            PlannerRuntimeUsed = true,
            CallsLlmProvider = false,
            CreatesPrompt = false,
            RuntimeExecutionAllowed = false,
            FilesystemAccessUsed = false,
            VerifiesEvidenceContent = controlledAction.VerificationResult?.Success == true && registryEntry.State == NodalOsExecutionRegistryState.Completed
        };

    private static NodalOsPlannerHandoffRender RenderHandoff(NodalOsPlannerHandoffPack handoff)
    {
        var markdown = $"""
            # NODAL OS Verified Mission Handoff

            ## What was reviewed
            {handoff.WhatWasReviewedRedacted}

            ## What is blocked
            {handoff.WhatIsBlockedRedacted}

            ## What needs user decision
            {handoff.WhatNeedsUserDecisionRedacted}

            ## Evidence
            {handoff.EvidenceRefsOnlyRedacted}

            ## What is not verified
            {handoff.WhatIsNotVerifiedRedacted}

            ## What cannot execute
            {handoff.WhatCannotExecuteRedacted}

            ## Recommended next safe step
            {handoff.RecommendedNextSafeStepRedacted}
            """;
        var html = $"""
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8"><title>NODAL OS Verified Mission Handoff</title></head>
            <body>
              <main data-nodal-os="verified-mission-handoff">
                <h1>NODAL OS Verified Mission Handoff</h1>
                <section><h2>What was reviewed</h2><p>{WebUtility.HtmlEncode(handoff.WhatWasReviewedRedacted)}</p></section>
                <section><h2>What is blocked</h2><p>{WebUtility.HtmlEncode(handoff.WhatIsBlockedRedacted)}</p></section>
                <section><h2>Evidence</h2><p>{WebUtility.HtmlEncode(handoff.EvidenceRefsOnlyRedacted)}</p></section>
                <section><h2>Next safe step</h2><p>{WebUtility.HtmlEncode(handoff.RecommendedNextSafeStepRedacted)}</p></section>
              </main>
            </body>
            </html>
            """;
        return new NodalOsPlannerHandoffRender
        {
            HandoffPackId = handoff.HandoffPackId,
            MarkdownRedacted = markdown,
            HtmlRedacted = html,
            Deterministic = true,
            ContainsRawPayload = false,
            ContainsExternalResource = false
        };
    }

    private static NodalOsCoreEvent Publish(
        NodalOsCoreEventBus eventBus,
        NodalOsCoreEventKind kind,
        NodalOsExecutionRegistryEntry entry,
        string missionId,
        string taskId,
        string summary,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyList<NodalOsEvidenceBridgeRef>? evidenceRefs = null)
    {
        var coreEvent = new NodalOsCoreEvent
        {
            EventId = $"fixture-core-{entry.RequestId}-{kind}-{entry.Transitions.Count}",
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
            HumanSummaryRedacted = summary,
            TechnicalSummaryRedacted = kind.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        var validation = eventBus.Publish(coreEvent);
        EnsureValid(validation);
        return coreEvent;
    }

    private static NodalOsEvidenceBridgeRef CreateEvidenceRef(
        string evidenceId,
        string kind,
        NodalOsEvidenceBridgeUseKind useKind,
        string? hash = null,
        string? ledgerRef = null) =>
        new()
        {
            EvidenceId = evidenceId,
            Kind = kind,
            Ref = null,
            Hash = hash,
            SourceKind = useKind == NodalOsEvidenceBridgeUseKind.VerificationSupport
                ? NodalOsEvidenceBridgeSourceKind.VerificationGate
                : NodalOsEvidenceBridgeSourceKind.AgentOperation,
            UseKind = useKind,
            Authority = useKind == NodalOsEvidenceBridgeUseKind.VerificationSupport
                ? NodalOsEvidenceBridgeAuthority.SupportsVerificationOnly
                : NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            LedgerRef = ledgerRef,
            Provenance = "Fixture-safe controlled MVP vertical slice.",
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static void EnsureValid(NodalOsCoreRuntimeValidationResult validation)
    {
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join(" | ", validation.Errors));
    }

    private sealed class FixtureOwnershipMonitor : IDesktopOwnershipMonitor
    {
        private readonly OwnershipSnapshot snapshot = new(0, 1, "NODAL OS Fixture", DateTimeOffset.UtcNow);
        public OwnershipSnapshot Capture() => snapshot;
        public bool HumanInputSince(OwnershipSnapshot baseline) => false;
        public bool ForegroundChanged(OwnershipSnapshot baseline) => false;
    }

    private sealed class FixtureReadExecutor(ElementIdentity identity) : IUiaPatternExecutor
    {
        public PatternExecutionResult Invoke(PatternExecutionRequest request) =>
            new(
                Success: request.ActionKind == "read",
                FailureKind: request.ActionKind == "read" ? null : FailureKind.PolicyDenied,
                Reasons: request.ActionKind == "read" ? ["fixture read completed"] : ["only read is allowed"],
                ObservedIdentity: identity,
                WindowFound: true,
                TargetVisible: true,
                TargetName: identity.Name,
                ObservedActions: 1,
                Signals: ["fixture.read=true"],
                InvokeTimeIdentityChecked: true,
                InvokeTimeIdentityVerdict: "Same",
                InvokeTimeIdentityReason: "fixture identity matched",
                ExpectedIdentityDigest: ElementFingerprintBuilder.Build(identity),
                ObservedIdentityDigest: ElementFingerprintBuilder.Build(identity));
    }

    private sealed class FixtureReadVerifier(ElementIdentity identity) : IStepVerifier
    {
        public StepVerificationResult Verify(RecipeSafetyContract contract, PatternExecutionResult dispatchResult) =>
            dispatchResult.Success && dispatchResult.ObservedIdentity is not null
                ? new StepVerificationResult(true, null, "Same", ["fixture read result verified"], identity)
                : new StepVerificationResult(false, FailureKind.Unverified, "Different", ["fixture read result could not be verified"], dispatchResult.ObservedIdentity);
    }
}
