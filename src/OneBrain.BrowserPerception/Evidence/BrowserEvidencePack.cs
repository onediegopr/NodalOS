namespace OneBrain.BrowserPerception;

public enum BrowserEvidenceKind
{
    PlanOnly,
    FixtureExecutionSucceeded,
    FixtureExecutionFailed,
    BlockageDetected,
    HumanHandoff,
    VerificationFailed
}

public enum BrowserEvidenceRedactionStatus
{
    None,
    Partial,
    Full
}

public sealed record BrowserEvidenceReference(
    string RefId,
    string RefKind,
    string Description,
    bool Redacted);

public sealed record BrowserEvidencePack(
    string EvidenceId,
    string CorrelationId,
    BrowserEvidenceKind EvidenceKind,
    string SnapshotBefore,
    string? SnapshotAfter,
    string? StrategyDecision,
    string? ActionPlan,
    string? ExecutionResult,
    string? BlockageReport,
    bool? ActionSucceeded,
    bool HumanHandoffTriggered,
    BrowserEvidenceRedactionStatus RedactionStatus,
    IReadOnlyList<string> SensitiveFieldsRedacted,
    string EvidenceSummary,
    IReadOnlyList<BrowserEvidenceReference> EvidenceRefs,
    DateTimeOffset CreatedAtUtc,
    bool MetadataOnly,
    bool FixtureOnly,
    bool CdpInvoked,
    bool WebSocketInvoked,
    bool BrowserLaunched,
    bool SystemBrowserUsed,
    bool ExtensionInvoked,
    bool ExternalNavigationAttempted,
    bool ProductFilesModified,
    bool LiveExecutionDisabled,
    bool NoSensitivePayloadGuarantee)
{
    public string SnapshotBeforeRef => SnapshotBefore;

    public string? SnapshotAfterRef => SnapshotAfter;
}

public sealed class BrowserEvidenceCollector
{
    private readonly BrowserEvidenceRedactor redactor = new();

    public BrowserEvidencePack CollectFromPlanOnly(
        StrategyRouterDecision decision,
        SafeBrowserActionPlan plan,
        BrowserPerceptionSnapshot beforeSnapshot)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(beforeSnapshot);

        var summary = redactor.RedactSummary(
            $"Plan only: strategy={decision.Strategy}; action={plan.ActionKind}; reason={plan.Reason}; humanHandoff={plan.RequiresHumanHandoff}.");
        var refs = BuildReferences(beforeSnapshot.SnapshotId, null, decision, plan, null, null);

