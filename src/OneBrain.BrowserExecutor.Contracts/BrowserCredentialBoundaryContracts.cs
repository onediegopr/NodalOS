using System.Text;
using System.Text.RegularExpressions;

namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserCredentialSignalKind
{
    LoginFormDetected,
    PasswordFieldDetected,
    OtpFieldDetected,
    TwoFactorPromptDetected,
    CaptchaDetected,
    TokenPromptDetected,
    ClaveFiscalDetected,
    CredentialSubmitDetected,
    PaymentActionDetected,
    SensitiveAccountAreaDetected,
    PrivateDataDetected,
    UnknownSensitivePrompt
}

public enum BrowserCredentialRisk { None, Low, Medium, High, Critical, Unknown }

public enum BrowserCredentialBoundaryDecisionKind
{
    AllowReadOnly,
    Block,
    RequiresHuman,
    RequiresApproval,
    RedactAndContinue,
    FailClosed
}

public enum BrowserSensitiveInputPolicy
{
    AllowReadOnlyOnly,
    RequireHumanForCredentials,
    BlockSensitiveSubmit,
    FailClosedOnUnknownSensitive
}

public enum BrowserHumanHandoffReason
{
    LoginRequired,
    PasswordRequired,
    TwoFactorRequired,
    CaptchaRequired,
    ClaveFiscalRequired,
    SensitiveSubmitRequired,
    FinancialActionRequired,
    UserConfirmationRequired,
    AutomationBlocked,
    UnknownSensitivePrompt
}

public enum BrowserHumanHandoffStatus { Created, WaitingForUser, UserCompleted, UserCancelled, Expired, Failed, Resumed }

public sealed record BrowserCredentialSignal(
    BrowserCredentialSignalKind Kind,
    BrowserCredentialRisk Risk,
    string FrameId,
    string ElementId,
    string Evidence,
    string Reason)
{
    public BrowserCredentialSignal Redact() => this with
    {
        Evidence = BrowserCredentialRedactor.Redact(Evidence),
        Reason = BrowserCredentialRedactor.Redact(Reason)
    };
}

public sealed record BrowserCredentialBoundary(
    string BoundaryId,
    string RunId,
    BrowserTargetContext TargetContext,
    DateTimeOffset DetectedAtUtc,
    IReadOnlyList<BrowserCredentialSignal> Signals,
    bool RedactionApplied,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ProofRefs)
{
    public bool HasStrongSignal => Signals.Any(signal => signal.Risk is BrowserCredentialRisk.High or BrowserCredentialRisk.Critical);
    public bool HasUnknownSensitiveSignal => Signals.Any(signal => signal.Kind == BrowserCredentialSignalKind.UnknownSensitivePrompt);

    public BrowserCredentialBoundary Redact() => this with
    {
        Signals = Signals.Select(signal => signal.Redact()).ToList(),
        RedactionApplied = true
    };

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(BoundaryId))
            errors.Add("BoundaryId is required.");
        if (string.IsNullOrWhiteSpace(RunId))
            errors.Add("RunId is required.");
        errors.AddRange(TargetContext.Validate().Errors);
        if (Signals.Count == 0)
            errors.Add("Credential boundary requires at least one signal.");
        if (ProofRefs.Count == 0)
            errors.Add("Credential boundary requires proof refs.");
        if (Signals.Any(signal => BrowserCredentialRedactor.ContainsSecret(signal.Evidence) || BrowserCredentialRedactor.ContainsSecret(signal.Reason)))
            errors.Add("Credential boundary contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserCredentialBoundaryDecision(
    BrowserCredentialBoundaryDecisionKind Decision,
    BrowserCredentialRisk Risk,
    BrowserHumanHandoffReason? HandoffReason,
    string Message,
    BrowserCredentialBoundary Boundary)
{
    public bool BlocksAutomation => Decision is BrowserCredentialBoundaryDecisionKind.Block or BrowserCredentialBoundaryDecisionKind.FailClosed;
    public bool RequiresHuman => Decision == BrowserCredentialBoundaryDecisionKind.RequiresHuman;
    public bool RequiresApproval => Decision == BrowserCredentialBoundaryDecisionKind.RequiresApproval;
}

public sealed record BrowserHumanHandoffContext(
    string RunId,
    string ActionId,
    string CorrelationId,
    string ProfileId,
    string SessionId,
    BrowserTargetContext TargetContext,
    string FrameId,
    string RedactedUrl,
    string RedactedTitle,
    bool RedactionApplied);

public sealed record BrowserHumanHandoffResumeToken(
    string TokenId,
    string RequestId,
    string CorrelationId,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc)
{
    public bool IsExpired(DateTimeOffset now) => ExpiresAtUtc <= now;
}

public sealed record BrowserHumanHandoffInstruction(
    string Message,
    IReadOnlyList<string> AllowedUserActions,
    IReadOnlyList<string> ResumeConditions);

