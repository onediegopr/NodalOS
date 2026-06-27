namespace OneBrain.WindowsComputerUse;

public enum ComputerUseStaticScanRuleId
{
    NoPInvokeOrDllImport,
    NoFlaUiOrLiveUia,
    NoRealMouseKeyboardControl,
    NoWindowManipulation,
    NoClipboardReal,
    NoRawScreenshotCapture,
    NoProcessShellSubprocess,
    NoBrowserLiveCdpWebSocketSafeInjection,
    NoProductUiEnablement,
    NoLiveReadyWording,
    NoReleasePublicPaidBetaUnlock,
    NoClaimDrift,
    NoSecretLeakage,
    ProtectedScopeNoDiff
}

public enum ComputerUseStaticScanCategory
{
    NoLiveNoAction,
    ProtectedBoundary,
    ClaimWording,
    SecretLeakage,
    JsonReport,
    ManualDiffCheck
}

public enum ComputerUseStaticScanScope
{
    WcuCode,
    WcuTests,
    WcuDocs,
    WcuReports,
    WcuPrompts,
    ProtectedStealthCore
}

public enum ComputerUseStaticScanExpectedOutcome
{
    NoMatches,
    NegativeReferencesOnly,
    SyntheticFixturesOnly,
    ProtectedScopeNoDiff,
    FailClosed
}

public sealed record ComputerUseAllowedNegativeReference(
    string Value,
    string Reason);

public sealed record ComputerUseStaticScanRule(
    ComputerUseStaticScanRuleId RuleId,
    string Name,
    ComputerUseStaticScanCategory Category,
    IReadOnlyList<ComputerUseStaticScanScope> Scopes,
    IReadOnlyList<string> ForbiddenPatterns,
    IReadOnlyList<ComputerUseAllowedNegativeReference> AllowedNegativeReferences,
    ComputerUseStaticScanExpectedOutcome ExpectedOutcome,
    string SourceOfTruth,
    string ManualCommand,
    string ArtifactOutput,
    bool ActionAuthorityGranted,
    bool LiveUnlockGranted);

public sealed record ComputerUseProtectedBoundaryRule(
    string BoundaryId,
    IReadOnlyList<string> ProtectedPaths,
    IReadOnlyList<string> AllowedWcuPaths,
    IReadOnlyList<string> ForbiddenWcuPaths,
    string FailureMode,
    bool AllowsStealthCoreDiff,
    bool AllowsSidepanelHashDebtMixing,
    bool AllowsLivePrototype);

public sealed record ComputerUseStaticScanFinding(
    ComputerUseStaticScanRuleId RuleId,
    string Pattern,
    string Path,
    bool AllowedNegativeReference,
    string Reason);

public sealed record ComputerUseStaticScanResult(
    string ArtifactId,
    bool Passed,
    IReadOnlyList<ComputerUseStaticScanFinding> Findings,
    bool ActionAuthorityGranted,
    bool LiveUnlockGranted,
    bool ProductAutomationEnabled);

