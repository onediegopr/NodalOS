using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserTargetSource { Unknown, Cdp, Playwright, ExtensionRelay, Fixture }
public enum BrowserActionType { Navigate, Click, TypeText, SelectOption, Read, WaitFor, Scroll, Extract, NoOp }
public enum BrowserRiskClass { ReadOnly, Low, Medium, High, Critical }
public enum BrowserVerificationStatus { Verified, Failed, Uncertain, Skipped }
public enum BrowserEvidenceType { CdpEvent, DomSnapshot, AccessibilitySnapshot, TextExtract, NavigationEvent, InputEvent, VerificationResult, Error, ScreenshotRegional, Log }
public enum BrowserSensitivityLevel { None, Low, Medium, High, Secret }
public enum BrowserHeartbeatStatus { Alive, Stale, Detached, Destroyed, Disconnected, Unknown }
public enum BrowserReplayStatus { New, InFlight, Completed, RejectedDuplicate, Expired, Failed }
public enum BrowserExecutorKind { Cdp, Playwright, ExtensionRelay, Hybrid }
public enum BrowserLaunchMode { AttachExisting, LaunchTemporaryProfile, LaunchPersistentManagedProfile, LaunchRealUserProfileWithConsent }
public enum BrowserProfileMode { Temporary, ManagedPersistent, RealUserProfile }
public enum BrowserCleanupPolicy { Required, CloseManagedProcess, KeepUserProfileProcess, Manual }
public enum BrowserPortSecurityPolicy { LocalhostOnly, ExplicitRemoteAllowed }
public enum BrowserSensitiveDataLoggingPolicy { RedactByDefault, DisabledForTestOnly }

public sealed record ContractValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static ContractValidationResult Valid { get; } = new(true, []);

    public static ContractValidationResult From(params string[] errors) =>
        errors.Length == 0 ? Valid : new ContractValidationResult(false, errors);
}

public sealed record BrowserTargetContext(
    string RunId,
    string BrowserId,
    string BrowserSessionId,
    string? BrowserContextId,
    string? WindowId,
    string TargetId,
    string? PageId,
    string? TabId,
    string FrameId,
    string? ParentFrameId,
    Uri Url,
    string Title,
    long Generation,
    string LivenessToken,
    DateTimeOffset ObservedAtUtc,
    bool? IsActive,
    bool? IsVisible,
    bool? IsUserFacing,
    string? ReadyState,
    BrowserTargetSource Source)
{
    public static string CreateLivenessToken(string targetId, string frameId, long generation)
    {
        var payload = $"{targetId}|{frameId}|{generation}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool IsStaleComparedTo(BrowserTargetContext current) =>
        TargetId != current.TargetId ||
        FrameId != current.FrameId ||
        Generation != current.Generation ||
        LivenessToken != current.LivenessToken;

    public string StableHash()
    {
        var payload = $"{BrowserSessionId}|{TargetId}|{FrameId}|{Generation}|{Url}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(RunId, "RunId", errors);
        Require(BrowserId, "BrowserId", errors);
        Require(BrowserSessionId, "BrowserSessionId", errors);
        Require(TargetId, "TargetId", errors);
        Require(FrameId, "FrameId", errors);
        Require(LivenessToken, "LivenessToken", errors);

        if (Generation < 0)
            errors.Add("Generation must be zero or positive.");

        var expected = CreateLivenessToken(TargetId, FrameId, Generation);
        if (!string.IsNullOrWhiteSpace(TargetId) &&
            !string.IsNullOrWhiteSpace(FrameId) &&
            !string.Equals(expected, LivenessToken, StringComparison.Ordinal))
            errors.Add("LivenessToken must be derived from TargetId + FrameId + Generation.");

        if (SecretRedactor.ContainsSecret(Url.ToString()) || SecretRedactor.ContainsSecret(Title))
            errors.Add("TargetContext must not contain secrets.");

        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }
}

public sealed record BrowserObservation(
    string ObservationId,
    string RunId,
    BrowserTargetContext TargetContext,
    DateTimeOffset ObservedAtUtc,
    Uri Url,
    string Title,
    string ReadyState,
    int FrameCount,
    string MainFrameId,
    string VisibleTextSummary,
    IReadOnlyList<ActionableElement> ActionableElements,
    IReadOnlyList<FormSummary> Forms,
    IReadOnlyList<LinkSummary> Links,
    IReadOnlyList<string> Warnings,
    bool PayloadLimitApplied,
    bool SensitivityRedactionApplied,
    IReadOnlyList<string> EvidenceRefs)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(ObservationId, "ObservationId", errors);
        Require(RunId, "RunId", errors);
        Require(MainFrameId, "MainFrameId", errors);

        errors.AddRange(TargetContext.Validate().Errors);

        if (FrameCount < 1)
            errors.Add("FrameCount must be at least one.");

        if (VisibleTextSummary.Length > BrowserPayloadLimits.VisibleTextSummaryMaxChars)
            errors.Add("VisibleTextSummary exceeds payload limit.");

        if (ActionableElements.Count > BrowserPayloadLimits.ActionableElementsMax)
            errors.Add("ActionableElements exceeds payload limit.");

        if (SecretRedactor.ContainsSecret(VisibleTextSummary) ||
            ActionableElements.Any(element => element.ContainsSecret()))
            errors.Add("Observation must not contain unredacted secrets.");

        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }

    public BrowserObservation Redact() => this with
    {
        Title = SecretRedactor.Redact(Title),
        VisibleTextSummary = SecretRedactor.Redact(VisibleTextSummary),
        ActionableElements = ActionableElements.Select(element => element.Redact()).ToList(),
        Forms = Forms.Select(form => form.Redact()).ToList(),
        Links = Links.Select(link => link.Redact()).ToList(),
        SensitivityRedactionApplied = true
    };

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }
}

