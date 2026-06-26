using System.Text.Json;
using System.Text.RegularExpressions;

namespace OneBrain.WindowsComputerUse;

public enum ComputerUseSnapshotSource
{
    Fixture,
    DesignOnly,
    LiveDisabled
}

public enum WindowTechnologyKind
{
    Unknown,
    UiaRich,
    UiaPoor,
    VisualOnly,
    SensitiveAuthSurface,
    UacAdminBlocked,
    ModalBlocked
}

public enum ComputerUseBlockageKind
{
    None,
    CredentialField,
    UacAdmin,
    DestructiveAction,
    ClipboardRisk,
    ScreenshotRisk,
    OcrLeakageRisk,
    AppNotAllowlisted,
    LowConfidenceLocator,
    VisualOnlyTarget,
    HiddenWindowOrModal,
    DpiMonitorMismatch,
    RemoteDesktopOrCitrix,
    EvidenceTamperingRisk,
    AuditLogBypassRisk
}

public enum ComputerUseBlockageSeverity
{
    Info,
    Warning,
    Critical
}

public enum ComputerUseActionKind
{
    Invoke,
    SetValue,
    Click,
    Scroll,
    Hotkey,
    WaitForElement,
    HumanHandoff
}

public enum ComputerUseHandoffReason
{
    None,
    CredentialField,
    UacAdmin,
    DestructiveAction,
    NonAllowlistedApp,
    LowConfidence,
    VisualOnlyTarget,
    SensitiveSurface,
    LiveExecutionDisabled,
    VerificationFailed
}

public enum ComputerUseEvidenceKind
{
    ObserveOnlySnapshot,
    PlannedActionDryRun,
    BlockageDetected,
    SensitiveSurfaceDetected,
    LowConfidenceLocator,
    HumanHandoff,
    VerificationFailureFixture
}

public enum ComputerUseRedactionStatus
{
    None,
    Partial,
    Full
}

