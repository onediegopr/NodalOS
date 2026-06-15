using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaLeakHardeningEvaluator
{
    public NexaLeakHardeningReport Evaluate(IReadOnlyDictionary<NexaLeakHardeningSurface, object> artifacts, NexaLeakHardeningCorpus? corpus = null)
    {
        corpus ??= NexaLeakHardeningCorpus.Default();
        var checks = new List<NexaLeakHardeningSurfaceCheck>();

        foreach (var surface in Enum.GetValues<NexaLeakHardeningSurface>())
        {
            var serialized = artifacts.TryGetValue(surface, out var artifact)
                ? NexaLeakHardeningSerialization.ToSafeJson(artifact)
                : "";
            var leaks = corpus.SecretValues.Where(secret => serialized.Contains(secret, StringComparison.Ordinal)).ToList();
            var redacted = !BrowserCredentialRedactor.ContainsSecret(serialized) && leaks.Count == 0;
            checks.Add(new NexaLeakHardeningSurfaceCheck(surface, surface.ToString(), leaks.Count == 0 && redacted, leaks, redacted));
        }

        return new NexaLeakHardeningReport(checks, corpus, RunDefaultFuzz().All(result => result.Passed), Completed: true);
    }

    public IReadOnlyList<NexaRedactionFuzzResult> RunDefaultFuzz()
    {
        var cases = new[]
        {
            new NexaRedactionFuzzCase("token=opaque-token-value-123456789", BrowserCredentialRedactor.Redacted, MustRedact: true, PreserveSafeFragment: false),
            new NexaRedactionFuzzCase("Cookie: synthetic-cookie-session-value", BrowserCredentialRedactor.Redacted, MustRedact: true, PreserveSafeFragment: false),
            new NexaRedactionFuzzCase("Authorization: Bearer synthetic-bearer-token", BrowserCredentialRedactor.Redacted, MustRedact: true, PreserveSafeFragment: false),
            new NexaRedactionFuzzCase("api_key=synthetic-api-key-value", BrowserCredentialRedactor.Redacted, MustRedact: true, PreserveSafeFragment: false),
            new NexaRedactionFuzzCase("C:\\Users\\secret\\synthetic-local-path-C-users-secret", BrowserCredentialRedactor.Redacted, MustRedact: true, PreserveSafeFragment: false),
            new NexaRedactionFuzzCase("https://preview.nexa.local/assets/readme.txt", "preview.nexa.local", MustRedact: false, PreserveSafeFragment: true),
            new NexaRedactionFuzzCase("synthetic-report.pdf", "synthetic-report.pdf", MustRedact: false, PreserveSafeFragment: true)
        };

        return cases.Select(test =>
        {
            var redacted = Redact(test.Input, NexaLeakHardeningCorpus.Default());
            var passed = test.MustRedact
                ? redacted.Contains(test.ExpectedFragment, StringComparison.Ordinal)
                : test.PreserveSafeFragment && redacted.Contains(test.ExpectedFragment, StringComparison.Ordinal);
            return new NexaRedactionFuzzResult(test.Input, redacted, passed);
        }).ToList();
    }

    public string Redact(string value, NexaLeakHardeningCorpus corpus)
    {
        var redacted = BrowserCredentialRedactor.Redact(value);
        foreach (var secret in corpus.SecretValues)
            redacted = redacted.Replace(secret, BrowserCredentialRedactor.Redacted, StringComparison.Ordinal);
        return redacted;
    }

    public IReadOnlyDictionary<NexaLeakHardeningSurface, object> CreateSafeSurfaceArtifacts()
    {
        var diagnostics = new NexaDiagnosticsBundle(
            new NexaDiagnosticsManifest("diag-bundle-private-preview", 1, "diagnostics-redaction-strict", NexaDiagnosticsHash.Sha256("redacted"), Redacted: true),
            [new NexaDiagnosticsSection("environment-summary", "Environment", "local private preview metadata only", Redacted: true)],
            new NexaHealthReport([new NexaHealthCheckResult("diagnostics-redaction", NexaHealthCheckStatus.Healthy, "redaction active", Redacted: true)], Redacted: true),
            Redacted: true);

        return new Dictionary<NexaLeakHardeningSurface, object>
        {
            [NexaLeakHardeningSurface.AdminAudit] = new { actor = "owner", action = "set-feature", decision = "blocked-sensitive-feature", redacted = true },
            [NexaLeakHardeningSurface.DiagnosticsBundle] = diagnostics,
            [NexaLeakHardeningSurface.SupportBundle] = new NexaSupportBundleResult(new NexaSupportModeDecision(NexaHealthCheckStatus.Healthy, "metadata only", Redacted: true), diagnostics, Redacted: true),
            [NexaLeakHardeningSurface.AuditExport] = new { exportId = "audit-export-local", scope = "tenant-local", eventCount = 2, redacted = true },
            [NexaLeakHardeningSurface.PublicApiDto] = new NexaPublicApiResponse(true, "design-only", [], new Dictionary<string, string> { ["status"] = "redacted" }, Redacted: true, ContainsSecret: false, ContainsCookie: false, ContainsBody: false),
            [NexaLeakHardeningSurface.LocalProductShellRenderModel] = new NexaLocalProductShellService().CreateShell().RenderModel,
            [NexaLeakHardeningSurface.OnboardingAudit] = new { onboarding = "free-plan-mock", email = "[REDACTED]", redacted = true },
            [NexaLeakHardeningSurface.BillingMockInvoicePreview] = new { provider = "MockOnly", amount = "0", realCharge = false, redacted = true },
            [NexaLeakHardeningSurface.EmailOutboxMock] = new { mode = "MockOutboxOnly", body = "[REDACTED]", delivered = false },
            [NexaLeakHardeningSurface.ReleaseUpdateManifest] = new { version = "local-preview", hash = "sha256-redacted", signature = "metadata-present", autoExecute = false },
            [NexaLeakHardeningSurface.InstallerDryRunReport] = new { dryRun = true, modifiedRealSystem = false, redacted = true },
            [NexaLeakHardeningSurface.PreProductionCheckpointReport] = new { status = "local-private-preview-allowed", sensitiveRealPilot = "blocked", redacted = true }
        };
    }
}

