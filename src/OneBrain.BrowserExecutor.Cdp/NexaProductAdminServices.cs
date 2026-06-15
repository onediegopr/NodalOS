using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaAdminPolicyEvaluator
{
    public NexaAdminDecision Evaluate(
        NexaProductAccount account,
        string actorId,
        NexaRole role,
        NexaAdminAction action,
        NexaAdminRolePolicy? policy = null)
    {
        policy ??= NexaAdminRolePolicy.Default();
        var capability = RequiredCapability(action);
        var decision = NexaAdminDecisionKind.Allowed;
        var reason = "admin action allowed by role policy";

        if (role == NexaRole.Unknown)
        {
            decision = NexaAdminDecisionKind.FailClosed;
            reason = "unknown admin role blocked";
        }
        else if (!policy.Allows(role, capability))
        {
            decision = NexaAdminDecisionKind.Denied;
            reason = "admin role lacks required capability";
        }
        else if (capability is NexaAdminCapability.ViewSecrets or NexaAdminCapability.MutateSensitiveData)
        {
            decision = NexaAdminDecisionKind.Denied;
            reason = "admin role cannot access secrets or sensitive mutations";
        }

        var audit = new NexaAdminAuditEvent(
            $"admin-audit-{Guid.NewGuid():N}",
            BrowserCredentialRedactor.Redact(actorId),
            role,
            account.AccountId,
            account.Organization.OrganizationId,
            action,
            decision,
            BrowserCredentialRedactor.Redact(reason),
            DateTimeOffset.UtcNow,
            BrowserCredentialRedactor.Redact("before summary redacted"),
            BrowserCredentialRedactor.Redact("after summary redacted"),
            Redacted: true);
        return new NexaAdminDecision(decision, action, capability, role, reason, audit);
    }

    private static NexaAdminCapability RequiredCapability(NexaAdminAction action) =>
        action switch
        {
            NexaAdminAction.ViewAccount or NexaAdminAction.ViewAudit => NexaAdminCapability.ViewReadOnly,
            NexaAdminAction.UpdateAccount => NexaAdminCapability.ManageAccount,
            NexaAdminAction.ManagePlan => NexaAdminCapability.ManagePlan,
            NexaAdminAction.AddWorker or NexaAdminAction.RemoveWorker or NexaAdminAction.UpdateWorker => NexaAdminCapability.ManageWorkers,
            NexaAdminAction.SupportInspect => NexaAdminCapability.SupportAccess,
            NexaAdminAction.ViewSecret => NexaAdminCapability.ViewSecrets,
            _ => NexaAdminCapability.MutateSensitiveData
        };
}