public sealed record BrowserHumanHandoffRequest(
    string RequestId,
    BrowserHumanHandoffStatus Status,
    BrowserHumanHandoffReason Reason,
    BrowserHumanHandoffContext Context,
    BrowserHumanHandoffResumeToken ResumeToken,
    BrowserHumanHandoffInstruction Instruction,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool RedactionApplied,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ProofRefs)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(RequestId))
            errors.Add("RequestId is required.");
        if (Context.TargetContext.IsStaleComparedTo(Context.TargetContext with { Generation = Context.TargetContext.Generation }))
            errors.Add("Invalid target context.");
        if (string.IsNullOrWhiteSpace(Context.RunId))
            errors.Add("RunId is required.");
        if (string.IsNullOrWhiteSpace(Context.ActionId))
            errors.Add("ActionId is required.");
        if (string.IsNullOrWhiteSpace(Context.CorrelationId))
            errors.Add("CorrelationId is required.");
        if (string.IsNullOrWhiteSpace(Context.SessionId))
            errors.Add("SessionId is required.");
        if (!RedactionApplied || !Context.RedactionApplied)
            errors.Add("Human handoff request requires redaction.");
        if (ProofRefs.Count == 0)
            errors.Add("Human handoff request requires proof refs.");
        if (BrowserCredentialRedactor.ContainsSecret(Instruction.Message) ||
            Instruction.AllowedUserActions.Any(BrowserCredentialRedactor.ContainsSecret) ||
            Instruction.ResumeConditions.Any(BrowserCredentialRedactor.ContainsSecret) ||
            BrowserCredentialRedactor.ContainsSecret(Context.RedactedUrl) ||
            BrowserCredentialRedactor.ContainsSecret(Context.RedactedTitle))
            errors.Add("Human handoff request contains unredacted secret-like content.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserHumanHandoffResumeDecision(
    bool CanResume,
    bool Success,
    BrowserHumanHandoffStatus Status,
    string Reason,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ProofRefs);

public static partial class BrowserCredentialRedactor
{
    public const string Redacted = "[REDACTED]";

    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        var redacted = BearerTokenPattern().Replace(value, match => $"{match.Groups[1].Value}{Redacted}");
        redacted = SecretLikePattern().Replace(redacted, match =>
        {
            var text = match.Value;
            var separatorIndex = text.IndexOfAny(['=', ':']);
            return separatorIndex >= 0 ? text[..(separatorIndex + 1)] + Redacted : Redacted;
        });
        redacted = SecretRedactor.Redact(redacted);
        redacted = JwtValuePattern().Replace(redacted, match => IsPlausibleJwt(match.Value) ? Redacted : match.Value);
        redacted = FormattedCuitPattern().Replace(redacted, Redacted);
        return ContextualIdentityPattern().Replace(redacted, match => $"{match.Groups[1].Value}{Redacted}");
    }

    public static bool ContainsSecret(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        var normalized = RedactedSecretPattern().Replace(value, "");
        return SecretRedactor.ContainsSecret(normalized) ||
            SecretLikePattern().IsMatch(normalized) ||
            BearerTokenPattern().IsMatch(normalized) ||
            JwtValuePattern().Matches(normalized).Any(match => IsPlausibleJwt(match.Value)) ||
            FormattedCuitPattern().IsMatch(normalized) ||
            ContextualIdentityPattern().IsMatch(normalized);
    }

    [GeneratedRegex("(password|passwd|pass|secret|token|access_token|refresh_token|id_token|api[_-]?key|cookie|set-cookie|authorization|bearer|otp|code|clave(?:\\s+fiscal)?|cuit|dni|sessionid|csrf|xsrf|jwt|client_secret)\\s*[:=]\\s*[^\\s;]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SecretLikePattern();

    [GeneratedRegex("\\b(\\d{2}-\\d{8}-\\d)\\b", RegexOptions.CultureInvariant)]
    private static partial Regex FormattedCuitPattern();

    [GeneratedRegex("\\b((?:dni|documento|identity|cuit|cuil|taxid)\\s*[:=]?\\s*)(\\d{7,8}|\\d{2}-\\d{8}-\\d)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ContextualIdentityPattern();

    [GeneratedRegex("\\b(Bearer\\s+)[A-Za-z0-9._-]{8,}\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BearerTokenPattern();

    [GeneratedRegex("\\b[A-Za-z0-9_-]{8,}\\.[A-Za-z0-9_-]{8,}\\.[A-Za-z0-9_-]{8,}\\b", RegexOptions.CultureInvariant)]
    private static partial Regex JwtValuePattern();

    [GeneratedRegex("(password|passwd|pass|secret|token|access_token|refresh_token|id_token|api[_-]?key|cookie|set-cookie|authorization|bearer|otp|code|clave(?:\\s+fiscal)?|cuit|dni|sessionid|csrf|xsrf|jwt|client_secret)\\s*[:=]\\s*\\[REDACTED\\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RedactedSecretPattern();

    private static bool IsPlausibleJwt(string value)
    {
        var parts = value.Split('.');
        if (parts.Length != 3 || parts.Any(part => part.Length < 8))
            return false;

        try
        {
            var headerJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
            return headerJson.Contains("\"alg\"", StringComparison.OrdinalIgnoreCase) ||
                headerJson.Contains("\"typ\"", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}