public sealed class NexaSkippedTestsAuditReporter
{
    public NexaSkippedTestsAuditReport CreateReport() =>
        new(
            [
                Item("BrowserCdpLiveCanNavigateToLocalFixtureReadOnly", NexaSkippedTestCategory.SandboxBrowser, "CDP live local opt-in", "ONEBRAIN_RUN_CDP_LIVE_TESTS", false, "Run before browser-runtime release candidate."),
                Item("BrowserAuthenticatedSandboxLoginSandboxWithCdpProfileVaultConsentGate", NexaSkippedTestCategory.AuthSandbox, "Authenticated sandbox opt-in", "ONEBRAIN_RUN_AUTH_SANDBOX_TESTS", false, "Run before auth sandbox regression sign-off."),
                Item("BrowserExternalReadOnlyLiveNavigatesToTestOwnedTarget", NexaSkippedTestCategory.ExternalTarget, "M51 external target deferred", "ONEBRAIN_RUN_EXTERNAL_READONLY_TARGET_TESTS", false, "Configure test-owned target before external preview."),
                Item("BrowserExternalReadOnlyLiveVerifiesSemanticProof", NexaSkippedTestCategory.ExternalTarget, "M51 external target deferred", "ONEBRAIN_RUN_EXTERNAL_READONLY_TARGET_TESTS", false, "Configure test-owned target before external preview."),
                Item("BrowserExternalReadOnlyLiveCapturesNetworkMetadataOnly", NexaSkippedTestCategory.ExternalTarget, "M51 external target deferred", "ONEBRAIN_RUN_EXTERNAL_READONLY_TARGET_TESTS", false, "Configure test-owned target before external preview."),
                Item("BrowserExternalReadOnlyLiveDoesNotPersistOpaqueQuery", NexaSkippedTestCategory.ExternalTarget, "M51 external target deferred", "ONEBRAIN_RUN_EXTERNAL_READONLY_TARGET_TESTS", false, "Configure test-owned target before external preview."),
                Item("BrowserExternalReadOnlyLiveCleansBrowserProcesses", NexaSkippedTestCategory.ExternalTarget, "M51 external target deferred", "ONEBRAIN_RUN_EXTERNAL_READONLY_TARGET_TESTS", false, "Configure test-owned target before external preview."),
                Item("BrowserSafeDownloadLiveDownloadsAllowedPdfToQuarantine", NexaSkippedTestCategory.DownloadUpload, "Safe download live opt-in", "ONEBRAIN_RUN_SAFE_DOWNLOAD_LIVE_TESTS", false, "Run before download workflow release."),
                Item("BrowserSafeUploadLiveUploadsSyntheticFileToLocalFixture", NexaSkippedTestCategory.DownloadUpload, "Safe upload live opt-in", "ONEBRAIN_RUN_SAFE_UPLOAD_LIVE_TESTS", false, "Run before upload workflow release."),
                Item("BrowserSensitiveReadOnlySimulationLiveFiscalFixturePasses", NexaSkippedTestCategory.SensitiveSimulation, "Sensitive simulation opt-in", "ONEBRAIN_RUN_SENSITIVE_READONLY_SIM_TESTS", false, "Run before sensitive simulation demo."),
                Item("BrowserSensitiveDocumentSimulationLiveDownloadsSyntheticDocument", NexaSkippedTestCategory.SensitiveSimulation, "Sensitive document simulation opt-in", "ONEBRAIN_RUN_SENSITIVE_DOCUMENT_SIM_TESTS", false, "Run before sensitive document demo."),
                Item("BrowserRecipeReplaySafeModeCompletesReadOnlyRecipe", NexaSkippedTestCategory.RecorderReplay, "Replay safe mode opt-in", "ONEBRAIN_RUN_RECIPE_REPLAY_SAFE_MODE_TESTS", false, "Run before recorder/replay private preview.")
            ],
            Completed: true,
            Redacted: true);

    private static NexaSkippedTestAuditItem Item(string name, NexaSkippedTestCategory category, string reason, string env, bool blocksLocalPreview, string action) =>
        new(name, category, reason, env, blocksLocalPreview, action);
}
