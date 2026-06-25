using System.Text.Json;

namespace OneBrain.ChromeLab.Bridge.Stealth;

public static class FrictionSignalRouter
{
    public static IReadOnlyList<FrictionSignal> FromCompanionObservation(
        string runId,
        JsonElement observation)
    {
        var signals = new List<FrictionSignal>();
        var now = DateTimeOffset.UtcNow;

        if (ReadBool(observation, "hasCaptchaLike"))
        {
            signals.Add(new FrictionSignal(
                SignalId: Guid.NewGuid().ToString("n"),
                TaskId: runId,
                Kind: FrictionSignalKind.CaptchaDetected,
                Severity: FrictionSignalSeverity.Critical,
                Source: "companion-observation",
                FrameId: "main",
                ElementId: null,
                Sitekey: null,
                BlockHttpCode: null,
                BlockPattern: null,
                RedactedEvidence: "CAPTCHA-like content detected in page observation",
                Reason: "Page contains CAPTCHA or anti-bot challenge keywords",
                DetectedAtUtc: now,
                AutoSolvable: false,
                SolverRecommendation: null,
                EvidenceRefs: [$"companion:captcha:{runId}"],
                ProofRefs: [$"proof:companion:captcha:{runId}"]));
        }

        if (ReadBool(observation, "hasTwoFactorLike"))
        {
            signals.Add(new FrictionSignal(
                SignalId: Guid.NewGuid().ToString("n"),
                TaskId: runId,
                Kind: FrictionSignalKind.TwoFactorDetected,
                Severity: FrictionSignalSeverity.Critical,
                Source: "companion-observation",
                FrameId: "main",
                ElementId: null,
                Sitekey: null,
                BlockHttpCode: null,
                BlockPattern: null,
                RedactedEvidence: "2FA-like content detected in page observation",
                Reason: "Page contains two-factor authentication keywords",
                DetectedAtUtc: now,
                AutoSolvable: false,
                SolverRecommendation: null,
                EvidenceRefs: [$"companion:2fa:{runId}"],
                ProofRefs: [$"proof:companion:2fa:{runId}"]));
        }

        if (ReadBool(observation, "hasPasswordField"))
        {
            signals.Add(new FrictionSignal(
                SignalId: Guid.NewGuid().ToString("n"),
                TaskId: runId,
                Kind: FrictionSignalKind.PasswordFieldDetected,
                Severity: FrictionSignalSeverity.Critical,
                Source: "companion-observation",
                FrameId: "main",
                ElementId: null,
                Sitekey: null,
                BlockHttpCode: null,
                BlockPattern: null,
                RedactedEvidence: "Password field detected in page observation",
                Reason: "Page contains password or credential input fields",
                DetectedAtUtc: now,
                AutoSolvable: false,
                SolverRecommendation: null,
                EvidenceRefs: [$"companion:password:{runId}"],
                ProofRefs: [$"proof:companion:password:{runId}"]));
        }

        return signals;
    }

    public static FrictionSignal FromStealthMessage(string taskId, JsonElement message)
    {
        var kind = ParseFrictionSignalKind(GetString(message, "kind"));
        var severity = ParseFrictionSignalSeverity(GetString(message, "severity"));

        return new FrictionSignal(
            SignalId: GetString(message, "signalId") ?? Guid.NewGuid().ToString("n"),
            TaskId: taskId,
            Kind: kind,
            Severity: severity,
            Source: GetString(message, "source") ?? "stealth-detector",
            FrameId: GetString(message, "frameId") ?? "main",
            ElementId: GetString(message, "elementId"),
            Sitekey: GetString(message, "sitekey"),
            BlockHttpCode: GetString(message, "blockHttpCode"),
            BlockPattern: GetString(message, "blockPattern"),
            RedactedEvidence: GetString(message, "redactedEvidence") ?? "",
            Reason: GetString(message, "reason") ?? "",
            DetectedAtUtc: ParseDateTime(message, "detectedAtUtc") ?? DateTimeOffset.UtcNow,
            AutoSolvable: ReadBool(message, "autoSolvable"),
            SolverRecommendation: GetString(message, "solverRecommendation"),
            EvidenceRefs: ReadStringArray(message, "evidenceRefs"),
            ProofRefs: ReadStringArray(message, "proofRefs"));
    }

    private static FrictionSignalKind ParseFrictionSignalKind(string? value) => value switch
    {
        "CaptchaDetected" => FrictionSignalKind.CaptchaDetected,
        "TwoFactorDetected" => FrictionSignalKind.TwoFactorDetected,
        "PasswordFieldDetected" => FrictionSignalKind.PasswordFieldDetected,
        "LoginFormDetected" => FrictionSignalKind.LoginFormDetected,
        "BotBlockDetected" => FrictionSignalKind.BotBlockDetected,
        "RateLimitDetected" => FrictionSignalKind.RateLimitDetected,
        "AccessDeniedDetected" => FrictionSignalKind.AccessDeniedDetected,
        "ServiceUnavailable" => FrictionSignalKind.ServiceUnavailable,
        "SuspiciousRedirect" => FrictionSignalKind.SuspiciousRedirect,
        _ => FrictionSignalKind.UnknownFriction
    };

    private static FrictionSignalSeverity ParseFrictionSignalSeverity(string? value) => value switch
    {
        "Info" => FrictionSignalSeverity.Info,
        "Warning" => FrictionSignalSeverity.Warning,
        "Critical" => FrictionSignalSeverity.Critical,
        "Fatal" => FrictionSignalSeverity.Fatal,
        _ => FrictionSignalSeverity.Critical
    };

    private static string? GetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private static bool ReadBool(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var prop)
            && prop.ValueKind is JsonValueKind.True or JsonValueKind.False
            && prop.GetBoolean();
    }

    private static DateTimeOffset? ParseDateTime(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            var str = prop.GetString();
            if (DateTimeOffset.TryParse(str, out var dt))
                return dt;
        }
        return null;
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.Array)
        {
            return prop.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() ?? "" : "")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }
        return [];
    }
}
