namespace OneBrain.BrowserPerception;

public enum PerceptionSignalKind
{
    DOM,
    ACCESSIBILITY,
    LAYOUT,
    SCREENSHOT,
    OCR_OR_VISION,
    CONSOLE,
    NETWORK,
    FRAME_TREE,
    SHADOW_DOM,
    FORM_STATE,
    LIFECYCLE,
    STORAGE_METADATA,
    HIT_TEST,
    MUTATION_HISTORY
}

public enum DetectedPageKind
{
    LegacyHtmlForm,
    ModernSpa,
    SsrApp,
    IframeHeavy,
    ShadowDomApp,
    CanvasVisualApp,
    AuthRequired,
    HumanVerificationRequired,
    BrokenOrUnstable,
    Unknown
}

public enum BrowserPerceptionStrategy
{
    DOM_FIRST,
    ACCESSIBILITY_FIRST,
    FRAME_TARGET_REQUIRED,
    SHADOW_DOM_REQUIRED,
    VISUAL_REQUIRED,
    NETWORK_DIAGNOSIS_REQUIRED,
    CONSOLE_DIAGNOSIS_REQUIRED,
    HUMAN_HANDOFF_REQUIRED,
    UNSUPPORTED_OR_HIGH_RISK
}

public enum BlockageKind
{
    None,
    AuthRequired,
    HumanVerificationRequired,
    NetworkFailure,
    ConsoleRuntimeError,
    UnsupportedPageShape,
    HighRiskOrContradictorySignals,
    Captcha,
    Login,
    TwoFactor,
    AntiBot,
    RateLimit,
    AccessDenied,
    Popup,
    CookieWall,
    BrokenPage,
    ConsoleError,
    Unknown
}

public enum BrowserPerceptionSeverity
{
    Info,
    Warning,
    Critical
}

public enum BlockageSeverity
{
    Info,
    Warning,
    Critical
}

public sealed record BrowserViewport(int Width, int Height, double DeviceScaleFactor);

public sealed record BrowserFrameSummary(
    int FrameCount,
    int CrossOriginFrameCount,
    int RelevantFrameCount,
    bool MainFrameReady);

public sealed record BrowserFormsSummary(
    int FormsCount,
    int InputsCount,
    int PasswordFieldsCount,
    int RequiredFieldsCount,
    bool LoginFormDetected,
    bool SensitiveSubmitDetected);

public sealed record BrowserInteractiveElementsSummary(
    int ElementsCount,
    int ButtonsCount,
    int LinksCount,
    int InputsCount,
    int DisabledCount,
    int VisibleCount,
    bool HasSemanticRoles);

public sealed record BrowserAccessibilitySummary(
    bool Available,
    int NamedControlsCount,
    int LandmarksCount,
    int InteractiveSemanticElementsCount,
    bool HasAmbiguousControls);

public sealed record BrowserLayoutVisibilitySummary(
    bool MainContentVisible,
    bool OverlayPresent,
    bool VisualOnlySurface,
    int HiddenInteractiveElementsCount);

public sealed record BrowserConsoleSummary(
    int ErrorCount,
    int WarningCount,
    int CriticalErrorCount,
    IReadOnlyList<string> RedactedMessages);

public sealed record BrowserNetworkSummary(
    int RequestCount,
    int FailedRequestCount,
    int CriticalFailureCount,
    bool AuthFailureDetected,
    bool ExternalNavigationAttempted,
    IReadOnlyList<int> FailedStatusCodes);

public sealed record BrowserLifecycleLoadingState(
    string ReadyState,
    bool NetworkIdle,
    bool HydrationLikely,
    bool SpaNavigationDetected,
    bool ServerRenderedMarkupPresent);

public sealed record BrowserScreenshotMetadata(
    bool Available,
    string? ArtifactRef,
    bool RawScreenshotStored,
    bool Redacted);

public sealed record BrowserStorageMetadata(
    int LocalStorageKeyCount,
    int SessionStorageKeyCount,
    int CookieCount,
    bool KeyNamesRedacted,
    bool ValuesCaptured);

