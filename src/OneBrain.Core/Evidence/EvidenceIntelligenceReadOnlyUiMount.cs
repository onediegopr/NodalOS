namespace OneBrain.Core.Evidence;

public sealed record EvidenceIntelligenceReadOnlyUiMountViewModel(
    string MountId,
    string Route,
    string NavigationLabel,
    EvidenceIntelligenceSurfaceViewModel Surface,
    IReadOnlyList<string> StatusBadges,
    IReadOnlyList<string> SafetyNotices,
    IReadOnlyList<string> VisibleSections,
    IReadOnlyList<string> AllowedUiActions,
    IReadOnlyList<string> ForbiddenUiActions,
    bool RouteVisible,
    bool UsesPresenter,
    bool UsesDeterministicFixture,
    bool ReadOnly,
    bool LocalOnly,
    bool RuntimeEnabled,
    bool BrowserCdpAutomationEnabled,
    bool WcuLiveEnabled,
    bool OcrLiveEnabled,
    bool ProviderCloudEnabled,
    bool DurablePersistenceEnabled,
    bool SemanticVectorBackendEnabled,
    bool FilesystemWritesEnabled);

public static class EvidenceIntelligenceReadOnlyUiMount
{
    public const string MountId = "evidence-intelligence.ui.read-only.mount.v1";
    public const string Route = "#evidenceIntelligenceSurface";
    public const string NavigationLabel = "Evidence Intelligence";

    public static EvidenceIntelligenceReadOnlyUiMountViewModel CreateFixture()
    {
        var surface = EvidenceIntelligenceReadOnlyPresenter.CreateFixture();
        return Create(surface);
    }

    public static EvidenceIntelligenceReadOnlyUiMountViewModel Create(EvidenceIntelligenceSurfaceViewModel surface) =>
        new(
            MountId,
            Route,
            NavigationLabel,
            surface,
            ["READ_ONLY", "LOCAL_ONLY", "NO_RUNTIME"],
            [
                "Read-only.",
                "Local fixture / local evidence only.",
                "Semantic backend disabled.",
                "No runtime actions.",
                "No browser/CDP automation.",
                "No WCU live.",
                "No OCR live.",
                "No filesystem writes.",
                "No provider/cloud calls.",
                "Human approval required for any real-world action."
            ],
            [
                "Evidence Index Summary",
                "Lexical Search Results",
                "Claim Scan Verdict",
                "Action Scan Verdict",
                "Contradictions",
                "Missing/Stale/Low-Confidence Evidence",
                "Typed Evidence Graph",
                "Action Readiness Matrix",
                "Required Human Actions",
                "Safe Next Step",
                "Semantic Backend Disabled Notice",
                "Local-Only / No-Runtime Notice"
            ],
            [
                "View details",
                "Copy report preview",
                "Refresh fixture preview",
                "Open read-only evidence summary"
            ],
            [
                "runtime-action-affordance",
                "browser-launch-affordance",
                "live-automation-affordance",
                "file-mutation-affordance",
                "provider-call-affordance"
            ],
            RouteVisible: true,
            UsesPresenter: true,
            UsesDeterministicFixture: true,
            ReadOnly: true,
            LocalOnly: true,
            RuntimeEnabled: false,
            BrowserCdpAutomationEnabled: false,
            WcuLiveEnabled: false,
            OcrLiveEnabled: false,
            ProviderCloudEnabled: false,
            DurablePersistenceEnabled: false,
            SemanticVectorBackendEnabled: false,
            FilesystemWritesEnabled: false);
}
