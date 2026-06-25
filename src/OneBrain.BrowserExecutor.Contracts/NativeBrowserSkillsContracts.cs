namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserSkillStatus
{
    Planned,
    DescriptorOnly,
    Disabled,
    Available
}

public enum BrowserSkillCapability
{
    CdpStateSnapshot,
    IndexedElements,
    SessionResilience,
    AccessFrictionDetection,
    HumanTakeover,
    NetworkEvidence,
    CdpOperationPlanning,
    StealthDescriptor,
    ProxyDescriptor,
    CaptchaDescriptor
}

public enum BrowserSessionSkillStatus
{
    Planned,
    Observed,
    Degraded,
    Blocked,
    NeedsHuman,
    Closed
}

public enum BrowserAccessFrictionType
{
    None,
    LoginWall,
    Captcha,
    RateLimit,
    PermissionPrompt,
    NetworkBlock,
    ProfileLock,
    Unknown
}

public enum BrowserSkillDescriptorMode
{
    NotAvailable,
    Planned,
    FutureDescriptorOnly,
    Disabled
}

public enum CaptchaHandlingMode
{
    DetectOnly,
    HumanTakeoverOnly,
    FutureReview,
    NotAvailable
}

public enum CdpOperationKind
{
    ReadState,
    NavigateCandidate,
    ClickCandidate,
    TypeCandidate,
    ScreenshotCandidate,
    NetworkMetadataCandidate,
    NoOp
}

public sealed record BrowserSkillManifest(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<BrowserSkillCapability> Capabilities,
    BrowserSkillStatus Status,
    string Version)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(Id, nameof(Id), errors);
        Require(Name, nameof(Name), errors);
        Require(Description, nameof(Description), errors);
        Require(Version, nameof(Version), errors);
        BrowserSafeIdentifierValidator.RequireSafe(Id, nameof(Id), errors);

        if (Capabilities.Count == 0)
            errors.Add("At least one browser skill capability is required.");

        if (BrowserCredentialRedactor.ContainsSecret(Name) ||
            BrowserCredentialRedactor.ContainsSecret(Description) ||
            BrowserCredentialRedactor.ContainsSecret(Version))
            errors.Add("Browser skill manifest contains secret-like content.");

        return Result(errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }

    private static ContractValidationResult Result(IReadOnlyList<string> errors) =>
        errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
}

public sealed record BrowserSkillCapabilityEnvelope(
    BrowserSkillManifest Manifest,
    bool BrowserActDependencyPresent,
    bool BrowserActReferenceOnly,
    bool RuntimeActive,
    IReadOnlyList<StealthProfile> StealthProfiles,
    IReadOnlyList<ProxyRouteProfile> ProxyRoutes,
    IReadOnlyList<CaptchaHandlingStrategy> CaptchaStrategies)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        errors.AddRange(Manifest.Validate().Errors);

        if (BrowserActDependencyPresent)
            errors.Add("BrowserAct dependency must not be present in native browser skills foundation.");

        if (!BrowserActReferenceOnly)
            errors.Add("BrowserAct must remain reference-only.");

        if (RuntimeActive)
            errors.Add("Native browser skills foundation is descriptor-only and cannot activate runtime.");

        foreach (var profile in StealthProfiles)
            errors.AddRange(profile.Validate().Errors);

        foreach (var route in ProxyRoutes)
            errors.AddRange(route.Validate().Errors);

        foreach (var strategy in CaptchaStrategies)
            errors.AddRange(strategy.Validate().Errors);

        return Result(errors);
    }

    private static ContractValidationResult Result(IReadOnlyList<string> errors) =>
        errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
}

public sealed record BrowserStateSnapshot(
    string Url,
    string Title,
    DateTimeOffset TimestampUtc,
    IReadOnlyList<BrowserIndexedElement> Elements)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (!Uri.TryCreate(Url, UriKind.Absolute, out _))
            errors.Add("Url must be absolute.");

        if (string.IsNullOrWhiteSpace(Title))
            errors.Add("Title is required.");

        if (TimestampUtc == default)
            errors.Add("TimestampUtc is required.");

        if (BrowserCredentialRedactor.ContainsSecret(Url) ||
            BrowserCredentialRedactor.ContainsSecret(Title))
            errors.Add("Browser state snapshot contains secret-like content.");

        foreach (var element in Elements)
            errors.AddRange(element.Validate().Errors);

        return Result(errors);
    }

    private static ContractValidationResult Result(IReadOnlyList<string> errors) =>
        errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
}

