using System.Net;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core.Workspace;

namespace OneBrain.AgentOperations.Core.Runtime;

public sealed record NodalOsBoundedWorkspaceMissionResult(
    string Decision,
    BoundedWorkspaceScanResult Scan,
    MissionPlan Plan,
    MissionRuntimeState Mission,
    MissionResumeCard ResumeCard,
    IReadOnlyList<NodalOsCoreTimelineProjection> Timeline,
    NodalOsPlannerHandoffPack Handoff,
    NodalOsPlannerHandoffRender HandoffRender,
    bool Completed,
    bool VerificationPassed,
    bool NetworkUsed,
    bool FilesystemMutationAllowed,
    bool ProductAuthorityGranted);

public sealed class NodalOsBoundedWorkspaceMissionScenario
{
    private readonly BoundedWorkspaceUnderstandingService _understanding;

    public NodalOsBoundedWorkspaceMissionScenario()
        : this(new BoundedWorkspaceUnderstandingService())
    {
    }

    public NodalOsBoundedWorkspaceMissionScenario(BoundedWorkspaceUnderstandingService understanding) =>
        _understanding = understanding ?? throw new ArgumentNullException(nameof(understanding));

    public async ValueTask<NodalOsBoundedWorkspaceMissionResult> RunAsync(
        string rootPath,
        BoundedWorkspaceScanLimits? limits = null,
        CancellationToken cancellationToken = default)
    {
        var plan = CreatePlan();
        var runtime = new LightweightMissionRuntime(
            plan,
            new MissionAuthorizationScope(
                plan.MissionId,
                new HashSet<string>(["filesystem.read"], StringComparer.Ordinal),
                new HashSet<MissionExecutionSurface>([MissionExecutionSurface.Filesystem]),
                MissionRiskLevel.Low),
            "bounded-workspace-run");
        runtime.Start("workspace-understanding-start");
        var stepId = plan.Steps.Single().Id;
        runtime.BeginStep(stepId, "workspace-understanding-step");
        runtime.RecordToolCallStarted(stepId, "filesystem.read", "workspace-scan-start");

        var scan = await _understanding.ScanAsync(
            new BoundedWorkspaceScanRequest(
                RootPath: rootPath,
                IncludeTextPreviews: true,
                Limits: limits ?? new BoundedWorkspaceScanLimits()),
            cancellationToken).ConfigureAwait(false);

        var accepted = scan.Decision == BoundedWorkspaceScanDecision.Accepted;
        var verificationPassed = accepted &&
                                 scan.SecretsExcluded &&
                                 !scan.FilesystemMutationAllowed &&
                                 !scan.NetworkUsed &&
                                 !scan.ProductAuthorityGranted &&
                                 scan.FilesRead > 0 &&
                                 scan.EvidenceDigest.Length == 64;
        if (accepted)
        {
            var evidenceRef = $"evidence:workspace-understanding:{scan.EvidenceDigest}";
            runtime.RecordToolCallCompleted(
                stepId,
                "filesystem.read",
                "workspace-scan-complete",
                [evidenceRef]);
            runtime.MarkReadyForVerification(
                stepId,
                "workspace-scan-ready",
                [evidenceRef]);
            runtime.VerifyStep(
                stepId,
                verificationPassed,
                "workspace-scan-verified",
                [$"evidence:workspace-understanding-verification:{scan.EvidenceDigest}"],
                failureReason: "Bounded workspace evidence failed verification.");
        }
        else
        {
            runtime.FailStep(
                stepId,
                SafeFinding(scan.Findings.FirstOrDefault() ?? "Bounded workspace scan was rejected."),
                "workspace-scan-blocked");
        }

        var eventBus = new NodalOsCoreEventBus();
        var timeline = new NodalOsMissionEventProjectionService().Project(runtime.Events, eventBus);
        var completed = runtime.State.Status == MissionStatus.Completed && verificationPassed;
        var decision = completed
            ? "GO_BOUNDED_WORKSPACE_UNDERSTANDING_VERIFIED"
            : scan.Cancelled
                ? "BLOCKED_BOUNDED_WORKSPACE_UNDERSTANDING_CANCELLED"
                : "BLOCKED_BOUNDED_WORKSPACE_UNDERSTANDING_FAIL_CLOSED";
        var handoff = BuildHandoff(plan, runtime, scan, timeline, completed);
        var handoffRender = RenderHandoff(handoff);

        return new NodalOsBoundedWorkspaceMissionResult(
            Decision: decision,
            Scan: scan,
            Plan: plan,
            Mission: runtime.State,
            ResumeCard: runtime.ResumeCard,
            Timeline: timeline,
            Handoff: handoff,
            HandoffRender: handoffRender,
            Completed: completed,
            VerificationPassed: verificationPassed,
            NetworkUsed: false,
            FilesystemMutationAllowed: false,
            ProductAuthorityGranted: false);
    }

    private static MissionPlan CreatePlan() => new(
        MissionId: "bounded-workspace-understanding",
        Version: 1,
        CreatedAt: DateTimeOffset.UtcNow,
        Goal: "Understand one explicitly selected local workspace within bounded read-only limits.",
        Steps:
        [
            new MissionStep(
                Id: "bounded-workspace-scan",
                ParentId: null,
                Intent: "Read bounded workspace metadata and redacted text samples",
                ExecutionSurface: MissionExecutionSurface.Filesystem,
                AllowedCapabilities: ["filesystem.read"],
                ExpectedEvidence:
                [
                    new MissionExpectedEvidence(
                        "workspace-understanding-digest",
                        "A redacted deterministic digest from a bounded local scan")
                ],
                RiskLevel: MissionRiskLevel.Low,
                ApprovalRequired: false,
                Dependencies: Array.Empty<string>(),
                Status: MissionStepStatus.Pending,
                Attempts: 0,
                LastFailure: null,
                EvidenceRefs: Array.Empty<string>())
        ],
        Status: MissionStatus.Active);