public sealed record PerceptionSignal(
    PerceptionSignalKind Kind,
    string Name,
    BrowserPerceptionSeverity Severity,
    string Summary,
    bool BlocksAutomation = false,
    bool RequiresHumanHandoff = false,
    bool Redacted = true);

public sealed record BrowserPerceptionSnapshot(
    string SnapshotId,
    DateTimeOffset CapturedAt,
    string PageUrlRedacted,
    string Title,
    BrowserViewport Viewport,
    BrowserFrameSummary Frames,
    bool ShadowDomPresent,
    bool CanvasPresent,
    BrowserFormsSummary Forms,
    BrowserInteractiveElementsSummary InteractiveElements,
    BrowserAccessibilitySummary Accessibility,
    BrowserLayoutVisibilitySummary LayoutVisibility,
    BrowserConsoleSummary Console,
    BrowserNetworkSummary Network,
    BrowserLifecycleLoadingState Lifecycle,
    BrowserScreenshotMetadata Screenshot,
    BrowserStorageMetadata StorageMetadata,
    IReadOnlyList<PerceptionSignal> Signals,
    string PageTextPreviewRedacted,
    string Source,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool Redacted,
    bool StoresRawDom,
    bool StoresSensitivePayloads);

public sealed record PageCapabilityProfile(
    DetectedPageKind DetectedPageKind,
    IReadOnlyList<PerceptionSignal> DetectedSignals,
    double Confidence,
    IReadOnlyList<string> Reasons,
    BrowserPerceptionStrategy RecommendedStrategy);

public sealed record PageTechnologyProfile(
    bool UsesFrames,
    bool UsesShadowDom,
    bool UsesCanvas,
    bool LooksLikeSpa,
    bool LooksLikeServerRendered,
    bool HasSemanticAccessibility,
    bool HasCriticalConsoleErrors,
    bool HasCriticalNetworkFailures);

public sealed record StrategyRouterDecision(
    BrowserPerceptionStrategy Strategy,
    double Confidence,
    IReadOnlyList<PerceptionSignalKind> RequiredSignals,
    IReadOnlyList<string> ProhibitedActions,
    bool HumanHandoffRequired,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> EvidenceRefs)
{
    public LocatorStrategy? LocatorStrategy { get; init; }

    public IReadOnlyList<BlockageReport> Blockages { get; init; } = [];
}

public sealed record BlockageReport(
    BlockageKind BlockageKind,
    BrowserPerceptionSeverity Severity,
    string Reason,
    bool CanContinueAutomatically,
    bool RequiresHumanHandoff,
    string EvidenceSummary)
{
    public BlockageSeverity BlockageSeverity =>
        Severity switch
        {
            BrowserPerceptionSeverity.Critical => BlockageSeverity.Critical,
            BrowserPerceptionSeverity.Warning => BlockageSeverity.Warning,
            _ => BlockageSeverity.Info
        };
}

public sealed record BrowserEvidencePack(
    string SnapshotBeforeRef,
    string? SnapshotAfterRef,
    string StrategyDecisionRef,
    string ConsoleSummaryRef,
    string NetworkSummaryRef,
    bool RedactionStatus,
    bool NoSensitivePayloadGuarantee);

public sealed record BrowserPerceptionFixture(
    string FixtureId,
    string Url,
    string Title,
    int FormsCount = 0,
    int InputsCount = 0,
    int ButtonsCount = 0,
    int LinksCount = 0,
    int SemanticControlCount = 0,
    bool LoginFormDetected = false,
    bool HumanVerificationDetected = false,
    bool AntiBotDetected = false,
    bool HasIframe = false,
    bool HasShadowDom = false,
    bool HasCanvas = false,
    bool HasConsoleCriticalError = false,
    bool HasNetworkCriticalFailure = false,
    int NetworkStatusCode = 0,
    bool IsSpa = false,
    bool IsSsr = false,
    bool OverlayPresent = false,
    string TextPreview = "",
    bool TwoFactorDetected = false,
    bool CookieWallDetected = false,
    bool PopupDetected = false);

