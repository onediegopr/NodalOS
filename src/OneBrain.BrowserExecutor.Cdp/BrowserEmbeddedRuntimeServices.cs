using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserRuntimeArchitectureDecisionService
{
    public BrowserRuntimeArchitectureDecision CreateDecision()
    {
        var options = new[]
        {
            new BrowserEmbeddedRuntimeOption(BrowserEmbeddedRuntimeKind.ChromeCdpExternal, PrimarySupported: true, SandboxPrototypeAllowed: true, ProductionAllowed: true, "Current primary runtime; strongest current evidence and CDP governance."),
            new BrowserEmbeddedRuntimeOption(BrowserEmbeddedRuntimeKind.WebView2Embedded, PrimarySupported: false, SandboxPrototypeAllowed: true, ProductionAllowed: false, "Useful for Windows packaging experiments, sandbox only until future hito."),
            new BrowserEmbeddedRuntimeOption(BrowserEmbeddedRuntimeKind.CefEmbedded, PrimarySupported: false, SandboxPrototypeAllowed: true, ProductionAllowed: false, "Cross-platform candidate with higher packaging and maintenance cost.")
        };

        var tradeoffs = new List<BrowserRuntimeArchitectureTradeoff>();
        AddTradeoffs(tradeoffs, BrowserEmbeddedRuntimeKind.ChromeCdpExternal, 9, 9, 8, 7, 8, 8, 8, 9, 9, 2);
        AddTradeoffs(tradeoffs, BrowserEmbeddedRuntimeKind.WebView2Embedded, 7, 8, 6, 8, 6, 7, 8, 7, 7, 6);
        AddTradeoffs(tradeoffs, BrowserEmbeddedRuntimeKind.CefEmbedded, 6, 8, 5, 5, 6, 7, 6, 5, 7, 7);

        var risks = new[]
        {
            Risk("risk-webview2-authority", BrowserEmbeddedRuntimeKind.WebView2Embedded, "High", "Embedded runtime could be incorrectly treated as authority.", "Core/FSM/Safety remains sole authority."),
            Risk("risk-cef-packaging", BrowserEmbeddedRuntimeKind.CefEmbedded, "Medium", "CEF increases package/update surface.", "Keep CEF design-only until release/update hardening."),
            Risk("risk-cookie-session", BrowserEmbeddedRuntimeKind.WebView2Embedded, "High", "Embedded profile can leak cookies/session if unmanaged.", "Sandbox fixture only; cookies/session sealed."),
            Risk("risk-maintenance", BrowserEmbeddedRuntimeKind.CefEmbedded, "Medium", "CEF maintenance burden can outpace current product readiness.", "Do not replace Chrome/CDP.")
        };

        return new BrowserRuntimeArchitectureDecision(
            "browser-runtime-architecture-m68",
            options,
            tradeoffs,
            risks,
            BrowserRuntimeArchitectureRecommendation.KeepChromeCdpPrimary,
            BrowserEmbeddedRuntimeKind.ChromeCdpExternal,
            ChromeCdpRemainsPrimary: true,
            EmbeddedRuntimeProductionDisabled: true,
            Redacted: true);
    }

    private static void AddTradeoffs(List<BrowserRuntimeArchitectureTradeoff> tradeoffs, BrowserEmbeddedRuntimeKind kind, int safety, int control, int maintenance, int packaging, int debugging, int profileIsolation, int enterpriseInstall, int crossPlatform, int evidenceAudit, int authorityLeakRisk)
    {
        tradeoffs.Add(new("Safety", kind, Assessment(safety), safety));
        tradeoffs.Add(new("Control", kind, Assessment(control), control));
        tradeoffs.Add(new("Maintenance", kind, Assessment(maintenance), maintenance));
        tradeoffs.Add(new("Packaging", kind, Assessment(packaging), packaging));
        tradeoffs.Add(new("Debugging", kind, Assessment(debugging), debugging));
        tradeoffs.Add(new("Profile isolation", kind, Assessment(profileIsolation), profileIsolation));
        tradeoffs.Add(new("Enterprise install", kind, Assessment(enterpriseInstall), enterpriseInstall));
        tradeoffs.Add(new("Cross-platform", kind, Assessment(crossPlatform), crossPlatform));
        tradeoffs.Add(new("Evidence/audit integration", kind, Assessment(evidenceAudit), evidenceAudit));
        tradeoffs.Add(new("Risk of authority leak", kind, authorityLeakRisk <= 3 ? "low risk" : "requires containment", authorityLeakRisk));
    }

    private static string Assessment(int score) => score >= 8 ? "strong" : score >= 6 ? "moderate" : "weak";

    private static BrowserRuntimeArchitectureRisk Risk(string id, BrowserEmbeddedRuntimeKind kind, string severity, string description, string mitigation) =>
        new(id, kind, severity, description, mitigation);
}