    private static NodalOsPlannerHandoffPack BuildHandoff(
        MissionPlan plan,
        LightweightMissionRuntime runtime,
        BoundedWorkspaceScanResult scan,
        IReadOnlyList<NodalOsCoreTimelineProjection> timeline,
        bool completed)
    {
        var blockers = completed
            ? scan.Truncated
                ? new[] { "Workspace scan completed inside configured limits; additional files remain outside this evidence set." }
                : Array.Empty<string>()
            : scan.Findings.Select(SafeFinding).DefaultIfEmpty("Workspace understanding did not complete.").ToArray();
        var evidenceRefs = timeline
            .SelectMany(value => value.EvidenceRefs)
            .Select(value => value.EvidenceId)
            .Append($"workspace-digest:{scan.EvidenceDigest}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new NodalOsPlannerHandoffPack
        {
            HandoffPackId = $"workspace-understanding-handoff-{scan.EvidenceDigest[..12]}",
            MissionRef = plan.MissionId,
            AssignmentRef = plan.Steps.Single().Id,
            TaskGraphDraftRefs = [$"workspace-root-fingerprint:{scan.RootFingerprint}", $"scan-decision:{scan.Decision}"],
            ReviewSessionRefs = [runtime.Events.LastOrDefault()?.CorrelationId ?? "workspace-understanding-session"],
            SelectedBlockersRedacted = blockers,
            OpenQuestionsRedacted = scan.Truncated
                ? ["Should the operator explicitly authorize a larger scan budget for a follow-up mission?"]
                : Array.Empty<string>(),
            MissingReadinessGatesRedacted = completed ? Array.Empty<string>() : ["BOUNDED_WORKSPACE_SCAN_ACCEPTED_AND_VERIFIED"],
            EvidenceRefs = evidenceRefs,
            TimelineRefs = timeline.Select(value => value.ProjectionId).Distinct(StringComparer.Ordinal).ToArray(),
            ContextRefsRedacted =
            [
                $"files-read:{scan.FilesRead}",
                $"files-skipped:{scan.FilesSkipped}",
                $"bytes-read:{scan.TotalBytesRead}",
                $"truncated:{scan.Truncated}"
            ],
            GuardrailRefs =
            [
                "explicit-local-root",
                "path-containment",
                "no-reparse-following",
                "bounded-files-and-bytes",
                "redaction-before-projection",
                "no-mutation",
                "no-network",
                "verification-before-completion"
            ],
            DisclaimersRedacted =
            [
                "Local read-only workspace understanding only.",
                "Text previews are bounded and redacted; excluded files are not evidence.",
                "No mutation, network provider, browser runtime or product authority was used."
            ],
            WhatWasReviewedRedacted = completed
                ? $"{scan.FilesRead} bounded files and {scan.TotalBytesRead} sampled bytes were reviewed and verified."
                : "Workspace scan boundaries and the fail-closed result were reviewed.",
            WhatIsBlockedRedacted = blockers.Length == 0 ? "No blocker inside the bounded workspace mission." : string.Join(" ", blockers),
            WhatNeedsUserDecisionRedacted = scan.Truncated
                ? "A larger budget requires a new explicit operator decision; this mission will not expand itself."
                : "No additional decision is required for this bounded read-only result.",
            EvidenceRefsOnlyRedacted = $"Evidence is reference-only and anchored by digest {scan.EvidenceDigest}.",
            WhatIsNotVerifiedRedacted = "Files outside configured extensions, excluded directories, sensitive filenames and bytes beyond the configured sample limits were not reviewed.",
            WhatCannotExecuteRedacted = "This handoff cannot write files, run commands, access the network, grant authority or expand the scan scope.",
            RecommendedNextSafeStepRedacted = completed
                ? "Present the verified workspace summary in Mission Control and use it as context for a reviewed plan."
                : "Correct the workspace root or limits and start a new bounded mission.",
            DraftOnly = false,
            IsAuthoritative = false,
            Executable = false,
            PlannerRuntimeUsed = true,
            CallsLlmProvider = false,
            CreatesPrompt = false,
            RuntimeExecutionAllowed = false,
            FilesystemAccessUsed = scan.RealFilesystemRead,
            VerifiesEvidenceContent = completed
        };
    }

    private static NodalOsPlannerHandoffRender RenderHandoff(NodalOsPlannerHandoffPack handoff)
    {
        var markdown = $"""
            # NODAL OS Bounded Workspace Handoff

            ## Result
            {handoff.WhatWasReviewedRedacted}

            ## Scope and blockers
            {handoff.WhatIsBlockedRedacted}

            ## Evidence
            {handoff.EvidenceRefsOnlyRedacted}

            ## Not reviewed
            {handoff.WhatIsNotVerifiedRedacted}

            ## Next safe step
            {handoff.RecommendedNextSafeStepRedacted}
            """;
        var html = $"""
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8"><title>NODAL OS Bounded Workspace Handoff</title></head>
            <body>
              <main data-nodal-os="bounded-workspace-handoff">
                <h1>NODAL OS Bounded Workspace Handoff</h1>
                <section><h2>Result</h2><p>{WebUtility.HtmlEncode(handoff.WhatWasReviewedRedacted)}</p></section>
                <section><h2>Scope and blockers</h2><p>{WebUtility.HtmlEncode(handoff.WhatIsBlockedRedacted)}</p></section>
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

    private static string SafeFinding(string value)
    {
        var normalized = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return normalized.Length <= 240 ? normalized : normalized[..239] + "…";
    }
}