public sealed class BrowserPerceptionSnapshotBuilder
{
    public BrowserPerceptionSnapshot FromFixture(BrowserPerceptionFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var signals = BuildSignals(fixture);
        var failedStatusCodes = fixture.NetworkStatusCode > 0 ? new[] { fixture.NetworkStatusCode } : [];
        return new BrowserPerceptionSnapshot(
            SnapshotId: "perception-fixture-" + fixture.FixtureId,
            CapturedAt: DateTimeOffset.UtcNow,
            PageUrlRedacted: RedactUrl(fixture.Url),
            Title: fixture.Title,
            Viewport: new BrowserViewport(1280, 720, 1),
            Frames: new BrowserFrameSummary(
                FrameCount: fixture.HasIframe ? 2 : 1,
                CrossOriginFrameCount: fixture.HasIframe ? 1 : 0,
                RelevantFrameCount: fixture.HasIframe ? 1 : 0,
                MainFrameReady: true),
            ShadowDomPresent: fixture.HasShadowDom,
            CanvasPresent: fixture.HasCanvas,
            Forms: new BrowserFormsSummary(
                fixture.FormsCount,
                fixture.InputsCount,
                PasswordFieldsCount: fixture.LoginFormDetected ? 1 : 0,
                RequiredFieldsCount: fixture.InputsCount > 0 ? 1 : 0,
                LoginFormDetected: fixture.LoginFormDetected,
                SensitiveSubmitDetected: fixture.LoginFormDetected),
            InteractiveElements: new BrowserInteractiveElementsSummary(
                fixture.ButtonsCount + fixture.LinksCount + fixture.InputsCount + fixture.SemanticControlCount,
                fixture.ButtonsCount,
                fixture.LinksCount,
                fixture.InputsCount,
                DisabledCount: 0,
                VisibleCount: fixture.ButtonsCount + fixture.LinksCount + fixture.InputsCount + fixture.SemanticControlCount,
                HasSemanticRoles: fixture.SemanticControlCount > 0),
            Accessibility: new BrowserAccessibilitySummary(
                Available: fixture.SemanticControlCount > 0,
                NamedControlsCount: fixture.SemanticControlCount,
                LandmarksCount: fixture.IsSpa || fixture.IsSsr ? 2 : 1,
                InteractiveSemanticElementsCount: fixture.SemanticControlCount,
                HasAmbiguousControls: false),
            LayoutVisibility: new BrowserLayoutVisibilitySummary(
                MainContentVisible: !fixture.OverlayPresent,
                OverlayPresent: fixture.OverlayPresent,
                VisualOnlySurface: fixture.HasCanvas,
                HiddenInteractiveElementsCount: 0),
            Console: new BrowserConsoleSummary(
                ErrorCount: fixture.HasConsoleCriticalError ? 1 : 0,
                WarningCount: 0,
                CriticalErrorCount: fixture.HasConsoleCriticalError ? 1 : 0,
                RedactedMessages: fixture.HasConsoleCriticalError ? ["runtime error redacted"] : []),
            Network: new BrowserNetworkSummary(
                RequestCount: fixture.HasNetworkCriticalFailure ? 3 : 1,
                FailedRequestCount: fixture.HasNetworkCriticalFailure ? 1 : 0,
                CriticalFailureCount: fixture.HasNetworkCriticalFailure ? 1 : 0,
                AuthFailureDetected: fixture.NetworkStatusCode is 401 or 403,
                ExternalNavigationAttempted: false,
                FailedStatusCodes: failedStatusCodes),
            Lifecycle: new BrowserLifecycleLoadingState(
                ReadyState: "complete",
                NetworkIdle: !fixture.HasNetworkCriticalFailure,
                HydrationLikely: fixture.IsSpa,
                SpaNavigationDetected: fixture.IsSpa,
                ServerRenderedMarkupPresent: fixture.IsSsr),
            Screenshot: new BrowserScreenshotMetadata(
                Available: true,
                ArtifactRef: "fixture:screenshot-metadata-only",
                RawScreenshotStored: false,
                Redacted: true),
            StorageMetadata: new BrowserStorageMetadata(
                LocalStorageKeyCount: 0,
                SessionStorageKeyCount: 0,
                CookieCount: 0,
                KeyNamesRedacted: true,
                ValuesCaptured: false),
            Signals: signals,
            PageTextPreviewRedacted: RedactText(fixture.TextPreview),
            Source: "fixture-safe-read-only",
            ExtensionUsed: false,
            SystemBrowserUsed: false,
            Redacted: true,
            StoresRawDom: false,
            StoresSensitivePayloads: false);
    }

