using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaPrivatePreviewOperatorFlowService
{
    public NexaPrivatePreviewOperatorResult Run(NexaPrivatePreviewOperatorFlow flow, NexaPrivatePreviewLocalReadiness readiness)
    {
        var reasons = new List<string>();
        if (!flow.Session.LocalOnly || !flow.FlowId.StartsWith("operator-flow-local", StringComparison.Ordinal))
            reasons.Add("operator flow must be local-only");
        if (!flow.Session.SingleTenant)
            reasons.Add("operator flow must be single-tenant");
        if (flow.Session.ConfigProfile is NexaConfigurationProfileKind.ProductionLocked or NexaConfigurationProfileKind.EnterpriseControlled or NexaConfigurationProfileKind.Unknown)
            reasons.Add("configuration profile unsafe");
        if (!readiness.ConfigProfileCompatible)
            reasons.Add("configuration profile incompatible");
        if (!readiness.LicenseMockValid)
            reasons.Add("mock license invalid or expired");
        if (!readiness.TenantGovernanceAvailable)
            reasons.Add("tenant governance unavailable");
        if (!readiness.DiagnosticsRedacted)
            reasons.Add("diagnostics redaction unavailable");
        if (!readiness.AuditKeyCustodyAvailable)
            reasons.Add("audit key custody missing");
        if (flow.PublicSaasEnabled)
            reasons.Add("public SaaS blocked");
        if (flow.RealBillingEnabled)
            reasons.Add("real billing blocked");
        if (flow.RealEmailEnabled)
            reasons.Add("real email blocked");
        if (flow.SensitiveRealPilotEnabled)
            reasons.Add("sensitive real pilot blocked");
        if (flow.ProductiveRecorderReplayEnabled)
            reasons.Add("productive recorder/replay blocked");
        if (flow.RequiresExternalMode && flow.M51ExternalProofDeferred)
            reasons.Add("M51 external proof deferred blocks external mode");
        if (!flow.UsedPrivateLocalApiInProcess)
            reasons.Add("private local API must be in-process");
        if (flow.ContainsSecretsCookiesBodies || !flow.Redacted || !flow.Evidence.Redacted)
            reasons.Add("operator flow report contains sensitive material");

        var steps = ExpectedSteps();
        if (steps.Any(step => flow.Steps.All(observed => observed.Step != step)))
            reasons.Add("operator flow missing required step");

        var decision = new NexaPrivatePreviewOperatorDecision(
            reasons.Count == 0 ? NexaPrivatePreviewOperatorDecisionKind.Allowed : NexaPrivatePreviewOperatorDecisionKind.Blocked,
            reasons,
            Redacted: true);
        return new NexaPrivatePreviewOperatorResult(
            decision,
            flow,
            reasons.Count == 0 ? NexaPrivatePreviewOperatorFinalStatus.Completed : NexaPrivatePreviewOperatorFinalStatus.Blocked,
            BrowserCredentialRedactor.Redact($"session {flow.Session.SessionId} completed with {reasons.Count} blockers"),
            Redacted: true);
    }

    public NexaPrivatePreviewOperatorFlow CreateSafeFlow() =>
        new(
            "operator-flow-local-safe",
            new NexaPrivatePreviewOperatorSession(
                "operator-session-local",
                "operator-local",
                NexaRole.Operator,
                "tenant-local",
                "workspace-local",
                NexaConfigurationProfileKind.LocalSandbox,
                "mock trial license active",
                "safe local features only",
                LocalOnly: true,
                SingleTenant: true),
            ExpectedSteps().Select(step => new NexaPrivatePreviewOperatorStep(step, Passed: true, "ok")).ToArray(),
            new NexaPrivatePreviewOperatorEvidence(["diag:local:redacted"], ["audit:local:redacted"], ["gate:local:passed"], Redacted: true),
            UsedPrivateLocalApiInProcess: true,
            PublicSaasEnabled: false,
            RealBillingEnabled: false,
            RealEmailEnabled: false,
            SensitiveRealPilotEnabled: false,
            ProductiveRecorderReplayEnabled: false,
            RequiresExternalMode: false,
            M51ExternalProofDeferred: true,
            ContainsSecretsCookiesBodies: false,
            Redacted: true);

    private static IReadOnlyList<NexaPrivatePreviewOperatorStepKind> ExpectedSteps() =>
    [
        NexaPrivatePreviewOperatorStepKind.StartSession,
        NexaPrivatePreviewOperatorStepKind.ValidateConfigProfile,
        NexaPrivatePreviewOperatorStepKind.ValidateMockLicense,
        NexaPrivatePreviewOperatorStepKind.ValidateTenantWorkspace,
        NexaPrivatePreviewOperatorStepKind.OpenLocalProductShell,
        NexaPrivatePreviewOperatorStepKind.CheckDiagnostics,
        NexaPrivatePreviewOperatorStepKind.CheckFeatureFlags,
        NexaPrivatePreviewOperatorStepKind.CheckAuditIntegrity,
        NexaPrivatePreviewOperatorStepKind.UsePrivateLocalApi,
        NexaPrivatePreviewOperatorStepKind.GenerateSessionSummary,
        NexaPrivatePreviewOperatorStepKind.CloseSession
    ];
}

