using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public enum BrowserRealSiteReadOnlyStatus
{
    Verified,
    Blocked,
    RequiresHuman,
    Uncertain,
    TimedOut,
    Failed,
    Skipped
}

public sealed record BrowserRealSiteReadOnlyOptions(
    string SiteName,
    Uri StartUrl,
    string ExpectedHost,
    string ExpectedText,
    Uri? PublicSearchUrl = null,
    string? SearchQuery = null,
    TimeSpan? Timeout = null);

public sealed record BrowserRealSiteBlockDetection(
    bool Blocked,
    BrowserRealSiteReadOnlyStatus Status,
    BrowserRuntimeErrorCode ErrorCode,
    string Reason)
{
    public static BrowserRealSiteBlockDetection None { get; } = new(false, BrowserRealSiteReadOnlyStatus.Verified, BrowserRuntimeErrorCode.None, "");
}

public sealed record BrowserRealSiteReadOnlyReport(
    string RunId,
    string SiteName,
    BrowserRealSiteReadOnlyStatus Status,
    BrowserRuntimeErrorCode ErrorCode,
    Uri? FinalUrl,
    string FinalTitle,
    BrowserVerificationStatus VerificationStatus,
    IReadOnlyList<BrowserEvidence> Evidence,
    IReadOnlyList<string> PolicyDecisions,
    IReadOnlyList<string> Diagnostics,
    bool CleanupCompleted,
    bool UsedServiceWorker,
    bool UsedRealProfile)
{
    public bool Success => Status == BrowserRealSiteReadOnlyStatus.Verified && VerificationStatus == BrowserVerificationStatus.Verified && Evidence.Count > 0;
}

public sealed class BrowserRealSiteReadOnlyPolicy
{
    private static readonly string[] SensitiveSelectors =
    [
        "password",
        "login",
        "signin",
        "sign-in",
        "cart",
        "checkout",
        "buy",
        "purchase",
        "favorite",
        "message",
        "account"
    ];

    public BrowserPolicyDecision EvaluateAction(BrowserAction action)
    {
        if (action.RiskClass is BrowserRiskClass.High or BrowserRiskClass.Critical || action.RequiresApproval)
            return BrowserPolicyDecision.Block(BrowserRuntimeErrorCode.ActionRejected, "real-site read-only scenario blocks sensitive or approval-gated actions");

        if (action.ActionType is BrowserActionType.Click or BrowserActionType.SelectOption)
            return BrowserPolicyDecision.Block(BrowserRuntimeErrorCode.ActionRejected, "real-site M6 blocks click/select by default; use public navigation/read verification only");

        if (action.ActionType == BrowserActionType.TypeText && action.Input is { HasModifyingValue: true })
            return BrowserPolicyDecision.Block(BrowserRuntimeErrorCode.ActionRejected, "real-site M6 does not type into live pages by default");

        var text = $"{action.Target.CandidateId} {action.Target.Selector} {action.Target.Text} {action.Target.Url}".ToLowerInvariant();
        if (SensitiveSelectors.Any(text.Contains))
            return BrowserPolicyDecision.Block(BrowserRuntimeErrorCode.ActionRejected, "target appears sensitive for read-only real-site policy");

        if (action.ActionType == BrowserActionType.Navigate && !IsPublicHttpUrl(action.Target.Url))
            return BrowserPolicyDecision.Block(BrowserRuntimeErrorCode.ActionRejected, "navigation target is not a public http/https URL");

        if (action.ActionType is BrowserActionType.Navigate or BrowserActionType.Read or BrowserActionType.WaitFor or BrowserActionType.Extract or BrowserActionType.NoOp)
            return BrowserPolicyDecision.Allow("real-site read-only action allowed");

        return BrowserPolicyDecision.Block(BrowserRuntimeErrorCode.ActionRejected, "action is not allowed in real-site read-only scenario");
    }

    public BrowserRealSiteBlockDetection DetectBlocking(BrowserObservation observation)
    {
        var url = observation.Url.ToString();
        var title = observation.Title;
        var text = observation.VisibleTextSummary;
        var combined = $"{url} {title} {text}".ToLowerInvariant();

        if (ContainsAny(combined, "captcha", "recaptcha", "hcaptcha", "verifica que eres", "verify you are human", "robot check", "unusual traffic"))
            return new BrowserRealSiteBlockDetection(true, BrowserRealSiteReadOnlyStatus.RequiresHuman, BrowserRuntimeErrorCode.ActionRejected, "captcha or anti-bot checkpoint detected");

        if (ContainsAny(combined, "access denied", "forbidden", "temporarily blocked", "request blocked"))
            return new BrowserRealSiteBlockDetection(true, BrowserRealSiteReadOnlyStatus.Blocked, BrowserRuntimeErrorCode.ActionRejected, "access denied or blocking page detected");

        if (ContainsAny(combined, "/login", "/signin", "inicia sesión para continuar", "sign in to continue", "password"))
            return new BrowserRealSiteBlockDetection(true, BrowserRealSiteReadOnlyStatus.RequiresHuman, BrowserRuntimeErrorCode.ActionRejected, "login wall detected");

        return BrowserRealSiteBlockDetection.None;
    }