    private static IReadOnlyList<PerceptionSignal> BuildSignals(BrowserPerceptionFixture fixture)
    {
        var signals = new List<PerceptionSignal>
        {
            new(PerceptionSignalKind.DOM, "dom-summary", BrowserPerceptionSeverity.Info, "DOM metadata collected."),
            new(PerceptionSignalKind.LIFECYCLE, "lifecycle-complete", BrowserPerceptionSeverity.Info, "Lifecycle metadata collected.")
        };

        if (fixture.SemanticControlCount > 0)
            signals.Add(new PerceptionSignal(PerceptionSignalKind.ACCESSIBILITY, "accessibility-controls", BrowserPerceptionSeverity.Info, "Accessibility controls present."));
        if (fixture.HasIframe)
            signals.Add(new PerceptionSignal(PerceptionSignalKind.FRAME_TREE, "relevant-frame", BrowserPerceptionSeverity.Warning, "Relevant iframe metadata present."));
        if (fixture.HasShadowDom)
            signals.Add(new PerceptionSignal(PerceptionSignalKind.SHADOW_DOM, "shadow-dom-present", BrowserPerceptionSeverity.Warning, "Shadow DOM marker present."));
        if (fixture.HasCanvas)
            signals.Add(new PerceptionSignal(PerceptionSignalKind.SCREENSHOT, "canvas-visual-surface", BrowserPerceptionSeverity.Warning, "Canvas visual marker present."));
        if (fixture.FormsCount > 0)
            signals.Add(new PerceptionSignal(PerceptionSignalKind.FORM_STATE, "form-state", BrowserPerceptionSeverity.Info, "Form metadata present."));
        if (fixture.HasConsoleCriticalError)
            signals.Add(new PerceptionSignal(PerceptionSignalKind.CONSOLE, "console-critical", BrowserPerceptionSeverity.Critical, "Critical console error.", BlocksAutomation: true));
        if (fixture.HasNetworkCriticalFailure)
            signals.Add(new PerceptionSignal(PerceptionSignalKind.NETWORK, "network-critical", BrowserPerceptionSeverity.Critical, "Critical network failure.", BlocksAutomation: true));
        if (fixture.LoginFormDetected)
            signals.Add(new PerceptionSignal(PerceptionSignalKind.FORM_STATE, "auth-required", BrowserPerceptionSeverity.Critical, "Login or credentials required.", BlocksAutomation: true, RequiresHumanHandoff: true));
        if (fixture.HumanVerificationDetected)
            signals.Add(new PerceptionSignal(PerceptionSignalKind.HIT_TEST, "captcha-marker", BrowserPerceptionSeverity.Critical, "CAPTCHA marker requires human handoff.", BlocksAutomation: true, RequiresHumanHandoff: true));
        if (fixture.TwoFactorDetected)
            signals.Add(new PerceptionSignal(PerceptionSignalKind.FORM_STATE, "two-factor-marker", BrowserPerceptionSeverity.Critical, "2FA marker requires human handoff.", BlocksAutomation: true, RequiresHumanHandoff: true));
        if (fixture.AntiBotDetected)
            signals.Add(new PerceptionSignal(PerceptionSignalKind.NETWORK, "anti-bot-marker", BrowserPerceptionSeverity.Critical, "Anti-bot marker requires human handoff.", BlocksAutomation: true, RequiresHumanHandoff: true));
        if (fixture.CookieWallDetected)
            signals.Add(new PerceptionSignal(PerceptionSignalKind.HIT_TEST, "cookie-wall", BrowserPerceptionSeverity.Warning, "Cookie wall marker present.", BlocksAutomation: false));
        if (fixture.PopupDetected)
            signals.Add(new PerceptionSignal(PerceptionSignalKind.HIT_TEST, "popup-modal", BrowserPerceptionSeverity.Warning, "Popup or modal marker present.", BlocksAutomation: false));

        return signals;
    }

