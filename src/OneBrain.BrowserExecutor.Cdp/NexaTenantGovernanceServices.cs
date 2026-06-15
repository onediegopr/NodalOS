using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaTenantGovernanceEvaluator
{
    public NexaTenantGovernanceDecision Evaluate(NexaTenantGovernanceRequest request)
    {
        if (!request.ActorTenant.Validate().IsValid || !request.TargetTenant.Validate().IsValid)
            return Decision(NexaTenantIsolationDecisionKind.FailClosed, "tenant validation failed", request);
        if (!request.Policy.AllowCrossTenantAccess && request.ActorTenant.AccountId != request.TargetTenant.AccountId)
            return Decision(NexaTenantIsolationDecisionKind.Denied, "cross tenant account access blocked", request);
        if (!request.Policy.AllowCrossTenantAccess && request.ActorTenant.OrganizationId != request.TargetTenant.OrganizationId)
            return Decision(NexaTenantIsolationDecisionKind.Denied, "cross tenant organization access blocked", request);
        if (request.ActorRole == NexaRole.Worker && request.ActorTenant.WorkspaceId != request.TargetTenant.WorkspaceId)
            return Decision(NexaTenantIsolationDecisionKind.Denied, "worker cannot access another workspace", request);
        if (request.ActorRole == NexaRole.Support && request.DataClassification is NexaTenantDataClassification.Secret or NexaTenantDataClassification.Sensitive)
            return Decision(NexaTenantIsolationDecisionKind.Denied, "support access is metadata-only", request);
        if (request.ActorRole == NexaRole.Viewer && request.RequestedAction is NexaAdminAction.UpdateAccount or NexaAdminAction.ManagePlan or NexaAdminAction.AddWorker or NexaAdminAction.UpdateWorker or NexaAdminAction.RemoveWorker)
            return Decision(NexaTenantIsolationDecisionKind.Denied, "viewer cannot mutate tenant state", request);
        if (request.DataClassification == NexaTenantDataClassification.Secret && !request.Policy.AllowSupportSecretAccess)
            return Decision(NexaTenantIsolationDecisionKind.Denied, "secret data access blocked by tenant policy", request);
        return Decision(NexaTenantIsolationDecisionKind.Allowed, "tenant governance allowed request", request);
    }

    private static NexaTenantGovernanceDecision Decision(NexaTenantIsolationDecisionKind decision, string reason, NexaTenantGovernanceRequest request) =>
        new(decision, BrowserCredentialRedactor.Redact(reason), request.ActorRole == NexaRole.Support ? NexaTenantScope.Support : NexaTenantScope.Workspace, Redacted: true);
}

public sealed class NexaAuditExportService
{
    private readonly NexaTenantGovernanceEvaluator _governance = new();

    public NexaAuditExportResult Export(NexaAuditExportRequest request, IReadOnlyList<NexaAdminAuditEvent> events, NexaTenant actorTenant, NexaTenant targetTenant)
    {
        if (request.ActorRole == NexaRole.Viewer)
            return Denied(request, "viewer cannot export audit without elevated permission");
        if (!request.SensitiveDataPolicyApproved && events.Any(e => e.Action is NexaAdminAction.ViewSecret))
            return Denied(request, "sensitive data export requires explicit policy");

        var governance = _governance.Evaluate(new NexaTenantGovernanceRequest(actorTenant, targetTenant, request.ActorRole, NexaAdminAction.ViewAudit, NexaTenantDataClassification.InternalMetadata, NexaTenantPolicy.Strict()));
        if (!governance.Allowed)
            return new NexaAuditExportResult(governance.Decision, governance.Reason, null, "", Redacted: true);

        var scoped = events
            .Where(e => request.Scope.Contains(e) && e.TimestampUtc >= request.FromUtc && e.TimestampUtc <= request.ToUtc)
            .Select(Redact)
            .ToArray();
        var payload = NexaAuditExportResult.SerializeEvents(scoped, request.Format);
        if (BrowserCredentialRedactor.ContainsSecret(payload))
            return Denied(request, "audit export redaction failed closed");

        var manifest = new NexaAuditExportManifest(
            request.ExportId,
            request.Scope,
            request.FromUtc,
            request.ToUtc,
            scoped.Length,
            request.RedactionPolicy.PolicyId,
            NexaAuditExportResult.ComputeHash(payload),
            scoped.Select(e => e.EventId).ToArray(),
            Redacted: true);
        return new NexaAuditExportResult(NexaTenantIsolationDecisionKind.Allowed, "audit export created", manifest, payload, Redacted: true);
    }

    private static NexaAuditExportResult Denied(NexaAuditExportRequest request, string reason) =>
        new(NexaTenantIsolationDecisionKind.Denied, BrowserCredentialRedactor.Redact(reason), null, "", Redacted: true);

    private static NexaAdminAuditEvent Redact(NexaAdminAuditEvent e) =>
        e with
        {
            ActorId = BrowserCredentialRedactor.Redact(e.ActorId),
            Reason = BrowserCredentialRedactor.Redact(e.Reason),
            BeforeSummary = BrowserCredentialRedactor.Redact(e.BeforeSummary),
            AfterSummary = BrowserCredentialRedactor.Redact(e.AfterSummary),
            Redacted = true
        };
}