public static class BrowserPayloadLimits
{
    public const int VisibleTextSummaryMaxChars = 4000;
    public const int ActionableElementsMax = 200;
    public const int FormsMax = 40;
    public const int LinksMax = 200;
}

public sealed record ActionableElement(
    string ElementId,
    string FrameId,
    string Role,
    string TagName,
    string Text,
    string AccessibleName,
    string Label,
    IReadOnlyList<string> SelectorCandidates,
    ElementBoundingBox? BoundingBox,
    bool IsVisible,
    bool IsEnabled,
    IReadOnlyList<string> RiskHints,
    double Confidence)
{
    public bool ContainsSecret() =>
        SecretRedactor.ContainsSecret(Text) ||
        SecretRedactor.ContainsSecret(AccessibleName) ||
        SecretRedactor.ContainsSecret(Label) ||
        SelectorCandidates.Any(SecretRedactor.ContainsSecret);

    public ActionableElement Redact() => this with
    {
        Text = SecretRedactor.Redact(Text),
        AccessibleName = SecretRedactor.Redact(AccessibleName),
        Label = SecretRedactor.Redact(Label),
        SelectorCandidates = SelectorCandidates.Select(SecretRedactor.Redact).ToList()
    };
}

public sealed record ElementBoundingBox(double X, double Y, double Width, double Height);

public sealed record FormSummary(string FormId, string FrameId, int InputCount, string Label, IReadOnlyList<string> RiskHints)
{
    public FormSummary Redact() => this with { Label = SecretRedactor.Redact(Label) };
}

public sealed record LinkSummary(string LinkId, string FrameId, Uri? Href, string Text)
{
    public LinkSummary Redact() => this with { Text = SecretRedactor.Redact(Text) };
}

