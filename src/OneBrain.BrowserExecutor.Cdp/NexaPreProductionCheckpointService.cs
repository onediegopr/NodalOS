using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaPreProductionCheckpointService
{
    public NexaPreProductionCheckpointReport Create(NexaPreProductionCheckpointRequest request)
    {
        var capabilities = Capabilities(request);
        var risks = Risks(request);
        var blockers = Blockers(request);
        var matrix = DecisionMatrix(request);
        var recommendation = Recommend(request);
        return new NexaPreProductionCheckpointReport(
            "preprod-checkpoint-m49",
            capabilities,
            new NexaPreProductionRiskRegister(risks),
            matrix,
            blockers,
            recommendation,
            Redacted: true,
            ContainsSecretsCookiesBodies: false);
    }

    private static IReadOnlyList<NexaPreProductionCapabilityStatus> Capabilities(NexaPreProductionCheckpointRequest request) =>
    [
        Cap(NexaPreProductionCapabilityKind.BrowserRuntime, true, true, "runtime governed"),
        Cap(NexaPreProductionCapabilityKind.CDPLiveReadOnly, true, true, "read-only proof complete"),
        Cap(NexaPreProductionCapabilityKind.SafeDownload, true, true, "safe download available"),
        Cap(NexaPreProductionCapabilityKind.SafeUpload, true, true, "safe upload available"),
        Cap(NexaPreProductionCapabilityKind.DocumentWorkflowSandbox, true, true, "sandbox workflow complete"),
        Cap(NexaPreProductionCapabilityKind.RecorderReadOnly, true, true, "read-only prototype only"),
        Cap(NexaPreProductionCapabilityKind.ReplaySafeMode, true, true, "safe mode only"),
        Cap(NexaPreProductionCapabilityKind.SensitiveSimulation, true, true, "simulation complete"),
        Cap(NexaPreProductionCapabilityKind.SensitiveRealPilot, false, request.SensitiveRealPilotDecisionApproved, "blocked before external audit"),
        Cap(NexaPreProductionCapabilityKind.ProductAdmin, true, true, "private preview ready"),
        Cap(NexaPreProductionCapabilityKind.Licensing, true, true, "mock/governance ready"),
        Cap(NexaPreProductionCapabilityKind.TenantGovernance, true, true, "tenant isolation model ready"),
        Cap(NexaPreProductionCapabilityKind.AuditExport, true, true, "redacted export ready"),
        Cap(NexaPreProductionCapabilityKind.Diagnostics, true, true, "redacted diagnostics ready"),
        Cap(NexaPreProductionCapabilityKind.PackagingDryRun, true, true, "dry-run only"),
        Cap(NexaPreProductionCapabilityKind.PublicApiBoundary, true, false, "design-only"),
        Cap(NexaPreProductionCapabilityKind.VaultOsBackedMinimal, true, false, "synthetic/test only"),
        Cap(NexaPreProductionCapabilityKind.ReleaseUpdateModel, true, false, "model-only update"),
        Cap(NexaPreProductionCapabilityKind.BillingMock, true, false, "mock-only"),
        Cap(NexaPreProductionCapabilityKind.OnboardingMock, true, false, "mock-only")
    ];

    private static NexaPreProductionCapabilityStatus Cap(NexaPreProductionCapabilityKind kind, bool available, bool productionEnabled, string status) =>
        new(kind, available, productionEnabled, status, Redacted: true);

    private static IReadOnlyList<NexaPreProductionRisk> Risks(NexaPreProductionCheckpointRequest request) =>
    [
        new("risk-m25b-blocked", "External low-risk target remains unavailable.", "Medium", "Provision a test-owned external target.", "M25B", !request.M25BExternalLowRiskTargetAvailable),
        new("risk-sensitive-real-pilot", "Sensitive real pilot is not approved.", "High", "Run external audit and compliance decision.", "M33B/M34B", !request.SensitiveRealPilotDecisionApproved),
        new("risk-public-saas", "Public SaaS remains disabled.", "High", "Build private/local API first and security review.", "Post-M49", !request.PublicSaasEnabled),
        new("risk-real-billing", "Real billing remains disabled.", "Medium", "Design billing real with provider sandbox first.", "BillingReal", !request.RealBillingEnabled),
        new("risk-auto-update", "Real auto-update remains disabled.", "Medium", "Add signing and executable update audit before enabling.", "ReleaseReal", !request.AutoUpdateRealEnabled)
    ];

    private static IReadOnlyList<NexaPreProductionBlocker> Blockers(NexaPreProductionCheckpointRequest request)
    {
        var blockers = new List<NexaPreProductionBlocker>();
        if (!request.M25BExternalLowRiskTargetAvailable)
            blockers.Add(new("blocker-m25b", "M25B external low-risk target is still blocked.", true));
        if (!request.SensitiveRealPilotDecisionApproved)
            blockers.Add(new("blocker-sensitive-real-pilot", "Sensitive real pilot requires external audit and decision.", true));
        if (!request.PublicSaasEnabled)
            blockers.Add(new("blocker-public-saas", "Public SaaS remains disabled.", true));
        if (!request.RealBillingEnabled)
            blockers.Add(new("blocker-real-billing", "Real billing remains disabled.", true));
        if (!request.RealEmailEnabled)
            blockers.Add(new("blocker-real-email", "Real email delivery remains disabled.", true));
        if (!request.AutoUpdateRealEnabled)
            blockers.Add(new("blocker-auto-update", "Real auto-update remains disabled.", true));
        if (!request.ProductiveRecorderReplayEnabled)
            blockers.Add(new("blocker-productive-recorder-replay", "Productive recorder/replay remains blocked.", true));
        if (!request.ProfileRawEnabled)
            blockers.Add(new("blocker-profile-raw", "Raw personal profile remains blocked.", true));
        if (!request.RealClientCredentialsEnabled)
            blockers.Add(new("blocker-real-client-credentials", "Real client credentials remain blocked.", true));
        return blockers;
    }

    private static NexaPreProductionDecisionMatrix DecisionMatrix(NexaPreProductionCheckpointRequest request) =>
        new(
            [
                Option("A", "Validate external auth safely.", "External dependency.", ["test-owned target"], request.M25BExternalLowRiskTargetAvailable ? [] : ["M25B target missing"], request.M25BExternalLowRiskTargetAvailable ? "advance with conditions" : "setup target first", ["M25B"]),
                Option("B", "Move product/admin toward private usage.", "Low operational risk.", ["local shell", "tenant governance"], [], "advance", ["PrivatePreview"]),
                Option("C", "Improve vault readiness.", "Secret handling risk.", ["OS-backed provider", "external audit"], [], "advance with conditions", ["VaultHardening"]),
                Option("D", "Improve browser containment.", "Architecture complexity.", ["runtime shell decision"], [], "evaluate", ["WebView2/CEF"]),
                Option("E", "Prepare private/local API.", "Boundary hardening needed.", ["M47 boundary", "tenant tests"], ["no public SaaS"], "advance locally only", ["PrivateApi"]),
                Option("F", "Prepare real billing design.", "Payment compliance.", ["provider sandbox design"], ["real billing disabled"], "design only", ["BillingDesign"]),
                Option("G", "Prepare sensitive pilot.", "Critical compliance risk.", ["external audit", "legal approval"], ["sensitive real pilot blocked"], "do not advance yet", ["M33B/M34B prep"]),
                Option("H", "External audit with Claude.", "Coordination overhead.", ["M1-M49 report"], [], "advance", ["ExternalAudit"])
            ]);

    private static NexaPreProductionDecisionOption Option(string id, string benefit, string risk, IReadOnlyList<string> preconditions, IReadOnlyList<string> blockers, string recommendation, IReadOnlyList<string> milestones) =>
        new(id, benefit, risk, preconditions, blockers, recommendation, milestones);

    private static NexaPreProductionRecommendation Recommend(NexaPreProductionCheckpointRequest request)
    {
        if (!request.SensitiveRealPilotDecisionApproved)
            return new(NexaPreProductionStatus.ReadyForProductAdminPrivatePreview, "Product/Admin private preview plus external audit checkpoint", ExternalAuditRequired: true);
        if (!request.M25BExternalLowRiskTargetAvailable)
            return new(NexaPreProductionStatus.ReadyForExternalLowRiskTargetSetup, "Provision test-owned external target before real external workflow", ExternalAuditRequired: true);
        return new(NexaPreProductionStatus.ReadyForLocalPrivatePilot, "Local private pilot only", ExternalAuditRequired: true);
    }
}