    private static string RedactUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return "redacted:invalid-url";
        return uri.GetLeftPart(UriPartial.Path);
    }

    private static string RedactText(string text) =>
        string.IsNullOrWhiteSpace(text) ? "" : text.Length > 240 ? string.Concat(text.AsSpan(0, 240), "...") : text;
}

public sealed class PageCapabilityClassifier
{
    public PageCapabilityProfile Classify(BrowserPerceptionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var reasons = new List<string>();
        var kind = DetectedPageKind.Unknown;
        var strategy = BrowserPerceptionStrategy.UNSUPPORTED_OR_HIGH_RISK;
        var confidence = 0.35;

        if (snapshot.Signals.Any(signal => signal.RequiresHumanHandoff))
        {
            var verification = snapshot.Signals.Any(signal =>
                signal.Name.Contains("human-verification", StringComparison.OrdinalIgnoreCase)
                || signal.Name.Contains("anti-bot", StringComparison.OrdinalIgnoreCase));
            kind = verification ? DetectedPageKind.HumanVerificationRequired : DetectedPageKind.AuthRequired;
            strategy = BrowserPerceptionStrategy.HUMAN_HANDOFF_REQUIRED;
            confidence = 0.98;
            reasons.Add(verification ? "CAPTCHA/2FA/anti-bot marker requires human review." : "Authentication or credential entry requires human review.");
        }
        else if (snapshot.Network.AuthFailureDetected)
        {
            kind = DetectedPageKind.AuthRequired;
            strategy = BrowserPerceptionStrategy.HUMAN_HANDOFF_REQUIRED;
            confidence = 0.92;
            reasons.Add("Network status indicates authorization failure.");
        }
        else if (snapshot.Network.CriticalFailureCount > 0)
        {
            kind = DetectedPageKind.BrokenOrUnstable;
            strategy = BrowserPerceptionStrategy.NETWORK_DIAGNOSIS_REQUIRED;
            confidence = 0.88;
            reasons.Add("Critical network failure blocks safe routing.");
        }
        else if (snapshot.Console.CriticalErrorCount > 0)
        {
            kind = DetectedPageKind.BrokenOrUnstable;
            strategy = BrowserPerceptionStrategy.CONSOLE_DIAGNOSIS_REQUIRED;
            confidence = 0.86;
            reasons.Add("Critical console/runtime error blocks safe routing.");
        }
        else if (snapshot.Frames.RelevantFrameCount > 0)
        {
            kind = DetectedPageKind.IframeHeavy;
            strategy = BrowserPerceptionStrategy.FRAME_TARGET_REQUIRED;
            confidence = 0.84;
            reasons.Add("Relevant iframe metadata requires frame targeting.");
        }
        else if (snapshot.ShadowDomPresent)
        {
            kind = DetectedPageKind.ShadowDomApp;
            strategy = BrowserPerceptionStrategy.SHADOW_DOM_REQUIRED;
            confidence = 0.84;
            reasons.Add("Shadow DOM marker changes locator strategy.");
        }
        else if (snapshot.CanvasPresent || snapshot.LayoutVisibility.VisualOnlySurface)
        {
            kind = DetectedPageKind.CanvasVisualApp;
            strategy = BrowserPerceptionStrategy.VISUAL_REQUIRED;
            confidence = 0.82;
            reasons.Add("Canvas or visual-only surface requires visual perception.");
        }
        else if (snapshot.Lifecycle.SpaNavigationDetected || snapshot.Lifecycle.HydrationLikely)
        {
            kind = DetectedPageKind.ModernSpa;
            strategy = BrowserPerceptionStrategy.ACCESSIBILITY_FIRST;
            confidence = snapshot.Accessibility.Available ? 0.82 : 0.68;
            reasons.Add("SPA-like lifecycle favors accessibility and semantic controls.");
        }
        else if (snapshot.Forms.FormsCount > 0)
        {
            kind = DetectedPageKind.LegacyHtmlForm;
            strategy = BrowserPerceptionStrategy.DOM_FIRST;
            confidence = 0.86;
            reasons.Add("Simple form metadata is suitable for DOM-first perception.");
        }
        else if (snapshot.Lifecycle.ServerRenderedMarkupPresent)
        {
            kind = DetectedPageKind.SsrApp;
            strategy = BrowserPerceptionStrategy.DOM_FIRST;
            confidence = 0.72;
            reasons.Add("Server-rendered markup is visible to DOM metadata.");
        }
        else
        {
            reasons.Add("Signals are insufficient or contradictory.");
        }

        return new PageCapabilityProfile(kind, snapshot.Signals, confidence, reasons, strategy);
    }

