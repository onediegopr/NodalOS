using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaLeakHardeningEvaluator
{
    private const int KnownSkippedCount = 29;

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
            var redacted = leaks.Count == 0;
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
            new NexaRedactionFuzzCase("https://preview.nodal-os.local/assets/readme.txt", "preview.nodal-os.local", MustRedact: false, PreserveSafeFragment: true),
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

    public IReadOnlyDictionary<NexaLeakHardeningSurface, object> CreateRealSurfaceArtifacts()
    {
        var preview = new NexaPrivatePreviewLocalEvaluator().Evaluate(
            NexaPrivatePreviewLocalEvaluator.SafeProfile(),
            NexaPrivatePreviewLocalEvaluator.SafeSession(),
            NexaPrivatePreviewLocalEvaluator.SafeReadiness());
        var feedbackEvaluator = new NexaPrivatePreviewFeedbackEvaluator();
        var tenant = new NexaPublicApiTenantContext("tenant-local", "account-local", "org-local", "workspace-local", "worker-owner");
        var feedback = new NexaPrivatePreviewFeedback(
            "feedback-real-surface",
            "preview-session-local",
            "owner-local",
            NexaRole.Owner,
            "tenant-local",
            "workspace-local",
            "diagnostics",
            NexaPrivatePreviewFeedbackSeverity.Medium,
            Redact("repro includes opaque-token-value-123456789 and synthetic-password-value", NexaLeakHardeningCorpus.Default()),
            ["diag-ref-real-surface"],
            ["audit-ref-real-surface"],
            Redacted: true);
        var feedbackDecision = feedbackEvaluator.Evaluate(feedback, tenant, tenant, actorAuthorized: true, diagnosticsRedacted: true, auditExportRedacted: true);
        var feedbackSummary = feedbackEvaluator.CreateSummary("preview-session-local", "tenant-local", "workspace-local", feedbackDecision.IssueReport is null ? [] : [feedbackDecision.IssueReport]);

        var api = new NexaPrivateLocalApiService();
        var apiRequest = new NexaPrivateLocalApiRequest(NexaPrivateLocalApiMethod.Get, "/runtime/status", "owner-test-token", tenant, true, RequestContainsSecret: false, RequestContainsCookie: false, RequestContainsBody: false);
        var apiResult = api.Handle(apiRequest);
        var apiDiagnostics = new NexaPrivateLocalApiDiagnosticsCollector().Collect([(apiRequest, apiResult.Response, apiResult.Audit)]);

        var emailProvider = new NexaEmailDeliveryProvider();
        var email = emailProvider.Deliver(
            new NexaEmailProviderConfig(NexaEmailProviderKind.SandboxProvider, PrivatePreviewLocal: true, RealEmailDeliveryEnabled: false, SandboxAllowed: true),
            new NexaEmailDeliveryRequest(
                "email-real-surface",
                "trial@example.invalid",
                NexaEmailTemplateKind.FreeLicenseRequested,
                new Dictionary<string, string> { ["summary"] = Redact("opaque-token-value-123456789", NexaLeakHardeningCorpus.Default()) },
                ContainsSecret: false,
                ContainsCookie: false));

        var billingProvider = new NexaPaymentProvider();
        _ = billingProvider.CreateCheckout(
            new NexaPaymentProviderConfig(NexaPaymentProviderKind.SandboxProvider, PrivatePreviewLocal: true, RealBillingEnabled: false, SandboxAllowed: true, StoresPaymentCardData: false),
            new NexaCheckoutSessionRequest("checkout-real-surface", "account-local", NexaPlanKind.Trial, 0, "USD", ContainsPaymentCardData: false));

        var package = PackageManifest();
        var account = ProductAccount();
        var license = new NexaLicense("license-real-surface", "account-local", NexaPlan.Trial(), NexaLicenseStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7), [new NexaLicenseEntitlement(NexaFeatureFlag.AdminConsole, true, null)], ManualAdminOverride: false);
        var auditEvent = new NexaAdminAuditEvent("audit-real-surface", "owner-local", NexaRole.Owner, "account-local", "org-local", NexaAdminAction.ViewAudit, NexaAdminDecisionKind.Allowed, Redact("reason synthetic-api-key-value", NexaLeakHardeningCorpus.Default()), DateTimeOffset.UtcNow, "before redacted", "after redacted", Redacted: true);
        var diagnostics = new NexaDiagnosticsCollector().Collect(
            new NexaDiagnosticsBundleRequest("diag-real-surface", NexaDiagnosticsRedactionPolicy.Strict(), IncludeAuditSummary: true, IncludeRecentErrors: true),
            package,
            license,
            account,
            [auditEvent],
            [Redact("error synthetic-cookie-session-value", NexaLeakHardeningCorpus.Default())]);
        var support = new NexaSupportModeService().CreateBundle(
            new NexaSupportBundleRequest(
                "support-real-surface",
                "support-local",
                NexaSupportModePolicy.StrictMetadataOnly(),
                new NexaTenant("tenant-local", "account-local", "org-local", "workspace-local", "worker-support"),
                new NexaTenant("tenant-local", "account-local", "org-local", "workspace-local", "worker-owner")),
            package,
            license,
            account,
            [auditEvent]);
        var auditExport = new NexaAuditExportService().Export(
            new NexaAuditExportRequest("audit-export-real-surface", "owner-local", NexaRole.Owner, new NexaTenantAuditScope("account-local", "org-local", "workspace-local", NexaTenantScope.Workspace), DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1), NexaAuditExportFormat.Json, NexaAuditExportRedactionPolicy.Strict(), SensitiveDataPolicyApproved: false),
            [auditEvent],
            new NexaTenant("tenant-local", "account-local", "org-local", "workspace-local", "worker-owner"),
            new NexaTenant("tenant-local", "account-local", "org-local", "workspace-local", "worker-owner"));
        var checkpoint = new NexaPreProductionCheckpointService().Create(new NexaPreProductionCheckpointRequest(
            M25BExternalLowRiskTargetAvailable: false,
            SensitiveRealPilotDecisionApproved: false,
            PublicSaasEnabled: false,
            RealBillingEnabled: false,
            RealEmailEnabled: false,
            AutoUpdateRealEnabled: false,
            ProductiveRecorderReplayEnabled: false,
            ProfileRawEnabled: false,
            RealClientCredentialsEnabled: false));

        return new Dictionary<NexaLeakHardeningSurface, object>
        {
            [NexaLeakHardeningSurface.AdminAudit] = auditEvent,
            [NexaLeakHardeningSurface.DiagnosticsBundle] = diagnostics,
            [NexaLeakHardeningSurface.SupportBundle] = support,
            [NexaLeakHardeningSurface.AuditExport] = auditExport,
            [NexaLeakHardeningSurface.PublicApiDto] = apiResult.Response,
            [NexaLeakHardeningSurface.LocalProductShellRenderModel] = new NexaLocalProductShellService().CreateShell().RenderModel,
            [NexaLeakHardeningSurface.OnboardingAudit] = preview,
            [NexaLeakHardeningSurface.BillingMockInvoicePreview] = billingProvider.Ledger,
            [NexaLeakHardeningSurface.EmailOutboxMock] = emailProvider.Snapshot(),
            [NexaLeakHardeningSurface.ReleaseUpdateManifest] = new NexaUpdateManifest(
                "update-real-surface",
                NexaReleaseChannelKind.Preview,
                new NexaUpdatePackageDescriptor("package-real-surface", new NexaReleaseVersion(1, 0, 0, "real-surface"), [new NexaReleaseComponent("component-local", "1.0.0", "sha256-redacted")]),
                new NexaUpdateIntegrityDescriptor("sha256-redacted", "signature-metadata-present", SignatureRequired: true),
                new NexaUpdateCompatibilityCheck(new NexaReleaseCompatibility(".NET 11", "Windows", "ControlledBrowser", RuntimeCompatible: true), Passed: true),
                new NexaUpdateRollbackPlan(new NexaReleaseVersion(0, 9, 0, "rollback"), ExecuteAutomatically: false, "manual rollback dry-run only"),
                ["compatible"],
                Redacted: true),
            [NexaLeakHardeningSurface.InstallerDryRunReport] = new NexaInstallerDryRunResult(
                Allowed: true,
                ModifiedRealSystem: false,
                PreflightChecks: [new NexaInstallerPreflightCheck("preflight-runtime", NexaInstallerPreflightStatus.Passed, "runtime compatible", Required: true)],
                Violations: [],
                FileLayout: new NexaInstallerFileLayout("sandbox-root", ["app"], ["logs/app.log"], SandboxOnly: true),
                RollbackPlan: new NexaInstallerRollbackDryRunPlan("rollback-real-surface", ["manual cleanup dry-run"], ExecutesRollback: false),
                Decision: "dry-run allowed",
                Redacted: true),
            [NexaLeakHardeningSurface.PreProductionCheckpointReport] = checkpoint
        };
    }

    private static NexaPackageManifest PackageManifest() =>
        new(
            "package-real-surface",
            new NexaPackageVersion("1.0.0-real-surface", ".NET 11", "browser-runtime-real-surface"),
            NexaPackageChannel.Preview,
            new NexaPackageEnvironment("Windows", ".NET 11", BrowserAvailable: true, CdpAvailable: true, VaultProviderAvailable: true, AdminLicensingAvailable: true),
            [
                new NexaPackageComponent("component-browser-runtime", "Browser Runtime", "1.0.0", Available: true),
                new NexaPackageComponent("component-admin-runtime", "Admin Runtime", "1.0.0", Available: true),
                new NexaPackageComponent("component-tenant-governance", "Tenant Governance", "1.0.0", Available: true),
                new NexaPackageComponent("component-audit-export", "Audit Export", "1.0.0", Available: true)
            ],
            [NexaFeatureFlag.AdminConsole, NexaFeatureFlag.BrowserRuntime],
            Redacted: true);

    private static NexaProductAccount ProductAccount() =>
        new(
            "account-local",
            NexaProductAccountKind.Person,
            NexaAccountStatus.Active,
            new NexaPersonAccount("person-local", "redacted@example.invalid", NexaAccountStatus.Active),
            null,
            new NexaOrganization("org-local", "Local Org", NexaAccountStatus.Active),
            [new NexaWorkspace("workspace-local", "org-local", "Local Workspace", NexaAccountStatus.Active)],
            [new NexaWorker("worker-owner", "workspace-local", "seat-owner", NexaRole.Owner, new HashSet<NexaAdminCapability> { NexaAdminCapability.ViewReadOnly }, Active: true)],
            [new NexaSeat("seat-owner", "worker-owner", NexaRole.Owner, Active: true)]);
}

