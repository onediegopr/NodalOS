using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserSensitiveReadOnlySimulationRunner
{
    private readonly SensitiveSitePolicyEvaluator _policyEvaluator = new();

    public BrowserSensitiveReadOnlySimulationResult Run(BrowserSensitiveReadOnlySimulationRequest request, BrowserSensitiveReadOnlySimulationPolicy policy)
    {
        var decision = _policyEvaluator.Evaluate(PolicyRequest(request, SensitiveSiteActionKind.ReadOnlyView), policy.SensitiveSitePolicy);
        if (!decision.Allowed)
            return Block(request, decision, "sensitive read-only simulation blocked by policy");
        if (policy.RequireApproval && request.ApprovalRefs.Count == 0)
            return Block(request, decision, "sensitive read-only simulation requires approval");
        if (policy.RequireGate && request.GateReport?.Passed != true)
            return Block(request, decision, "sensitive read-only simulation requires passing gate");
        if (policy.RequireReadOnlyGuard && !request.ReadOnlyGuardActive)
            return Block(request, decision, "sensitive read-only simulation requires read-only guard");
        if (!request.SubmitBlocked || !request.PaymentBlocked || !request.SigningBlocked || !request.DeleteBlocked)
            return Block(request, decision, "sensitive read-only simulation requires irreversible actions blocked");
        if (policy.RequireSemanticProof && (!request.SemanticProofPresent || !request.DashboardVisible || !request.StatusVisible))
            return Block(request, decision, "sensitive read-only simulation requires semantic proof");

        var evidenceRefs = new[] { $"evidence-sensitive-readonly-{request.RunId}", $"proof-sensitive-readonly-{request.CorrelationId}" };
        var verification = Verification(request, "sensitive read-only dashboard/status verified", evidenceRefs);
        var audit = Audit(request, "Verified", "sensitive read-only simulation verified", decision);
        var evidence = new BrowserSensitiveReadOnlySimulationEvidence(
            request.SimulatedCategory,
            decision.Classification?.RiskLevel ?? SensitiveSiteRiskLevel.Critical,
            decision.ApprovalRequirement,
            request.ApprovalRefs,
            evidenceRefs,
            decision.WhatRemainsBlocked,
            CookiesSecretsBodiesExcluded: true,
            Redacted: true);
        var steps = new[]
        {
            new BrowserSensitiveReadOnlySimulationStep("step-sensitive-policy", SensitiveSiteActionKind.ReadOnlyView, "Executed", BrowserVerificationStatus.Verified, evidenceRefs, [audit.EventId], "")
        };
        return new BrowserSensitiveReadOnlySimulationResult(BrowserSensitiveSimulationStatus.Verified, decision, steps, evidence, verification, audit, "sensitive read-only simulation verified", Redacted: true);
    }

    private BrowserSensitiveReadOnlySimulationResult Block(BrowserSensitiveReadOnlySimulationRequest request, SensitiveSitePolicyDecision decision, string reason)
    {
        var audit = Audit(request, "Blocked", reason, decision);
        var evidence = new BrowserSensitiveReadOnlySimulationEvidence(
            request.SimulatedCategory,
            decision.Classification?.RiskLevel ?? SensitiveSiteRiskLevel.Critical,
            decision.ApprovalRequirement,
            request.ApprovalRefs,
            [],
            decision.WhatRemainsBlocked,
            CookiesSecretsBodiesExcluded: true,
            Redacted: true);
        var steps = new[]
        {
            new BrowserSensitiveReadOnlySimulationStep("step-sensitive-policy", SensitiveSiteActionKind.ReadOnlyView, "Blocked", BrowserVerificationStatus.Failed, [], [audit.EventId], BrowserCredentialRedactor.Redact(reason))
        };
        return new BrowserSensitiveReadOnlySimulationResult(BrowserSensitiveSimulationStatus.Blocked, decision, steps, evidence, null, audit, BrowserCredentialRedactor.Redact(reason), Redacted: true);
    }

    private static SensitiveSitePolicyRequest PolicyRequest(BrowserSensitiveReadOnlySimulationRequest request, SensitiveSiteActionKind action) =>
        new(
            request.RunId,
            request.ActionId,
            request.CorrelationId,
            request.SiteUri,
            action,
            DataSensitive: true,
            request.ApprovalRefs,
            ["evidence-sensitive-policy"],
            PolicyContext(request.GateReport));

    private static SensitiveSitePolicyContext PolicyContext(BrowserRuntimePhaseCloseReport? gate)
    {
        var state = gate?.ObservedState;
        return new SensitiveSitePolicyContext(
            UserConsentValid: true,
            ApprovalProvided: SensitiveSiteHumanApprovalRequirement.SingleApprovalRequired,
            gate,
            state?.ProfileState ?? BrowserRuntimeProfileState.UserProfileControlledWithConsent,
            state?.VaultState ?? BrowserRuntimeVaultState.MinimalSandboxActive,
            state?.ReplayState ?? BrowserRuntimeReplayState.SafeModeReadOnlyActive,
            state?.RecorderState ?? BrowserRuntimeRecorderState.ReadOnlyPrototypeActive,
            state?.NetworkCaptureMode ?? BrowserNetworkCaptureMode.MetadataOnly,
            state?.RequestBodyCaptureSupported ?? false,
            state?.ResponseBodyCaptureSupported ?? false,
            state?.SensitiveHeaderValueCaptureSupported ?? false,
            SafeDownloadAvailable: true,
            SafeUploadAvailable: true);
    }

    private static BrowserVerification Verification(BrowserSensitiveReadOnlySimulationRequest request, string outcome, IReadOnlyList<string> evidenceRefs)
    {
        var context = new BrowserTargetContext(
            request.RunId,
            "chrome-cdp",
            "session-sensitive-simulation",
            null,
            null,
            "target-sensitive-simulation",
            "page-sensitive-simulation",
            null,
            "main",
            null,
            new Uri($"{request.SiteUri.Scheme}://{request.SiteUri.Host}{request.SiteUri.AbsolutePath}"),
            "Sensitive Read-Only Simulation",
            0,
            BrowserTargetContext.CreateLivenessToken("target-sensitive-simulation", "main", 0),
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            "complete",
            BrowserTargetSource.Cdp);
        return new BrowserVerification(
            $"verification-{Guid.NewGuid():N}",
            request.RunId,
            "step-sensitive-readonly",
            request.ActionId,
            context,
            new BrowserExpectedOutcome(outcome, null, "read-only guard active", null),
            null,
            null,
            BrowserVerificationStatus.Verified,
            0.96,
            evidenceRefs,
            null,
            DateTimeOffset.UtcNow,
            evidenceRefs.Where(e => e.StartsWith("proof-", StringComparison.OrdinalIgnoreCase)).ToArray());
    }

    private static BrowserAuditLedgerEvent Audit(BrowserSensitiveReadOnlySimulationRequest request, string decision, string reason, SensitiveSitePolicyDecision policyDecision) =>
        BrowserPersistentAuditLedger.Create(
            BrowserAuditLedgerEventKind.PolicyBlocked,
            request.RunId,
            request.ActionId,
            request.CorrelationId,
            "profile-controlled",
            "session-sensitive-simulation",
            null,
            null,
            null,
            decision,
            BrowserCredentialRedactor.Redact(reason),
            new Dictionary<string, string>
            {
                ["category"] = request.SimulatedCategory.ToString(),
                ["risk"] = (policyDecision.Classification?.RiskLevel ?? SensitiveSiteRiskLevel.Critical).ToString(),
                ["action"] = SensitiveSiteActionKind.ReadOnlyView.ToString(),
                ["approvalRefs"] = request.ApprovalRefs.Count.ToString(),
                ["blocked"] = string.Join("|", policyDecision.WhatRemainsBlocked)
            });
}

