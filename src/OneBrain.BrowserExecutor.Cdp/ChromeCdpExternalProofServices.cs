using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class ChromeCdpExternalPreflightService
{
    public ChromeCdpExternalPreflightResult Evaluate(ChromeCdpExternalPreflightRequest request)
    {
        var reasons = new List<string>();
        if (!request.OptInEnabled)
            reasons.Add("Chrome/CDP external live proof opt-in missing");
        if (string.IsNullOrWhiteSpace(request.BrowserExecutablePath) || !File.Exists(request.BrowserExecutablePath))
            reasons.Add("Chrome/Edge executable unavailable");
        if (!string.Equals(request.TargetHost, NexaTargetBindingReadinessEvaluator.RecommendedDomain, StringComparison.OrdinalIgnoreCase))
            reasons.Add("target host is not allowlisted for external CDP proof");
        if (request.UsesPersonalProfile || request.UsesDefaultUserDataDir || request.ProfileDirectory.Contains("User Data", StringComparison.OrdinalIgnoreCase))
            reasons.Add("personal or default Chrome profile blocked");
        if (request.CookiesPersisted)
            reasons.Add("cookie persistence blocked");
        if (request.CredentialsAvailable || request.SavedPasswordsAvailable)
            reasons.Add("credentials and saved passwords blocked");
        if (request.PersonalExtensionsEnabled)
            reasons.Add("personal extensions blocked");
        if (!request.CdpSessionControlled)
            reasons.Add("controlled CDP session required");
        if (!request.ReadOnlyOnly)
            reasons.Add("only read-only navigation is allowed");

        var unsafeProfile = request.UsesPersonalProfile || request.UsesDefaultUserDataDir || request.ProfileDirectory.Contains("User Data", StringComparison.OrdinalIgnoreCase);
        var status = reasons.Count == 0
            ? ChromeCdpExternalPreflightStatus.ReadyForExternalCdpReadOnlyProof
            : unsafeProfile
                ? ChromeCdpExternalPreflightStatus.UnsafeProfileBlocked
                : string.IsNullOrWhiteSpace(request.BrowserExecutablePath) || !File.Exists(request.BrowserExecutablePath)
                    ? ChromeCdpExternalPreflightStatus.ChromeCdpUnavailable
                    : !request.OptInEnabled
                        ? ChromeCdpExternalPreflightStatus.ChromeCdpAvailable
                        : request.CdpSessionControlled
                            ? ChromeCdpExternalPreflightStatus.IsolatedProfileReady
                            : ChromeCdpExternalPreflightStatus.CdpPreflightFailed;

        return new ChromeCdpExternalPreflightResult(
            status,
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            CanAttemptLiveProof: status == ChromeCdpExternalPreflightStatus.ReadyForExternalCdpReadOnlyProof,
            LaunchesBrowser: false,
            Redacted: true);
    }

    public ChromeCdpExternalPreflightRequest CreateDefaultRequest(bool optInEnabled)
    {
        var executable = ChromeCdpBrowserLauncher.FindBrowserExecutable();
        return new ChromeCdpExternalPreflightRequest(
            optInEnabled,
            executable,
            Path.Combine(Path.GetTempPath(), "onebrain-browser-profiles", "external-cdp-preflight"),
            UsesPersonalProfile: false,
            UsesDefaultUserDataDir: false,
            CookiesPersisted: false,
            CredentialsAvailable: false,
            PersonalExtensionsEnabled: false,
            SavedPasswordsAvailable: false,
            CdpSessionControlled: true,
            NexaTargetBindingReadinessEvaluator.RecommendedDomain,
            ReadOnlyOnly: true);
    }
}

public sealed class UnavailableChromeCdpExternalProbe : INexaChromeCdpExternalProbe
{
    public bool IsAvailable => false;

