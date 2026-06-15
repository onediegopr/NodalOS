using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserSensitiveSimulationM33AM34ATests
{
    [TestMethod]
    public void BrowserSensitiveReadOnlySimulationRequiresApproval()
    {
        var result = ReadOnlyResult();

        Assert.AreEqual(SensitiveSiteHumanApprovalRequirement.SingleApprovalRequired, result.Evidence.ApprovalRequirement);
        Assert.IsTrue(result.AllowsDone, result.Reason);
    }

    [TestMethod]
    public void BrowserSensitiveReadOnlySimulationFailsWithoutApproval()
    {
        var result = ReadOnlyResult(requestMutation: r => r with { ApprovalRefs = [] });

        Assert.AreEqual(BrowserSensitiveSimulationStatus.Blocked, result.Status);
        StringAssert.Contains(result.Reason, "policy");
    }

    [TestMethod]
    public void BrowserSensitiveReadOnlySimulationFailsWhenGateFails()
    {
        var gate = BrowserVaultMinimalM23Tests.GateReport(SafeState() with { SensitiveHeaderValueCaptureSupported = true });
        var result = ReadOnlyResult(requestMutation: r => r with { GateReport = gate });

        Assert.AreEqual(BrowserSensitiveSimulationStatus.Blocked, result.Status);
        Assert.IsFalse(result.AllowsDone);
    }

    [TestMethod]
    public void BrowserSensitiveReadOnlySimulationBlocksSubmit() =>
        AssertReadOnlyBlocks(r => r with { SubmitBlocked = false });

    [TestMethod]
    public void BrowserSensitiveReadOnlySimulationBlocksPayment() =>
        AssertReadOnlyBlocks(r => r with { PaymentBlocked = false });

    [TestMethod]
    public void BrowserSensitiveReadOnlySimulationBlocksSigning() =>
        AssertReadOnlyBlocks(r => r with { SigningBlocked = false });

    [TestMethod]
    public void BrowserSensitiveReadOnlySimulationBlocksDelete() =>
        AssertReadOnlyBlocks(r => r with { DeleteBlocked = false });

    [TestMethod]
    public void BrowserSensitiveReadOnlySimulationAllowsReadOnlyDashboard()
    {
        var result = ReadOnlyResult();

        Assert.AreEqual(BrowserSensitiveSimulationStatus.Verified, result.Status);
        CollectionAssert.Contains(result.Evidence.WhatRemainsBlocked.ToList(), "submit");
        CollectionAssert.Contains(result.Evidence.WhatRemainsBlocked.ToList(), "pay");
    }

    [TestMethod]
    public void BrowserSensitiveReadOnlySimulationRequiresSemanticProof()
    {
        var result = ReadOnlyResult(requestMutation: r => r with { SemanticProofPresent = false });

        Assert.AreEqual(BrowserSensitiveSimulationStatus.Blocked, result.Status);
        Assert.IsNull(result.Verification);
    }

    [TestMethod]
    public void BrowserSensitiveReadOnlySimulationDoesNotExposeCookiesSecretsBodies()
    {
        var result = ReadOnlyResult();
        var text = result.ToString()!;

        Assert.IsTrue(result.Evidence.CookiesSecretsBodiesExcluded);
        Assert.IsFalse(text.Contains("session=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("set-cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("synthetic-secret", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("request body", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserSensitiveReadOnlySimulationReportsWhatRemainsBlocked()
    {
        var result = ReadOnlyResult();

        CollectionAssert.Contains(result.Evidence.WhatRemainsBlocked.ToList(), "sign");
        CollectionAssert.Contains(result.Evidence.WhatRemainsBlocked.ToList(), "delete");
    }

    [TestMethod]
    public void BrowserSensitiveDocumentSimulationRequiresApprovalForDownload()
    {
        var result = DocumentResultAsync(requestMutation: r => r with { DownloadApprovalRefs = [] }).GetAwaiter().GetResult();

        Assert.AreEqual(BrowserSensitiveSimulationStatus.Blocked, result.Status);
    }

    [TestMethod]
    public void BrowserSensitiveDocumentSimulationRequiresApprovalForUpload()
    {
        var result = DocumentResultAsync(requestMutation: r => r with { UploadApprovalRefs = [] }).GetAwaiter().GetResult();

        Assert.AreEqual(BrowserSensitiveSimulationStatus.Blocked, result.Status);
    }

    [TestMethod]
    public void BrowserSensitiveDocumentSimulationUsesSafeDownload()
    {
        var result = DocumentResultAsync().GetAwaiter().GetResult();

        Assert.IsTrue(result.DownloadResult?.AllowsDone);
        Assert.IsTrue(result.DownloadResult!.Artifact!.Quarantined);
    }

    [TestMethod]
    public void BrowserSensitiveDocumentSimulationUsesSafeUpload()
    {
        var result = DocumentResultAsync().GetAwaiter().GetResult();

        Assert.IsTrue(result.UploadResult?.AllowsDone);
        Assert.IsFalse(result.UploadResult!.Artifact!.ContentCaptured);
    }

    [TestMethod]
    public void BrowserSensitiveDocumentSimulationBlocksDocumentContentCapture()
    {
        var result = DocumentResultAsync(requestMutation: r => r with { DocumentContentCaptureAttempted = true }).GetAwaiter().GetResult();

        Assert.AreEqual(BrowserSensitiveSimulationStatus.Blocked, result.Status);
        StringAssert.Contains(result.Reason, "content capture");
    }

    [TestMethod]
    public void BrowserSensitiveDocumentSimulationBlocksSubmitAfterUpload()
    {
        var result = DocumentResultAsync(requestMutation: r => r with { SubmitAfterUploadAttempted = true }).GetAwaiter().GetResult();

        Assert.AreEqual(BrowserSensitiveSimulationStatus.Blocked, result.Status);
    }

    [TestMethod]
    public void BrowserSensitiveDocumentSimulationBlocksPaymentSigningDelete()
    {
        Assert.AreEqual(BrowserSensitiveSimulationStatus.Blocked, DocumentResultAsync(requestMutation: r => r with { PaymentAttempted = true }).GetAwaiter().GetResult().Status);
        Assert.AreEqual(BrowserSensitiveSimulationStatus.Blocked, DocumentResultAsync(requestMutation: r => r with { SigningAttempted = true }).GetAwaiter().GetResult().Status);
        Assert.AreEqual(BrowserSensitiveSimulationStatus.Blocked, DocumentResultAsync(requestMutation: r => r with { DeleteAttempted = true }).GetAwaiter().GetResult().Status);
    }

    [TestMethod]
    public void BrowserSensitiveDocumentSimulationRequiresHashMimeSize()
    {
        var result = DocumentResultAsync().GetAwaiter().GetResult();

        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Evidence.DownloadArtifact?.Sha256));
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Evidence.UploadArtifact?.Sha256));
        Assert.IsTrue(result.Evidence.DownloadArtifact?.SizeBytes > 0);
        Assert.IsTrue(result.Evidence.UploadArtifact?.SizeBytes > 0);
        Assert.AreEqual("application/pdf", result.Evidence.DownloadArtifact?.MimeType);
    }

    [TestMethod]
    public void BrowserSensitiveDocumentSimulationDoesNotExposeDocumentContentCookiesSecretsBodies()
    {
        var result = DocumentResultAsync().GetAwaiter().GetResult();
        var text = result.ToString()!;

        Assert.IsTrue(result.Evidence.CookiesSecretsBodiesExcluded);
        Assert.IsFalse(result.Evidence.DocumentContentCaptured);
        Assert.IsFalse(text.Contains("SYNTHETIC_DOCUMENT_CONTENT", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("session=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("set-cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("request body", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BrowserSensitiveDocumentSimulationProducesAuditSummary()
    {
        var result = DocumentResultAsync().GetAwaiter().GetResult();

        Assert.IsTrue(result.AuditEvent.Validate().IsValid);
        Assert.IsTrue(result.Evidence.EvidenceRefs.Count > 0);
        Assert.IsTrue(result.AllowsDone, result.Reason);
    }

    [TestMethod]
    public void BrowserSensitiveDocumentSimulationModelsDoubleApprovalForFutureCriticalActions()
    {
        var result = DocumentResultAsync().GetAwaiter().GetResult();

        Assert.IsTrue(result.Evidence.DoubleApprovalModeled);
        Assert.AreEqual(SensitiveSiteHumanApprovalRequirement.Prohibited, SensitiveSitePolicyEvaluatorFor(SensitiveSiteActionKind.Submit).ApprovalRequirement);
        Assert.AreEqual(SensitiveSiteHumanApprovalRequirement.Prohibited, SensitiveSitePolicyEvaluatorFor(SensitiveSiteActionKind.Pay).ApprovalRequirement);
        Assert.AreEqual(SensitiveSiteHumanApprovalRequirement.Prohibited, SensitiveSitePolicyEvaluatorFor(SensitiveSiteActionKind.Sign).ApprovalRequirement);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateAllowsSensitiveReadOnlySimulation()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, SafeState() with { SensitiveReadOnlySimulationActive = true });

        Assert.IsTrue(report.Passed, string.Join("; ", report.FailedChecks));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateAllowsSensitiveDocumentSimulation()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, SafeState() with { SensitiveDocumentSimulationActive = true });

        Assert.IsTrue(report.Passed, string.Join("; ", report.FailedChecks));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsSensitiveRealPilot() =>
        AssertGateFails(SafeState() with { SensitiveSiteRealPilotActive = true });

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsSensitiveRealDocuments()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, SafeState() with { SensitiveDocumentRealActive = true });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "sensitive real documents blocked");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsSensitiveIrreversibleActions() =>
        AssertGateFails(SafeState() with { SensitiveSiteIrreversibleActionActive = true });

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsSensitiveDocumentContentCapture()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, SafeState() with { SensitiveDocumentContentCaptureEnabled = true });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "sensitive document content capture blocked");
    }

    [TestMethod]
    public void BrowserSensitiveReadOnlySimulationLiveFiscalFixturePasses()
    {
        if (Environment.GetEnvironmentVariable("ONEBRAIN_RUN_SENSITIVE_READONLY_SIM_TESTS") != "1")
            Assert.Inconclusive("Set ONEBRAIN_RUN_SENSITIVE_READONLY_SIM_TESTS=1 to run sensitive read-only simulation fixture.");

        var fixture = BrowserSensitiveReadOnlySimulationFixtureServer.FiscalLocal();
        var result = ReadOnlyResult(requestMutation: r => r with { SiteUri = new Uri(fixture.BaseUri, "/fiscal/dashboard") });

        Assert.IsTrue(result.AllowsDone, result.Reason);
    }

    [TestMethod]
    public void BrowserSensitiveReadOnlySimulationLiveBlocksSubmit()
    {
        if (Environment.GetEnvironmentVariable("ONEBRAIN_RUN_SENSITIVE_READONLY_SIM_TESTS") != "1")
            Assert.Inconclusive("Set ONEBRAIN_RUN_SENSITIVE_READONLY_SIM_TESTS=1 to run sensitive read-only simulation fixture.");

        var result = ReadOnlyResult(requestMutation: r => r with { SubmitBlocked = false });

        Assert.AreEqual(BrowserSensitiveSimulationStatus.Blocked, result.Status);
    }

    [TestMethod]
    public void BrowserSensitiveDocumentSimulationLiveDownloadsSyntheticDocument()
    {
        if (Environment.GetEnvironmentVariable("ONEBRAIN_RUN_SENSITIVE_DOCUMENT_SIM_TESTS") != "1")
            Assert.Inconclusive("Set ONEBRAIN_RUN_SENSITIVE_DOCUMENT_SIM_TESTS=1 to run sensitive document simulation fixture.");

        var result = DocumentResultAsync().GetAwaiter().GetResult();

        Assert.IsTrue(result.DownloadResult?.AllowsDone);
    }

    [TestMethod]
    public void BrowserSensitiveDocumentSimulationLiveUploadsSyntheticDocument()
    {
        if (Environment.GetEnvironmentVariable("ONEBRAIN_RUN_SENSITIVE_DOCUMENT_SIM_TESTS") != "1")
            Assert.Inconclusive("Set ONEBRAIN_RUN_SENSITIVE_DOCUMENT_SIM_TESTS=1 to run sensitive document simulation fixture.");

        var result = DocumentResultAsync().GetAwaiter().GetResult();

        Assert.IsTrue(result.UploadResult?.AllowsDone);
    }

    private static void AssertReadOnlyBlocks(Func<BrowserSensitiveReadOnlySimulationRequest, BrowserSensitiveReadOnlySimulationRequest> mutation)
    {
        var result = ReadOnlyResult(requestMutation: mutation);

        Assert.AreEqual(BrowserSensitiveSimulationStatus.Blocked, result.Status);
    }

    private static void AssertGateFails(BrowserRuntimeObservedState state)
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, state);

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "sensitive sites policy defined");
    }

    private static BrowserSensitiveReadOnlySimulationResult ReadOnlyResult(Func<BrowserSensitiveReadOnlySimulationRequest, BrowserSensitiveReadOnlySimulationRequest>? requestMutation = null)
    {
        var fixture = BrowserSensitiveReadOnlySimulationFixtureServer.FiscalLocal();
        var request = new BrowserSensitiveReadOnlySimulationRequest(
            "run-sensitive-readonly",
            "action-sensitive-readonly",
            "corr-sensitive-readonly",
            new Uri(fixture.BaseUri, "/fiscal/dashboard"),
            SensitiveSiteCategory.Fiscal,
            ["approval-sensitive-readonly"],
            BrowserVaultMinimalM23Tests.GateReport(SafeState()),
            DashboardVisible: true,
            StatusVisible: true,
            ReadOnlyGuardActive: true,
            SubmitBlocked: true,
            PaymentBlocked: true,
            SigningBlocked: true,
            DeleteBlocked: true,
            SemanticProofPresent: true);
        request = requestMutation?.Invoke(request) ?? request;
        return new BrowserSensitiveReadOnlySimulationRunner().Run(request, new BrowserSensitiveReadOnlySimulationPolicy(Policy(request.SiteUri.Host), RequireApproval: true, RequireGate: true, RequireSemanticProof: true, RequireReadOnlyGuard: true));
    }

    private static async Task<BrowserSensitiveDocumentSimulationResult> DocumentResultAsync(Func<BrowserSensitiveDocumentSimulationRequest, BrowserSensitiveDocumentSimulationRequest>? requestMutation = null)
    {
        using var downloadRoot = BrowserVaultMinimalM23Tests.TempDir();
        using var uploadRoot = BrowserVaultMinimalM23Tests.TempDir();
        using var materializedRoot = BrowserVaultMinimalM23Tests.TempDir();
        using var server = await BrowserSafeUploadM27Tests.BrowserSafeUploadFixtureServer.StartAsync();

        var materialized = Materialize(materializedRoot.Path, "synthetic-sensitive-simulation.pdf", "%PDF-1.4 synthetic M34A fixture");
        var uploadFile = Materialize(uploadRoot.Path, "synthetic-sensitive-simulation.pdf", "%PDF-1.4 synthetic upload fixture");
        var siteUri = new Uri("http://127.0.0.1/fiscal/document-page");
        var consent = Consent();
        var gate = BrowserVaultMinimalM23Tests.GateReport(SafeState() with
        {
            SensitiveDocumentSimulationActive = true,
            SafeUploadState = BrowserRuntimeUploadState.SafeUploadActive,
            SafeUploadAllowlistValid = true,
            SafeUploadApprovalPresent = true,
            SafeUploadControlledRoot = true,
            SafeUploadHashRequired = true,
            DownloadState = BrowserRuntimeDownloadState.SafeDownloadActive,
            SafeDownloadAllowlistValid = true,
            SafeDownloadQuarantineEnabled = true,
            SafeDownloadHashRequired = true
        });
        var uploadApproval = new BrowserSafeUploadApproval($"approval-{Guid.NewGuid():N}", "core-policy", DateTimeOffset.UtcNow, Authoritative: true, Redacted: true);
        var request = new BrowserSensitiveDocumentSimulationRequest(
            "run-sensitive-document",
            "action-sensitive-document",
            "corr-sensitive-document",
            siteUri,
            ["approval-sensitive-download"],
            ["approval-sensitive-upload"],
            gate,
            new BrowserSafeDownloadRequest("run-sensitive-document", "action-download", "corr-sensitive-document", "session-sensitive-document", new Uri("http://127.0.0.1/fiscal/download-synthetic-document"), "synthetic-sensitive-simulation.pdf", "application/pdf", null),
            materialized,
            new BrowserSafeUploadRequest("run-sensitive-document", "action-upload", "corr-sensitive-document", "session-sensitive-document", server.Url("/upload"), uploadFile, "synthetic-sensitive-simulation.pdf", "application/pdf", consent, gate, uploadApproval),
            DocumentContentCaptureAttempted: false,
            SubmitAfterUploadAttempted: false,
            PaymentAttempted: false,
            SigningAttempted: false,
            DeleteAttempted: false,
            FinalStatusVisible: true,
            SemanticProofPresent: true);
        request = requestMutation?.Invoke(request) ?? request;
        var downloadPolicy = new BrowserSafeDownloadPolicy(downloadRoot.Path, Set("127.0.0.1"), Set(".pdf", ".json", ".csv"), Set("application/pdf", "application/json", "text/csv"), 1024 * 1024, true, true, false, false);
        var uploadPolicy = new BrowserSafeUploadPolicy(uploadRoot.Path, Set("127.0.0.1"), Set("/upload"), Set(".pdf", ".json", ".csv", ".txt"), Set("application/pdf", "application/json", "text/csv", "text/plain"), 1024 * 1024, true, true, false, false);

        return await new BrowserSensitiveDocumentSimulationRunner().RunAsync(request, new BrowserSensitiveDocumentSimulationPolicy(Policy(request.SiteUri.Host), downloadPolicy, uploadPolicy, true, true, true));
    }

    private static SensitiveSitePolicyDecision SensitiveSitePolicyEvaluatorFor(SensitiveSiteActionKind action)
    {
        var request = new SensitiveSitePolicyRequest("run-sensitive", "action-sensitive", "corr-sensitive", new Uri("http://127.0.0.1/fiscal/dashboard"), action, true, ["approval-1", "approval-2"], ["evidence-sensitive"], new SensitiveSitePolicyContext(true, SensitiveSiteHumanApprovalRequirement.DoubleApprovalRequired, BrowserVaultMinimalM23Tests.GateReport(SafeState()), BrowserRuntimeProfileState.UserProfileControlledWithConsent, BrowserRuntimeVaultState.MinimalSandboxActive, BrowserRuntimeReplayState.SafeModeReadOnlyActive, BrowserRuntimeRecorderState.ReadOnlyPrototypeActive, BrowserNetworkCaptureMode.MetadataOnly, false, false, false, true, true));
        return new SensitiveSitePolicyEvaluator().Evaluate(request, Policy("127.0.0.1"));
    }

    private static BrowserConsentGrant Consent()
    {
        var service = new BrowserConsentService();
        var request = service.CreateRequest(BrowserConsentCapability.SecretUse, BrowserConsentScope.Session, "run-sensitive-document", "action-sensitive-document", "corr-sensitive-document", "core-test", "sensitive document simulation", TimeSpan.FromMinutes(5));
        return service.Decide(request, BrowserConsentStatus.Granted, "core-test", $"proof-{Guid.NewGuid():N}", DateTimeOffset.UtcNow).Grant!;
    }

    private static SensitiveSitePolicy Policy(string host) =>
        SensitiveSitePolicy.Default(new Dictionary<string, SensitiveSiteClassification>(StringComparer.OrdinalIgnoreCase)
        {
            [host] = new(host, SensitiveSiteCategory.Fiscal, SensitiveSiteRiskLevel.Critical, TestOnlySimulation: true),
            ["127.0.0.1"] = new("127.0.0.1", SensitiveSiteCategory.Fiscal, SensitiveSiteRiskLevel.Critical, TestOnlySimulation: true)
        });

    private static BrowserRuntimeObservedState SafeState() =>
        BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with
        {
            SensitiveSitesPolicyDefined = true,
            SensitiveSiteReadOnlySimulationAllowed = true,
            ProfileState = BrowserRuntimeProfileState.UserProfileControlledWithConsent,
            ControlledProfileConsentValid = true,
            RecorderState = BrowserRuntimeRecorderState.ReadOnlyPrototypeActive,
            ReplayState = BrowserRuntimeReplayState.SafeModeReadOnlyActive
        };

    private static string Materialize(string root, string fileName, string content)
    {
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    private static IReadOnlySet<string> Set(params string[] values) =>
        new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
}