public sealed class NexaPrivatePreviewIssueTriageService
{
    public NexaPrivatePreviewIssueDecision Triage(NexaPrivatePreviewIssueTriage issue)
    {
        var reasons = new List<string>();
        var actions = new List<NexaPrivatePreviewIssueAction>();

        var securityBlocker =
            issue.SecretCookieBodyLeak ||
            issue.CrossTenantAccess ||
            issue.VaultRawExposure ||
            issue.SupportCanSeeSecret ||
            issue.PublicApiExposure ||
            issue.RealBillingEmailEnabled ||
            issue.SensitiveRealPilotEnabled;
        var releaseBlocker =
            issue.BuildOrTestFailed ||
            issue.GateFailed ||
            issue.DiagnosticsUnavailable ||
            issue.AuditIntegrityUnavailable ||
            issue.RunbookMissing ||
            issue.ApiRoleEnforcementBroken;

        if (issue.ContainsSecretsCookiesBodies)
            reasons.Add("issue report redacted sensitive material");
        if (securityBlocker)
        {
            reasons.Add("security blocker");
            actions.Add(Action("security-action", "SecurityOwner", "fix security blocker before next preview", required: true));
            return Decision(NexaPrivatePreviewIssueDecisionKind.SecurityBlocker, NexaPrivatePreviewIssueDisposition.Blocked, reasons, actions);
        }
        if (releaseBlocker)
        {
            reasons.Add("release blocker");
            actions.Add(Action("release-action", "ReleaseOwner", "fix release blocker before next preview", required: true));
            return Decision(NexaPrivatePreviewIssueDecisionKind.ReleaseBlocker, NexaPrivatePreviewIssueDisposition.Blocked, reasons, actions);
        }
        if (issue.Severity is NexaPrivatePreviewIssueSeverity.High or NexaPrivatePreviewIssueSeverity.Critical or NexaPrivatePreviewIssueSeverity.Blocker)
        {
            reasons.Add("high severity requires action");
            actions.Add(Action("high-severity-action", "ProductOwner", "triage high severity issue before next preview", required: true));
            return Decision(NexaPrivatePreviewIssueDecisionKind.FixBeforeNextPreview, NexaPrivatePreviewIssueDisposition.Open, reasons, actions);
        }

        reasons.Add("accepted non-blocking issue");
        return Decision(NexaPrivatePreviewIssueDecisionKind.Accept, NexaPrivatePreviewIssueDisposition.Accepted, reasons, actions);
    }

    private static NexaPrivatePreviewIssueDecision Decision(NexaPrivatePreviewIssueDecisionKind kind, NexaPrivatePreviewIssueDisposition disposition, IReadOnlyList<string> reasons, IReadOnlyList<NexaPrivatePreviewIssueAction> actions) =>
        new(kind, disposition, reasons.Select(BrowserCredentialRedactor.Redact).ToArray(), actions, Redacted: true);

    private static NexaPrivatePreviewIssueAction Action(string id, string owner, string summary, bool required) =>
        new(id, owner, BrowserCredentialRedactor.Redact(summary), required);
}