    private static bool IsPublicHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        !string.IsNullOrWhiteSpace(uri.Host);

    private static bool ContainsAny(string value, params string[] needles) =>
        needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));
}

public sealed class BrowserRealSiteReadOnlyScenario
{
    private readonly BrowserRealSiteReadOnlyPolicy _policy = new();

    public static BrowserRealSiteReadOnlyOptions MercadoLibrePublicSearch(string query = "sonoff rf bridge") =>
        new(
            SiteName: "MercadoLibre public read-only",
            StartUrl: new Uri("https://www.mercadolibre.com.ar/"),
            ExpectedHost: "mercadolibre.com.ar",
            ExpectedText: query,
            PublicSearchUrl: new Uri($"https://listado.mercadolibre.com.ar/{Uri.EscapeDataString(query).Replace("%20", "-")}"),
            SearchQuery: query,
            Timeout: TimeSpan.FromSeconds(20));

    public BrowserPolicyDecision EvaluateAction(BrowserAction action) => _policy.EvaluateAction(action);

    public BrowserRealSiteBlockDetection DetectBlocking(BrowserObservation observation) => _policy.DetectBlocking(observation);

    public BrowserVerification VerifyObservation(BrowserObservation observation, BrowserRealSiteReadOnlyOptions options, string stepId, string? actionId)
    {
        var block = _policy.DetectBlocking(observation);
        if (block.Blocked)
        {
            return Verification(observation, options, stepId, actionId, BrowserVerificationStatus.Uncertain, 0.1, block.Reason);
        }

        var hostOk = observation.Url.Host.EndsWith(options.ExpectedHost, StringComparison.OrdinalIgnoreCase);
        var textOk = string.IsNullOrWhiteSpace(options.ExpectedText) ||
            observation.VisibleTextSummary.Contains(options.ExpectedText, StringComparison.OrdinalIgnoreCase) ||
            observation.Title.Contains(options.ExpectedText, StringComparison.OrdinalIgnoreCase) ||
            observation.Url.ToString().Contains(options.ExpectedText.Replace(" ", "-"), StringComparison.OrdinalIgnoreCase);

        if (hostOk && textOk && observation.EvidenceRefs.Count > 0)
            return Verification(observation, options, stepId, actionId, BrowserVerificationStatus.Verified, 0.9, null);

        return Verification(observation, options, stepId, actionId, BrowserVerificationStatus.Uncertain, 0.35, "read-only real-site expectation was not strongly verified");
    }