public sealed class BrowserEmbeddedRuntimeSandboxRunner
{
    public BrowserEmbeddedRuntimeSandbox CreateSandbox(BrowserEmbeddedRuntimeKind kind = BrowserEmbeddedRuntimeKind.WebView2Embedded) =>
        new(
            kind,
            EnabledByDefault: false,
            LocalFixtureOnly: true,
            ProductionDisabled: true,
            ["/embedded/status", "/embedded/readonly", "/embedded/metadata"],
            "NEXA_EMBEDDED_RUNTIME_SANDBOX_OK");

    public BrowserEmbeddedRuntimeSandboxResult Run(BrowserEmbeddedRuntimeSandboxRequest request)
    {
        var reasons = new List<string>();
        if (!request.EnableSandbox)
            reasons.Add("embedded runtime sandbox disabled by default");
        if (request.RuntimeKind is BrowserEmbeddedRuntimeKind.ChromeCdpExternal or BrowserEmbeddedRuntimeKind.Disabled or BrowserEmbeddedRuntimeKind.Future)
            reasons.Add("embedded runtime sandbox requires WebView2 or CEF model");
        if (request.AllowExternalSites || !request.FixturePath.StartsWith("/embedded/", StringComparison.Ordinal))
            reasons.Add("embedded runtime sandbox local fixture only");
        if (request.AllowDownloads)
            reasons.Add("embedded runtime downloads disabled by default");
        if (request.AllowUploads)
            reasons.Add("embedded runtime uploads disabled by default");
        if (request.RuntimeAuthoritative)
            reasons.Add("embedded runtime cannot be authoritative");

        var allowed = reasons.Count == 0;
        var evidence = new BrowserEmbeddedRuntimeEvidence(
            allowed ? [$"embedded:{request.RequestId}:semantic-proof"] : [],
            SemanticProofVerified: allowed,
            CookiesExposed: false,
            BodiesCaptured: false,
            SensitiveHeaderValuesCaptured: false,
            Redacted: true);
        var decision = new BrowserEmbeddedRuntimeSafetyDecision(
            allowed ? BrowserEmbeddedRuntimeSafetyDecisionKind.SandboxOnly : BrowserEmbeddedRuntimeSafetyDecisionKind.Blocked,
            reasons,
            Redacted: true);

        return new BrowserEmbeddedRuntimeSandboxResult(
            decision,
            evidence,
            Capabilities(),
            NavigatedLocalFixture: allowed,
            ReadDom: allowed,
            ReplacedChromeCdp: false,
            ProductionActivated: false,
            allowed ? "NEXA_EMBEDDED_RUNTIME_SANDBOX_OK" : "",
            Redacted: true);
    }

    private static IReadOnlyList<BrowserEmbeddedRuntimeCapability> Capabilities() =>
    [
        Capability(BrowserRuntimeCapabilityKind.NavigationReadOnly, BrowserRuntimeProviderCapabilityStatus.SandboxOnly, requiresGate: true, requiresApproval: false),
        Capability(BrowserRuntimeCapabilityKind.DomReadOnly, BrowserRuntimeProviderCapabilityStatus.SandboxOnly, requiresGate: true, requiresApproval: false),
        Capability(BrowserRuntimeCapabilityKind.NetworkMetadataOnly, BrowserRuntimeProviderCapabilityStatus.SandboxOnly, requiresGate: true, requiresApproval: false),
        Capability(BrowserRuntimeCapabilityKind.SafeDownload, BrowserRuntimeProviderCapabilityStatus.Unsupported, requiresGate: true, requiresApproval: true),
        Capability(BrowserRuntimeCapabilityKind.SafeUpload, BrowserRuntimeProviderCapabilityStatus.Unsupported, requiresGate: true, requiresApproval: true),
        Capability(BrowserRuntimeCapabilityKind.ProfileControlled, BrowserRuntimeProviderCapabilityStatus.DesignOnly, requiresGate: true, requiresApproval: true),
        Capability(BrowserRuntimeCapabilityKind.VaultBoundary, BrowserRuntimeProviderCapabilityStatus.DesignOnly, requiresGate: true, requiresApproval: true),
        Capability(BrowserRuntimeCapabilityKind.RecorderReadOnly, BrowserRuntimeProviderCapabilityStatus.DesignOnly, requiresGate: true, requiresApproval: true),
        Capability(BrowserRuntimeCapabilityKind.ReplaySafeMode, BrowserRuntimeProviderCapabilityStatus.DesignOnly, requiresGate: true, requiresApproval: true),
        Capability(BrowserRuntimeCapabilityKind.SensitiveSimulation, BrowserRuntimeProviderCapabilityStatus.Unsupported, requiresGate: true, requiresApproval: true),
        Capability(BrowserRuntimeCapabilityKind.ExternalReadOnly, BrowserRuntimeProviderCapabilityStatus.Unsupported, requiresGate: true, requiresApproval: true)
    ];

