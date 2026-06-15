using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed record BrowserAuthenticatedSandboxRequest(
    string RunId,
    string ActionId,
    string CorrelationId,
    BrowserVaultSecretReference UserReference,
    BrowserVaultSecretReference PassReference,
    BrowserConsentGrant? Consent,
    BrowserRuntimePhaseCloseReport GateReport,
    Uri LoginUrl,
    string UserSelector,
    string PassSelector,
    string SubmitSelector,
    string DashboardProofText);

public sealed record BrowserAuthenticatedSandboxResult(
    bool Executed,
    BrowserVerification Verification,
    IReadOnlyList<BrowserVaultAuditEvent> VaultAuditEvents,
    IReadOnlyList<string> EvidenceRefs,
    bool CookieValuesExposed,
    bool SessionStorageExposed,
    bool SecretValueExposed,
    string Reason)
{
    public bool AllowsStepDone =>
        Executed &&
        Verification.Status == BrowserVerificationStatus.Verified &&
        Verification.HasSemanticProof &&
        !CookieValuesExposed &&
        !SessionStorageExposed &&
        !SecretValueExposed;
}

public sealed class BrowserAuthenticatedSandboxScenario
{
    public async Task<BrowserAuthenticatedSandboxResult> RunAsync(
        ChromeCdpPageSession page,
        BrowserVaultMinimalSandboxProvider vault,
        BrowserAuthenticatedSandboxRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.GateReport.Passed)
            return Blocked(request, "phase gate failed");

        var policy = BrowserVaultAccessPolicy.SandboxRetrieval;
        using var user = await RetrieveAsync(vault, request.UserReference, request, policy, cancellationToken).ConfigureAwait(false);
        using var pass = await RetrieveAsync(vault, request.PassReference, request, policy, cancellationToken).ConfigureAwait(false);
        if (user is null || pass is null)
            return Blocked(request, "vault boundary denied");

        await page.NavigateAsync(request.LoginUrl, cancellationToken).ConfigureAwait(false);
        var target = await page.GetCurrentTargetContextAsync(request.RunId, cancellationToken).ConfigureAwait(false);
        var userAction = TypeAction(request, target, "step-sandbox-user", request.UserSelector, user.RequireValue());
        var passAction = TypeAction(request, target, "step-sandbox-pass", request.PassSelector, pass.RequireValue());
        var submitAction = ClickAction(request, target);

        var userResult = await page.ExecuteActionAsync(userAction, cancellationToken).ConfigureAwait(false);
        var passResult = await page.ExecuteActionAsync(passAction, cancellationToken).ConfigureAwait(false);
        var submitResult = await page.ExecuteActionAsync(submitAction, cancellationToken).ConfigureAwait(false);
        await page.SubmitFormForCoreBoundaryAsync("form", cancellationToken).ConfigureAwait(false);
        await page.DrainEventsAsync(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        var verification = await page.VerifyAsync(submitAction, cancellationToken: cancellationToken).ConfigureAwait(false);

        var exposed = PublicSurfaceContainsMaterial(pass.RequireValue(), userResult, passResult, submitResult, verification, page.NetworkEvents);
        var executed = userResult.Executed && passResult.Executed && submitResult.Executed;
        var verified = verification.AllowsStepDone();
        return new BrowserAuthenticatedSandboxResult(
            Executed: executed,
            verification,
            [user.AuditEvent, pass.AuditEvent],
            [.. verification.ProofReferences, .. verification.EvidenceRefs],
            CookieValuesExposed: false,
            SessionStorageExposed: false,
            SecretValueExposed: exposed,
            Reason: verified && executed && !exposed
                ? "sandbox flow verified"
                : $"sandbox flow blocked: executed={executed}; verified={verified}; status={verification.Status}; failure={BrowserCredentialRedactor.Redact(verification.FailureReason ?? "none")}; exposed={exposed}");
    }

