namespace OneBrain.Core.AppProfiles;

public static class AppProfilePolicy
{
    public static AppProfileValidationResult Validate(AppProfile profile)
    {
        var issues = new List<AppProfileValidationIssue>();
        var capabilities = profile.SupportedCapabilities.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(profile.Id))
            issues.Add(Issue("blocked", "missing_id", "App profile is missing id.", "Add a stable profile id."));
        if (string.IsNullOrWhiteSpace(profile.Name))
            issues.Add(Issue("error", "missing_name", "App profile is missing name.", "Add a human-readable name."));

        var externalFragile = profile.Status == AppProfileStatuses.ExternalFragile ||
                              capabilities.Contains(AppProfileCapabilities.ExternalFragile);
        if (externalFragile && !profile.RiskPolicy.DiagnosticAllowed && !capabilities.Contains(AppProfileCapabilities.DiagnosticAllowed))
            issues.Add(Issue("blocked", "external_fragile_requires_diagnostic_allowed", "External-fragile profiles require diagnosticAllowed.", "Enable diagnostic_allowed or keep profile blocked."));

        if (capabilities.Contains(AppProfileCapabilities.Login) || !profile.RiskPolicy.BlocksLogin)
            issues.Add(Issue("blocked", "login_blocked_by_default", "Login capability must remain blocked by default.", "Remove login capability or require platform approval."));
        if (capabilities.Contains(AppProfileCapabilities.AcceptCookies) || !profile.RiskPolicy.BlocksCookies)
            issues.Add(Issue("blocked", "cookies_blocked_by_default", "Cookie acceptance must remain blocked by default.", "Remove cookie capability or require platform approval."));
        if (capabilities.Contains(AppProfileCapabilities.Payment) || !profile.RiskPolicy.BlocksPayment)
            issues.Add(Issue("blocked", "payment_blocked_by_default", "Payment capability must remain blocked by default.", "Remove payment capability."));
        if (capabilities.Contains(AppProfileCapabilities.Purchase) || !profile.RiskPolicy.BlocksPurchase)
            issues.Add(Issue("blocked", "purchase_blocked_by_default", "Purchase capability must remain blocked by default.", "Remove purchase capability."));

        if ((capabilities.Contains(AppProfileCapabilities.SafeClick) || capabilities.Contains(AppProfileCapabilities.TextInput)) &&
            profile.Status != AppProfileStatuses.Draft &&
            !profile.RiskPolicy.RequiresApprovalForSubmit)
            issues.Add(Issue("error", "interactive_capability_requires_approval_policy", "Interactive capabilities require approval policy before activation.", "Keep profile as draft or require approval."));

        if (!profile.RiskPolicy.ReadOnlyByDefault)
            issues.Add(Issue("warning", "not_read_only_by_default", "Profile is not read-only by default.", "Prefer read-only by default for v0."));

        var hasBlockingIssue = issues.Any(issue => issue.Severity is "blocked" or "error");
        return new AppProfileValidationResult(
            CanActivate: !hasBlockingIssue,
            RequiresValidationBeforePromotion: issues.Count > 0 || profile.Status == AppProfileStatuses.Draft,
            Issues: issues);
    }

    private static AppProfileValidationIssue Issue(string severity, string code, string message, string remediation)
    {
        return new AppProfileValidationIssue(severity, code, message, remediation);
    }
}