    private static BrowserEmbeddedRuntimeCapability Capability(BrowserRuntimeCapabilityKind capability, BrowserRuntimeProviderCapabilityStatus status, bool requiresGate, bool requiresApproval) =>
        new(capability, status, requiresGate, requiresApproval);
}

public sealed class BrowserRuntimeProviderCompatibilityEvaluator
{
    public BrowserRuntimeProviderCompatibilityReport Evaluate(BrowserRuntimeProvider provider)
    {
        var reasons = new List<string>();
        if (!provider.SafetyProfile.CoreAuthorityRequired || provider.SafetyProfile.RuntimeAuthoritative)
            reasons.Add("runtime cannot be authoritative");
        if (provider.CapabilitySet.StatusFor(BrowserRuntimeCapabilityKind.NetworkMetadataOnly) is BrowserRuntimeProviderCapabilityStatus.Unsupported)
            reasons.Add("runtime must support network metadata-only");
        if (provider.SafetyProfile.ExposesCookiesOrSession)
            reasons.Add("runtime exposes cookies/session");
        if (provider.SafetyProfile.CapturesBodies)
            reasons.Add("runtime captures request/response bodies");
        if (provider.SafetyProfile.CapturesSensitiveHeaderValues)
            reasons.Add("runtime captures sensitive header values");
        if (provider.SafetyProfile.AllowsUnsafeDownloadUpload)
            reasons.Add("runtime allows unsafe download/upload");
        if (provider.SafetyProfile.AllowsIrreversibleActions)
            reasons.Add("runtime allows irreversible actions");
        if (!provider.SafetyProfile.ProducesEvidenceRefs)
            reasons.Add("runtime must produce evidence refs");
        if (!provider.SafetyProfile.RespectsCoreFsmSafety)
            reasons.Add("runtime must respect Core/FSM/Safety");
        if (provider.SafetyProfile.ProductionActive && provider.Kind is BrowserRuntimeProviderKind.WebView2EmbeddedSandbox or BrowserRuntimeProviderKind.CefEmbeddedSandbox)
            reasons.Add("embedded runtime production active is blocked");
        if (provider.SafetyProfile.ReplacesChromeCdpPrimary)
            reasons.Add("Chrome/CDP cannot be replaced without future decision");

        var decision = reasons.Count > 0
            ? BrowserRuntimeProviderDecisionKind.Blocked
            : provider.Kind == BrowserRuntimeProviderKind.ChromeCdpExternal
                ? BrowserRuntimeProviderDecisionKind.Compatible
                : BrowserRuntimeProviderDecisionKind.SandboxOnly;

        return new BrowserRuntimeProviderCompatibilityReport(
            provider,
            new BrowserRuntimeProviderDecision(decision, reasons, Redacted: true),
            ChromeCdpRemainsPrimary: !provider.SafetyProfile.ReplacesChromeCdpPrimary,
            EmbeddedRuntimeProductionDisabled: !provider.SafetyProfile.ProductionActive || provider.Kind == BrowserRuntimeProviderKind.ChromeCdpExternal,
            Redacted: true);
    }

    public static BrowserRuntimeProvider ChromeCdpPrimary() =>
        new(BrowserRuntimeProviderKind.ChromeCdpExternal, Primary: true, SandboxOnly: false, DesignOnly: false, CapabilitySet([
            Supported(BrowserRuntimeCapabilityKind.NavigationReadOnly),
            Supported(BrowserRuntimeCapabilityKind.DomReadOnly),
            Supported(BrowserRuntimeCapabilityKind.NetworkMetadataOnly),
            RequiresPolicy(BrowserRuntimeCapabilityKind.SafeDownload),
            RequiresPolicy(BrowserRuntimeCapabilityKind.SafeUpload),
            Supported(BrowserRuntimeCapabilityKind.ProfileControlled),
            Supported(BrowserRuntimeCapabilityKind.VaultBoundary),
            Supported(BrowserRuntimeCapabilityKind.RecorderReadOnly),
            Supported(BrowserRuntimeCapabilityKind.ReplaySafeMode),
            Supported(BrowserRuntimeCapabilityKind.SensitiveSimulation),
            RequiresPolicy(BrowserRuntimeCapabilityKind.ExternalReadOnly)
        ]), SafeProfile(productionActive: false, replacesChrome: false));