public sealed record BrowserAction(
    string ActionId,
    string IdempotencyKey,
    string RunId,
    string StepId,
    BrowserTargetContext TargetContext,
    string FrameId,
    BrowserActionType ActionType,
    BrowserActionTarget Target,
    BrowserActionInput? Input,
    BrowserExpectedOutcome ExpectedOutcome,
    BrowserRiskClass RiskClass,
    int TimeoutMs,
    bool RequiresApproval,
    DateTimeOffset CreatedAtUtc)
{
    public bool IsReadOnly => ActionType is BrowserActionType.Read or BrowserActionType.Extract or BrowserActionType.WaitFor or BrowserActionType.NoOp;
    public bool CanModifyState => ActionType is BrowserActionType.Navigate or BrowserActionType.Click or BrowserActionType.TypeText or BrowserActionType.SelectOption or BrowserActionType.Scroll;

    public string Fingerprint()
    {
        var payload = $"{RunId}|{StepId}|{TargetContext.StableHash()}|{FrameId}|{ActionType}|{Target}|{Input}|{ExpectedOutcome}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    public ContractValidationResult Validate(BrowserTargetContext? currentTarget = null)
    {
        var errors = new List<string>();
        Require(ActionId, "ActionId", errors);
        Require(RunId, "RunId", errors);
        Require(StepId, "StepId", errors);
        Require(FrameId, "FrameId", errors);
        errors.AddRange(TargetContext.Validate().Errors);

        if (CanModifyState && string.IsNullOrWhiteSpace(IdempotencyKey))
            errors.Add("Modifying actions require IdempotencyKey.");

        if (RiskClass == BrowserRiskClass.Critical && !RequiresApproval)
            errors.Add("Critical actions require approval.");

        if (RiskClass == BrowserRiskClass.ReadOnly && !IsReadOnly)
            errors.Add("ReadOnly risk class cannot be used for modifying actions.");

        if (IsReadOnly && Input is { HasModifyingValue: true })
            errors.Add("ReadOnly actions cannot carry modifying input.");

        if (TimeoutMs <= 0)
            errors.Add("TimeoutMs is required.");

        if (string.IsNullOrWhiteSpace(ExpectedOutcome.Description))
            errors.Add("ExpectedOutcome is required.");

        if (currentTarget is not null && TargetContext.IsStaleComparedTo(currentTarget))
            errors.Add("TargetContext is stale.");

        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }
}

public sealed record BrowserActionTarget(string CandidateId, string? Selector, string? Text, string? Url);
public sealed record BrowserActionInput(string? Text, string? Value, bool HasModifyingValue);
public sealed record BrowserExpectedOutcome(string Description, string? UrlContains, string? TextContains, string? ElementCandidateId);

public sealed record BrowserVerification(
    string VerificationId,
    string RunId,
    string StepId,
    string? ActionId,
    BrowserTargetContext TargetContext,
    BrowserExpectedOutcome ExpectedOutcome,
    string? PreObservationId,
    string? PostObservationId,
    BrowserVerificationStatus Status,
    double Confidence,
    IReadOnlyList<string> EvidenceRefs,
    string? FailureReason,
    DateTimeOffset VerifiedAtUtc,
    IReadOnlyList<string>? ProofRefs = null)
{
    public IReadOnlyList<string> ProofReferences => ProofRefs ?? [];
    public bool HasSemanticProof => ProofReferences.Count > 0;

    public bool AllowsStepDone(bool allowSkippedByPolicy = false) =>
        (Status == BrowserVerificationStatus.Verified && HasSemanticProof) ||
        (allowSkippedByPolicy && Status == BrowserVerificationStatus.Skipped);

    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(VerificationId, "VerificationId", errors);
        Require(RunId, "RunId", errors);
        Require(StepId, "StepId", errors);
        errors.AddRange(TargetContext.Validate().Errors);

        if (Status == BrowserVerificationStatus.Failed && string.IsNullOrWhiteSpace(FailureReason))
            errors.Add("Failed verification requires FailureReason.");

        if (Status == BrowserVerificationStatus.Verified && !HasSemanticProof)
            errors.Add("Verified status requires semantic proof refs.");

        if (EvidenceRefs.Count == 0 && ProofReferences.Count == 0 && string.IsNullOrWhiteSpace(FailureReason))
            errors.Add("Verification requires evidence refs, proof refs, or a failure reason.");

        if (Confidence is < 0 or > 1)
            errors.Add("Confidence must be between 0 and 1.");

        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }
}