    public PageTechnologyProfile BuildTechnologyProfile(BrowserPerceptionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new PageTechnologyProfile(
            UsesFrames: snapshot.Frames.RelevantFrameCount > 0,
            UsesShadowDom: snapshot.ShadowDomPresent,
            UsesCanvas: snapshot.CanvasPresent,
            LooksLikeSpa: snapshot.Lifecycle.SpaNavigationDetected || snapshot.Lifecycle.HydrationLikely,
            LooksLikeServerRendered: snapshot.Lifecycle.ServerRenderedMarkupPresent,
            HasSemanticAccessibility: snapshot.Accessibility.Available,
            HasCriticalConsoleErrors: snapshot.Console.CriticalErrorCount > 0,
            HasCriticalNetworkFailures: snapshot.Network.CriticalFailureCount > 0);
    }
}

public sealed class StrategyRouter
{
    private static readonly string[] DefaultProhibitedActions =
    [
        "submitForm",
        "uploadFile",
        "downloadFile",
        "navigateExternal",
        "solveCaptcha",
        "useCredentials",
        "executeArbitraryJs",
        "writeFilesystem",
        "shellCommand",
        "useSystemBrowserFallback",
        "useExtensionFallback"
    ];

    public StrategyRouterDecision Route(PageCapabilityProfile profile) =>
        RouteCore(profile, snapshot: null);

    public StrategyRouterDecision Route(PageCapabilityProfile profile, BrowserPerceptionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return RouteCore(profile, snapshot);
    }

    private static StrategyRouterDecision RouteCore(PageCapabilityProfile profile, BrowserPerceptionSnapshot? snapshot)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var strategy = profile.RecommendedStrategy;
        var reasons = profile.Reasons.ToList();
        var humanHandoff = strategy == BrowserPerceptionStrategy.HUMAN_HANDOFF_REQUIRED;
        var blockages = snapshot is null
            ? new BlockageDetector().DetectBlockages(profile)
            : new BlockageDetector().DetectBlockages(snapshot);
        LocatorStrategy? locatorStrategy = null;