public static class ComputerUseStaticScanCatalog
{
    public static IReadOnlyList<ComputerUseStaticScanRule> Rules { get; } =
    [
        Rule(ComputerUseStaticScanRuleId.NoPInvokeOrDllImport, "no P/Invoke / DllImport", ComputerUseStaticScanCategory.NoLiveNoAction, ["DllImport", "LibraryImport", "[DllImport", "[LibraryImport"], ["NO_P_INVOKE_OR_DLLIMPORT", "No P/Invoke", "No DllImport"]),
        Rule(ComputerUseStaticScanRuleId.NoFlaUiOrLiveUia, "no FlaUI / live UIA", ComputerUseStaticScanCategory.NoLiveNoAction, ["FlaUI", "System.Windows.Automation", "AddAutomationEventHandler"], ["NO_FLAUI_OR_LIVE_UIA", "No FlaUI", "live UIA subscription is prohibited"]),
        Rule(ComputerUseStaticScanRuleId.NoRealMouseKeyboardControl, "no real mouse/keyboard", ComputerUseStaticScanCategory.NoLiveNoAction, ["SendInput", "mouse_event", "keybd_event", "SetCursorPos", "Cursor.Position", "SendKeys"], ["NO_REAL_MOUSE_KEYBOARD_CONTROL", "No real mouse", "No real keyboard"]),
        Rule(ComputerUseStaticScanRuleId.NoWindowManipulation, "no window manipulation", ComputerUseStaticScanCategory.NoLiveNoAction, ["SetForegroundWindow", "PostMessage", "SendMessage", "ShowWindow", "MoveWindow"], ["WCU_NO_WINDOW_MANIPULATION_GATE", "window manipulation is prohibited"]),
        Rule(ComputerUseStaticScanRuleId.NoClipboardReal, "no clipboard", ComputerUseStaticScanCategory.NoLiveNoAction, ["Clipboard.", "GetClipboardData", "SetClipboardData"], ["NO_CLIPBOARD_REAL", "No clipboard", "Clipboard access remains prohibited"]),
        Rule(ComputerUseStaticScanRuleId.NoRawScreenshotCapture, "no raw screenshot capture", ComputerUseStaticScanCategory.NoLiveNoAction, ["Graphics.CopyFromScreen", "CopyFromScreen", "ScreenCapture", "raw screenshot bytes"], ["NO_REAL_SCREENSHOT_CAPTURE", "No raw screenshot", "raw screenshot persistence remains prohibited"]),
        Rule(ComputerUseStaticScanRuleId.NoProcessShellSubprocess, "no shell/subprocess runtime", ComputerUseStaticScanCategory.NoLiveNoAction, ["Process.Start", "UseShellExecute", "cmd.exe", "powershell.exe"], ["No shell/subprocess runtime", "No Process.Start in WCU runtime"]),
        Rule(ComputerUseStaticScanRuleId.NoBrowserLiveCdpWebSocketSafeInjection, "no browser live/CDP/WebSocket/Safe Injection", ComputerUseStaticScanCategory.NoLiveNoAction, ["chrome.debugger", "CDP live", "WebSocket live bridge", "Safe Injection live"], ["NO_CDP_LIVE_EXECUTION", "NO_SAFE_INJECTION_LIVE", "browser live/CDP remains blocked"]),
        Rule(ComputerUseStaticScanRuleId.NoProductUiEnablement, "no product UI enablement", ComputerUseStaticScanCategory.ClaimWording, ["ProductAutomationEnabled=true", "\"ProductAutomationEnabled\": true", "product automation enabled"], ["ProductAutomationEnabled=false", "ProductAutomationEnabled remains false"]),
        Rule(ComputerUseStaticScanRuleId.NoLiveReadyWording, "no live-ready wording", ComputerUseStaticScanCategory.ClaimWording, ["safe to start live implementation", "live-ready", "production-ready"], ["not live-ready", "No production-ready or live-ready claim"]),
        Rule(ComputerUseStaticScanRuleId.NoReleasePublicPaidBetaUnlock, "no release/public/paid beta unlock", ComputerUseStaticScanCategory.ClaimWording, ["public release unlocked", "paid beta unlocked", "\"public_release_unlock\": true", "\"paid_beta_unlock\": true"], ["public_release_unlock: false", "paid_beta_unlock: false", "Release/public/paid beta unlock: 0%"]),
        Rule(ComputerUseStaticScanRuleId.NoClaimDrift, "no claim drift", ComputerUseStaticScanCategory.JsonReport, ["\"live_prototype_authorized\": true", "\"LiveReadPermitted\": true", "\"ActionAuthorityGranted\": true", "containment pass grants live go"], ["live_prototype_authorized\": false", "LiveReadPermitted=false", "ActionAuthorityGranted=false", "BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO"]),
        Rule(ComputerUseStaticScanRuleId.NoSecretLeakage, "no secret leakage", ComputerUseStaticScanCategory.SecretLeakage, ["sk-live", "ghp_live", "AKIA", "-----BEGIN PRIVATE KEY-----"], ["synthetic redaction fixture", "sk-testSecret", "ghp_fakeSecretToken"]),
        Rule(ComputerUseStaticScanRuleId.ProtectedScopeNoDiff, "protected scope no diff", ComputerUseStaticScanCategory.ProtectedBoundary,
            [
                "stealth-engine/src/evasion/**",
                "stealth-engine/src/captcha/**",
                "stealth-engine/src/fingerprint/**",
                "stealth-engine/src/behavior/**",
                "stealth-engine/src/proxy/**",
                "stealth-engine/src/antiBlocking/**",
                "stealth-engine/src/handoff/**",
                "stealth-engine/src/StealthSession.js",
                "stealth-engine/src/StealthBrowserManager.js",
                "stealth-engine/src/index.js",
                "stealth-engine/tests/stealth-suite.test.js"
            ],
            ["protected scope PASS", "protected scope no diff"])
    ];