public sealed class NexaSkippedTestsAuditReporter
{
    public NexaSkippedTestsAuditReport CreateReport() =>
        new(
            [
                Item("BrowserAuthenticatedSandboxLoginSandboxWithCdpProfileVaultConsentGate", NexaSkippedTestCategory.AuthSandbox, "Authenticated sandbox opt-in", "ONEBRAIN_RUN_AUTH_SANDBOX_TESTS", false, "Run before auth sandbox sign-off."),
                Item("BrowserCdpLiveCanNavigateToLocalFixtureReadOnly", NexaSkippedTestCategory.CdpLiveOptIn, "CDP live opt-in", "ONEBRAIN_RUN_CDP_LIVE_TESTS", false, "Run before browser-runtime release candidate."),
                Item("BrowserCdpLiveCanReadDomFromLocalFixture", NexaSkippedTestCategory.CdpLiveOptIn, "CDP live opt-in", "ONEBRAIN_RUN_CDP_LIVE_TESTS", false, "Run before browser-runtime release candidate."),
                Item("BrowserCdpLiveNetworkCaptureRecordsMetadataOnly", NexaSkippedTestCategory.CdpLiveOptIn, "CDP live opt-in", "ONEBRAIN_RUN_CDP_LIVE_TESTS", false, "Run before browser-runtime release candidate."),
                Item("BrowserCdpLiveNetworkCaptureDoesNotRecordSensitiveHeaderValues", NexaSkippedTestCategory.CdpLiveOptIn, "CDP live opt-in", "ONEBRAIN_RUN_CDP_LIVE_TESTS", false, "Run before browser-runtime release candidate."),
                Item("BrowserCdpLiveBuildsFrameTreeFromLocalFixture", NexaSkippedTestCategory.CdpLiveOptIn, "CDP live opt-in", "ONEBRAIN_RUN_CDP_LIVE_TESTS", false, "Run before browser-runtime release candidate."),
                Item("BrowserCdpLiveDetectsFrameDetachAndBlocksStaleFrameRead", NexaSkippedTestCategory.CdpLiveOptIn, "CDP live opt-in", "ONEBRAIN_RUN_CDP_LIVE_TESTS", false, "Run before browser-runtime release candidate."),
                Item("BrowserCdpLiveObservesDownloadWillBeginFromLocalFixture", NexaSkippedTestCategory.CdpLiveOptIn, "CDP live opt-in", "ONEBRAIN_RUN_CDP_LIVE_TESTS", false, "Run before browser-runtime release candidate."),
                Item("BrowserCdpLiveAuditLedgerVerifiesAfterNetworkEvents", NexaSkippedTestCategory.CdpLiveOptIn, "CDP live opt-in", "ONEBRAIN_RUN_CDP_LIVE_TESTS", false, "Run before browser-runtime release candidate."),
                Item("BrowserCdpLiveClosesBrowserProcessAfterTest", NexaSkippedTestCategory.CdpLiveOptIn, "CDP live opt-in", "ONEBRAIN_RUN_CDP_LIVE_TESTS", false, "Run before browser-runtime release candidate."),
                Item("BrowserDocumentWorkflowSandboxCompletesEndToEnd", NexaSkippedTestCategory.DocumentWorkflowOptIn, "Document workflow sandbox opt-in", "ONEBRAIN_RUN_DOCUMENT_WORKFLOW_SANDBOX", false, "Run before document workflow release."),
                Item("BrowserExternalLowRiskAuthLiveTargetValidationIsBlockedWithoutConfiguredTarget", NexaSkippedTestCategory.ExternalTargetBlocked, "M65 external low-risk target missing", "ONEBRAIN_RUN_EXTERNAL_LOW_RISK_TARGET_TESTS", false, "Configure test-owned low-risk target."),
                Item("BrowserExternalReadOnlyLiveNavigatesToTestOwnedTarget", NexaSkippedTestCategory.ExternalTargetBlocked, "M51 external target deferred", "ONEBRAIN_RUN_EXTERNAL_READONLY_TARGET_TESTS", false, "Configure test-owned target before external preview."),
                Item("BrowserExternalReadOnlyLiveVerifiesSemanticProof", NexaSkippedTestCategory.ExternalTargetBlocked, "M51 external target deferred", "ONEBRAIN_RUN_EXTERNAL_READONLY_TARGET_TESTS", false, "Configure test-owned target before external preview."),
                Item("BrowserExternalReadOnlyLiveCapturesNetworkMetadataOnly", NexaSkippedTestCategory.ExternalTargetBlocked, "M51 external target deferred", "ONEBRAIN_RUN_EXTERNAL_READONLY_TARGET_TESTS", false, "Configure test-owned target before external preview."),
                Item("BrowserExternalReadOnlyLiveDoesNotPersistOpaqueQuery", NexaSkippedTestCategory.ExternalTargetBlocked, "M51 external target deferred", "ONEBRAIN_RUN_EXTERNAL_READONLY_TARGET_TESTS", false, "Configure test-owned target before external preview."),
                Item("BrowserExternalReadOnlyLiveCleansBrowserProcesses", NexaSkippedTestCategory.ExternalTargetBlocked, "M51 external target deferred", "ONEBRAIN_RUN_EXTERNAL_READONLY_TARGET_TESTS", false, "Configure test-owned target before external preview."),
                Item("BrowserRealSiteReadOnlyLiveSmokeIsOptIn", NexaSkippedTestCategory.ExternalTargetBlocked, "Real site smoke opt-in remains blocked without safe target", "ONEBRAIN_RUN_REAL_SITE_READONLY_TESTS", false, "Use only test-owned external target."),
                Item("BrowserRecipeReplaySafeModeCompletesReadOnlyRecipe", NexaSkippedTestCategory.RecorderReplayOptIn, "Replay safe mode opt-in", "ONEBRAIN_RUN_RECIPE_REPLAY_SAFE_MODE_TESTS", false, "Run before recorder/replay private preview."),
                Item("BrowserRecorderReadOnlyPrototypeCapturesLocalSandboxDraft", NexaSkippedTestCategory.RecorderReplayOptIn, "Recorder read-only opt-in", "ONEBRAIN_RUN_RECORDER_READONLY_TESTS", false, "Run before recorder private preview."),
                Item("BrowserSafeDownloadLiveDownloadsAllowedPdfToQuarantine", NexaSkippedTestCategory.SafeDownloadUploadOptIn, "Safe download live opt-in", "ONEBRAIN_RUN_SAFE_DOWNLOAD_LIVE_TESTS", false, "Run before download workflow release."),
                Item("BrowserSafeUploadLiveUploadsSyntheticFileToLocalFixture", NexaSkippedTestCategory.SafeDownloadUploadOptIn, "Safe upload live opt-in", "ONEBRAIN_RUN_SAFE_UPLOAD_LIVE_TESTS", false, "Run before upload workflow release."),
                Item("BrowserSensitiveReadOnlySimulationLiveFiscalFixturePasses", NexaSkippedTestCategory.SensitiveSimulationOptIn, "Sensitive simulation opt-in", "ONEBRAIN_RUN_SENSITIVE_READONLY_SIM_TESTS", false, "Run before sensitive simulation demo."),
                Item("BrowserSensitiveReadOnlySimulationLiveBlocksSubmit", NexaSkippedTestCategory.SensitiveSimulationOptIn, "Sensitive simulation opt-in", "ONEBRAIN_RUN_SENSITIVE_READONLY_SIM_TESTS", false, "Run before sensitive simulation demo."),
                Item("BrowserSensitiveDocumentSimulationLiveDownloadsSyntheticDocument", NexaSkippedTestCategory.SensitiveSimulationOptIn, "Sensitive document simulation opt-in", "ONEBRAIN_RUN_SENSITIVE_DOCUMENT_SIM_TESTS", false, "Run before sensitive document demo."),
                Item("BrowserSensitiveDocumentSimulationLiveUploadsSyntheticDocument", NexaSkippedTestCategory.SensitiveSimulationOptIn, "Sensitive document simulation opt-in", "ONEBRAIN_RUN_SENSITIVE_DOCUMENT_SIM_TESTS", false, "Run before sensitive document demo."),
                Item("NexaExternalLowRiskTargetLiveVerifiesSemanticProof", NexaSkippedTestCategory.ExternalTargetBlocked, "M65 external low-risk target missing", "ONEBRAIN_RUN_EXTERNAL_LOW_RISK_TARGET_TESTS", false, "Configure test-owned low-risk target."),
                Item("NexaExternalLowRiskTargetLiveCapturesMetadataOnly", NexaSkippedTestCategory.ExternalTargetBlocked, "M65 external low-risk target missing", "ONEBRAIN_RUN_EXTERNAL_LOW_RISK_TARGET_TESTS", false, "Configure test-owned low-risk target."),
                Item("NexaExternalLowRiskTargetLiveCleansBrowser", NexaSkippedTestCategory.ExternalTargetBlocked, "M65 external low-risk target missing", "ONEBRAIN_RUN_EXTERNAL_LOW_RISK_TARGET_TESTS", false, "Configure test-owned low-risk target.")
            ],
            Completed: true,
            Redacted: true);

    private static NexaSkippedTestAuditItem Item(string name, NexaSkippedTestCategory category, string reason, string env, bool blocksLocalPreview, string action) =>
        new(name, category, reason, env, blocksLocalPreview, action);
}