    private static async ValueTask<BrowserVaultCoreSecretHandle?> RetrieveAsync(
        BrowserVaultMinimalSandboxProvider vault,
        BrowserVaultSecretReference reference,
        BrowserAuthenticatedSandboxRequest request,
        BrowserVaultAccessPolicy policy,
        CancellationToken cancellationToken)
    {
        var retrieve = new BrowserVaultRetrieveRequest(
            RequestId: $"vault-request-{Guid.NewGuid():N}",
            request.RunId,
            request.ActionId,
            request.CorrelationId,
            "profile-controlled",
            "session-controlled",
            reference,
            request.Consent,
            request.GateReport,
            policy,
            "sandbox form fill",
            DateTimeOffset.UtcNow);
        return await vault.RetrieveForCoreBoundaryAsync(retrieve, cancellationToken).ConfigureAwait(false);
    }

    private static BrowserAuthenticatedSandboxResult Blocked(BrowserAuthenticatedSandboxRequest request, string reason)
    {
        var context = new BrowserTargetContext(
            request.RunId,
            "chrome-cdp",
            "session-controlled",
            null,
            null,
            "target-blocked",
            "page-blocked",
            null,
            "main",
            null,
            request.LoginUrl,
            "Sandbox",
            0,
            BrowserTargetContext.CreateLivenessToken("target-blocked", "main", 0),
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            "blocked",
            BrowserTargetSource.Cdp);
        var verification = new BrowserVerification(
            $"verification-{Guid.NewGuid():N}",
            request.RunId,
            "step-sandbox-blocked",
            request.ActionId,
            context,
            new BrowserExpectedOutcome("sandbox dashboard proof", null, request.DashboardProofText, null),
            null,
            null,
            BrowserVerificationStatus.Failed,
            0,
            [],
            BrowserCredentialRedactor.Redact(reason),
            DateTimeOffset.UtcNow,
            []);
        return new BrowserAuthenticatedSandboxResult(false, verification, [], [], false, false, false, BrowserCredentialRedactor.Redact(reason));
    }

    private static BrowserAction TypeAction(BrowserAuthenticatedSandboxRequest request, BrowserTargetContext target, string stepId, string selector, string value) =>
        new(
            ActionId: $"action-{Guid.NewGuid():N}",
            IdempotencyKey: $"idem-{Guid.NewGuid():N}",
            RunId: request.RunId,
            StepId: stepId,
            TargetContext: target,
            FrameId: target.FrameId,
            ActionType: BrowserActionType.TypeText,
            Target: new BrowserActionTarget(selector.TrimStart('#'), selector, selector, null),
            Input: new BrowserActionInput(value, value, HasModifyingValue: true),
            ExpectedOutcome: new BrowserExpectedOutcome("sandbox input accepted", null, null, null),
            RiskClass: BrowserRiskClass.Low,
            TimeoutMs: 8000,
            RequiresApproval: false,
            CreatedAtUtc: DateTimeOffset.UtcNow);

    private static BrowserAction ClickAction(BrowserAuthenticatedSandboxRequest request, BrowserTargetContext target) =>
        new(
            ActionId: request.ActionId,
            IdempotencyKey: $"idem-{Guid.NewGuid():N}",
            RunId: request.RunId,
            StepId: "step-sandbox-submit",
            TargetContext: target,
            FrameId: target.FrameId,
            ActionType: BrowserActionType.Click,
            Target: new BrowserActionTarget(request.SubmitSelector.TrimStart('#'), request.SubmitSelector, request.SubmitSelector, null),
            Input: null,
            ExpectedOutcome: new BrowserExpectedOutcome("sandbox dashboard visible", null, request.DashboardProofText, null),
            RiskClass: BrowserRiskClass.Low,
            TimeoutMs: 8000,
            RequiresApproval: false,
            CreatedAtUtc: DateTimeOffset.UtcNow);

    private static bool PublicSurfaceContainsMaterial(string pass, ChromeCdpActionResult userResult, ChromeCdpActionResult passResult, ChromeCdpActionResult submitResult, BrowserVerification verification, IEnumerable<BrowserNetworkCaptureEvent> networkEvents)
    {
        var text = string.Join("\n", userResult.ToString(), passResult.ToString(), submitResult.ToString(), verification.ToString(), string.Join("\n", networkEvents.Select(e => e.ToString())));
        return text.Contains(pass, StringComparison.Ordinal);
    }
}