public sealed record BrowserIndexedElement(
    string ElementId,
    string Role,
    string Name,
    string Selector,
    string Text,
    string Hint)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(ElementId, nameof(ElementId), errors);
        Require(Role, nameof(Role), errors);
        Require(Selector, nameof(Selector), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ElementId, nameof(ElementId), errors);

        if (BrowserCredentialRedactor.ContainsSecret(Name) ||
            BrowserCredentialRedactor.ContainsSecret(Selector) ||
            BrowserCredentialRedactor.ContainsSecret(Text) ||
            BrowserCredentialRedactor.ContainsSecret(Hint))
            errors.Add("Browser indexed element contains secret-like content.");

        return Result(errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }

    private static ContractValidationResult Result(IReadOnlyList<string> errors) =>
        errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
}

public sealed record BrowserSkillSessionDescriptor(
    string SessionId,
    BrowserSessionSkillStatus Status,
    DateTimeOffset StartedAtUtc,
    BrowserSessionResilienceReport Resilience,
    IReadOnlyList<AccessFrictionEvent> FrictionEvents)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(SessionId))
            errors.Add("SessionId is required.");

        BrowserSafeIdentifierValidator.RequireSafe(SessionId, nameof(SessionId), errors);

        if (StartedAtUtc == default)
            errors.Add("StartedAtUtc is required.");

        errors.AddRange(Resilience.Validate().Errors);

        foreach (var frictionEvent in FrictionEvents)
            errors.AddRange(frictionEvent.Validate().Errors);

        return Result(errors);
    }

    private static ContractValidationResult Result(IReadOnlyList<string> errors) =>
        errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
}

public sealed record BrowserSessionResilienceReport(
    string ReportId,
    bool CanRetry,
    string Status,
    string LastErrorRedacted,
    string RecoverySuggestion)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(ReportId, nameof(ReportId), errors);
        Require(Status, nameof(Status), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ReportId, nameof(ReportId), errors);

        if (BrowserCredentialRedactor.ContainsSecret(LastErrorRedacted) ||
            BrowserCredentialRedactor.ContainsSecret(RecoverySuggestion))
            errors.Add("Browser session resilience report contains secret-like content.");

        return Result(errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }

    private static ContractValidationResult Result(IReadOnlyList<string> errors) =>
        errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
}

public sealed record AccessFrictionEvent(
    string EventId,
    BrowserAccessFrictionType FrictionType,
    string BlockedReason,
    bool HumanTakeoverNeeded,
    string RecoverySuggestion)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(EventId, nameof(EventId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(EventId, nameof(EventId), errors);

        if (FrictionType == BrowserAccessFrictionType.None && HumanTakeoverNeeded)
            errors.Add("Human takeover cannot be required for a no-friction event.");

        if (BrowserCredentialRedactor.ContainsSecret(BlockedReason) ||
            BrowserCredentialRedactor.ContainsSecret(RecoverySuggestion))
            errors.Add("Access friction event contains secret-like content.");

        return Result(errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }

    private static ContractValidationResult Result(IReadOnlyList<string> errors) =>
        errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
}

public sealed record BlockedFlowRecoveryPlan(
    string PlanId,
    string BlockedReason,
    bool HumanTakeoverNeeded,
    string RecoverySuggestion)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(PlanId, nameof(PlanId), errors);
        Require(BlockedReason, nameof(BlockedReason), errors);
        BrowserSafeIdentifierValidator.RequireSafe(PlanId, nameof(PlanId), errors);

        if (!HumanTakeoverNeeded && string.IsNullOrWhiteSpace(RecoverySuggestion))
            errors.Add("RecoverySuggestion is required when no human takeover is requested.");

        if (BrowserCredentialRedactor.ContainsSecret(BlockedReason) ||
            BrowserCredentialRedactor.ContainsSecret(RecoverySuggestion))
            errors.Add("Blocked flow recovery plan contains secret-like content.");

        return Result(errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }

    private static ContractValidationResult Result(IReadOnlyList<string> errors) =>
        errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
}

