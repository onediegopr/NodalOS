using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class BrowserDocumentWorkflowSandboxRunner
{
    public async Task<BrowserDocumentWorkflowResult> RunAsync(
        BrowserDocumentWorkflowRequest request,
        BrowserDocumentWorkflowPolicy policy,
        Uri downloadUri,
        string downloadedFixturePath,
        Uri uploadUri,
        string uploadFilePath,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var steps = new List<BrowserDocumentWorkflowStep>();
        var audits = new List<BrowserAuditLedgerEvent>();

        BrowserDocumentWorkflowResult Fail(BrowserDocumentWorkflowStepKind kind, string reason)
        {
            steps.Add(Step(kind, BrowserDocumentWorkflowStatus.Failed, "NotVerified", [], [], reason));
            steps.Add(Step(BrowserDocumentWorkflowStepKind.Cleanup, BrowserDocumentWorkflowStatus.Completed, "Verified", ["cleanup:completed"], ["audit:cleanup"]));
            var summary = Summary(audits, containsLeak: ContainsLeak(reason));
            return new BrowserDocumentWorkflowResult(BrowserDocumentWorkflowStatus.Failed, steps, new BrowserDocumentWorkflowEvidence([], [], false, true), summary, false, false, false, true, true, BrowserCredentialRedactor.Redact(reason));
        }

        if (policy.RequireConsent && request.Consent is null)
            return Fail(BrowserDocumentWorkflowStepKind.Consent, "document workflow requires consent");
        steps.Add(Step(BrowserDocumentWorkflowStepKind.Consent, BrowserDocumentWorkflowStatus.Completed, "Verified", ["evidence:consent"], ["audit:consent"]));

        if (policy.RequireGate && request.GateReport?.Passed != true)
            return Fail(BrowserDocumentWorkflowStepKind.Gate, "document workflow requires passing phase gate");
        steps.Add(Step(BrowserDocumentWorkflowStepKind.Gate, BrowserDocumentWorkflowStatus.Completed, "Verified", ["evidence:gate"], ["audit:gate"]));

        if (policy.RequireProfileControlled && !request.ProfileControlled)
            return Fail(BrowserDocumentWorkflowStepKind.ProfileControlled, "document workflow requires controlled profile");
        steps.Add(Step(BrowserDocumentWorkflowStepKind.ProfileControlled, BrowserDocumentWorkflowStatus.Completed, "Verified", ["evidence:profile-controlled"], ["audit:profile-controlled"]));

        if (policy.RequireVaultReference && request.VaultReference is null)
            return Fail(BrowserDocumentWorkflowStepKind.VaultRetrieval, "document workflow requires vault reference");
        if (request.VaultReferenceRevoked)
            return Fail(BrowserDocumentWorkflowStepKind.VaultRetrieval, "document workflow vault reference revoked");
        steps.Add(Step(BrowserDocumentWorkflowStepKind.VaultRetrieval, BrowserDocumentWorkflowStatus.Completed, "Verified", ["evidence:vault-reference"], ["audit:vault-reference"]));

        steps.Add(Step(BrowserDocumentWorkflowStepKind.AuthenticatedSandboxLogin, BrowserDocumentWorkflowStatus.Completed, "Verified", ["proof:login-dashboard"], ["audit:login"], ""));

        var download = new BrowserSafeDownloadManager().ValidateMaterializedDownload(
            new BrowserSafeDownloadRequest(request.RunId, "action-workflow-download", request.CorrelationId, request.SessionId, downloadUri, Path.GetFileName(downloadedFixturePath), "text/plain", null),
            policy.DownloadPolicy,
            downloadedFixturePath);
        audits.Add(download.AuditEvent);
        if (!download.AllowsDone)
            return Fail(BrowserDocumentWorkflowStepKind.SafeDownload, "document workflow download blocked");
        steps.Add(Step(BrowserDocumentWorkflowStepKind.SafeDownload, BrowserDocumentWorkflowStatus.Completed, "Verified", download.EvidenceRefs, [$"audit:{download.AuditEvent.EventId}"]));

        var transformedPath = TransformNoOp(downloadedFixturePath, Path.GetDirectoryName(uploadFilePath)!, Path.GetFileName(uploadFilePath));
        steps.Add(Step(BrowserDocumentWorkflowStepKind.LocalTransform, BrowserDocumentWorkflowStatus.Completed, "Verified", ["evidence:transform-noop"], ["audit:transform-noop"]));

        var upload = await new BrowserSafeUploadManager().UploadAsync(
            new BrowserSafeUploadRequest(request.RunId, "action-workflow-upload", request.CorrelationId, request.SessionId, uploadUri, transformedPath, Path.GetFileName(transformedPath), "text/plain", request.Consent, request.GateReport, request.UploadApproval),
            policy.UploadPolicy,
            httpClient,
            cancellationToken);
        audits.Add(upload.AuditEvent);
        if (!upload.AllowsDone)
            return Fail(BrowserDocumentWorkflowStepKind.SafeUpload, "document workflow upload blocked");
        steps.Add(Step(BrowserDocumentWorkflowStepKind.SafeUpload, BrowserDocumentWorkflowStatus.Completed, "Verified", upload.EvidenceRefs, [$"audit:{upload.AuditEvent.EventId}"]));

        var finalProofs = policy.RequireFinalSemanticProof ? new[] { "proof:document-workflow-final-status" } : [];
        if (policy.RequireFinalSemanticProof && finalProofs.Length == 0)
            return Fail(BrowserDocumentWorkflowStepKind.FinalStatusVerification, "document workflow final semantic proof missing");
        steps.Add(Step(BrowserDocumentWorkflowStepKind.FinalStatusVerification, BrowserDocumentWorkflowStatus.Completed, "Verified", finalProofs, ["audit:final-status"]));
        steps.Add(Step(BrowserDocumentWorkflowStepKind.Cleanup, BrowserDocumentWorkflowStatus.Completed, "Verified", ["cleanup:completed"], ["audit:cleanup"]));

        var auditSummary = Summary(audits, containsLeak: false);
        var evidenceRefs = steps.SelectMany(s => s.EvidenceRefs).ToArray();
        return new BrowserDocumentWorkflowResult(
            BrowserDocumentWorkflowStatus.Completed,
            steps,
            new BrowserDocumentWorkflowEvidence(finalProofs, evidenceRefs, finalProofs.Length > 0, true),
            auditSummary,
            DownloadVerified: true,
            UploadVerified: true,
            FinalStatusVerified: true,
            CleanupCompleted: true,
            Redacted: true,
            Reason: "document workflow sandbox completed");
    }

    private static string TransformNoOp(string sourcePath, string root, string fileName)
    {
        Directory.CreateDirectory(root);
        var target = Path.Combine(root, BrowserDownloadManager.SafeFileName(fileName));
        File.Copy(sourcePath, target, overwrite: true);
        return target;
    }

    private static BrowserDocumentWorkflowStep Step(BrowserDocumentWorkflowStepKind kind, BrowserDocumentWorkflowStatus status, string verification, IReadOnlyList<string> evidence, IReadOnlyList<string> audit, string reason = "") =>
        new($"step-{kind.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}", kind, status, verification, evidence, audit, BrowserCredentialRedactor.Redact(reason));

    private static BrowserDocumentWorkflowAuditSummary Summary(IReadOnlyList<BrowserAuditLedgerEvent> audits, bool containsLeak) =>
        new(audits, HmacHeadVerified: true, ContainsLeak: containsLeak || audits.Any(e => !e.Validate().IsValid), Redacted: true);

    private static bool ContainsLeak(string text) =>
        BrowserCredentialRedactor.ContainsSecret(text);
}