        return BuildPack(
            EvidenceKind: BrowserEvidenceKind.PlanOnly,
            CorrelationId: CorrelationFrom(beforeSnapshot.SnapshotId),
            SnapshotBefore: beforeSnapshot.SnapshotId,
            SnapshotAfter: null,
            StrategyDecision: decision.Strategy.ToString(),
            ActionPlan: plan.ActionKind.ToString(),
            ExecutionResult: null,
            BlockageReport: null,
            ActionSucceeded: null,
            HumanHandoffTriggered: plan.RequiresHumanHandoff || decision.HumanHandoffRequired,
            summary,
            refs,
            AdditionalSensitiveFields: []);
    }

    public BrowserEvidencePack CollectFromExecution(
        ControlledActionExecutionResult result,
        BrowserPerceptionSnapshot beforeSnapshot,
        BrowserPerceptionSnapshot afterSnapshot)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(beforeSnapshot);
        ArgumentNullException.ThrowIfNull(afterSnapshot);

        var kind = result.Succeeded
            ? BrowserEvidenceKind.FixtureExecutionSucceeded
            : result.FailedPostcondition
                ? BrowserEvidenceKind.VerificationFailed
                : BrowserEvidenceKind.FixtureExecutionFailed;
        var summary = redactor.RedactSummary(
            $"Fixture execution: action={result.ActionKind}; attempted={result.Attempted}; succeeded={result.Succeeded}; reason={result.Reason}; markers={string.Join('|', result.EvidenceDraft.SyntheticMarkers)}.");
        var refs = BuildReferences(beforeSnapshot.SnapshotId, afterSnapshot.SnapshotId, null, null, result, null);

        return BuildPack(
            kind,
            result.EvidenceDraft.CorrelationId,
            beforeSnapshot.SnapshotId,
            afterSnapshot.SnapshotId,
            StrategyDecision: null,
            ActionPlan: result.ActionKind.ToString(),
            ExecutionResult: result.Reason,
            BlockageReport: null,
            ActionSucceeded: result.Succeeded,
            HumanHandoffTriggered: result.RequiresHumanHandoff,
            summary,
            refs,
            summary.SensitiveFieldsRedacted);
    }

    public BrowserEvidencePack CollectFromBlockage(
        BlockageReport blockage,
        BrowserPerceptionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(blockage);
        ArgumentNullException.ThrowIfNull(snapshot);

        var summary = redactor.RedactSummary(
            $"Blockage detected: kind={blockage.BlockageKind}; severity={blockage.Severity}; reason={blockage.Reason}; evidence={blockage.EvidenceSummary}.");
        var refs = BuildReferences(snapshot.SnapshotId, null, null, null, null, blockage);

        return BuildPack(
            BrowserEvidenceKind.BlockageDetected,
            CorrelationFrom(snapshot.SnapshotId),
            snapshot.SnapshotId,
            SnapshotAfter: null,
            StrategyDecision: null,
            ActionPlan: null,
            ExecutionResult: null,
            BlockageReport: blockage.BlockageKind.ToString(),
            ActionSucceeded: null,
            HumanHandoffTriggered: blockage.RequiresHumanHandoff,
            summary,
            refs,
            summary.SensitiveFieldsRedacted);
    }

    public BrowserEvidencePack CollectFromHumanHandoff(
        StrategyRouterDecision decision,
        BlockageReport? blockage,
        BrowserPerceptionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(snapshot);

        var summary = redactor.RedactSummary(
            $"Human handoff: strategy={decision.Strategy}; blockage={blockage?.BlockageKind.ToString() ?? "none"}; reasons={string.Join(" | ", decision.Reasons)}.");
        var refs = BuildReferences(snapshot.SnapshotId, null, decision, null, null, blockage);

        return BuildPack(
            BrowserEvidenceKind.HumanHandoff,
            CorrelationFrom(snapshot.SnapshotId),
            snapshot.SnapshotId,
            SnapshotAfter: null,
            StrategyDecision: decision.Strategy.ToString(),
            ActionPlan: null,
            ExecutionResult: null,
            BlockageReport: blockage?.BlockageKind.ToString(),
            ActionSucceeded: null,
            HumanHandoffTriggered: true,
            summary,
            refs,
            summary.SensitiveFieldsRedacted);
    }

    public BrowserEvidencePack CollectFromVerificationFailure(
        SafeBrowserActionPlan plan,
        PreActionVerificationResult? preResult,
        PostActionVerificationResult? postResult,
        BrowserPerceptionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(snapshot);

        var reason = postResult?.Reason ?? preResult?.Reason ?? "Verification failed.";
        var summary = redactor.RedactSummary(
            $"Verification failed: action={plan.ActionKind}; reason={reason}; preFailed={preResult?.FailedPreconditions.Count ?? 0}; postFailed={postResult?.FailedPostconditions.Count ?? 0}.");
        var refs = BuildReferences(snapshot.SnapshotId, null, null, plan, null, null);

        return BuildPack(
            BrowserEvidenceKind.VerificationFailed,
            CorrelationFrom(snapshot.SnapshotId),
            snapshot.SnapshotId,
            SnapshotAfter: null,
            StrategyDecision: null,
            ActionPlan: plan.ActionKind.ToString(),
            ExecutionResult: reason,
            BlockageReport: null,
            ActionSucceeded: false,
            HumanHandoffTriggered: preResult?.RequiresHumanHandoff == true || postResult?.RequiresHumanHandoff == true,
            summary,
            refs,
            summary.SensitiveFieldsRedacted);
    }

    public BrowserEvidencePack CollectFromSensitiveField(
        string fieldName,
        string value,
        BrowserPerceptionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var redacted = redactor.RedactField(fieldName, value);
        var refs = BuildReferences(snapshot.SnapshotId, null, null, null, null, null);

        return BuildPack(
            BrowserEvidenceKind.PlanOnly,
            CorrelationFrom(snapshot.SnapshotId),
            snapshot.SnapshotId,
            SnapshotAfter: null,
            StrategyDecision: null,
            ActionPlan: null,
            ExecutionResult: null,
            BlockageReport: null,
            ActionSucceeded: null,
            HumanHandoffTriggered: false,
            new BrowserEvidenceRedactionResult(
                $"Sensitive field {fieldName}: {redacted.Value}",
                redacted.SensitiveFieldsRedacted,
                redacted.Status),
            refs,
            redacted.SensitiveFieldsRedacted);
    }

    private BrowserEvidencePack BuildPack(
        BrowserEvidenceKind EvidenceKind,
        string CorrelationId,
        string SnapshotBefore,
        string? SnapshotAfter,
        string? StrategyDecision,
        string? ActionPlan,
        string? ExecutionResult,
        string? BlockageReport,
        bool? ActionSucceeded,
        bool HumanHandoffTriggered,
        BrowserEvidenceRedactionResult summary,
        IReadOnlyList<BrowserEvidenceReference> refs,
        IReadOnlyList<string> AdditionalSensitiveFields)
    {
        var executionResult = redactor.RedactSummary(ExecutionResult);
        var sensitiveFields = summary.SensitiveFieldsRedacted
            .Concat(executionResult.SensitiveFieldsRedacted)
            .Concat(AdditionalSensitiveFields)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var redactionStatus = redactor.MergeStatuses(summary.Status, executionResult.Status);

        return new BrowserEvidencePack(
            EvidenceId: "browser-evidence-" + Guid.NewGuid().ToString("N"),
            CorrelationId,
            EvidenceKind,
            SnapshotBefore,
            SnapshotAfter,
            StrategyDecision,
            ActionPlan,
            ExecutionResult: executionResult.Value,
            BlockageReport,
            ActionSucceeded,
            HumanHandoffTriggered,
            RedactionStatus: sensitiveFields.Length > 0 ? BrowserEvidenceRedactionStatus.Partial : redactionStatus,
            SensitiveFieldsRedacted: sensitiveFields,
            EvidenceSummary: summary.Value,
            EvidenceRefs: refs,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            MetadataOnly: true,
            FixtureOnly: true,
            CdpInvoked: false,
            WebSocketInvoked: false,
            BrowserLaunched: false,
            SystemBrowserUsed: false,
            ExtensionInvoked: false,
            ExternalNavigationAttempted: false,
            ProductFilesModified: false,
            LiveExecutionDisabled: true,
            NoSensitivePayloadGuarantee: true);
    }

    private static IReadOnlyList<BrowserEvidenceReference> BuildReferences(
        string snapshotBefore,
        string? snapshotAfter,
        StrategyRouterDecision? decision,
        SafeBrowserActionPlan? plan,
        ControlledActionExecutionResult? result,
        BlockageReport? blockage)
    {
        var refs = new List<BrowserEvidenceReference>
        {
            new(snapshotBefore, "snapshot-before", "Metadata-only perception snapshot before action or decision.", Redacted: true)
        };

        if (!string.IsNullOrWhiteSpace(snapshotAfter))
            refs.Add(new BrowserEvidenceReference(snapshotAfter, "snapshot-after", "Metadata-only fixture snapshot after synthetic execution.", Redacted: true));
        if (decision is not null)
            refs.Add(new BrowserEvidenceReference("strategy:" + decision.Strategy, "strategy-decision", "Strategy router decision metadata.", Redacted: true));
        if (plan is not null)
            refs.Add(new BrowserEvidenceReference("action-plan:" + plan.ActionKind, "action-plan", "Safe action plan metadata.", Redacted: true));
        if (result is not null)
            refs.Add(new BrowserEvidenceReference("execution:" + result.EvidenceDraft.CorrelationId, "fixture-execution", "Fixture-only execution result metadata.", Redacted: true));
        if (blockage is not null)
            refs.Add(new BrowserEvidenceReference("blockage:" + blockage.BlockageKind, "blockage", "Blockage detector metadata.", Redacted: true));

        return refs;
    }

    private static string CorrelationFrom(string snapshotId) =>
        string.IsNullOrWhiteSpace(snapshotId)
            ? "missing-correlation"
            : snapshotId.Length <= 80 ? snapshotId : snapshotId[..80];
}