    public Task<ChromeCdpExternalProbeResult> ProbeReadOnlyAsync(
        ChromeCdpReadOnlyEvidencePolicy policy,
        NexaExternalTestOwnedTarget target,
        IReadOnlyList<string> allowedRoutes,
        IReadOnlyList<string> blockedRoutes,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ChromeCdpExternalProbeResult(
            ChromeCdpExternalProbeStatus.ChromeCdpUnavailable,
            IsRealChromeCdpSession: false,
            NavigatedToAllowedTarget: false,
            DomOrPageMetadataCaptured: false,
            BrowserVersion: "unavailable",
            TargetUrl: target.BaseUrl ?? "",
            RoutesVisited: [],
            PolicyBlockedRoutes: blockedRoutes,
            Snapshot: null,
            SubmittedOrMutated: false,
            UsedCredentials: false,
            UsedLoginOrPayment: false,
            PersistedCookies: false,
            CapturedSensitiveHeaderValues: false,
            PersistedFullDomOrBody: false,
            SecretsCookiesTokensDetected: false,
            EvidenceRefs: ["chrome-cdp:unavailable"],
            ReasonCodes: ["Chrome/CDP external probe unavailable"],
            Redacted: true));
}

public sealed class RealChromeCdpExternalProbe(ChromeCdpBrowserLauncher? launcher = null) : INexaChromeCdpExternalProbe
{
    private readonly ChromeCdpBrowserLauncher _launcher = launcher ?? new ChromeCdpBrowserLauncher();

    public bool IsAvailable => ChromeCdpBrowserLauncher.FindBrowserExecutable() is not null;

    public async Task<ChromeCdpExternalProbeResult> ProbeReadOnlyAsync(
        ChromeCdpReadOnlyEvidencePolicy policy,
        NexaExternalTestOwnedTarget target,
        IReadOnlyList<string> allowedRoutes,
        IReadOnlyList<string> blockedRoutes,
        CancellationToken cancellationToken)
    {
        var executable = ChromeCdpBrowserLauncher.FindBrowserExecutable();
        if (executable is null)
            return await new UnavailableChromeCdpExternalProbe().ProbeReadOnlyAsync(policy, target, allowedRoutes, blockedRoutes, cancellationToken).ConfigureAwait(false);
        if (target.BaseUrl is null || !Uri.TryCreate(target.BaseUrl, UriKind.Absolute, out var baseUri) || !policy.AllowedHosts.Contains(baseUri.Host))
            return Failed(ChromeCdpExternalProbeStatus.TargetPolicyBlocked, target.BaseUrl ?? "", blockedRoutes, ["target host not allowlisted"]);

        await using var session = await _launcher.LaunchAsync(new ChromeCdpOptions(executable, Headless: true, StartupTimeout: TimeSpan.FromSeconds(15)), cancellationToken).ConfigureAwait(false);
        await using var page = await session.CreatePageAsync(new Uri(baseUri, allowedRoutes[0]), cancellationToken).ConfigureAwait(false);
        using var version = await session.GetVersionAsync(cancellationToken).ConfigureAwait(false);
        var browserVersion = version.RootElement.TryGetProperty("Browser", out var browser) ? browser.GetString() ?? "Chrome/CDP" : "Chrome/CDP";
        var visited = new List<string>();
        ChromeCdpDomReadOnlySnapshot? snapshot = null;

        try
        {
            foreach (var route in allowedRoutes)
            {
                if (!policy.AllowedPaths.Contains(route))
                    return Failed(ChromeCdpExternalProbeStatus.TargetPolicyBlocked, route, blockedRoutes, ["route not allowlisted"]);
                var uri = new Uri(baseUri, route);
                await page.NavigateAsync(uri, cancellationToken).ConfigureAwait(false);
                var observation = await page.ObserveAsync("external-cdp-readonly", cancellationToken: cancellationToken).ConfigureAwait(false);
                visited.Add(route);
                snapshot = new ChromeCdpDomReadOnlySnapshot(
                    BrowserCredentialRedactor.Redact(observation.Url.ToString()),
                    BrowserCredentialRedactor.Redact(observation.Title),
                    ["url", "title", "readyState", "frameCount", "actionableCount"],
                    observation.ActionableElements.Count,
                    FullDomPersisted: false,
                    ContainsSensitiveMaterial: ContainsUnsafe(observation.VisibleTextSummary),
                    Redacted: true);
                if (snapshot.ContainsSensitiveMaterial)
                    return Failed(ChromeCdpExternalProbeStatus.SafetyRejected, uri.ToString(), blockedRoutes, ["DOM/page metadata safety scan failed"]);
            }

            return new ChromeCdpExternalProbeResult(
                ChromeCdpExternalProbeStatus.PassedReadOnlyDomProof,
                IsRealChromeCdpSession: true,
                NavigatedToAllowedTarget: visited.Count == allowedRoutes.Count,
                DomOrPageMetadataCaptured: snapshot is not null,
                BrowserCredentialRedactor.Redact(browserVersion),
                BrowserCredentialRedactor.Redact(baseUri.ToString()),
                visited,
                blockedRoutes,
                snapshot,
                SubmittedOrMutated: false,
                UsedCredentials: false,
                UsedLoginOrPayment: false,
                PersistedCookies: false,
                CapturedSensitiveHeaderValues: false,
                PersistedFullDomOrBody: false,
                SecretsCookiesTokensDetected: false,
                EvidenceRefs: ["cdp:browser-session:redacted", "cdp:navigation:redacted", "cdp:dom-metadata:redacted"],
                ReasonCodes: ["real Chrome/CDP read-only DOM proof passed"],
                Redacted: true);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException or InvalidOperationException or System.Net.WebSockets.WebSocketException)
        {
            return Failed(ChromeCdpExternalProbeStatus.NavigationFailed, target.BaseUrl ?? "", blockedRoutes, [$"Chrome/CDP navigation failed: {ex.GetType().Name}"]);
        }
    }