public sealed record UiElementBounds(int X, int Y, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

public sealed record UiAutomationPatternCapabilities(
    bool SupportsInvoke,
    bool SupportsValue,
    bool SupportsScroll,
    bool SupportsSelection,
    bool SupportsText,
    bool SupportsFocus);

public sealed record UiElementIdentity(
    string AutomationId,
    string RuntimeId,
    string Name,
    string ControlType,
    string ClassName,
    string ProcessName,
    IReadOnlyList<string> Ancestry);

public sealed record UiElementNode(
    UiElementIdentity Identity,
    UiElementBounds Bounds,
    UiAutomationPatternCapabilities Patterns,
    bool IsVisible,
    bool IsEnabled,
    bool IsPasswordField = false,
    bool IsCredentialField = false,
    bool IsDestructive = false,
    bool IsVisualOnly = false,
    IReadOnlyList<UiElementNode>? Children = null)
{
    public IReadOnlyList<UiElementNode> SafeChildren => Children ?? [];
}

public sealed record WindowContext(
    string WindowId,
    string ProcessName,
    string Title,
    string ClassName,
    UiElementBounds Bounds,
    bool IsAllowlisted,
    bool IsModal,
    bool IsUacLike,
    bool IsRemoteDesktopLike,
    double DpiScale,
    IReadOnlyList<UiElementNode> Elements);

public sealed record ComputerUseSnapshot(
    string SnapshotId,
    ComputerUseSnapshotSource Source,
    DateTimeOffset CapturedAtUtc,
    string Scenario,
    IReadOnlyList<WindowContext> Windows,
    bool ClipboardCaptured,
    bool ScreenshotPersisted,
    bool OcrTextPersisted,
    bool RealMouseUsed,
    bool RealKeyboardUsed,
    bool LiveUiaActionUsed,
    bool ShellOrSubprocessUsed,
    bool ProductUiEnabledActions,
    bool Redacted);

public sealed record ComputerUseTechnologyProfile(
    WindowTechnologyKind TechnologyKind,
    double Confidence,
    IReadOnlyList<string> Signals,
    IReadOnlyList<string> Reasons,
    bool RequiresHumanHandoff);

public sealed record ComputerUseCapabilityClassification(
    WindowTechnologyKind TechnologyKind,
    double Confidence,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> MissingSignals,
    bool CanPlanFixtureOnly,
    bool RequiresVisualFallback,
    bool RequiresHumanHandoff,
    IReadOnlyList<string> Reasons);

public sealed record ComputerUseBlockage(
    ComputerUseBlockageKind Kind,
    ComputerUseBlockageSeverity Severity,
    string Reason,
    bool CanContinueAutomatically,
    bool RequiresHumanHandoff);

public sealed record ComputerUseSensitiveSurface(
    string SurfaceId,
    string Category,
    string Reason,
    bool RequiresHumanHandoff);

public sealed record ComputerUseLocatorCandidate(
    UiElementIdentity Identity,
    UiElementBounds Bounds,
    double Confidence,
    string SelectorKind,
    string Reason,
    bool RequiresApproval,
    bool RequiresHumanHandoff,
    bool RequiresVisualFallback,
    bool CandidateOnly = true);

public sealed record ComputerUseActionCandidate(
    ComputerUseActionKind ActionKind,
    ComputerUseLocatorCandidate? Target,
    string Objective,
    double Confidence,
    bool DryRunOnly,
    bool RequiresExplicitFutureApproval,
    bool RequiresHumanHandoff,
    IReadOnlyList<ComputerUseHandoffReason> HandoffReasons,
    string Reason);

public sealed record ComputerUsePolicyDecision(
    bool AllowedToPlan,
    bool AllowedToExecuteLive,
    bool FixtureOnly,
    IReadOnlyList<ComputerUseActionCandidate> Candidates,
    IReadOnlyList<ComputerUseBlockage> Blockages,
    IReadOnlyList<ComputerUseSensitiveSurface> SensitiveSurfaces,
    IReadOnlyList<string> Reasons);

public sealed record ComputerUseVerificationExpectation(
    string Kind,
    string Expected,
    string EvidenceRef);

public sealed record ComputerUseVerificationResult(
    bool Succeeded,
    bool RequiresHumanHandoff,
    IReadOnlyList<string> FailedExpectations,
    string Reason);

public sealed record ComputerUseEvidenceRef(
    string RefId,
    string RefKind,
    string Description,
    bool Redacted);

public sealed record ComputerUseRedactionResult(
    string Value,
    ComputerUseRedactionStatus Status,
    IReadOnlyList<string> SensitiveFieldsRedacted);

public sealed record ComputerUseEvidencePack(
    string EvidenceId,
    string CorrelationId,
    ComputerUseEvidenceKind EvidenceKind,
    string SnapshotRef,
    ComputerUsePolicyDecision? PolicyDecision,
    ComputerUseActionCandidate? PlannedAction,
    ComputerUseVerificationResult? VerificationResult,
    IReadOnlyList<ComputerUseBlockage> Blockages,
    IReadOnlyList<ComputerUseSensitiveSurface> SensitiveSurfaces,
    ComputerUseRedactionStatus RedactionStatus,
    IReadOnlyList<string> SensitiveFieldsRedacted,
    string EvidenceSummary,
    IReadOnlyList<ComputerUseEvidenceRef> EvidenceRefs,
    bool LiveExecutionEnabled,
    bool RealMouseUsed,
    bool RealKeyboardUsed,
    bool LiveUiaActionUsed,
    bool ClipboardCaptured,
    bool ScreenshotPersisted,
    DateTimeOffset CreatedAtUtc);

public static class FixtureComputerUseSnapshotBuilder
{
    public static ComputerUseSnapshot NotepadLikeUiaRichApp() =>
        Create("wcu-fixture-notepad", "notepad-like-uia-rich", [
            Window("notepad-main", "notepad", "Untitled - Notepad", "Notepad", true, false, false, false, [
                Element("editor", "runtime-editor", "Text Editor", "Edit", "Edit", "notepad", true, true, value: true, text: true, focus: true),
                Element("save-menu", "runtime-save", "Save", "MenuItem", "MenuItem", "notepad", true, true, invoke: true, destructive: true)
            ])
        ]);

    public static ComputerUseSnapshot CalculatorLikeUiaRichApp() =>
        Create("wcu-fixture-calculator", "calculator-like-uia-rich", [
            Window("calculator-main", "calculator", "Calculator", "ApplicationFrameWindow", true, false, false, false, [
                Element("num1Button", "runtime-1", "One", "Button", "Button", "calculator", true, true, invoke: true),
                Element("plusButton", "runtime-plus", "Plus", "Button", "Button", "calculator", true, true, invoke: true),
                Element("equalButton", "runtime-equals", "Equals", "Button", "Button", "calculator", true, true, invoke: true)
            ])
        ]);

    public static ComputerUseSnapshot ElectronLikeUiaPoorApp() =>
        Create("wcu-fixture-electron", "electron-like-uia-poor", [
            Window("electron-main", "electron-app", "Electron App", "Chrome_WidgetWin_1", true, false, false, false, [
                Element("", "", "Submit", "Pane", "Chrome_RenderWidgetHostHWND", "electron-app", true, true)
            ])
        ]);

    public static ComputerUseSnapshot PasswordLoginForm() =>
        Create("wcu-fixture-login", "password-login-form", [
            Window("login-main", "login-app", "Sign in", "LoginWindow", true, false, false, false, [
                Element("username", "runtime-user", "Email", "Edit", "TextBox", "login-app", true, true, value: true),
                Element("password", "runtime-password", "Password", "Edit", "PasswordBox", "login-app", true, true, value: true, password: true, credential: true),
                Element("signin", "runtime-signin", "Sign in", "Button", "Button", "login-app", true, true, invoke: true)
            ])
        ]);

    public static ComputerUseSnapshot UacAdminBlocker() =>
        Create("wcu-fixture-uac", "uac-admin-like-blocker", [
            Window("uac-dialog", "consent", "User Account Control", "#32770", false, true, true, false, [
                Element("yes", "runtime-uac-yes", "Yes", "Button", "Button", "consent", true, true, invoke: true),
                Element("no", "runtime-uac-no", "No", "Button", "Button", "consent", true, true, invoke: true)
            ])
        ]);

    public static ComputerUseSnapshot ModalDialogBlocker() =>
        Create("wcu-fixture-modal", "modal-dialog-blocker", [
            Window("modal-main", "docs-app", "Confirm overwrite", "#32770", true, true, false, false, [
                Element("ok", "runtime-ok", "Overwrite", "Button", "Button", "docs-app", true, true, invoke: true, destructive: true),
                Element("cancel", "runtime-cancel", "Cancel", "Button", "Button", "docs-app", true, true, invoke: true)
            ])
        ]);

    public static ComputerUseSnapshot CustomCanvasVisualOnlyApp() =>
        Create("wcu-fixture-canvas", "custom-canvas-visual-only", [
            Window("canvas-main", "visual-app", "Custom Canvas", "CanvasHost", true, false, false, false, [
                Element("", "", "Draw Surface", "Pane", "Canvas", "visual-app", true, true, visualOnly: true)
            ])
        ]);

    private static ComputerUseSnapshot Create(string id, string scenario, IReadOnlyList<WindowContext> windows) =>
        new(
            SnapshotId: id,
            Source: ComputerUseSnapshotSource.Fixture,
            CapturedAtUtc: DateTimeOffset.UnixEpoch,
            Scenario: scenario,
            Windows: windows,
            ClipboardCaptured: false,
            ScreenshotPersisted: false,
            OcrTextPersisted: false,
            RealMouseUsed: false,
            RealKeyboardUsed: false,
            LiveUiaActionUsed: false,
            ShellOrSubprocessUsed: false,
            ProductUiEnabledActions: false,
            Redacted: true);

    private static WindowContext Window(
        string id,
        string process,
        string title,
        string className,
        bool allowlisted,
        bool modal,
        bool uac,
        bool remote,
        IReadOnlyList<UiElementNode> elements) =>
        new(id, process, title, className, new UiElementBounds(0, 0, 1024, 768), allowlisted, modal, uac, remote, 1.0, elements);

    private static UiElementNode Element(
        string automationId,
        string runtimeId,
        string name,
        string controlType,
        string className,
        string process,
        bool visible,
        bool enabled,
        bool invoke = false,
        bool value = false,
        bool scroll = false,
        bool selection = false,
        bool text = false,
        bool focus = false,
        bool password = false,
        bool credential = false,
        bool destructive = false,
        bool visualOnly = false) =>
        new(
            new UiElementIdentity(automationId, runtimeId, name, controlType, className, process, [process, controlType, name]),
            new UiElementBounds(10, 10, 120, 32),
            new UiAutomationPatternCapabilities(invoke, value, scroll, selection, text, focus),
            visible,
            enabled,
            password,
            credential,
            destructive,
            visualOnly);
}

public sealed class ComputerUseCapabilityClassifier
{
    public ComputerUseCapabilityClassification Classify(ComputerUseSnapshot snapshot)
    {
        var blockages = new ComputerUseBlockageDetector().Detect(snapshot);
        var sensitive = new ComputerUseSensitiveSurfaceDetector().Detect(snapshot);
        var hasVisualOnly = snapshot.Windows.SelectMany(w => w.Elements).Any(e => e.IsVisualOnly);
        var hasUiaRich = snapshot.Windows.SelectMany(w => w.Elements).Any(e =>
            !string.IsNullOrWhiteSpace(e.Identity.AutomationId) &&
            (e.Patterns.SupportsInvoke || e.Patterns.SupportsValue || e.Patterns.SupportsText));
        var hasUiaPoor = snapshot.Windows.SelectMany(w => w.Elements).Any(e =>
            string.IsNullOrWhiteSpace(e.Identity.AutomationId) &&
            string.IsNullOrWhiteSpace(e.Identity.RuntimeId));

        if (blockages.Any(b => b.Kind == ComputerUseBlockageKind.UacAdmin))
        {
            return Classification(WindowTechnologyKind.UacAdminBlocked, 0.96, [], ["uac.admin.blockage"], true, ["UAC/admin blocker detected."]);
        }

        if (blockages.Any(b => b.Kind == ComputerUseBlockageKind.HiddenWindowOrModal))
        {
            return Classification(WindowTechnologyKind.ModalBlocked, 0.9, [], ["modal.blockage"], true, ["Modal blocker detected."]);
        }

        if (sensitive.Count > 0)
        {
            return Classification(WindowTechnologyKind.SensitiveAuthSurface, 0.95, ["sensitive.surface"], [], true, ["Sensitive credential surface detected."]);
        }

        if (hasVisualOnly)
        {
            return Classification(WindowTechnologyKind.VisualOnly, 0.72, ["visual.only"], ["uia.identity"], true, ["Visual-only target requires future visual/OCR provider and human handoff."]);
        }

        if (hasUiaRich)
        {
            return Classification(WindowTechnologyKind.UiaRich, 0.88, ["uia.patterns", "automation.id"], [], false, ["UIA-rich fixture metadata is available."]);
        }

        if (hasUiaPoor)
        {
            return Classification(WindowTechnologyKind.UiaPoor, 0.52, ["window.metadata"], ["automation.id", "runtime.id"], true, ["UIA-poor fixture requires fallback design and handoff."]);
        }

        return Classification(WindowTechnologyKind.Unknown, 0.25, [], ["known.signals"], true, ["Insufficient fixture metadata."]);
    }

    public ComputerUseTechnologyProfile BuildTechnologyProfile(ComputerUseSnapshot snapshot)
    {
        var classification = Classify(snapshot);
        return new ComputerUseTechnologyProfile(
            classification.TechnologyKind,
            classification.Confidence,
            classification.Capabilities,
            classification.Reasons,
            classification.RequiresHumanHandoff);
    }

    private static ComputerUseCapabilityClassification Classification(
        WindowTechnologyKind kind,
        double confidence,
        IReadOnlyList<string> capabilities,
        IReadOnlyList<string> missing,
        bool handoff,
        IReadOnlyList<string> reasons) =>
        new(kind, confidence, capabilities, missing, CanPlanFixtureOnly: !handoff || kind == WindowTechnologyKind.UiaRich, RequiresVisualFallback: kind == WindowTechnologyKind.VisualOnly, RequiresHumanHandoff: handoff, reasons);
}

public sealed class ComputerUseBlockageDetector
{
    public IReadOnlyList<ComputerUseBlockage> Detect(ComputerUseSnapshot snapshot)
    {
        var blockages = new List<ComputerUseBlockage>();

        foreach (var window in snapshot.Windows)
        {
            if (!window.IsAllowlisted)
            {
                blockages.Add(Critical(ComputerUseBlockageKind.AppNotAllowlisted, $"Window '{window.WindowId}' is not allowlisted."));
            }

            if (window.IsUacLike)
            {
                blockages.Add(Critical(ComputerUseBlockageKind.UacAdmin, "UAC/admin-like surface requires human handoff."));
            }

            if (window.IsModal)
            {
                blockages.Add(new ComputerUseBlockage(ComputerUseBlockageKind.HiddenWindowOrModal, ComputerUseBlockageSeverity.Warning, "Modal or hidden-window blocker detected.", false, true));
            }

            if (window.IsRemoteDesktopLike)
            {
                blockages.Add(Critical(ComputerUseBlockageKind.RemoteDesktopOrCitrix, "Remote desktop/Citrix-like surface is not fixture-executable."));
            }

            if (Math.Abs(window.DpiScale - 1.0) > 0.25)
            {
                blockages.Add(new ComputerUseBlockage(ComputerUseBlockageKind.DpiMonitorMismatch, ComputerUseBlockageSeverity.Warning, "DPI/monitor mismatch requires future validation.", false, true));
            }

            foreach (var element in Flatten(window.Elements))
            {
                if (element.IsPasswordField || element.IsCredentialField)
                {
                    blockages.Add(Critical(ComputerUseBlockageKind.CredentialField, $"Sensitive credential element '{SafeName(element)}' requires human handoff."));
                }

                if (element.IsDestructive)
                {
                    blockages.Add(Critical(ComputerUseBlockageKind.DestructiveAction, $"Destructive action candidate '{SafeName(element)}' is blocked."));
                }

                if (element.IsVisualOnly)
                {
                    blockages.Add(new ComputerUseBlockage(ComputerUseBlockageKind.VisualOnlyTarget, ComputerUseBlockageSeverity.Warning, "Visual-only target cannot be actioned in this block.", false, true));
                }
            }
        }

        if (snapshot.ClipboardCaptured)
        {
            blockages.Add(Critical(ComputerUseBlockageKind.ClipboardRisk, "Clipboard capture is not allowed."));
        }

        if (snapshot.ScreenshotPersisted)
        {
            blockages.Add(Critical(ComputerUseBlockageKind.ScreenshotRisk, "Raw screenshots must not be persisted."));
        }

        return blockages;
    }

    internal static IEnumerable<UiElementNode> Flatten(IEnumerable<UiElementNode> elements)
    {
        foreach (var element in elements)
        {
            yield return element;
            foreach (var child in Flatten(element.SafeChildren))
            {
                yield return child;
            }
        }
    }

    private static ComputerUseBlockage Critical(ComputerUseBlockageKind kind, string reason) =>
        new(kind, ComputerUseBlockageSeverity.Critical, reason, false, true);

    private static string SafeName(UiElementNode element) =>
        string.IsNullOrWhiteSpace(element.Identity.AutomationId) ? element.Identity.Name : element.Identity.AutomationId;
}

public sealed class ComputerUseSensitiveSurfaceDetector
{
    private static readonly string[] SensitiveTerms =
    [
        "password", "credential", "token", "secret", "otp", "2fa", "mfa", "api key",
        "apikey", "credit card", "card number", "cvv", "ssn", "social security", "clipboard"
    ];

    public IReadOnlyList<ComputerUseSensitiveSurface> Detect(ComputerUseSnapshot snapshot)
    {
        var surfaces = new List<ComputerUseSensitiveSurface>();
        foreach (var element in snapshot.Windows.SelectMany(w => ComputerUseBlockageDetector.Flatten(w.Elements)))
        {
            var combined = $"{element.Identity.AutomationId} {element.Identity.Name} {element.Identity.ControlType} {element.Identity.ClassName}".ToLowerInvariant();
            var matched = SensitiveTerms.FirstOrDefault(combined.Contains);
            if (element.IsPasswordField || element.IsCredentialField || matched is not null)
            {
                surfaces.Add(new ComputerUseSensitiveSurface(
                    SurfaceId: string.IsNullOrWhiteSpace(element.Identity.AutomationId) ? element.Identity.RuntimeId : element.Identity.AutomationId,
                    Category: matched ?? "credential",
                    Reason: "Sensitive surface detected from fixture metadata.",
                    RequiresHumanHandoff: true));
            }
        }

        if (snapshot.ClipboardCaptured)
        {
            surfaces.Add(new ComputerUseSensitiveSurface("clipboard", "clipboard", "Clipboard capture marker is sensitive.", true));
        }

        return surfaces;
    }
}

public sealed class ComputerUseLocatorEngine
{
    public IReadOnlyList<ComputerUseLocatorCandidate> GenerateCandidates(ComputerUseSnapshot snapshot, string objective)
    {
        var elements = snapshot.Windows.SelectMany(window => ComputerUseBlockageDetector.Flatten(window.Elements).Select(element => (window, element)));
        return elements
            .Select(pair => Score(pair.window, pair.element, objective))
            .OrderByDescending(candidate => candidate.Confidence)
            .ThenBy(candidate => candidate.Identity.Name)
            .ToList();
    }

    private static ComputerUseLocatorCandidate Score(WindowContext window, UiElementNode element, string objective)
    {
        var confidence = 0.2;
        var reasons = new List<string>();

        if (!string.IsNullOrWhiteSpace(element.Identity.AutomationId))
        {
            confidence += 0.3;
            reasons.Add("automation-id");
        }

        if (!string.IsNullOrWhiteSpace(element.Identity.RuntimeId))
        {
            confidence += 0.2;
            reasons.Add("runtime-id");
        }

        if (!string.IsNullOrWhiteSpace(element.Identity.ControlType))
        {
            confidence += 0.1;
            reasons.Add("control-type");
        }

        if (element.Identity.Name.Contains(objective, StringComparison.OrdinalIgnoreCase) ||
            objective.Contains(element.Identity.Name, StringComparison.OrdinalIgnoreCase))
        {
            confidence += 0.2;
            reasons.Add("name-match");
        }

        if (element.Bounds.IsEmpty)
        {
            confidence -= 0.2;
            reasons.Add("empty-bounds");
        }

        if (element.IsVisualOnly)
        {
            confidence = Math.Min(confidence, 0.45);
            reasons.Add("visual-fallback-required");
        }

        if (!window.IsAllowlisted)
        {
            confidence = Math.Min(confidence, 0.35);
            reasons.Add("app-not-allowlisted");
        }

        var requiresHandoff = confidence < 0.6 || element.IsVisualOnly || !window.IsAllowlisted || element.IsPasswordField || element.IsCredentialField;
        var selectorKind = !string.IsNullOrWhiteSpace(element.Identity.AutomationId)
            ? "AutomationId+Process+ControlType"
            : !string.IsNullOrWhiteSpace(element.Identity.RuntimeId)
                ? "RuntimeId+Ancestry"
                : !element.Bounds.IsEmpty
                    ? "BoundingBoxFallback"
                    : "OcrVisualPlaceholder";

        return new ComputerUseLocatorCandidate(
            element.Identity,
            element.Bounds,
            Math.Clamp(confidence, 0, 0.99),
            selectorKind,
            string.Join(",", reasons),
            RequiresApproval: true,
            RequiresHumanHandoff: requiresHandoff,
            RequiresVisualFallback: element.IsVisualOnly || selectorKind == "OcrVisualPlaceholder");
    }
}

public sealed class ComputerUseSafeActionPlanner
{
    public ComputerUsePolicyDecision Plan(ComputerUseSnapshot snapshot, string objective, ComputerUseActionKind requestedAction)
    {
        var classifier = new ComputerUseCapabilityClassifier().Classify(snapshot);
        var blockages = new ComputerUseBlockageDetector().Detect(snapshot);
        var sensitive = new ComputerUseSensitiveSurfaceDetector().Detect(snapshot);
        var locator = new ComputerUseLocatorEngine().GenerateCandidates(snapshot, objective).FirstOrDefault();
        var reasons = new List<string>();
        var handoffReasons = new List<ComputerUseHandoffReason>();

        if (snapshot.Source != ComputerUseSnapshotSource.Fixture)
        {
            reasons.Add("Live or non-fixture source is disabled.");
            handoffReasons.Add(ComputerUseHandoffReason.LiveExecutionDisabled);
        }

        if (blockages.Any(b => b.RequiresHumanHandoff))
        {
            reasons.Add("Blocking surface requires human handoff.");
            handoffReasons.AddRange(blockages.Select(ToHandoffReason).Where(r => r != ComputerUseHandoffReason.None));
        }

        if (sensitive.Count > 0 || ContainsSensitiveOrCredentialObjective(objective))
        {
            reasons.Add("Sensitive objective or surface requires handoff.");
            handoffReasons.Add(ComputerUseHandoffReason.SensitiveSurface);
        }

        if (classifier.Confidence < 0.6 || locator?.Confidence < 0.6)
        {
            reasons.Add("Low-confidence locator or classification blocks planning.");
            handoffReasons.Add(ComputerUseHandoffReason.LowConfidence);
        }

        if (locator?.RequiresVisualFallback == true)
        {
            reasons.Add("Visual-only target requires future fallback design.");
            handoffReasons.Add(ComputerUseHandoffReason.VisualOnlyTarget);
        }

        var requiresHandoff = handoffReasons.Count > 0;
        var action = requiresHandoff ? ComputerUseActionKind.HumanHandoff : requestedAction;
        var candidate = new ComputerUseActionCandidate(
            action,
            requiresHandoff ? null : locator,
            objective,
            requiresHandoff ? 0 : Math.Min(classifier.Confidence, locator?.Confidence ?? 0),
            DryRunOnly: true,
            RequiresExplicitFutureApproval: requestedAction is ComputerUseActionKind.Click or ComputerUseActionKind.SetValue or ComputerUseActionKind.Hotkey,
            RequiresHumanHandoff: requiresHandoff,
            HandoffReasons: handoffReasons.Distinct().ToList(),
            Reason: requiresHandoff ? string.Join(" ", reasons.Distinct()) : "Fixture-safe dry-run plan only; no execution.");

        return new ComputerUsePolicyDecision(
            AllowedToPlan: !requiresHandoff,
            AllowedToExecuteLive: false,
            FixtureOnly: true,
            Candidates: [candidate],
            Blockages: blockages,
            SensitiveSurfaces: sensitive,
            Reasons: reasons.Count == 0 ? ["No live execution is authorized; dry-run only."] : reasons.Distinct().ToList());
    }

    private static bool ContainsSensitiveOrCredentialObjective(string objective)
    {
        var value = objective.ToLowerInvariant();
        string[] blocked =
        [
            "password", "credential", "token", "otp", "2fa", "mfa", "uac", "admin",
            "delete", "overwrite", "format", "payment", "submit", "clipboard"
        ];
        return blocked.Any(value.Contains);
    }

    private static ComputerUseHandoffReason ToHandoffReason(ComputerUseBlockage blockage) =>
        blockage.Kind switch
        {
            ComputerUseBlockageKind.CredentialField => ComputerUseHandoffReason.CredentialField,
            ComputerUseBlockageKind.UacAdmin => ComputerUseHandoffReason.UacAdmin,
            ComputerUseBlockageKind.DestructiveAction => ComputerUseHandoffReason.DestructiveAction,
            ComputerUseBlockageKind.AppNotAllowlisted => ComputerUseHandoffReason.NonAllowlistedApp,
            ComputerUseBlockageKind.LowConfidenceLocator => ComputerUseHandoffReason.LowConfidence,
            ComputerUseBlockageKind.VisualOnlyTarget => ComputerUseHandoffReason.VisualOnlyTarget,
            _ => ComputerUseHandoffReason.None
        };
}

public sealed class ComputerUsePerceptionRouter
{
    public ComputerUsePolicyDecision Route(ComputerUseSnapshot snapshot, string objective, ComputerUseActionKind requestedAction) =>
        new ComputerUseSafeActionPlanner().Plan(snapshot, objective, requestedAction);
}

public sealed class ComputerUseEvidenceRedactor
{
    private static readonly Regex SensitiveKeyValuePattern = new(
        @"(?i)\b(password|passwd|pwd|token|api[_ -]?key|secret|authorization|credential|otp|2fa|mfa|cookie|session|clipboard|cvv|ssn)\b\s*[:=]\s*[^,\s;]+",
        RegexOptions.Compiled);

    private static readonly Regex EmailPattern = new(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.Compiled);
    private static readonly Regex JwtPattern = new(@"eyJ[a-zA-Z0-9_-]+\.eyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+", RegexOptions.Compiled);
    private static readonly Regex CreditCardPattern = new(@"\b(?:\d{4}[-\s]?){3}\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex SsnPattern = new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex TokenPattern = new(@"\b(sk-[A-Za-z0-9_-]{8,}|ghp_[A-Za-z0-9_]{8,}|Bearer\s+[A-Za-z0-9._-]{12,})\b", RegexOptions.Compiled);

    public ComputerUseRedactionResult Redact(string value)
    {
        var redacted = value;
        var fields = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        redacted = SensitiveKeyValuePattern.Replace(redacted, match =>
        {
            fields.Add(NormalizeSensitiveField(match.Groups[1].Value));
            return "[REDACTED]";
        });
        redacted = Replace(EmailPattern, redacted, fields, "email");
        redacted = Replace(JwtPattern, redacted, fields, "jwt");
        redacted = Replace(CreditCardPattern, redacted, fields, "credit-card");
        redacted = Replace(SsnPattern, redacted, fields, "ssn");
        redacted = Replace(TokenPattern, redacted, fields, "token");

        return new ComputerUseRedactionResult(
            redacted,
            fields.Count == 0 ? ComputerUseRedactionStatus.None : ComputerUseRedactionStatus.Partial,
            fields.ToList());
    }

    private static string Replace(Regex regex, string input, ISet<string> fields, string category) =>
        regex.Replace(input, _ =>
        {
            fields.Add(category);
            return "[REDACTED]";
        });

    private static string NormalizeSensitiveField(string field)
    {
        var normalized = field.Trim().ToLowerInvariant().Replace("_", "-").Replace(" ", "-");
        return normalized switch
        {
            "passwd" or "pwd" => "password",
            "api-key" => "api-key",
            "2fa" or "mfa" => "otp-mfa",
            _ => normalized
        };
    }
}

public sealed class ComputerUseEvidencePackBuilder
{
    private readonly ComputerUseEvidenceRedactor _redactor = new();

    public ComputerUseEvidencePack Build(
        ComputerUseEvidenceKind kind,
        ComputerUseSnapshot snapshot,
        ComputerUsePolicyDecision? decision = null,
        ComputerUseActionCandidate? action = null,
        ComputerUseVerificationResult? verification = null,
        string summary = "")
    {
        var blockages = decision?.Blockages ?? [];
        var sensitive = decision?.SensitiveSurfaces ?? [];
        var rawSummary = string.IsNullOrWhiteSpace(summary)
            ? $"{kind}; snapshot={snapshot.SnapshotId}; scenario={snapshot.Scenario}; action={action?.ActionKind.ToString() ?? "none"}"
            : summary;
        var redaction = _redactor.Redact(rawSummary);

        return new ComputerUseEvidencePack(
            EvidenceId: $"wcu-evidence-{Guid.NewGuid():N}",
            CorrelationId: snapshot.SnapshotId,
            EvidenceKind: kind,
            SnapshotRef: snapshot.SnapshotId,
            PolicyDecision: decision,
            PlannedAction: action,
            VerificationResult: verification,
            Blockages: blockages,
            SensitiveSurfaces: sensitive,
            RedactionStatus: redaction.Status,
            SensitiveFieldsRedacted: redaction.SensitiveFieldsRedacted,
            EvidenceSummary: redaction.Value,
            EvidenceRefs: [new ComputerUseEvidenceRef(snapshot.SnapshotId, "snapshot", "Fixture snapshot metadata only.", true)],
            LiveExecutionEnabled: false,
            RealMouseUsed: snapshot.RealMouseUsed,
            RealKeyboardUsed: snapshot.RealKeyboardUsed,
            LiveUiaActionUsed: snapshot.LiveUiaActionUsed,
            ClipboardCaptured: snapshot.ClipboardCaptured,
            ScreenshotPersisted: snapshot.ScreenshotPersisted,
            CreatedAtUtc: DateTimeOffset.UnixEpoch);
    }

    public string Serialize(ComputerUseEvidencePack pack) =>
        JsonSerializer.Serialize(pack, new JsonSerializerOptions { WriteIndented = true });
}