public sealed class BrowserSensitiveDocumentSimulationRunner
{
    private readonly SensitiveSitePolicyEvaluator _policyEvaluator = new();
    private readonly BrowserSafeDownloadManager _downloadManager = new();
    private readonly BrowserSafeUploadManager _uploadManager = new();

    public async Task<BrowserSensitiveDocumentSimulationResult> RunAsync(BrowserSensitiveDocumentSimulationRequest request, BrowserSensitiveDocumentSimulationPolicy policy, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        var downloadDecision = _policyEvaluator.Evaluate(PolicyRequest(request, SensitiveSiteActionKind.DownloadDocument, request.DownloadApprovalRefs), policy.SensitiveSitePolicy);
        var uploadDecision = _policyEvaluator.Evaluate(PolicyRequest(request, SensitiveSiteActionKind.UploadDocument, request.UploadApprovalRefs), policy.SensitiveSitePolicy);
        if (!downloadDecision.Allowed)
            return Block(request, downloadDecision, uploadDecision, null, null, "sensitive document download blocked by policy");
        if (!uploadDecision.Allowed)
            return Block(request, downloadDecision, uploadDecision, null, null, "sensitive document upload blocked by policy");
        if (policy.RequireApprovalForDownload && request.DownloadApprovalRefs.Count == 0)
            return Block(request, downloadDecision, uploadDecision, null, null, "sensitive document download requires approval");
        if (policy.RequireApprovalForUpload && request.UploadApprovalRefs.Count == 0)
            return Block(request, downloadDecision, uploadDecision, null, null, "sensitive document upload requires approval");
        if (policy.BlockDocumentContentCapture && request.DocumentContentCaptureAttempted)
            return Block(request, downloadDecision, uploadDecision, null, null, "sensitive document content capture blocked");
        if (request.SubmitAfterUploadAttempted || request.PaymentAttempted || request.SigningAttempted || request.DeleteAttempted)
            return Block(request, downloadDecision, uploadDecision, null, null, "sensitive irreversible document action blocked");

        var download = _downloadManager.ValidateMaterializedDownload(request.DownloadRequest, policy.DownloadPolicy, request.MaterializedDownloadPath);
        if (!download.AllowsDone)
            return Block(request, downloadDecision, uploadDecision, download, null, "safe download verification failed");

        var upload = await _uploadManager.UploadAsync(request.UploadRequest, policy.UploadPolicy, httpClient, cancellationToken);
        if (!upload.AllowsDone)
            return Block(request, downloadDecision, uploadDecision, download, upload, "safe upload verification failed");
        if (!request.FinalStatusVisible || !request.SemanticProofPresent)
            return Block(request, downloadDecision, uploadDecision, download, upload, "sensitive document simulation requires final semantic proof");

        var evidenceRefs = new[] { $"evidence-sensitive-document-{request.RunId}", $"proof-sensitive-document-{request.CorrelationId}" }
            .Concat(download.EvidenceRefs)
            .Concat(upload.EvidenceRefs)
            .ToArray();
        var verification = Verification(request, "sensitive document download/upload/status verified", evidenceRefs);
        var audit = Audit(request, "Verified", "sensitive document simulation verified", download, upload);
        var evidence = Evidence(request, downloadDecision, download, upload, evidenceRefs, DocumentContentCaptured: false);
        return new BrowserSensitiveDocumentSimulationResult(BrowserSensitiveSimulationStatus.Verified, downloadDecision, uploadDecision, download, upload, evidence, verification, audit, "sensitive document simulation verified", Redacted: true);
    }

