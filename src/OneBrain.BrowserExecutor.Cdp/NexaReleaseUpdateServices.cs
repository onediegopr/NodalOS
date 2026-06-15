using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaUpdateEligibilityEvaluator
{
    public NexaUpdateEligibilityResult Evaluate(NexaUpdateEligibilityRequest request)
    {
        var manifestValidation = request.TargetManifest.Validate();
        if (!manifestValidation.IsValid)
            return Result(request, NexaUpdateEligibilityDecisionKind.FailClosed, string.Join("; ", manifestValidation.Errors));
        if (!request.Channel.Enabled || request.Channel.Kind == NexaReleaseChannelKind.Disabled)
            return Result(request, NexaUpdateEligibilityDecisionKind.Denied, "release channel disabled");
        if (request.Channel.Kind != request.TargetManifest.Channel)
            return Result(request, NexaUpdateEligibilityDecisionKind.Denied, "target channel incompatible");
        if (request.EnterprisePinnedVersion is not null && request.Channel.Kind == NexaReleaseChannelKind.EnterprisePinned && request.TargetManifest.Package.Version != request.EnterprisePinnedVersion)
            return Result(request, NexaUpdateEligibilityDecisionKind.Denied, "enterprise pinned version mismatch");
        if (!request.TargetManifest.Compatibility.Passed || !request.TargetManifest.Compatibility.Compatibility.RuntimeCompatible)
            return Result(request, NexaUpdateEligibilityDecisionKind.Denied, "runtime incompatible");
        if (request.Channel.TenantPolicyRequired && !request.TenantPolicyAllows)
            return Result(request, NexaUpdateEligibilityDecisionKind.Denied, "tenant policy does not allow release channel");
        if (request.Channel.RequiresAdminApproval && !request.AdminApprovalPresent)
            return Result(request, NexaUpdateEligibilityDecisionKind.RequiresApproval, "admin approval required");
        if (request.AutoExecuteRequested)
            return Result(request, NexaUpdateEligibilityDecisionKind.FailClosed, "auto-update execution is disabled");
        return Result(request, NexaUpdateEligibilityDecisionKind.Eligible, "update eligible for manual controlled planning");
    }

    private static NexaUpdateEligibilityResult Result(NexaUpdateEligibilityRequest request, NexaUpdateEligibilityDecisionKind decision, string reason)
    {
        var audit = new NexaReleaseAuditEvent($"release-audit-{Guid.NewGuid():N}", request.Channel.Kind, request.TargetManifest.Package.Version, decision, BrowserCredentialRedactor.Redact(reason), DateTimeOffset.UtcNow, Redacted: true);
        return new NexaUpdateEligibilityResult(decision, BrowserCredentialRedactor.Redact(reason), UpdateExecuted: false, audit);
    }
}

public sealed class NexaRollbackEvaluator
{
    public NexaRollbackEligibility Evaluate(NexaRollbackPlan plan) =>
        plan.ExecuteAutomatically
            ? new NexaRollbackEligibility(false, "rollback cannot execute automatically", ExecutesRollback: false)
            : new NexaRollbackEligibility(true, "rollback plan is model-only", ExecutesRollback: false);

    public NexaRollbackDecision Decide(NexaRollbackPlan plan)
    {
        var eligibility = Evaluate(plan);
        return new NexaRollbackDecision(eligibility.Eligible, eligibility.Reason, plan, Executed: false);
    }
}