    private static ChromeCdpExternalProbeResult Failed(ChromeCdpExternalProbeStatus status, string targetUrl, IReadOnlyList<string> blockedRoutes, IReadOnlyList<string> reasons) =>
        new(
            status,
            IsRealChromeCdpSession: false,
            NavigatedToAllowedTarget: false,
            DomOrPageMetadataCaptured: false,
            BrowserVersion: "unavailable",
            BrowserCredentialRedactor.Redact(targetUrl),
            RoutesVisited: [],
            PolicyBlockedRoutes: blockedRoutes,
            Snapshot: null,
            SubmittedOrMutated: false,
            UsedCredentials: false,
            UsedLoginOrPayment: false,
            PersistedCookies: false,
            CapturedSensitiveHeaderValues: false,
            PersistedFullDomOrBody: false,
            SecretsCookiesTokensDetected: false,
            EvidenceRefs: ["cdp:failed:redacted"],
            ReasonCodes: reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            Redacted: true);

    private static bool ContainsUnsafe(string value) =>
        BrowserCredentialRedactor.ContainsSecret(value) ||
        value.Contains("set-cookie", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("bearer ", StringComparison.OrdinalIgnoreCase);
}

public sealed class ChromeCdpExternalProofRunner(INexaChromeCdpExternalProbe? probe = null)
{
    private static readonly string[] AllowedRoutes = ["/", "/health/", "/ownership/", "/products/", "/document/", "/report/"];
    private static readonly string[] BlockedRoutes = ["/disabled-form/", "/blocked-login/", "/blocked-checkout/", "/blocked-destructive-action/"];

    private readonly INexaChromeCdpExternalProbe _probe = probe ?? new UnavailableChromeCdpExternalProbe();
    private readonly NexaExternalProofHarness _harness = new();
    private readonly NexaExternalReadOnlyEvidencePackBuilder _evidence = new();
    private readonly NexaExternalEvidenceLedgerPersistence _persistence = new();
    private readonly NexaLiveProofSafetyGate _gate = new();
    private readonly M65DedicatedEvidenceReviewer _m65 = new();
    private readonly ChromeCdpExternalPreflightService _preflight = new();

    public Task<ChromeCdpExternalProofResult> RunAsync(bool optIn, BrowserPersistentAuditLedger? ledger = null, CancellationToken cancellationToken = default) =>
        RunAsync(optIn, targetVerified: true, ledger, cancellationToken);