    public static ComputerUseProtectedBoundaryRule ProtectedBoundary { get; } =
        new(
            BoundaryId: "WCU_PROTECTED_STEALTH_CORE_BOUNDARY",
            ProtectedPaths:
            [
                "stealth-engine/src/evasion/**",
                "stealth-engine/src/captcha/**",
                "stealth-engine/src/fingerprint/**",
                "stealth-engine/src/behavior/**",
                "stealth-engine/src/proxy/**",
                "stealth-engine/src/antiBlocking/**",
                "stealth-engine/src/handoff/**",
                "stealth-engine/src/StealthSession.js",
                "stealth-engine/src/StealthBrowserManager.js",
                "stealth-engine/src/index.js",
                "stealth-engine/tests/stealth-suite.test.js"
            ],
            AllowedWcuPaths:
            [
                "src/OneBrain.WindowsComputerUse/**",
                "tests/OneBrain.Safety.Tests/**",
                "docs/architecture/computer-use/**",
                "docs/qa/computer-use/**",
                "docs/handoff/**",
                "docs/prompts/computer-use/**"
            ],
            ForbiddenWcuPaths:
            [
                "stealth-engine/** protected subset",
                "browser live/CDP implementation paths",
                "sidepanel extension hash baseline debt"
            ],
            FailureMode: "Fail closed; do not commit WCU containment changes when protected scope has diff.",
            AllowsStealthCoreDiff: false,
            AllowsSidepanelHashDebtMixing: false,
            AllowsLivePrototype: false);

    public static ComputerUseStaticScanResult EvaluateText(string artifactId, string path, string text)
    {
        var findings = new List<ComputerUseStaticScanFinding>();
        foreach (var rule in Rules)
        {
            foreach (var pattern in rule.ForbiddenPatterns)
            {
                if (!text.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    continue;

                var allowed = rule.AllowedNegativeReferences.Any(reference =>
                    text.Contains(reference.Value, StringComparison.OrdinalIgnoreCase));
                findings.Add(new ComputerUseStaticScanFinding(
                    rule.RuleId,
                    pattern,
                    path,
                    allowed,
                    allowed ? "Allowed negative/prohibition reference." : "Forbidden executable or unlock pattern."));
            }
        }

        return new ComputerUseStaticScanResult(
            artifactId,
            Passed: findings.All(f => f.AllowedNegativeReference),
            Findings: findings,
            ActionAuthorityGranted: false,
            LiveUnlockGranted: false,
            ProductAutomationEnabled: false);
    }

    private static ComputerUseStaticScanRule Rule(
        ComputerUseStaticScanRuleId id,
        string name,
        ComputerUseStaticScanCategory category,
        IReadOnlyList<string> forbidden,
        IReadOnlyList<string> allowed) =>
        new(
            id,
            name,
            category,
            [ComputerUseStaticScanScope.WcuCode, ComputerUseStaticScanScope.WcuTests, ComputerUseStaticScanScope.WcuDocs, ComputerUseStaticScanScope.WcuReports, ComputerUseStaticScanScope.WcuPrompts],
            forbidden,
            allowed.Select(value => new ComputerUseAllowedNegativeReference(value, "Negative/prohibition-only reference.")).ToArray(),
            ComputerUseStaticScanExpectedOutcome.NegativeReferencesOnly,
            "ComputerUseStaticScanCatalog",
            "rg over changed WCU files; do not execute live providers or OS actions.",
            "static-scan-report.json",
            ActionAuthorityGranted: false,
            LiveUnlockGranted: false);
}
