using System.Text.Json;

namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaLeakHardeningSurface
{
    AdminAudit,
    DiagnosticsBundle,
    SupportBundle,
    AuditExport,
    PublicApiDto,
    LocalProductShellRenderModel,
    OnboardingAudit,
    BillingMockInvoicePreview,
    EmailOutboxMock,
    ReleaseUpdateManifest,
    InstallerDryRunReport,
    PreProductionCheckpointReport
}

public sealed record NexaLeakHardeningCorpus(IReadOnlyList<string> SecretValues)
{
    public static NexaLeakHardeningCorpus Default() =>
        new(
            [
                "opaque-token-value-123456789",
                "synthetic-password-value",
                "synthetic-cookie-session-value",
                "synthetic-api-key-value",
                "synthetic-bearer-token",
                "synthetic-refresh-token",
                "synthetic-query-token",
                "synthetic-local-path-C-users-secret",
                "synthetic-session-storage-value",
                "synthetic-vault-raw-value",
                "synthetic-payment-card-value"
            ]);
}

public sealed record NexaLeakHardeningSurfaceCheck(
    NexaLeakHardeningSurface Surface,
    string ArtifactId,
    bool Passed,
    IReadOnlyList<string> LeakedValues,
    bool Redacted);

public sealed record NexaLeakHardeningReport(
    IReadOnlyList<NexaLeakHardeningSurfaceCheck> Checks,
    NexaLeakHardeningCorpus Corpus,
    bool RedactionFuzzPassed,
    bool Completed)
{
    public bool IsSafe =>
        Completed &&
        RedactionFuzzPassed &&
        Checks.Count == Enum.GetValues<NexaLeakHardeningSurface>().Length &&
        Checks.All(check => check.Passed && check.Redacted && check.LeakedValues.Count == 0);
}

public sealed record NexaRedactionFuzzCase(string Input, string ExpectedFragment, bool MustRedact, bool PreserveSafeFragment);

public sealed record NexaRedactionFuzzResult(string Input, string Redacted, bool Passed);

public enum NexaSkippedTestCategory
{
    LiveOptIn,
    ExternalTarget,
    SensitiveSimulation,
    SandboxBrowser,
    RecorderReplay,
    DownloadUpload,
    AuthSandbox,
    CdpLiveOptIn,
    ExternalTargetBlocked,
    SensitiveSimulationOptIn,
    DocumentWorkflowOptIn,
    RecorderReplayOptIn,
    OsBackedOptIn,
    SafeDownloadUploadOptIn,
    Other
}

public sealed record NexaSkippedTestAuditItem(
    string TestName,
    NexaSkippedTestCategory Category,
    string Reason,
    string? OptInEnvironmentVariable,
    bool BlocksLocalPrivatePreview,
    string RecommendedAction);

public sealed record NexaSkippedTestsAuditReport(IReadOnlyList<NexaSkippedTestAuditItem> Items, bool Completed, bool Redacted)
{
    public bool BlocksLocalPrivatePreview => Items.Any(item => item.BlocksLocalPrivatePreview);
}

public sealed record NexaSkippedTestsCategoryAuditResult(
    int ExpectedCount,
    int ActualCount,
    IReadOnlySet<NexaSkippedTestCategory> ExpectedCategories,
    IReadOnlySet<NexaSkippedTestCategory> ActualCategories,
    IReadOnlyList<string> ReasonCodes,
    bool Passed,
    bool Redacted);

public static class NexaLeakHardeningSerialization
{
    public static string ToSafeJson(object value) =>
        JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = false });
}