    public async Task<ChromeCdpExternalProofResult> RunAsync(bool optIn, bool targetVerified, BrowserPersistentAuditLedger? ledger = null, CancellationToken cancellationToken = default)
    {
        var target = NexaFirstReadOnlyLiveProofRunner.CreateLiveTarget();
        var targetDecision = new NexaExternalTestOwnedTargetEvaluator().Evaluate(target, DateTimeOffset.UtcNow);
        var binding = new NexaTargetBindingReadinessEvaluator().CreateDefault(
            NexaTargetBindingDnsMode.DelegatedToVercel,
            targetVerified ? NexaTargetBindingVerificationStatus.OwnershipVerified : NexaTargetBindingVerificationStatus.HttpsReady);
        var safety = _gate.Evaluate(GateRequest(binding, target, optIn), DateTimeOffset.UtcNow);
        var preflight = _preflight.Evaluate(_preflight.CreateDefaultRequest(optIn) with
        {
            BrowserExecutablePath = _probe.IsAvailable ? ChromeCdpBrowserLauncher.FindBrowserExecutable() ?? Environment.ProcessPath : null
        });
        var harnessRequest = new NexaExternalProofHarnessRequest(optIn, target, NexaTargetBindingReadinessEvaluator.RecommendedDomain, "/", "GET", false, false, false, false, "operator-cdp-proof");
        var harness = _harness.Evaluate(harnessRequest, DateTimeOffset.UtcNow);

        if (!optIn)
        {
            var skippedPack = _evidence.Build(harness, harnessRequest, runtimeExecuted: false, runtimePassed: false, NexaExternalProofProbeKind.ModeledFake);
            return Build(ChromeCdpExternalProofStatus.SkippedNoOptIn, Readiness(optIn, targetVerified, safety, chromeAvailable: false, ready: false, preflight.ReasonCodes), null, skippedPack, executed: false);
        }

        if (!targetVerified || targetDecision.Status != NexaExternalTestOwnedTargetStatus.ApprovedReadOnlyTestOwned || !safety.ReadyForReadOnlyLiveProof || !harness.CanExecuteReadOnlyNavigation)
        {
            var blockedPack = _evidence.Build(harness, harnessRequest, runtimeExecuted: false, runtimePassed: false, NexaExternalProofProbeKind.ModeledFake);
            return Build(ChromeCdpExternalProofStatus.BlockedPolicyViolation, Readiness(optIn, targetVerified, safety, _probe.IsAvailable, ready: false, ["target, binding, or safety gate not ready"]), null, blockedPack, executed: false);
        }

        if (!_probe.IsAvailable || preflight.Status == ChromeCdpExternalPreflightStatus.ChromeCdpUnavailable)
        {
            var unavailablePack = _evidence.Build(harness, harnessRequest, runtimeExecuted: false, runtimePassed: false, NexaExternalProofProbeKind.ModeledFake);
            return Build(ChromeCdpExternalProofStatus.ChromeCdpUnavailable, Readiness(optIn, targetVerified, safety, chromeAvailable: false, ready: false, preflight.ReasonCodes.Count > 0 ? preflight.ReasonCodes : ["Chrome/CDP probe unavailable"]), null, unavailablePack, executed: false);
        }

        var policy = Policy();
        var probeResult = await _probe.ProbeReadOnlyAsync(policy, target, AllowedRoutes, BlockedRoutes, cancellationToken).ConfigureAwait(false);
        var passed = IsSafeRealChromeCdpProof(probeResult);
        var probeKind = passed ? NexaExternalProofProbeKind.RealChromeCdp : NexaExternalProofProbeKind.ModeledFake;
        var packStatus = _evidence.Build(harness, harnessRequest, runtimeExecuted: true, runtimePassed: passed, probeKind) with
        {
            RuntimeProvider = passed ? "ChromeCdpExternalReadOnly" : "ModeledFake",
            Tooling = passed ? "ChromeCdpExternalReadOnly" : "ModeledFake",
            RuntimeCapabilities = passed
                ? ["BrowserNavigationReadOnly", "DomSnapshotReadOnly", "PageMetadataReadOnly", "NetworkMetadataRedacted", "CoreGoverned"]
                : ["ModeledReadOnly", "CoreGoverned"],
            LogRefs = probeResult.EvidenceRefs.Concat([
                $"browserVersion:{BrowserCredentialRedactor.Redact(probeResult.BrowserVersion)}",
                $"routes:{string.Join(',', probeResult.RoutesVisited.Select(BrowserCredentialRedactor.Redact))}",
                $"blockedRoutes:{string.Join(',', probeResult.PolicyBlockedRoutes.Select(BrowserCredentialRedactor.Redact))}"
            ]).ToArray(),
            RedactionSummary = "Chrome/CDP DOM metadata snapshot captured read-only; full DOM/body not persisted; redacted metadata and safety summary only; no cookies, sensitive headers, tokens, credentials, submit, mutation, login, or payment persisted"
        };
        var pack = passed && ledger is not null
            ? _persistence.PersistIfEligible(packStatus, ledger)
            : packStatus;
        var status = passed
            ? ChromeCdpExternalProofStatus.PassedReadOnlyProof
            : probeResult.Status == ChromeCdpExternalProbeStatus.ChromeCdpUnavailable
                ? ChromeCdpExternalProofStatus.ChromeCdpUnavailable
                : ChromeCdpExternalProofStatus.FailedRuntime;

        return Build(status, Readiness(optIn, targetVerified, safety, _probe.IsAvailable, passed, probeResult.ReasonCodes), probeResult, pack, executed: probeResult.IsRealChromeCdpSession);
    }

