namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsEvidenceBridgeSourceKind
{
    AgentOperation,
    Mission,
    AgentTask,
    RunReport,
    ProgressReport,
    VerificationGate,
    RecipeManifest,
    StepLibrary,
    BrowserRuntime,
    OcrObservation,
    Manual,
    Unknown
}

public enum NodalOsEvidenceBridgeUseKind
{
    DiagnosticOnly,
    Auxiliary,
    VerificationSupport,
    AuditTrail
}

public enum NodalOsEvidenceBridgeAuthority
{
    NoAuthority,
    SupportsVerificationOnly,
    DiagnosticOnly
}

public enum NodalOsEvidenceSensitivity
{
    Unknown,
    NonSensitive,
    PotentiallySensitive,
    Sensitive,
    SecretRedacted
}

public enum NodalOsEvidenceRedactionState
{
    Unknown,
    NotRequired,
    Redacted,
    RedactionRequired,
    RejectedSensitive
}

public sealed record NodalOsEvidenceBridgeRef
{
    public required string EvidenceId { get; init; }

    public required string Kind { get; init; }

    public string? Ref { get; init; }

    public string? Hash { get; init; }

    public required NodalOsEvidenceBridgeSourceKind SourceKind { get; init; }

    public required NodalOsEvidenceBridgeUseKind UseKind { get; init; }

    public required NodalOsEvidenceBridgeAuthority Authority { get; init; }

    public required NodalOsEvidenceSensitivity Sensitivity { get; init; }

    public required NodalOsEvidenceRedactionState RedactionState { get; init; }

    public string? LedgerRef { get; init; }

    public string? Provenance { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NodalOsEvidenceBridgeResult
{
    public required bool Accepted { get; init; }

    public required NodalOsEvidenceBridgeRef Evidence { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record NodalOsEvidenceBridgeOptions
{
    public bool RequireRedactionForPotentiallySensitive { get; init; } = true;

    public bool RejectSensitiveWithoutRedaction { get; init; } = true;

    public bool AllowLedgerRefOptional { get; init; } = true;
}