    private BrowserSensitiveDocumentSimulationResult Block(BrowserSensitiveDocumentSimulationRequest request, SensitiveSitePolicyDecision downloadDecision, SensitiveSitePolicyDecision uploadDecision, BrowserSafeDownloadResult? download, BrowserSafeUploadResult? upload, string reason)
    {
        var audit = Audit(request, "Blocked", reason, download, upload);
        var evidence = Evidence(request, downloadDecision, download, upload, [], request.DocumentContentCaptureAttempted);
        return new BrowserSensitiveDocumentSimulationResult(BrowserSensitiveSimulationStatus.Blocked, downloadDecision, uploadDecision, download, upload, evidence, null, audit, BrowserCredentialRedactor.Redact(reason), Redacted: true);
    }

    private static BrowserSensitiveDocumentSimulationEvidence Evidence(BrowserSensitiveDocumentSimulationRequest request, SensitiveSitePolicyDecision decision, BrowserSafeDownloadResult? download, BrowserSafeUploadResult? upload, IReadOnlyList<string> evidenceRefs, bool DocumentContentCaptured) =>
        new(
            decision.Classification?.Category ?? SensitiveSiteCategory.Fiscal,
            decision.Classification?.RiskLevel ?? SensitiveSiteRiskLevel.Critical,
            [.. request.DownloadApprovalRefs, .. request.UploadApprovalRefs],
            download?.Artifact,
            upload?.Artifact,
            evidenceRefs,
            decision.ReasonCodes.Select(r => r.ToString()).ToArray(),
            DoubleApprovalModeled: true,
            DocumentContentCaptured,
            CookiesSecretsBodiesExcluded: true,
            Redacted: true);

