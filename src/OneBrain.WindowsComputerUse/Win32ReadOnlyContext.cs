namespace OneBrain.WindowsComputerUse;

public enum Win32WindowPlacement
{
    Unknown,
    Normal,
    Minimized,
    Maximized,
    Hidden
}

public enum Win32ContextCollectionStatus
{
    Disabled,
    FixtureOnly,
    NotConfigured,
    Failed
}

public sealed record Win32WindowIdentity(
    string HwndOpaque,
    string TitleRedacted,
    string ClassName,
    bool IsForeground,
    bool IsTopLevel,
    bool IsVisible,
    bool IsEnabled);

public sealed record Win32ProcessContext(
    int ProcessId,
    string ProcessName,
    string ProcessPathRedacted,
    bool IsAllowlisted);

public sealed record Win32MonitorContext(
    string MonitorId,
    int X,
    int Y,
    int Width,
    int Height,
    bool IsPrimary);

public sealed record Win32DpiContext(
    double DpiScale,
    int DpiX,
    int DpiY,
    bool MismatchDetected);

public sealed record Win32ModalContext(
    bool IsModal,
    string? OwnerHwndOpaque,
    string ReasonRedacted);

public sealed record Win32WindowContext(
    Win32WindowIdentity Identity,
    Win32ProcessContext Process,
    Win32MonitorContext Monitor,
    Win32DpiContext Dpi,
    Win32WindowPlacement Placement,
    Win32ModalContext Modal,
    int ZOrderIndex,
    bool ActionAuthority,
    bool Redacted,
    IReadOnlyList<string> EvidenceRefs);

public sealed record Win32ContextCollectionOptions(
    string Scenario,
    bool IncludeForegroundWindowMetadata = true,
    bool IncludeProcessPath = true,
    bool IncludeMonitorDpi = true,
    bool AllowWindowManipulation = false,
    bool AllowFocusStealing = false,
    bool AllowInputInjection = false,
    bool AllowClipboard = false,
    bool AllowScreenshots = false);

public sealed record Win32ContextCollectionResult(
    Win32ContextCollectionStatus Status,
    Win32WindowContext? ActiveWindow,
    IReadOnlyList<Win32WindowContext> Windows,
    bool ReadRealPc,
    bool WindowManipulationUsed,
    bool FocusStealingUsed,
    bool InputInjectionUsed,
    bool ClipboardUsed,
    bool ScreenshotCaptured,
    bool ActionAuthority,
    IReadOnlyList<string> Reasons);

public interface IWin32ContextReadOnlyCollector
{
    Win32ContextCollectionResult Collect(Win32ContextCollectionOptions options);
}

public sealed class DisabledWin32ContextReadOnlyCollector : IWin32ContextReadOnlyCollector
{
    public Win32ContextCollectionResult Collect(Win32ContextCollectionOptions options)
    {
        var reasons = new List<string> { "Win32 live context collection is disabled by default." };
        AddIf(reasons, options.AllowWindowManipulation, "Window manipulation is prohibited.");
        AddIf(reasons, options.AllowFocusStealing, "Focus stealing is prohibited.");
        AddIf(reasons, options.AllowInputInjection, "Input injection is prohibited.");
        AddIf(reasons, options.AllowClipboard, "Clipboard access is prohibited.");
        AddIf(reasons, options.AllowScreenshots, "Screenshot capture is prohibited.");

        return new Win32ContextCollectionResult(
            Win32ContextCollectionStatus.Disabled,
            ActiveWindow: null,
            Windows: [],
            ReadRealPc: false,
            WindowManipulationUsed: false,
            FocusStealingUsed: false,
            InputInjectionUsed: false,
            ClipboardUsed: false,
            ScreenshotCaptured: false,
            ActionAuthority: false,
            Reasons: reasons);
    }

    private static void AddIf(ICollection<string> reasons, bool condition, string reason)
    {
        if (condition)
        {
            reasons.Add(reason);
        }
    }
}

public sealed class FixtureWin32ContextReadOnlyCollector : IWin32ContextReadOnlyCollector
{
    private readonly Win32ContextCollectionResult _result;

