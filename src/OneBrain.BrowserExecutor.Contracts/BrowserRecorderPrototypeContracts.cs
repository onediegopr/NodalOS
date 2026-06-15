namespace OneBrain.BrowserExecutor.Contracts;

public enum BrowserRecorderRiskAssessment
{
    ReadOnly,
    RequiresHuman,
    Risky,
    Blocked
}

public sealed record BrowserRecorderSession(string SessionId, string RunId, DateTimeOffset StartedAtUtc, bool ReadOnly, bool Redacted);

public sealed record BrowserRecorderStartRequest(string RunId, Uri StartUri, IReadOnlySet<string> AllowlistedHosts);

public sealed record BrowserRecorderStopRequest(string SessionId, DateTimeOffset StoppedAtUtc);

public sealed record BrowserRecorderTargetDescriptor(string SemanticLabel, string SafeSelector, string SafeUrl, string Host);

public sealed record BrowserRecorderVerificationCandidate(string CandidateId, string Description, bool Required);

public sealed record BrowserRecorderObservation(
    Uri Url,
    string Title,
    string VisibleText,
    IReadOnlyList<string> SemanticTargets,
    bool HasForm,
    bool HasSubmit,
    bool HasDownloadLink,
    string? RawCookie,
    string? RawBody,
    string? FullLocalPath);

public sealed record BrowserRecorderCapturedStep(
    string StepId,
    BrowserRecordedActionKind ActionKind,
    BrowserRecorderTargetDescriptor Target,
    BrowserRecorderVerificationCandidate VerificationCandidate,
    BrowserRecorderRiskAssessment Risk,
    IReadOnlyList<string> Preconditions,
    IReadOnlyList<string> EvidenceRefs,
    bool Executable,
    bool StoresSecret,
    bool StoresCookie,
    bool StoresBody)
{
    public bool IsSafeReadOnly =>
        !Executable &&
        !StoresSecret &&
        !StoresCookie &&
        !StoresBody &&
        VerificationCandidate.Required &&
        Risk == BrowserRecorderRiskAssessment.ReadOnly;
}

public sealed record BrowserRecorderDraftRecipe(
    string RecipeId,
    int SchemaVersion,
    IReadOnlyList<BrowserRecorderCapturedStep> Steps,
    bool ExecutableByDefault,
    bool Redacted,
    BrowserRecipeVersioningPolicy VersioningPolicy)
{
    public bool IsSafeDraft =>
        !ExecutableByDefault &&
        Redacted &&
        SchemaVersion > 0 &&
        VersioningPolicy.CurrentSchemaVersion == SchemaVersion &&
        Steps.All(s => !s.StoresSecret && !s.StoresCookie && !s.StoresBody);
}

public sealed record BrowserRecorderSanitizationResult(BrowserRecorderDraftRecipe Draft, bool SecretsRemoved, bool CookiesRemoved, bool BodiesRemoved, bool FullPathsRemoved);

