namespace OneBrain.BrowserPerception;

public sealed class BlockageDetector
{
    public IReadOnlyList<BlockageReport> DetectBlockages(BrowserPerceptionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var reports = new List<BlockageReport>();

        AddSignalBlockages(snapshot.Signals, reports);
        AddNetworkBlockages(snapshot.Network, reports);
        AddConsoleBlockages(snapshot.Console, reports);

        return reports
            .GroupBy(report => report.BlockageKind)
            .Select(group => group.OrderByDescending(report => report.Severity).First())
            .ToArray();
    }

    public IReadOnlyList<BlockageReport> DetectBlockages(PageCapabilityProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var reports = new List<BlockageReport>();
        AddSignalBlockages(profile.DetectedSignals, reports);

        if (profile.DetectedPageKind == DetectedPageKind.AuthRequired)
        {
            reports.Add(HumanHandoff(BlockageKind.Login, "Authentication or credential entry requires human handoff."));
        }
        else if (profile.DetectedPageKind == DetectedPageKind.HumanVerificationRequired)
        {
            reports.Add(HumanHandoff(BlockageKind.Captcha, "Human verification requires handoff; no bypass is allowed."));
        }
        else if (profile.DetectedPageKind == DetectedPageKind.BrokenOrUnstable)
        {
            reports.Add(new BlockageReport(
                BlockageKind.BrokenPage,
                BrowserPerceptionSeverity.Critical,
                "Page is broken or unstable and blocks future action routing.",
                CanContinueAutomatically: false,
                RequiresHumanHandoff: false,
                EvidenceSummary: "Profile classified page as broken or unstable."));
        }

        return reports
            .GroupBy(report => report.BlockageKind)
            .Select(group => group.OrderByDescending(report => report.Severity).First())
            .ToArray();
    }

    private static void AddSignalBlockages(
        IReadOnlyList<PerceptionSignal> signals,
        ICollection<BlockageReport> reports)
    {
        foreach (var signal in signals)
        {
            // Signal names are metadata markers emitted by fixture/read-only collectors.
            // Matching is normalized here so casing and incidental whitespace do not change diagnostics.
            var name = NormalizeSignalName(signal.Name);
            if (Contains(name, "captcha") || Contains(name, "human-verification"))
            {
                reports.Add(HumanHandoff(BlockageKind.Captcha, "CAPTCHA marker requires human handoff; no bypass is allowed."));
            }
            else if (Contains(name, "two-factor") || Contains(name, "2fa"))
            {
                reports.Add(HumanHandoff(BlockageKind.TwoFactor, "2FA marker requires human handoff; no bypass is allowed."));
            }
            else if (Contains(name, "anti-bot"))
            {
                reports.Add(HumanHandoff(BlockageKind.AntiBot, "Anti-bot marker requires human handoff; no bypass is allowed."));
            }
            else if (Contains(name, "auth-required") || Contains(name, "login"))
            {
                reports.Add(HumanHandoff(BlockageKind.Login, "Login or credential entry requires human handoff."));
            }
            else if (Contains(name, "cookie-wall"))
            {
                reports.Add(new BlockageReport(
                    BlockageKind.CookieWall,
                    BrowserPerceptionSeverity.Warning,
                    "Cookie wall detected as a future diagnostic condition; no dismissal is executed.",
                    CanContinueAutomatically: true,
                    RequiresHumanHandoff: false,
                    EvidenceSummary: signal.Summary));
            }
            else if (Contains(name, "popup") || Contains(name, "modal"))
            {
                reports.Add(new BlockageReport(
                    BlockageKind.Popup,
                    BrowserPerceptionSeverity.Warning,
                    "Popup/modal detected as a future diagnostic condition; no close action is executed.",
                    CanContinueAutomatically: true,
                    RequiresHumanHandoff: false,
                    EvidenceSummary: signal.Summary));
            }
        }
    }

    private static void AddNetworkBlockages(
        BrowserNetworkSummary network,
        ICollection<BlockageReport> reports)
    {
        foreach (var statusCode in network.FailedStatusCodes)
        {
            switch (statusCode)
            {
                case 401:
                    reports.Add(HumanHandoff(BlockageKind.Login, "HTTP 401 indicates authentication is required."));
                    break;
                case 403:
                    reports.Add(new BlockageReport(
                        BlockageKind.AccessDenied,
                        BrowserPerceptionSeverity.Critical,
                        "HTTP 403 access denied blocks automatic continuation.",
                        CanContinueAutomatically: false,
                        RequiresHumanHandoff: false,
                        EvidenceSummary: "Network metadata reported 403."));
                    break;
                case 429:
                    reports.Add(new BlockageReport(
                        BlockageKind.RateLimit,
                        BrowserPerceptionSeverity.Critical,
                        "HTTP 429 rate limit may require future backoff policy; no bypass is allowed.",
                        CanContinueAutomatically: false,
                        RequiresHumanHandoff: false,
                        EvidenceSummary: "Network metadata reported 429."));
                    break;
                case >= 500:
                    reports.Add(new BlockageReport(
                        BlockageKind.NetworkFailure,
                        BrowserPerceptionSeverity.Critical,
                        "Server/network failure blocks automatic continuation.",
                        CanContinueAutomatically: false,
                        RequiresHumanHandoff: false,
                        EvidenceSummary: "Network metadata reported " + statusCode + "."));
                    break;
            }
        }

        if (network.CriticalFailureCount > 0 && reports.All(report => report.BlockageKind != BlockageKind.NetworkFailure))
        {
            reports.Add(new BlockageReport(
                BlockageKind.NetworkFailure,
                BrowserPerceptionSeverity.Critical,
                "Critical network failure blocks automatic continuation.",
                CanContinueAutomatically: false,
                RequiresHumanHandoff: false,
                EvidenceSummary: "Network metadata reported critical failure count."));
        }
    }

    private static void AddConsoleBlockages(
        BrowserConsoleSummary console,
        ICollection<BlockageReport> reports)
    {
        if (console.CriticalErrorCount <= 0)
            return;

        reports.Add(new BlockageReport(
            BlockageKind.ConsoleError,
            BrowserPerceptionSeverity.Critical,
            "Critical console/runtime error blocks automatic continuation.",
            CanContinueAutomatically: false,
            RequiresHumanHandoff: false,
            EvidenceSummary: "Console metadata reported critical errors."));
    }

    private static BlockageReport HumanHandoff(BlockageKind kind, string reason) =>
        new(
            kind,
            BrowserPerceptionSeverity.Critical,
            reason,
            CanContinueAutomatically: false,
            RequiresHumanHandoff: true,
            EvidenceSummary: reason);

    private static bool Contains(string value, string expected) =>
        value.Contains(expected, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeSignalName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return string.Join(' ', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
