using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class SensitiveSitePolicyEvaluator
{
    public SensitiveSitePolicyDecision Evaluate(SensitiveSitePolicyRequest request, SensitiveSitePolicy policy)
    {
        var reasons = new List<SensitiveSiteReason>();
        var violations = new List<SensitiveSitePolicyViolation>();
        var remainsBlocked = new List<string>
        {
            "submit",
            "pay",
            "sign",
            "delete",
            "publish",
            "productive replay",
            "productive recorder"
        };

        if (!policy.Classifications.TryGetValue(request.SiteUri.Host, out var classification))
        {
            Add(SensitiveSiteReason.UnknownDomain, "sensitive site domain is not classified");
            return Decision(SensitiveSiteDecisionKind.Blocked, null, SensitiveSiteHumanApprovalRequirement.Prohibited);
        }

        if (classification.Category == SensitiveSiteCategory.Unknown)
            Add(SensitiveSiteReason.UnknownCategory, "sensitive site category is unknown");

        var approval = ApprovalFor(classification, request.ActionKind);
        var decision = DecisionFor(request.ActionKind, approval);

        if (IsIrreversible(request.ActionKind))
            Add(SensitiveSiteReason.IrreversibleActionBlocked, "irreversible action blocked by default");
        if (request.ActionKind == SensitiveSiteActionKind.Pay)
            Add(SensitiveSiteReason.PaymentBlocked, "payment blocked by default");
        if (request.ActionKind == SensitiveSiteActionKind.Submit)
            Add(SensitiveSiteReason.SubmitBlocked, "submit blocked by default");
        if (request.ActionKind == SensitiveSiteActionKind.Sign)
            Add(SensitiveSiteReason.SigningBlocked, "signing blocked by default");
        if (request.ActionKind == SensitiveSiteActionKind.DownloadDocument && (!policy.RequireSafeDownloadForDocumentDownload || !request.Context.SafeDownloadAvailable))
            Add(SensitiveSiteReason.SensitiveDownloadRequiresSafeDownload, "sensitive download requires safe download policy");
        if (request.ActionKind == SensitiveSiteActionKind.UploadDocument && (!policy.RequireSafeUploadForDocumentUpload || !request.Context.SafeUploadAvailable))
            Add(SensitiveSiteReason.SensitiveUploadRequiresSafeUpload, "sensitive upload requires safe upload policy");
        if (RequiresApproval(approval) && !ApprovalSatisfied(approval, request))
            Add(SensitiveSiteReason.SensitiveSiteRequiresApproval, "sensitive site action requires human approval");
        if (request.Context.GateReport?.Passed != true)
            Add(SensitiveSiteReason.GateFailed, "sensitive site policy requires passing gate");
        if (request.Context.ProfileState == BrowserRuntimeProfileState.RawUserProfileActive)
            Add(SensitiveSiteReason.ProfileRawBlocked, "raw user profile is blocked for sensitive sites");
        if (request.Context.ReplayState == BrowserRuntimeReplayState.ProductiveActive)
            Add(SensitiveSiteReason.ProductiveReplayBlocked, "productive replay is blocked for sensitive sites");
        if (request.Context.RecorderState == BrowserRuntimeRecorderState.ProductiveActive)
            Add(SensitiveSiteReason.ProductiveRecorderBlocked, "productive recorder is blocked for sensitive sites");
        if (request.Context.NetworkCaptureMode != BrowserNetworkCaptureMode.MetadataOnly || request.Context.RequestBodyCaptureSupported || request.Context.ResponseBodyCaptureSupported || request.Context.SensitiveHeaderValueCaptureSupported)
            Add(SensitiveSiteReason.UnsafeNetworkCapture, "sensitive site policy requires metadata-only network capture");

        if (reasons.Count == 0 && request.ActionKind == SensitiveSiteActionKind.ReadOnlyView)
            reasons.Add(SensitiveSiteReason.AllowedReadOnlySimulation);

        if (violations.Count > 0)
            decision = IsProhibited(request.ActionKind) ? SensitiveSiteDecisionKind.Prohibited : SensitiveSiteDecisionKind.Blocked;

        return Decision(decision, classification, approval);

        void Add(SensitiveSiteReason reason, string message)
        {
            reasons.Add(reason);
            violations.Add(new SensitiveSitePolicyViolation(reason, message));
        }

        SensitiveSitePolicyDecision Decision(SensitiveSiteDecisionKind kind, SensitiveSiteClassification? site, SensitiveSiteHumanApprovalRequirement required)
        {
            var audit = BrowserPersistentAuditLedger.Create(
                BrowserAuditLedgerEventKind.PolicyBlocked,
                request.RunId,
                request.ActionId,
                request.CorrelationId,
                "profile-sensitive-policy",
                "session-sensitive-policy",
                null,
                null,
                null,
                kind.ToString(),
                BrowserCredentialRedactor.Redact(string.Join(",", reasons.Select(r => r.ToString()))),
                new Dictionary<string, string>
                {
                    ["category"] = site?.Category.ToString() ?? "Unknown",
                    ["risk"] = site?.RiskLevel.ToString() ?? "Unknown",
                    ["action"] = request.ActionKind.ToString(),
                    ["approval"] = required.ToString(),
                    ["blocked"] = string.Join("|", remainsBlocked)
                });
            return new SensitiveSitePolicyDecision(
                kind,
                site,
                required,
                reasons,
                violations,
                remainsBlocked,
                new SensitiveSiteAuditRequirement(true, true, true, true, true, true, true, ExcludeSecretsCookiesBodies: true),
                audit,
                Redacted: true);
        }
    }

    public static SensitiveSiteRiskLevel DefaultRisk(SensitiveSiteCategory category) =>
        category switch
        {
            SensitiveSiteCategory.Fiscal or SensitiveSiteCategory.Banking or SensitiveSiteCategory.Payments or SensitiveSiteCategory.Identity => SensitiveSiteRiskLevel.Critical,
            SensitiveSiteCategory.ERP or SensitiveSiteCategory.Payroll or SensitiveSiteCategory.Healthcare or SensitiveSiteCategory.Legal or SensitiveSiteCategory.Government or SensitiveSiteCategory.ProductionAdmin or SensitiveSiteCategory.CustomerData => SensitiveSiteRiskLevel.HighRisk,
            SensitiveSiteCategory.Financial => SensitiveSiteRiskLevel.Critical,
            _ => SensitiveSiteRiskLevel.Prohibited
        };

    private static SensitiveSiteHumanApprovalRequirement ApprovalFor(SensitiveSiteClassification classification, SensitiveSiteActionKind action) =>
        action switch
        {
            SensitiveSiteActionKind.ReadOnlyView => classification.RiskLevel == SensitiveSiteRiskLevel.Critical ? SensitiveSiteHumanApprovalRequirement.SingleApprovalRequired : SensitiveSiteHumanApprovalRequirement.NoApprovalRequired,
            SensitiveSiteActionKind.DownloadDocument or SensitiveSiteActionKind.UploadDocument or SensitiveSiteActionKind.FillForm or SensitiveSiteActionKind.SaveDraft => SensitiveSiteHumanApprovalRequirement.SingleApprovalRequired,
            SensitiveSiteActionKind.Submit or SensitiveSiteActionKind.Pay or SensitiveSiteActionKind.Delete or SensitiveSiteActionKind.Publish or SensitiveSiteActionKind.Approve or SensitiveSiteActionKind.Sign => SensitiveSiteHumanApprovalRequirement.Prohibited,
            SensitiveSiteActionKind.ChangeCredentials or SensitiveSiteActionKind.ChangeProfile => SensitiveSiteHumanApprovalRequirement.Prohibited,
            _ => SensitiveSiteHumanApprovalRequirement.DoubleApprovalRequired
        };

    private static SensitiveSiteDecisionKind DecisionFor(SensitiveSiteActionKind action, SensitiveSiteHumanApprovalRequirement approval) =>
        approval == SensitiveSiteHumanApprovalRequirement.Prohibited || IsProhibited(action)
            ? SensitiveSiteDecisionKind.Prohibited
            : action == SensitiveSiteActionKind.ReadOnlyView
                ? SensitiveSiteDecisionKind.AllowReadOnlyWithApproval
                : SensitiveSiteDecisionKind.RequiresApproval;

    private static bool ApprovalSatisfied(SensitiveSiteHumanApprovalRequirement approval, SensitiveSitePolicyRequest request) =>
        approval switch
        {
            SensitiveSiteHumanApprovalRequirement.NoApprovalRequired => true,
            SensitiveSiteHumanApprovalRequirement.SingleApprovalRequired => request.HumanApprovalRefs.Count >= 1,
            SensitiveSiteHumanApprovalRequirement.DoubleApprovalRequired => request.HumanApprovalRefs.Count >= 2,
            SensitiveSiteHumanApprovalRequirement.ExplicitTypedConfirmationRequired => request.HumanApprovalRefs.Count >= 1,
            _ => false
        };

    private static bool RequiresApproval(SensitiveSiteHumanApprovalRequirement approval) =>
        approval is SensitiveSiteHumanApprovalRequirement.SingleApprovalRequired or SensitiveSiteHumanApprovalRequirement.DoubleApprovalRequired or SensitiveSiteHumanApprovalRequirement.ExplicitTypedConfirmationRequired;

    private static bool IsIrreversible(SensitiveSiteActionKind action) =>
        action is SensitiveSiteActionKind.Submit or SensitiveSiteActionKind.Pay or SensitiveSiteActionKind.Delete or SensitiveSiteActionKind.Publish or SensitiveSiteActionKind.Approve or SensitiveSiteActionKind.Sign;

    private static bool IsProhibited(SensitiveSiteActionKind action) =>
        action is SensitiveSiteActionKind.ChangeCredentials or SensitiveSiteActionKind.ChangeProfile or SensitiveSiteActionKind.Submit or SensitiveSiteActionKind.Pay or SensitiveSiteActionKind.Delete or SensitiveSiteActionKind.Publish or SensitiveSiteActionKind.Approve or SensitiveSiteActionKind.Sign;
}