    public FixtureWin32ContextReadOnlyCollector(Win32ContextCollectionResult result)
    {
        _result = result;
    }

    public Win32ContextCollectionResult Collect(Win32ContextCollectionOptions options) =>
        _result with
        {
            ReadRealPc = false,
            WindowManipulationUsed = false,
            FocusStealingUsed = false,
            InputInjectionUsed = false,
            ClipboardUsed = false,
            ScreenshotCaptured = false,
            ActionAuthority = false
        };
}

public static class FixtureWin32ContextFactory
{
    public static Win32ContextCollectionResult NotepadActive() =>
        Result(Window("hwnd-notepad", "Untitled - Notepad", "Notepad", "notepad", @"C:\Program Files\WindowsApps\Notepad\Notepad.exe", foreground: true));

    public static Win32ContextCollectionResult ElectronActive() =>
        Result(Window("hwnd-electron", "Electron App", "Chrome_WidgetWin_1", "electron-app", @"C:\Users\diego\AppData\Local\ElectronApp\app.exe", foreground: true));

    public static Win32ContextCollectionResult ModalDialog() =>
        Result(Window("hwnd-modal", "Confirm overwrite", "#32770", "docs-app", @"C:\Program Files\DocsApp\docs.exe", foreground: true, modal: true, owner: "hwnd-docs"));

    public static Win32ContextCollectionResult LoginActiveWithSensitiveTitle() =>
        Result(Window("hwnd-login", "Sign in user@example.com", "LoginWindow", "login-app", @"C:\Users\diego\AppData\Local\LoginApp\login.exe", foreground: true));

    public static Win32ContextCollectionResult UacAdminLike() =>
        Result(Window("hwnd-uac", "User Account Control", "#32770", "consent", @"C:\Windows\System32\consent.exe", foreground: true, modal: true, allowlisted: false));

    public static Win32ContextCollectionResult EmptyBlocked() =>
        Result(Window("hwnd-empty", "Loading blocked unavailable", "Chrome_WidgetWin_1", "electron-app", @"C:\Users\diego\AppData\Local\ElectronApp\app.exe", foreground: true));

    public static Win32ContextCollectionResult DpiMismatch() =>
        Result(Window("hwnd-dpi", "Diagnostics", "DiagWindow", "diagnostics", @"C:\Program Files\Diagnostics\diag.exe", foreground: true) with
        {
            Dpi = new Win32DpiContext(1.5, 144, 144, MismatchDetected: true)
        });

    private static Win32ContextCollectionResult Result(Win32WindowContext active) =>
        new(
            Win32ContextCollectionStatus.FixtureOnly,
            active,
            [active],
            ReadRealPc: false,
            WindowManipulationUsed: false,
            FocusStealingUsed: false,
            InputInjectionUsed: false,
            ClipboardUsed: false,
            ScreenshotCaptured: false,
            ActionAuthority: false,
            Reasons: ["Fixture Win32 context metadata only."]);

    private static Win32WindowContext Window(
        string hwnd,
        string title,
        string className,
        string processName,
        string processPath,
        bool foreground,
        bool modal = false,
        string? owner = null,
        bool allowlisted = true)
    {
        var redactor = new ComputerUseEvidenceRedactor();
        var redactedTitle = redactor.Redact(title).Value;
        var redactedPath = redactor.Redact(processPath).Value;
        return new Win32WindowContext(
            new Win32WindowIdentity(hwnd, redactedTitle, className, foreground, IsTopLevel: true, IsVisible: true, IsEnabled: true),
            new Win32ProcessContext(ProcessId: 1000, processName, redactedPath, allowlisted),
            new Win32MonitorContext("fixture-monitor-1", 0, 0, 1920, 1080, IsPrimary: true),
            new Win32DpiContext(1.0, 96, 96, MismatchDetected: false),
            Win32WindowPlacement.Normal,
            new Win32ModalContext(modal, owner, modal ? "Modal/top-level owner relationship detected." : "No modal owner."),
            ZOrderIndex: 0,
            ActionAuthority: false,
            Redacted: true,
            EvidenceRefs: [$"win32:{hwnd}:redacted"]);
    }
}
