using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaExternalTestOwnedTargetEvaluator
{
    private static readonly string[] SensitiveHostTokens = ["afip", "bank", "banco", "fiscal", "tax", "erp", "gov", "gob", "payment", "pay", "stripe", "paypal", "mercadopago"];
    private static readonly HashSet<string> ReadOnlyMethods = new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD" };
    private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    public NexaExternalTestOwnedTargetDecision Evaluate(NexaExternalTestOwnedTarget? target, DateTimeOffset nowUtc)
    {
        if (target is null || string.IsNullOrWhiteSpace(target.BaseUrl))
            return Decision(NexaExternalTestOwnedTargetStatus.MissingTarget, target, ["external test-owned target missing"], allows: false);
        if (!Uri.TryCreate(target.BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            return Decision(NexaExternalTestOwnedTargetStatus.PolicyRejected, target, ["target base URL must be HTTP/HTTPS"], allows: false);

        var reasons = new List<string>();
        if (!target.ExplicitlyTestOwned || target.OwnershipProofMode == NexaExternalTargetOwnershipProofMode.None || string.IsNullOrWhiteSpace(target.ApprovalRef))
            reasons.Add("explicit test-owned approval required");
        if (target.ValidUntilUtc is not null && target.ValidUntilUtc <= nowUtc)
            return Decision(NexaExternalTestOwnedTargetStatus.Expired, target, ["target approval expired"], allows: false);
        if (target.AllowedHosts.Count == 0 || !target.AllowedHosts.Contains(uri.Host))
            reasons.Add("host must be explicitly allowlisted");
        if (target.DeniedHosts.Contains(uri.Host) || target.DeniedHosts.Any(denied => uri.Host.Contains(denied, StringComparison.OrdinalIgnoreCase)))
            reasons.Add("host is denied");
        if (SensitiveHostTokens.Any(token => uri.Host.Contains(token, StringComparison.OrdinalIgnoreCase)) ||
            target.DataSensitivityProfile is NexaExternalTargetDataSensitivityProfile.FinancialFiscalGovernmentBlocked or NexaExternalTargetDataSensitivityProfile.SensitiveBlocked)
            return Decision(NexaExternalTestOwnedTargetStatus.BlockedSensitiveSurface, target, ["financial fiscal government or sensitive host blocked"], allows: false);
        if (target.AllowedPaths.Count == 0)
            reasons.Add("allowed read-only paths required");
        if (target.DeniedPaths.Any(path => target.AllowedPaths.Contains(path)))
            reasons.Add("allowed paths cannot include denied paths");
        if (target.AllowedMethods.Count == 0 || target.AllowedMethods.Any(method => !ReadOnlyMethods.Contains(method)) || target.DeniedMethods.Any(method => !MutatingMethods.Contains(method)))
            reasons.Add("only HTTP GET/HEAD read-only methods are allowed");
        if (target.CredentialPolicy != NexaExternalTargetCredentialPolicy.NoCredentials)
            reasons.Add("real or synthetic login is not allowed for this proof harness");
        if (target.SubmitPolicy != NexaExternalTargetSubmitPolicy.ReadOnlyNoSubmit)
            reasons.Add("submit/mutation policy must be read-only no-submit");
        if (target.DataSensitivityProfile is NexaExternalTargetDataSensitivityProfile.PersonalDataBlocked)
            reasons.Add("real personal data is blocked");
        if (target.EvidencePolicy != NexaExternalTargetEvidencePolicy.MetadataOnlyRedacted)
            reasons.Add("evidence policy must be metadata-only redacted");

        if (reasons.Count > 0)
        {
            var ownershipOnly = reasons.All(reason => reason.Contains("test-owned", StringComparison.OrdinalIgnoreCase));
            return Decision(ownershipOnly ? NexaExternalTestOwnedTargetStatus.OwnershipUnverified : NexaExternalTestOwnedTargetStatus.PolicyRejected, target, reasons, allows: false);
        }

        return Decision(NexaExternalTestOwnedTargetStatus.ApprovedReadOnlyTestOwned, target, ["approved read-only test-owned target"], allows: true);
    }

    private static NexaExternalTestOwnedTargetDecision Decision(NexaExternalTestOwnedTargetStatus status, NexaExternalTestOwnedTarget? target, IReadOnlyList<string> reasons, bool allows) =>
        new(status, RedactTarget(target), reasons.Select(BrowserCredentialRedactor.Redact).ToArray(), allows, Redacted: true);

    private static NexaExternalTestOwnedTarget? RedactTarget(NexaExternalTestOwnedTarget? target) =>
        target is null
            ? null
            : target with
            {
                BaseUrl = RedactUrl(target.BaseUrl),
                ComplianceNotes = BrowserCredentialRedactor.Redact(target.ComplianceNotes),
                OperatorOwner = BrowserCredentialRedactor.Redact(target.OperatorOwner),
                ApprovalRef = BrowserCredentialRedactor.Redact(target.ApprovalRef)
            };

    internal static string RedactUrl(string? url)
    {
        var redacted = BrowserCredentialRedactor.Redact(url);
        if (!Uri.TryCreate(redacted, UriKind.Absolute, out var uri))
            return redacted;
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}{(string.IsNullOrEmpty(uri.Query) ? "" : "?[REDACTED_QUERY]")}";
    }
}

public sealed class NexaExternalProofHarness
{
    private readonly NexaExternalTestOwnedTargetEvaluator _targetEvaluator = new();
    private readonly NexaOperatorBlockerExplanationService _explanations = new();

    public NexaExternalProofHarnessDecision Evaluate(NexaExternalProofHarnessRequest request, DateTimeOffset nowUtc)
    {
        if (!request.OptInEnabled)
        {
            var targetDecision = _targetEvaluator.Evaluate(request.Target, nowUtc);
            return Decision(NexaExternalProofHarnessDecisionKind.SkippedNoOptIn, targetDecision, ["external proof harness opt-in missing"], NexaOperatorBlockerScenario.SkippedTestsBlockExternalLive, canExecute: false);
        }

        var decision = _targetEvaluator.Evaluate(request.Target, nowUtc);
        if (decision.Status == NexaExternalTestOwnedTargetStatus.MissingTarget)
            return Decision(NexaExternalProofHarnessDecisionKind.BlockedNoTarget, decision, decision.ReasonCodes, NexaOperatorBlockerScenario.MissingTestOwnedExternalTarget, canExecute: false);
        if (!decision.AllowsReadOnlyProof || request.Target is null)
            return Decision(NexaExternalProofHarnessDecisionKind.BlockedPolicyViolation, decision, decision.ReasonCodes, NexaOperatorBlockerScenario.CorePermissionMissing, canExecute: false);

        var reasons = new List<string>();
        if (!request.Target.AllowedHosts.Contains(request.RequestedHost))
            reasons.Add("requested host not allowlisted");
        if (!request.Target.AllowedPaths.Contains(request.RequestedPath) || request.Target.DeniedPaths.Contains(request.RequestedPath))
            reasons.Add("requested path not allowlisted for read-only proof");
        if (!request.Target.AllowedMethods.Contains(request.RequestedMethod) || request.Target.DeniedMethods.Contains(request.RequestedMethod))
            reasons.Add("requested method rejected before execution");
        if (request.WouldCaptureBodies)
            reasons.Add("body capture is blocked");
        if (request.WouldCaptureSensitiveHeaderValues)
            reasons.Add("sensitive header value capture is blocked");
        if (request.WouldPersistCookies)
            reasons.Add("cookie persistence is blocked");
        if (request.WouldSubmit)
            reasons.Add("submit/pay/sign/delete is blocked");

        if (reasons.Count > 0)
            return Decision(NexaExternalProofHarnessDecisionKind.BlockedPolicyViolation, decision, reasons, NexaOperatorBlockerScenario.IrreversibleActionBlocked, canExecute: false);

        return Decision(NexaExternalProofHarnessDecisionKind.AllowedReadOnlyProof, decision, ["read-only proof may execute against approved test-owned target"], NexaOperatorBlockerScenario.SkippedTestsBlockExternalLive, canExecute: true);
    }

    private NexaExternalProofHarnessDecision Decision(
        NexaExternalProofHarnessDecisionKind kind,
        NexaExternalTestOwnedTargetDecision targetDecision,
        IReadOnlyList<string> reasons,
        NexaOperatorBlockerScenario scenario,
        bool canExecute) =>
        new(kind, targetDecision, reasons.Select(BrowserCredentialRedactor.Redact).ToArray(), _explanations.Explain(scenario, ["external-proof-harness:redacted"]), canExecute, Redacted: true);
}

public sealed class NexaExternalReadOnlyEvidencePackBuilder
{
    public NexaExternalReadOnlyEvidencePack Build(NexaExternalProofHarnessDecision harnessDecision, NexaExternalProofHarnessRequest request, bool runtimeExecuted, bool runtimePassed)
    {
        var status = harnessDecision.Decision switch
        {
            NexaExternalProofHarnessDecisionKind.SkippedNoOptIn => NexaExternalReadOnlyEvidencePackStatus.SkippedNoOptIn,
            NexaExternalProofHarnessDecisionKind.BlockedNoTarget => NexaExternalReadOnlyEvidencePackStatus.BlockedNoTarget,
            NexaExternalProofHarnessDecisionKind.BlockedPolicyViolation => NexaExternalReadOnlyEvidencePackStatus.BlockedPolicyViolation,
            NexaExternalProofHarnessDecisionKind.AllowedReadOnlyProof when !runtimeExecuted => NexaExternalReadOnlyEvidencePackStatus.PreparedButNotExecuted,
            NexaExternalProofHarnessDecisionKind.AllowedReadOnlyProof when runtimeExecuted && runtimePassed => NexaExternalReadOnlyEvidencePackStatus.PassedReadOnlyProof,
            NexaExternalProofHarnessDecisionKind.AllowedReadOnlyProof when runtimeExecuted => NexaExternalReadOnlyEvidencePackStatus.FailedRuntime,
            _ => NexaExternalReadOnlyEvidencePackStatus.PreparedButNotExecuted
        };

        var candidate = status == NexaExternalReadOnlyEvidencePackStatus.PassedReadOnlyProof &&
            harnessDecision.TargetDecision.Status == NexaExternalTestOwnedTargetStatus.ApprovedReadOnlyTestOwned;

        return new NexaExternalReadOnlyEvidencePack(
            $"external-readonly-proof-{Guid.NewGuid():N}",
            harnessDecision.TargetDecision.Target?.TargetId,
            harnessDecision.TargetDecision.Status,
            DateTimeOffset.UtcNow,
            "ChromeCdpExternal",
            ["NavigationReadOnly", "DomReadOnly", "NetworkMetadataOnly", "CoreGoverned"],
            BrowserCredentialRedactor.Redact($"{request.RequestedMethod} {request.RequestedPath}"),
            harnessDecision.CanExecuteReadOnlyNavigation ? "read-only navigation" : "none",
            ["POST", "PUT", "PATCH", "DELETE", "submit", "pay", "sign", "delete", "upload"],
            BrowserCredentialRedactor.Redact(request.RequestedHost),
            BrowserCredentialRedactor.Redact(request.RequestedPath),
            BrowserCredentialRedactor.Redact(request.RequestedMethod),
            "metadata-only; no cookies, bodies, sensitive headers, tokens, or personal data persisted",
            [],
            ["external-proof-harness:redacted"],
            harnessDecision.ReasonCodes,
            harnessDecision.CanExecuteReadOnlyNavigation ? [] : [harnessDecision.Explanation],
            candidate ? "candidate proof passed; M51/M65 still require explicit closure decision" : "external/live remains blocked or deferred",
            status,
            candidate,
            Redacted: true);
    }
}