    public static BrowserRuntimeProvider WebView2Sandbox() =>
        EmbeddedSandbox(BrowserRuntimeProviderKind.WebView2EmbeddedSandbox);

    public static BrowserRuntimeProvider CefSandbox() =>
        EmbeddedSandbox(BrowserRuntimeProviderKind.CefEmbeddedSandbox);

    public static BrowserRuntimeProvider Unsafe(BrowserRuntimeProviderKind kind, BrowserRuntimeProviderSafetyProfile profile) =>
        new(kind, Primary: false, SandboxOnly: true, DesignOnly: false, CapabilitySet([
            Supported(BrowserRuntimeCapabilityKind.NavigationReadOnly),
            Supported(BrowserRuntimeCapabilityKind.DomReadOnly),
            Supported(BrowserRuntimeCapabilityKind.NetworkMetadataOnly)
        ]), profile);

    private static BrowserRuntimeProvider EmbeddedSandbox(BrowserRuntimeProviderKind kind) =>
        new(kind, Primary: false, SandboxOnly: true, DesignOnly: true, CapabilitySet([
            Sandbox(BrowserRuntimeCapabilityKind.NavigationReadOnly),
            Sandbox(BrowserRuntimeCapabilityKind.DomReadOnly),
            Sandbox(BrowserRuntimeCapabilityKind.NetworkMetadataOnly),
            Unsupported(BrowserRuntimeCapabilityKind.SafeDownload),
            Unsupported(BrowserRuntimeCapabilityKind.SafeUpload),
            DesignOnly(BrowserRuntimeCapabilityKind.ProfileControlled),
            DesignOnly(BrowserRuntimeCapabilityKind.VaultBoundary),
            DesignOnly(BrowserRuntimeCapabilityKind.RecorderReadOnly),
            DesignOnly(BrowserRuntimeCapabilityKind.ReplaySafeMode),
            Unsupported(BrowserRuntimeCapabilityKind.SensitiveSimulation),
            Unsupported(BrowserRuntimeCapabilityKind.ExternalReadOnly)
        ]), SafeProfile(productionActive: false, replacesChrome: false));

    private static BrowserRuntimeProviderCapabilitySet CapabilitySet(IReadOnlyList<BrowserRuntimeProviderCapability> capabilities) =>
        new(capabilities);

    private static BrowserRuntimeProviderCapability Supported(BrowserRuntimeCapabilityKind capability) =>
        new(capability, BrowserRuntimeProviderCapabilityStatus.Supported);

    private static BrowserRuntimeProviderCapability Sandbox(BrowserRuntimeCapabilityKind capability) =>
        new(capability, BrowserRuntimeProviderCapabilityStatus.SandboxOnly);

    private static BrowserRuntimeProviderCapability DesignOnly(BrowserRuntimeCapabilityKind capability) =>
        new(capability, BrowserRuntimeProviderCapabilityStatus.DesignOnly);

    private static BrowserRuntimeProviderCapability Unsupported(BrowserRuntimeCapabilityKind capability) =>
        new(capability, BrowserRuntimeProviderCapabilityStatus.Unsupported);

    private static BrowserRuntimeProviderCapability RequiresPolicy(BrowserRuntimeCapabilityKind capability) =>
        new(capability, BrowserRuntimeProviderCapabilityStatus.RequiresGate);

    public static BrowserRuntimeProviderSafetyProfile SafeProfile(bool productionActive, bool replacesChrome) =>
        new(
            CoreAuthorityRequired: true,
            RuntimeAuthoritative: false,
            ExposesCookiesOrSession: false,
            CapturesBodies: false,
            CapturesSensitiveHeaderValues: false,
            AllowsUnsafeDownloadUpload: false,
            AllowsIrreversibleActions: false,
            ProducesEvidenceRefs: true,
            RespectsCoreFsmSafety: true,
            ProductionActive: productionActive,
            ReplacesChromeCdpPrimary: replacesChrome);
}