public sealed record HumanTakeoverRequest(
    string RequestId,
    string SessionId,
    BrowserAccessFrictionType Reason,
    string Message,
    DateTimeOffset RequestedAtUtc)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(RequestId, nameof(RequestId), errors);
        Require(SessionId, nameof(SessionId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(RequestId, nameof(RequestId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(SessionId, nameof(SessionId), errors);

        if (Reason == BrowserAccessFrictionType.None)
            errors.Add("Human takeover request requires a friction reason.");

        if (RequestedAtUtc == default)
            errors.Add("RequestedAtUtc is required.");

        if (BrowserCredentialRedactor.ContainsSecret(Message))
            errors.Add("Human takeover request contains secret-like content.");

        return Result(errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }

    private static ContractValidationResult Result(IReadOnlyList<string> errors) =>
        errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
}

public sealed record NetworkEvidenceCandidate(
    string CandidateId,
    string Method,
    string Url,
    int StatusCode,
    bool MetadataOnly,
    bool Redacted)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(CandidateId, nameof(CandidateId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(CandidateId, nameof(CandidateId), errors);

        if (!Uri.TryCreate(Url, UriKind.Absolute, out _))
            errors.Add("Url must be absolute.");

        if (!MetadataOnly)
            errors.Add("Network evidence candidate must be metadata-only.");

        if (!Redacted)
            errors.Add("Network evidence candidate must be redacted.");

        if (BrowserCredentialRedactor.ContainsSecret(Url) ||
            BrowserCredentialRedactor.ContainsSecret(Method))
            errors.Add("Network evidence candidate contains secret-like content.");

        return Result(errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }

    private static ContractValidationResult Result(IReadOnlyList<string> errors) =>
        errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
}

public sealed record CdpOperationCandidate(
    string OperationId,
    CdpOperationKind Kind,
    string TargetSelector,
    bool RuntimeExecutable,
    string Notes)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(OperationId, nameof(OperationId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(OperationId, nameof(OperationId), errors);

        if (RuntimeExecutable)
            errors.Add("CDP operation candidate is descriptive only and cannot be runtime executable.");

        if (BrowserCredentialRedactor.ContainsSecret(TargetSelector) ||
            BrowserCredentialRedactor.ContainsSecret(Notes))
            errors.Add("CDP operation candidate contains secret-like content.");

        return Result(errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }

    private static ContractValidationResult Result(IReadOnlyList<string> errors) =>
        errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
}

public sealed record StealthProfile(
    bool Available,
    BrowserSkillDescriptorMode Mode,
    string Reason,
    string Provider,
    string Notes)
{
    public ContractValidationResult Validate() =>
        BrowserSkillDescriptorValidator.ValidateDescriptor(nameof(StealthProfile), Available, Mode, Reason, Provider, Notes);
}

public sealed record ProxyRouteProfile(
    bool Available,
    BrowserSkillDescriptorMode Mode,
    string Reason,
    string Provider,
    string Notes)
{
    public ContractValidationResult Validate() =>
        BrowserSkillDescriptorValidator.ValidateDescriptor(nameof(ProxyRouteProfile), Available, Mode, Reason, Provider, Notes);
}

public sealed record CaptchaChallengeEvent(
    string ChallengeId,
    string Url,
    CaptchaHandlingMode HandlingMode,
    bool AutoSolveAllowed,
    string Notes)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(ChallengeId, nameof(ChallengeId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(ChallengeId, nameof(ChallengeId), errors);

        if (!Uri.TryCreate(Url, UriKind.Absolute, out _))
            errors.Add("Url must be absolute.");

        if (AutoSolveAllowed)
            errors.Add("Captcha auto-solving is not part of native browser skills foundation.");

        if (BrowserCredentialRedactor.ContainsSecret(Url) ||
            BrowserCredentialRedactor.ContainsSecret(Notes))
            errors.Add("Captcha challenge event contains secret-like content.");

        return Result(errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }

    private static ContractValidationResult Result(IReadOnlyList<string> errors) =>
        errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
}

public sealed record CaptchaHandlingStrategy(
    string StrategyId,
    CaptchaHandlingMode Mode,
    bool AutoSolveAllowed,
    string Notes)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        Require(StrategyId, nameof(StrategyId), errors);
        BrowserSafeIdentifierValidator.RequireSafe(StrategyId, nameof(StrategyId), errors);

        if (AutoSolveAllowed)
            errors.Add("Captcha auto-solving is not part of native browser skills foundation.");

        if (Mode == CaptchaHandlingMode.NotAvailable && AutoSolveAllowed)
            errors.Add("Unavailable captcha strategy cannot allow auto-solving.");

        if (BrowserCredentialRedactor.ContainsSecret(Notes))
            errors.Add("Captcha handling strategy contains secret-like content.");

        return Result(errors);
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{name} is required.");
    }

    private static ContractValidationResult Result(IReadOnlyList<string> errors) =>
        errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
}

internal static class BrowserSkillDescriptorValidator
{
    public static ContractValidationResult ValidateDescriptor(
        string descriptorName,
        bool available,
        BrowserSkillDescriptorMode mode,
        string reason,
        string provider,
        string notes)
    {
        var errors = new List<string>();

        if (available)
            errors.Add($"{descriptorName} cannot be available in native browser skills foundation.");

        if (mode != BrowserSkillDescriptorMode.FutureDescriptorOnly && mode != BrowserSkillDescriptorMode.Disabled)
            errors.Add($"{descriptorName} must be future descriptor only or disabled.");

        if (string.IsNullOrWhiteSpace(reason))
            errors.Add($"{descriptorName} reason is required.");

        if (BrowserCredentialRedactor.ContainsSecret(reason) ||
            BrowserCredentialRedactor.ContainsSecret(provider) ||
            BrowserCredentialRedactor.ContainsSecret(notes))
            errors.Add($"{descriptorName} contains secret-like content.");

        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}