        if (blockages.Any(blockage => blockage.RequiresHumanHandoff)
            || strategy == BrowserPerceptionStrategy.HUMAN_HANDOFF_REQUIRED
            || profile.DetectedSignals.Any(signal => signal.RequiresHumanHandoff))
        {
            strategy = BrowserPerceptionStrategy.HUMAN_HANDOFF_REQUIRED;
            humanHandoff = true;
            reasons.Add("Human handoff has priority over all automated strategies.");
        }
        else if (blockages.Any(blockage =>
            blockage.Severity == BrowserPerceptionSeverity.Critical
            && blockage.BlockageKind is BlockageKind.AccessDenied or BlockageKind.RateLimit or BlockageKind.BrokenPage or BlockageKind.NetworkFailure or BlockageKind.ConsoleError or BlockageKind.ConsoleRuntimeError))
        {
            var criticalBlockage = blockages.First(blockage => blockage.Severity == BrowserPerceptionSeverity.Critical);
            strategy = criticalBlockage.BlockageKind switch
            {
                BlockageKind.ConsoleError or BlockageKind.ConsoleRuntimeError => BrowserPerceptionStrategy.CONSOLE_DIAGNOSIS_REQUIRED,
                BlockageKind.BrokenPage when profile.RecommendedStrategy is BrowserPerceptionStrategy.CONSOLE_DIAGNOSIS_REQUIRED or BrowserPerceptionStrategy.NETWORK_DIAGNOSIS_REQUIRED => profile.RecommendedStrategy,
                _ => BrowserPerceptionStrategy.NETWORK_DIAGNOSIS_REQUIRED
            };
            reasons.Add("Critical blockage detected before locator routing.");
        }
        else if (profile.DetectedSignals.Any(signal => signal.Kind == PerceptionSignalKind.NETWORK && signal.Severity == BrowserPerceptionSeverity.Critical))
        {
            strategy = BrowserPerceptionStrategy.NETWORK_DIAGNOSIS_REQUIRED;
            reasons.Add("Critical network signal blocks action routing.");
        }
        else if (profile.DetectedSignals.Any(signal => signal.Kind == PerceptionSignalKind.CONSOLE && signal.Severity == BrowserPerceptionSeverity.Critical))
        {
            strategy = BrowserPerceptionStrategy.CONSOLE_DIAGNOSIS_REQUIRED;
            reasons.Add("Critical console signal blocks action routing.");
        }
        else if (profile.Confidence < 0.45 || profile.DetectedPageKind == DetectedPageKind.Unknown)
        {
            strategy = BrowserPerceptionStrategy.UNSUPPORTED_OR_HIGH_RISK;
            reasons.Add("Unknown or low-confidence page cannot receive automatic action routing.");
        }
        else if (snapshot is not null)
        {
            var technologyProfile = new PageCapabilityClassifier().BuildTechnologyProfile(snapshot);
            locatorStrategy = new LocatorEngine().SelectLocatorStrategy(technologyProfile, snapshot);
            reasons.Add("Locator strategy selected: " + locatorStrategy.Strategy + ".");
        }

        if (locatorStrategy is null && humanHandoff)
        {
            locatorStrategy = LocatorStrategy.HumanHandoff("Human handoff required before locator routing.");
        }

        return new StrategyRouterDecision(
            Strategy: strategy,
            Confidence: profile.Confidence,
            RequiredSignals: RequiredSignalsFor(strategy),
            ProhibitedActions: DefaultProhibitedActions,
            HumanHandoffRequired: humanHandoff,
            Reasons: reasons.Distinct(StringComparer.Ordinal).ToArray(),
            EvidenceRefs: ["pending:evidence-pack"])
        {
            LocatorStrategy = locatorStrategy,
            Blockages = blockages
        };
    }

    private static IReadOnlyList<PerceptionSignalKind> RequiredSignalsFor(BrowserPerceptionStrategy strategy) =>
        strategy switch
        {
            BrowserPerceptionStrategy.DOM_FIRST => [PerceptionSignalKind.DOM, PerceptionSignalKind.FORM_STATE],
            BrowserPerceptionStrategy.ACCESSIBILITY_FIRST => [PerceptionSignalKind.DOM, PerceptionSignalKind.ACCESSIBILITY],
            BrowserPerceptionStrategy.FRAME_TARGET_REQUIRED => [PerceptionSignalKind.FRAME_TREE, PerceptionSignalKind.DOM],
            BrowserPerceptionStrategy.SHADOW_DOM_REQUIRED => [PerceptionSignalKind.SHADOW_DOM, PerceptionSignalKind.DOM],
            BrowserPerceptionStrategy.VISUAL_REQUIRED => [PerceptionSignalKind.SCREENSHOT, PerceptionSignalKind.LAYOUT],
            BrowserPerceptionStrategy.NETWORK_DIAGNOSIS_REQUIRED => [PerceptionSignalKind.NETWORK],
            BrowserPerceptionStrategy.CONSOLE_DIAGNOSIS_REQUIRED => [PerceptionSignalKind.CONSOLE],
            BrowserPerceptionStrategy.HUMAN_HANDOFF_REQUIRED => [PerceptionSignalKind.HIT_TEST, PerceptionSignalKind.FORM_STATE],
            _ => [PerceptionSignalKind.DOM, PerceptionSignalKind.LIFECYCLE]
        };
}