    public async Task<BrowserRealSiteReadOnlyReport> RunLiveAsync(
        BrowserRealSiteReadOnlyOptions options,
        string browserExecutablePath,
        CancellationToken cancellationToken = default)
    {
        var runId = "real-readonly-" + Guid.NewGuid().ToString("N");
        var evidence = new List<BrowserEvidence>();
        var policies = new List<string>();
        var diagnostics = new List<string>();
        var cleanupCompleted = false;

        await using var session = await new ChromeCdpBrowserLauncher().LaunchAsync(new ChromeCdpOptions(
            browserExecutablePath,
            Headless: true,
            StartupTimeout: TimeSpan.FromSeconds(15)), cancellationToken).ConfigureAwait(false);

        try
        {
            await using var page = await session.CreatePageAsync(options.StartUrl, cancellationToken).ConfigureAwait(false);
            var dispatcher = new ChromeCdpBrowserActionDispatcher(page);
            var runner = new BrowserExecutorStepRunner(new BrowserExecutorPolicyGate(), dispatcher, dispatcher);
            var capabilities = Capabilities();

            var before = await page.ObserveAsync(runId, payloadLimitApplied: true, cancellationToken).ConfigureAwait(false);
            evidence.Add(Evidence(runId, "before", null, null, before.TargetContext, $"before:{before.Url.Host}", before.ObservationId));
            var beforeBlock = _policy.DetectBlocking(before);
            if (beforeBlock.Blocked)
                return Report(runId, options, beforeBlock.Status, beforeBlock.ErrorCode, before.Url, before.Title, BrowserVerificationStatus.Uncertain, evidence, policies, [beforeBlock.Reason], cleanupCompleted: true);

            if (options.PublicSearchUrl is not null)
            {
                var navigate = CreateAction(
                    runId,
                    "navigate-public-search",
                    "m6-navigate-public-search",
                    "m6-navigate-public-search-" + Guid.NewGuid().ToString("N"),
                    before.TargetContext,
                    BrowserActionType.Navigate,
                    new BrowserActionTarget("public-search-url", null, "public search URL", options.PublicSearchUrl.ToString()),
                    null,
                    new BrowserExpectedOutcome("public search URL loaded", options.PublicSearchUrl.Host, null, null),
                    BrowserRiskClass.Low);
                var policy = _policy.EvaluateAction(navigate);
                policies.Add(policy.Reason);
                if (!policy.Allowed)
                    return Report(runId, options, BrowserRealSiteReadOnlyStatus.Blocked, policy.ErrorCode ?? BrowserRuntimeErrorCode.ActionRejected, before.Url, before.Title, BrowserVerificationStatus.Uncertain, evidence, policies, [policy.Reason], cleanupCompleted: true);

                var navigateResult = await runner.ExecuteAsync(new BrowserExecutorStepRequest("m6-public-search", navigate, capabilities, before.TargetContext), cancellationToken).ConfigureAwait(false);
                evidence.AddRange(navigateResult.Evidence.BrowserEvidence);
                diagnostics.Add(navigateResult.Message);
                if (!navigateResult.Success)
                    return Report(runId, options, ToStatus(navigateResult), navigateResult.ErrorCode, before.Url, before.Title, navigateResult.Verification?.Status ?? BrowserVerificationStatus.Uncertain, evidence, policies, diagnostics, cleanupCompleted: true);
            }

            var after = await page.ObserveAsync(runId, payloadLimitApplied: true, cancellationToken).ConfigureAwait(false);
            evidence.Add(Evidence(runId, "after", null, null, after.TargetContext, $"after:{after.Url.Host}", after.ObservationId));
            var afterBlock = _policy.DetectBlocking(after);
            if (afterBlock.Blocked)
                return Report(runId, options, afterBlock.Status, afterBlock.ErrorCode, after.Url, after.Title, BrowserVerificationStatus.Uncertain, evidence, policies, [.. diagnostics, afterBlock.Reason], cleanupCompleted: true);

            var verification = VerifyObservation(after, options, "verify-read-only-result", null);
            evidence.Add(Evidence(runId, "verify-read-only-result", null, verification.VerificationId, after.TargetContext, $"verification:{verification.Status}", string.Join(",", verification.EvidenceRefs)));

            var status = verification.Status == BrowserVerificationStatus.Verified
                ? BrowserRealSiteReadOnlyStatus.Verified
                : BrowserRealSiteReadOnlyStatus.Uncertain;
            var error = status == BrowserRealSiteReadOnlyStatus.Verified ? BrowserRuntimeErrorCode.None : BrowserRuntimeErrorCode.VerificationUncertain;
            return Report(runId, options, status, error, after.Url, after.Title, verification.Status, evidence, policies, diagnostics, cleanupCompleted: true);
        }
        catch (OperationCanceledException)
        {
            return Report(runId, options, BrowserRealSiteReadOnlyStatus.TimedOut, BrowserRuntimeErrorCode.ActionTimeout, null, "", BrowserVerificationStatus.Uncertain, evidence, policies, ["live read-only scenario timed out or was cancelled"], cleanupCompleted);
        }
        catch (Exception ex)
        {
            return Report(runId, options, BrowserRealSiteReadOnlyStatus.Failed, BrowserRuntimeErrorCode.UnexpectedException, null, "", BrowserVerificationStatus.Uncertain, evidence, policies, [ex.GetType().Name + ": " + ex.Message], cleanupCompleted);
        }
        finally
        {
            cleanupCompleted = true;
        }
    }

    private static BrowserExecutorCapabilities Capabilities() =>
        new(
            ExecutorId: "m6-real-site-readonly",
            ExecutorKind: BrowserExecutorKind.Cdp,
            Capabilities: new HashSet<BrowserActionType> { BrowserActionType.Navigate, BrowserActionType.Read, BrowserActionType.WaitFor, BrowserActionType.Extract, BrowserActionType.NoOp },
            RiskLimit: BrowserRiskClass.Low,
            SupportsTrustedInput: false,
            SupportsDomSnapshot: true,
            SupportsAccessibilitySnapshot: false,
            SupportsScreenshots: false,
            SupportsFrames: true,
            SupportsDownloads: false,
            SupportsFileUpload: false,
            RequiresBrowserLaunch: true,
            RequiresRemoteDebugging: true,
            CanAttachExistingBrowser: false,
            CanUsePersistentProfile: false,
            CanUseRealUserProfile: false);