public sealed class NexaPrivatePreviewGoNoGoService
{
    public NexaPrivatePreviewGoNoGoReport Evaluate(NexaPrivatePreviewExitCriteria criteria, IReadOnlyList<NexaPrivatePreviewIssueDecision> issues)
    {
        var blockers = new List<string>();
        if (!criteria.BuildOk || !criteria.SuiteOk || !criteria.GateOk)
            blockers.Add("build suite or gate not ok");
        if (!criteria.NoCriticalHighSecurityBlockers || issues.Any(issue => issue.Decision == NexaPrivatePreviewIssueDecisionKind.SecurityBlocker))
            blockers.Add("security blocker unresolved");
        if (!criteria.NoUnresolvedReleaseBlockers || issues.Any(issue => issue.Decision == NexaPrivatePreviewIssueDecisionKind.ReleaseBlocker))
            blockers.Add("release blocker unresolved");
        if (!criteria.AuditKeyCustodyOk)
            blockers.Add("audit key custody missing");
        if (!criteria.DiagnosticsRedactionOk)
            blockers.Add("diagnostics redaction missing");
        if (!criteria.TenantGovernanceOk)
            blockers.Add("tenant governance missing");
        if (!criteria.PrivateLocalApiRoleEnforcementOk)
            blockers.Add("private local API role enforcement missing");
        if (!criteria.SupportMetadataOnly)
            blockers.Add("support mode must be metadata-only");
        if (!criteria.BillingEmailMockOrSandbox)
            blockers.Add("billing/email must remain mock or sandbox");
        if (!criteria.PublicSaasDisabled || !criteria.PublicApiListenerDisabled)
            blockers.Add("public SaaS/API listener must remain disabled");
        if (!criteria.M51ExternalProofStatusExplicit)
            blockers.Add("M51 external proof status missing");
        if (criteria.ContainsSecretsCookiesBodies)
            blockers.Add("Go/No-Go report contains sensitive material");

        var decision = blockers.Count switch
        {
            > 0 when blockers.Any(blocker => blocker.Contains("security", StringComparison.OrdinalIgnoreCase)) => NexaPrivatePreviewGoNoGoDecisionKind.NoGoSecurityBlocker,
            > 0 when blockers.Any(blocker => blocker.Contains("release", StringComparison.OrdinalIgnoreCase) || blocker.Contains("build", StringComparison.OrdinalIgnoreCase)) => NexaPrivatePreviewGoNoGoDecisionKind.NoGoReleaseBlocker,
            > 0 => NexaPrivatePreviewGoNoGoDecisionKind.NoGoMissingEvidence,
            _ when criteria.M51ExternalProofDeferred => NexaPrivatePreviewGoNoGoDecisionKind.GoForNextLocalPreview,
            _ => NexaPrivatePreviewGoNoGoDecisionKind.GoForExternalTargetSetup
        };
        var recommendation = decision switch
        {
            NexaPrivatePreviewGoNoGoDecisionKind.GoForNextLocalPreview => NexaPrivatePreviewNextStageRecommendation.ContinueLocalPrivatePreview,
            NexaPrivatePreviewGoNoGoDecisionKind.GoForExternalTargetSetup => NexaPrivatePreviewNextStageRecommendation.CreateExternalTestOwnedTarget,
            NexaPrivatePreviewGoNoGoDecisionKind.NoGoSecurityBlocker => NexaPrivatePreviewNextStageRecommendation.StopForExternalAudit,
            NexaPrivatePreviewGoNoGoDecisionKind.NoGoReleaseBlocker => NexaPrivatePreviewNextStageRecommendation.ContinueLocalPrivatePreview,
            _ => NexaPrivatePreviewNextStageRecommendation.ContinueLocalPrivatePreview
        };

        return new NexaPrivatePreviewGoNoGoReport(
            "private-preview-go-nogo",
            criteria,
            issues,
            decision,
            recommendation,
            blockers.Select(BrowserCredentialRedactor.Redact).ToArray(),
            PublicSaasStillDisabled: criteria.PublicSaasDisabled,
            RealBillingStillDisabled: criteria.BillingEmailMockOrSandbox,
            RealEmailStillDisabled: criteria.BillingEmailMockOrSandbox,
            M51DeferredExplicit: criteria.M51ExternalProofStatusExplicit && criteria.M51ExternalProofDeferred,
            Redacted: true);
    }

    public static NexaPrivatePreviewExitCriteria SafeCriteria() =>
        new(
            BuildOk: true,
            SuiteOk: true,
            GateOk: true,
            NoCriticalHighSecurityBlockers: true,
            NoUnresolvedReleaseBlockers: true,
            AuditKeyCustodyOk: true,
            DiagnosticsRedactionOk: true,
            TenantGovernanceOk: true,
            PrivateLocalApiRoleEnforcementOk: true,
            SupportMetadataOnly: true,
            BillingEmailMockOrSandbox: true,
            PublicSaasDisabled: true,
            PublicApiListenerDisabled: true,
            M51ExternalProofStatusExplicit: true,
            M51ExternalProofDeferred: true,
            ContainsSecretsCookiesBodies: false);
}