public sealed class BlockageReportBuilder
{
    public BlockageReport Build(PageCapabilityProfile profile, StrategyRouterDecision decision)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(decision);

        return decision.Strategy switch
        {
            BrowserPerceptionStrategy.HUMAN_HANDOFF_REQUIRED => new BlockageReport(
                profile.DetectedPageKind == DetectedPageKind.AuthRequired ? BlockageKind.AuthRequired : BlockageKind.HumanVerificationRequired,
                BrowserPerceptionSeverity.Critical,
                "Manual review is required before any future action.",
                CanContinueAutomatically: false,
                RequiresHumanHandoff: true,
                EvidenceSummary: string.Join(" ", decision.Reasons)),
            BrowserPerceptionStrategy.NETWORK_DIAGNOSIS_REQUIRED => new BlockageReport(
                BlockageKind.NetworkFailure,
                BrowserPerceptionSeverity.Critical,
                "Network diagnosis is required before any future action.",
                CanContinueAutomatically: false,
                RequiresHumanHandoff: false,
                EvidenceSummary: string.Join(" ", decision.Reasons)),
            BrowserPerceptionStrategy.CONSOLE_DIAGNOSIS_REQUIRED => new BlockageReport(
                BlockageKind.ConsoleRuntimeError,
                BrowserPerceptionSeverity.Critical,
                "Console/runtime diagnosis is required before any future action.",
                CanContinueAutomatically: false,
                RequiresHumanHandoff: false,
                EvidenceSummary: string.Join(" ", decision.Reasons)),
            BrowserPerceptionStrategy.UNSUPPORTED_OR_HIGH_RISK => new BlockageReport(
                BlockageKind.HighRiskOrContradictorySignals,
                BrowserPerceptionSeverity.Warning,
                "Unsupported or low-confidence page shape.",
                CanContinueAutomatically: false,
                RequiresHumanHandoff: false,
                EvidenceSummary: string.Join(" ", decision.Reasons)),
            _ => new BlockageReport(
                BlockageKind.None,
                BrowserPerceptionSeverity.Info,
                "No blockage detected in read-only foundation.",
                CanContinueAutomatically: false,
                RequiresHumanHandoff: false,
                EvidenceSummary: "Read-only strategy selected; no action authority granted.")
        };
    }
}

public sealed class BrowserEvidencePackBuilder
{
    public BrowserEvidencePack Build(BrowserPerceptionSnapshot snapshot, StrategyRouterDecision decision)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(decision);

        return new BrowserEvidencePack(
            SnapshotBeforeRef: snapshot.SnapshotId,
            SnapshotAfterRef: null,
            StrategyDecisionRef: "strategy:" + decision.Strategy,
            ConsoleSummaryRef: "console:metadata-only",
            NetworkSummaryRef: "network:metadata-only",
            RedactionStatus: snapshot.Redacted,
            NoSensitivePayloadGuarantee: !snapshot.StoresRawDom && !snapshot.StoresSensitivePayloads && !snapshot.StorageMetadata.ValuesCaptured);
    }
}