    private static BrowserAction CreateAction(
        string runId,
        string stepId,
        string actionId,
        string idempotencyKey,
        BrowserTargetContext targetContext,
        BrowserActionType actionType,
        BrowserActionTarget target,
        BrowserActionInput? input,
        BrowserExpectedOutcome expected,
        BrowserRiskClass risk) =>
        new(
            ActionId: actionId,
            IdempotencyKey: idempotencyKey,
            RunId: runId,
            StepId: stepId,
            TargetContext: targetContext,
            FrameId: targetContext.FrameId,
            ActionType: actionType,
            Target: target,
            Input: input,
            ExpectedOutcome: expected,
            RiskClass: risk,
            TimeoutMs: 15000,
            RequiresApproval: false,
            CreatedAtUtc: DateTimeOffset.UtcNow);

    private static BrowserVerification Verification(
        BrowserObservation observation,
        BrowserRealSiteReadOnlyOptions options,
        string stepId,
        string? actionId,
        BrowserVerificationStatus status,
        double confidence,
        string? failureReason) =>
        new(
            VerificationId: Guid.NewGuid().ToString("N"),
            RunId: observation.RunId,
            StepId: stepId,
            ActionId: actionId,
            TargetContext: observation.TargetContext,
            ExpectedOutcome: new BrowserExpectedOutcome($"read-only public content for {options.SiteName}", options.ExpectedHost, options.ExpectedText, null),
            PreObservationId: null,
            PostObservationId: observation.ObservationId,
            Status: status,
            Confidence: confidence,
            EvidenceRefs: observation.EvidenceRefs,
            FailureReason: failureReason,
            VerifiedAtUtc: DateTimeOffset.UtcNow,
            ProofRefs: status == BrowserVerificationStatus.Verified ? [$"proof:{observation.ObservationId}:{options.ExpectedHost}"] : []);

    private static BrowserEvidence Evidence(string runId, string stepId, string? actionId, string? verificationId, BrowserTargetContext targetContext, string summary, string? payloadRef) =>
        new(
            EvidenceId: Guid.NewGuid().ToString("N"),
            RunId: runId,
            StepId: stepId,
            ActionId: actionId,
            VerificationId: verificationId,
            TargetContext: targetContext,
            EvidenceType: verificationId is null ? BrowserEvidenceType.TextExtract : BrowserEvidenceType.VerificationResult,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Summary: summary,
            PayloadRef: payloadRef,
            InlinePayload: null,
            RedactionApplied: true,
            SensitivityLevel: BrowserSensitivityLevel.Low);

    private static BrowserRealSiteReadOnlyReport Report(
        string runId,
        BrowserRealSiteReadOnlyOptions options,
        BrowserRealSiteReadOnlyStatus status,
        BrowserRuntimeErrorCode errorCode,
        Uri? finalUrl,
        string finalTitle,
        BrowserVerificationStatus verificationStatus,
        IReadOnlyList<BrowserEvidence> evidence,
        IReadOnlyList<string> policy,
        IReadOnlyList<string> diagnostics,
        bool cleanupCompleted) =>
        new(runId, options.SiteName, status, errorCode, finalUrl, finalTitle, verificationStatus, evidence, policy, diagnostics, cleanupCompleted, UsedServiceWorker: false, UsedRealProfile: false);

    private static BrowserRealSiteReadOnlyStatus ToStatus(BrowserExecutorStepResult result) =>
        result.FinalState switch
        {
            BrowserExecutorStepState.Verified => BrowserRealSiteReadOnlyStatus.Verified,
            BrowserExecutorStepState.ApprovalRequired or BrowserExecutorStepState.RequiresHuman => BrowserRealSiteReadOnlyStatus.RequiresHuman,
            BrowserExecutorStepState.Uncertain => BrowserRealSiteReadOnlyStatus.Uncertain,
            BrowserExecutorStepState.TimedOut => BrowserRealSiteReadOnlyStatus.TimedOut,
            BrowserExecutorStepState.Blocked => BrowserRealSiteReadOnlyStatus.Blocked,
            _ => BrowserRealSiteReadOnlyStatus.Failed
        };
}