public sealed record BrowserEvidence(
    string EvidenceId,
    string RunId,
    string? StepId,
    string? ActionId,
    string? VerificationId,
    BrowserTargetContext? TargetContext,
    BrowserEvidenceType EvidenceType,
    DateTimeOffset CreatedAtUtc,
    string Summary,
    string? PayloadRef,
    string? InlinePayload,
    bool RedactionApplied,
    BrowserSensitivityLevel SensitivityLevel)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(EvidenceId, "EvidenceId", errors);
        Require(RunId, "RunId", errors);
        Require(Summary, "Summary", errors);

        if (TargetContext is not null)
            errors.AddRange(TargetContext.Validate().Errors);

        if (SensitivityLevel == BrowserSensitivityLevel.Secret && !RedactionApplied)
            errors.Add("Secret evidence requires redaction.");

        if (InlinePayload is not null && SecretRedactor.ContainsSecret(InlinePayload))
            errors.Add("InlinePayload contains unredacted secret-like content.");

        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }
}

public sealed record BrowserHeartbeat(
    string HeartbeatId,
    string BrowserSessionId,
    string? TargetId,
    string? FrameId,
    long? Generation,
    DateTimeOffset ObservedAtUtc,
    int? RoundTripMs,
    BrowserHeartbeatStatus Status,
    string? Reason)
{
    public bool IsStrongAlive => Status == BrowserHeartbeatStatus.Alive && RoundTripMs is >= 0;

    public static BrowserHeartbeat FromTargetComparison(
        string heartbeatId,
        string browserSessionId,
        BrowserTargetContext expected,
        BrowserTargetContext? observed,
        DateTimeOffset observedAtUtc,
        int? roundTripMs)
    {
        if (observed is null)
            return new(heartbeatId, browserSessionId, expected.TargetId, expected.FrameId, expected.Generation, observedAtUtc, roundTripMs, BrowserHeartbeatStatus.Unknown, "no observed target");

        if (expected.TargetId != observed.TargetId)
            return new(heartbeatId, browserSessionId, expected.TargetId, expected.FrameId, expected.Generation, observedAtUtc, roundTripMs, BrowserHeartbeatStatus.Destroyed, "target changed or destroyed");

        if (expected.FrameId != observed.FrameId)
            return new(heartbeatId, browserSessionId, expected.TargetId, expected.FrameId, expected.Generation, observedAtUtc, roundTripMs, BrowserHeartbeatStatus.Detached, "frame changed or detached");

        if (expected.Generation != observed.Generation)
            return new(heartbeatId, browserSessionId, expected.TargetId, expected.FrameId, expected.Generation, observedAtUtc, roundTripMs, BrowserHeartbeatStatus.Stale, "generation mismatch");

        return new(heartbeatId, browserSessionId, observed.TargetId, observed.FrameId, observed.Generation, observedAtUtc, roundTripMs, BrowserHeartbeatStatus.Alive, null);
    }
}

public sealed record BrowserIdempotencyRecord(
    string ActionId,
    string IdempotencyKey,
    string RunId,
    string StepId,
    string TargetContextHash,
    string ActionFingerprint,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    BrowserReplayStatus Status);

public sealed record BrowserIdempotencyDecision(bool Allowed, BrowserReplayStatus Status, string Reason, BrowserIdempotencyRecord? ExistingRecord);

public sealed class BrowserIdempotencyLedger
{
    private readonly Dictionary<string, BrowserIdempotencyRecord> _records = new(StringComparer.Ordinal);

    public BrowserIdempotencyDecision TryBegin(BrowserAction action, DateTimeOffset now)
    {
        var validation = action.Validate();
        if (!validation.IsValid)
            return new(false, BrowserReplayStatus.Failed, string.Join("; ", validation.Errors), null);

        if (_records.TryGetValue(action.IdempotencyKey, out var existing))
        {
            if (existing.ExpiresAtUtc is not null && existing.ExpiresAtUtc <= now)
                return new(true, BrowserReplayStatus.Expired, "expired duplicate requires explicit policy before re-execution", existing);

            if (!string.Equals(existing.ActionFingerprint, action.Fingerprint(), StringComparison.Ordinal))
                return new(false, BrowserReplayStatus.Failed, "same idempotency key with different fingerprint", existing);

            return new(false, existing.Status == BrowserReplayStatus.Completed ? BrowserReplayStatus.Completed : BrowserReplayStatus.RejectedDuplicate, "duplicate action", existing);
        }

        var record = new BrowserIdempotencyRecord(
            action.ActionId,
            action.IdempotencyKey,
            action.RunId,
            action.StepId,
            action.TargetContext.StableHash(),
            action.Fingerprint(),
            now,
            now.AddMinutes(10),
            BrowserReplayStatus.InFlight);
        _records[action.IdempotencyKey] = record;
        return new(true, BrowserReplayStatus.New, "new action", record);
    }