    public static ChromeCdpReadOnlyEvidencePolicy Policy() =>
        new(
            RequireRealChromeCdpSession: true,
            RequireNavigationEvidence: true,
            RequireDomPageMetadata: true,
            BlockFullDomPersistence: true,
            BlockSubmitMutationLoginPayment: true,
            BlockCookiesTokensSecrets: true,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { NexaTargetBindingReadinessEvaluator.RecommendedDomain },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/", "/health/", "/ownership/", "/products/", "/document/", "/report/" });

    private static NexaLiveProofSafetyGateRequest GateRequest(NexaTargetBindingConfig binding, NexaExternalTestOwnedTarget target, bool optIn) =>
        new(binding, target, optIn, NexaTargetBindingReadinessEvaluator.RecommendedDomain, "/", "GET", false, false, false, false, false, false, false, true, false, "approval:lab-vercel-readonly");

    private static bool IsSafeRealChromeCdpProof(ChromeCdpExternalProbeResult result) =>
        result.Status == ChromeCdpExternalProbeStatus.PassedReadOnlyDomProof &&
        result.IsRealChromeCdpSession &&
        result.NavigatedToAllowedTarget &&
        result.DomOrPageMetadataCaptured &&
        result.Snapshot is { Redacted: true, FullDomPersisted: false, ContainsSensitiveMaterial: false } &&
        !result.SubmittedOrMutated &&
        !result.UsedCredentials &&
        !result.UsedLoginOrPayment &&
        !result.PersistedCookies &&
        !result.CapturedSensitiveHeaderValues &&
        !result.PersistedFullDomOrBody &&
        !result.SecretsCookiesTokensDetected &&
        result.Redacted;

    private static ChromeCdpExternalProofReadiness Readiness(
        bool optIn,
        bool targetVerified,
        NexaLiveProofSafetyGateDecision safety,
        bool chromeAvailable,
        bool ready,
        IReadOnlyList<string> reasons) =>
        new(optIn, targetVerified, safety.ReadyForReadOnlyLiveProof, chromeAvailable, ready, reasons.Select(BrowserCredentialRedactor.Redact).ToArray(), Redacted: true);

    private ChromeCdpExternalProofResult Build(
        ChromeCdpExternalProofStatus status,
        ChromeCdpExternalProofReadiness readiness,
        ChromeCdpExternalProbeResult? probeResult,
        NexaExternalReadOnlyEvidencePack pack,
        bool executed)
    {
        var review = _m65.Review(new M65DedicatedEvidenceReviewInput(
            M51Closed: true,
            ScenarioPlanReady: true,
            pack.ProbeKind,
            pack.Tooling,
            LedgerRefPresent: !string.IsNullOrWhiteSpace(pack.LedgerRef) && pack.PersistenceStatus == NexaExternalEvidencePersistenceStatus.PersistedRedactedLedger,
            TargetVerified: readiness.TargetVerified,
            ReadOnlyProofPassed: status == ChromeCdpExternalProofStatus.PassedReadOnlyProof,
            ChromeCdpDomProofPassed: status == ChromeCdpExternalProofStatus.PassedReadOnlyProof && pack.ProbeKind == NexaExternalProofProbeKind.RealChromeCdp,
            SecretsCookiesTokensDetected: probeResult?.SecretsCookiesTokensDetected == true,
            SubmitMutationPaymentLoginDetected: probeResult?.SubmittedOrMutated == true || probeResult?.UsedLoginOrPayment == true,
            PolicyViolationDetected: status == ChromeCdpExternalProofStatus.BlockedPolicyViolation,
            ScopeRequiresChromeCdpDomProof: true,
            PublicSaasEnabled: false,
            RealBillingEnabled: false,
            RealEmailEnabled: false,
            RealCredentialsEnabled: false,
            SensitiveSurfaceEnabled: false));

        return new ChromeCdpExternalProofResult(status, readiness, probeResult, pack, review, executed, Redacted: true);
    }
}