    private static SensitiveSitePolicyRequest PolicyRequest(BrowserSensitiveDocumentSimulationRequest request, SensitiveSiteActionKind action, IReadOnlyList<string> approvals) =>
        new(
            request.RunId,
            request.ActionId,
            request.CorrelationId,
            request.SiteUri,
            action,
            DataSensitive: true,
            approvals,
            ["evidence-sensitive-document-policy"],
            new SensitiveSitePolicyContext(
                UserConsentValid: true,
                ApprovalProvided: SensitiveSiteHumanApprovalRequirement.SingleApprovalRequired,
                request.GateReport,
                request.GateReport?.ObservedState?.ProfileState ?? BrowserRuntimeProfileState.UserProfileControlledWithConsent,
                request.GateReport?.ObservedState?.VaultState ?? BrowserRuntimeVaultState.MinimalSandboxActive,
                request.GateReport?.ObservedState?.ReplayState ?? BrowserRuntimeReplayState.SafeModeReadOnlyActive,
                request.GateReport?.ObservedState?.RecorderState ?? BrowserRuntimeRecorderState.ReadOnlyPrototypeActive,
                request.GateReport?.ObservedState?.NetworkCaptureMode ?? BrowserNetworkCaptureMode.MetadataOnly,
                request.GateReport?.ObservedState?.RequestBodyCaptureSupported ?? false,
                request.GateReport?.ObservedState?.ResponseBodyCaptureSupported ?? false,
                request.GateReport?.ObservedState?.SensitiveHeaderValueCaptureSupported ?? false,
                SafeDownloadAvailable: true,
                SafeUploadAvailable: true));

    private static BrowserVerification Verification(BrowserSensitiveDocumentSimulationRequest request, string outcome, IReadOnlyList<string> evidenceRefs)
    {
        var context = new BrowserTargetContext(
            request.RunId,
            "chrome-cdp",
            request.DownloadRequest.SessionId,
            null,
            null,
            "target-sensitive-document",
            "page-sensitive-document",
            null,
            "main",
            null,
            new Uri($"{request.SiteUri.Scheme}://{request.SiteUri.Host}{request.SiteUri.AbsolutePath}"),
            "Sensitive Document Simulation",
            0,
            BrowserTargetContext.CreateLivenessToken("target-sensitive-document", "main", 0),
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            "complete",
            BrowserTargetSource.Cdp);
        return new BrowserVerification(
            $"verification-{Guid.NewGuid():N}",
            request.RunId,
            "step-sensitive-document",
            request.ActionId,
            context,
            new BrowserExpectedOutcome(outcome, null, "document status verified", null),
            null,
            null,
            BrowserVerificationStatus.Verified,
            0.96,
            evidenceRefs,
            null,
            DateTimeOffset.UtcNow,
            evidenceRefs.Where(e => e.StartsWith("proof-", StringComparison.OrdinalIgnoreCase)).ToArray());
    }

    private static BrowserAuditLedgerEvent Audit(BrowserSensitiveDocumentSimulationRequest request, string decision, string reason, BrowserSafeDownloadResult? download, BrowserSafeUploadResult? upload) =>
        BrowserPersistentAuditLedger.Create(
            BrowserAuditLedgerEventKind.PolicyBlocked,
            request.RunId,
            request.ActionId,
            request.CorrelationId,
            "profile-controlled",
            request.DownloadRequest.SessionId,
            null,
            null,
            null,
            decision,
            BrowserCredentialRedactor.Redact(reason),
            new Dictionary<string, string>
            {
                ["category"] = SensitiveSiteCategory.Fiscal.ToString(),
                ["action"] = "SensitiveDocumentSimulation",
                ["downloadSha256"] = download?.Artifact?.Sha256 ?? "[NOT_CAPTURED]",
                ["uploadSha256"] = upload?.Artifact?.Sha256 ?? "[NOT_CAPTURED]",
                ["downloadMime"] = download?.Artifact?.MimeType ?? "[NOT_CAPTURED]",
                ["uploadMime"] = upload?.Artifact?.MimeType ?? "[NOT_CAPTURED]",
                ["downloadSize"] = download?.Artifact?.SizeBytes.ToString() ?? "0",
                ["uploadSize"] = upload?.Artifact?.SizeBytes.ToString() ?? "0"
            });
}
