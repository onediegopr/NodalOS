namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsSensitivePolicyCategory
{
    ApiKeys,
    AuthHeaderTokens,
    BrowserSessionValues,
    PrivateKeys,
    CredentialPhrases,
    OAuthTokens,
    CloudCredentials,
    DatabaseUrls,
    EnvironmentSecrets,
    CustomSensitiveMarkers
}

public sealed record NodalOsSecretDetectionPolicyPreview
{
    public required string PolicyPreviewId { get; init; }

    public required string WorkspaceRef { get; init; }

    public required string MissionRef { get; init; }

    public required string ScopePreviewRef { get; init; }

    public required bool IsPreviewOnly { get; init; }

    public required bool UsesRealContent { get; init; }

    public required bool ReadsFiles { get; init; }

    public required bool PerformsSecretDetectionOnRealData { get; init; }

    public required bool CanBlockScan { get; init; }

    public required bool CanRedactFindings { get; init; }

    public required bool RequiresUserReview { get; init; }

    public required bool RequiresAuditBeforeEnablement { get; init; }

    public IReadOnlyList<NodalOsSensitivePolicyCategory> Categories { get; init; } = [];

    public required string PatternPolicyRefsRedacted { get; init; }

    public required string EntropyPolicyRefsRedacted { get; init; }

    public required string FilenameHintRefsRedacted { get; init; }

    public required string ExtensionHintRefsRedacted { get; init; }

    public required string AllowlistRefsRedacted { get; init; }

    public required bool RequiresFalsePositiveReview { get; init; }

    public required bool RequiresRedaction { get; init; }

    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    public IReadOnlyList<string> TimelineRefs { get; init; } = [];
}

public sealed record NodalOsSecretDetectionReadinessResult
{
    public required string ReadinessId { get; init; }

    public required string PolicyPreviewRef { get; init; }

    public required bool ReadyForRealSecretDetection { get; init; }

    public required bool ReadyForRealScan { get; init; }

    public required bool ReadyForLlmContextBuild { get; init; }

    public required bool CanReadFile { get; init; }

    public required bool CanInspectRealContent { get; init; }

    public required bool CanEmitRawSecret { get; init; }

    public required bool CanPersistRawSecret { get; init; }

    public required bool CanSendSecretToLlm { get; init; }

    public required bool CanSendSecretToCloud { get; init; }

    public IReadOnlyList<string> MissingRequirementsRedacted { get; init; } = [];

    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];

    public required string UserFacingExplanationRedacted { get; init; }

    public IReadOnlyList<string> GuardrailRefs { get; init; } = [];
}
