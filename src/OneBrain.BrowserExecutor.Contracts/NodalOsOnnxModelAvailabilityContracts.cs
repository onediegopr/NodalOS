namespace OneBrain.BrowserExecutor.Contracts;

// M215-M217 - ONNX model availability inventory, manifest reconciliation, and guarded retry gate.

public enum NodalOsOnnxModelAvailabilityStatus
{
    PresentAndVerified,
    Missing,
    PathMismatch,
    HashMismatch,
    SizeMismatch,
    ManifestMissing,
    DiscoveryMismatch,
    RoleMismatch,
    IgnoredCorrectly,
    UnexpectedCommittedModel,
    Unknown
}

public enum NodalOsOnnxModelAcquisitionReadinessDecision
{
    ReadyForGuardedSyntheticTextRetry,
    ReadyForModelDownload,
    BlockedByModelMissing,
    BlockedByModelManifestMismatch,
    BlockedByModelHashMismatch,
    BlockedByModelPathDiscovery,
    BlockedByUnexpectedCommittedModel,
    BlockedByAcquisitionScript,
    NotReady
}

public sealed record NodalOsOnnxModelPathResolution(
    string ModelId,
    string ExpectedRelativePath,
    string ExpectedFileName,
    string ResolvedPath,
    bool PathUnderRepository,
    bool FileNameMatches,
    bool NoAuthority);

public sealed record NodalOsOnnxModelInventoryEntry(
    string ModelId,
    NodalOsPaddleOcrOnnxModelKind Role,
    string ExpectedFileName,
    string ExpectedRelativePath,
    NodalOsOnnxModelPathResolution PathResolution,
    bool Exists,
    string ExpectedSha256,
    string? ActualSha256,
    long ExpectedSizeBytes,
    long? ActualSizeBytes,
    string ManifestSourceUrl,
    bool GitIgnored,
    bool Committed,
    NodalOsOnnxModelAvailabilityStatus AvailabilityStatus,
    string Reason);

public sealed record NodalOsOnnxModelInventory(
    string InventoryId,
    string RepositoryRoot,
    string ManifestPath,
    bool ManifestExists,
    IReadOnlyList<NodalOsOnnxModelInventoryEntry> Entries,
    bool NoSaas,
    bool NoRawPersistence,
    bool NoAuthority,
    DateTimeOffset CreatedAtUtc);

public sealed record NodalOsOnnxModelManifestReconciliationResult(
    string ResultId,
    NodalOsOnnxModelInventory Inventory,
    bool RequiredDetectionPresentAndVerified,
    bool RequiredRecognitionPresentAndVerified,
    bool OptionalClassificationPresentAndVerified,
    bool AnyHashMismatch,
    bool AnySizeMismatch,
    bool AnyPathMismatch,
    bool AnyUnexpectedCommittedModel,
    bool ScriptsPresent,
    string DownloadCommand,
    string VerifyCommand,
    string RollbackCommand,
    NodalOsOnnxModelAcquisitionReadinessDecision Decision,
    string Reason);

public sealed record NodalOsGuardedSyntheticTextRetryPlan(
    string PlanId,
    bool RetryExecuted,
    bool RetryAllowed,
    bool UsesOutOfProcessGuard,
    string NextDownloadCommand,
    string NextVerifyCommand,
    IReadOnlyList<string> ExpectedFiles,
    IReadOnlyDictionary<string, string> ExpectedSha256,
    IReadOnlyDictionary<string, long> ExpectedSizes,
    NodalOsOnnxModelAcquisitionReadinessDecision Decision,
    string Reason);