    public void Complete(string idempotencyKey)
    {
        if (_records.TryGetValue(idempotencyKey, out var existing))
            _records[idempotencyKey] = existing with { Status = BrowserReplayStatus.Completed };
    }
}

public sealed record BrowserExecutorCapabilities(
    string ExecutorId,
    BrowserExecutorKind ExecutorKind,
    IReadOnlySet<BrowserActionType> Capabilities,
    BrowserRiskClass RiskLimit,
    bool SupportsTrustedInput,
    bool SupportsDomSnapshot,
    bool SupportsAccessibilitySnapshot,
    bool SupportsScreenshots,
    bool SupportsFrames,
    bool SupportsDownloads,
    bool SupportsFileUpload,
    bool RequiresBrowserLaunch,
    bool RequiresRemoteDebugging,
    bool CanAttachExistingBrowser,
    bool CanUsePersistentProfile,
    bool CanUseRealUserProfile)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(ExecutorId))
            errors.Add("ExecutorId is required.");
        if (Capabilities.Count == 0)
            errors.Add("Capabilities are required.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }

    public bool CanExecute(BrowserAction action)
    {
        if (!Capabilities.Contains(action.ActionType))
            return false;

        if (action.RiskClass > RiskLimit)
            return false;

        if (!SupportsTrustedInput && action.ActionType is BrowserActionType.Click or BrowserActionType.TypeText or BrowserActionType.SelectOption)
            return false;

        return true;
    }
}

public sealed record ChromeLauncherPolicy(
    BrowserLaunchMode LaunchMode,
    string CdpHost,
    int? CdpPort,
    bool BindLocalhostOnly,
    BrowserProfileMode ProfileMode,
    string? UserDataDir,
    bool LoadExtension,
    bool AllowAttachExisting,
    bool RequireExplicitUserConsentForRealProfile,
    BrowserCleanupPolicy CleanupPolicy,
    BrowserPortSecurityPolicy PortSecurityPolicy,
    BrowserSensitiveDataLoggingPolicy SensitiveDataLoggingPolicy)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();

        if (BindLocalhostOnly && CdpHost is not "127.0.0.1" and not "localhost" and not "::1")
            errors.Add("CDP must bind to localhost by default.");

        if (PortSecurityPolicy == BrowserPortSecurityPolicy.LocalhostOnly && CdpHost is not "127.0.0.1" and not "localhost" and not "::1")
            errors.Add("Remote CDP host is blocked by default.");

        if (ProfileMode == BrowserProfileMode.RealUserProfile && !RequireExplicitUserConsentForRealProfile)
            errors.Add("Real user profile requires explicit consent.");

        if (LaunchMode == BrowserLaunchMode.AttachExisting && CdpPort is null)
            errors.Add("AttachExisting requires CdpPort.");

        if (SensitiveDataLoggingPolicy != BrowserSensitiveDataLoggingPolicy.RedactByDefault)
            errors.Add("Sensitive data logging must redact by default.");

        if (CleanupPolicy == BrowserCleanupPolicy.Manual && LaunchMode != BrowserLaunchMode.AttachExisting)
            errors.Add("Managed launches require cleanup policy.");

        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public static partial class SecretRedactor
{
    public const string Redacted = "[REDACTED]";

    public static string Redact(string value) =>
        SecretPattern().Replace(value, Redacted);

    public static bool ContainsSecret(string value) =>
        !string.IsNullOrEmpty(value) && SecretPattern().IsMatch(value);

    [GeneratedRegex("(sk-[A-Za-z0-9_\\-]{12,}|bearer\\s+[A-Za-z0-9._\\-]+|authorization\\s*[:=]|cookie\\s*[:=]|password\\s*[:=]|refresh_token\\s*[:=]|access_token\\s*[:=]|api[_-]?key\\s*[:=])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SecretPattern();
}
