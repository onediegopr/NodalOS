using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaExternalTestOwnedTargetEvaluator
{
    private static readonly string[] SensitiveHostTokens = ["afip", "bank", "banco", "fiscal", "tax", "erp", "gov", "gob", "payment", "pay", "stripe", "paypal", "mercadopago"];
    private static readonly HashSet<string> ReadOnlyMethods = new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD" };
    private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    public NexaExternalTestOwnedTargetDecision Evaluate(NexaExternalTestOwnedTarget? target, DateTimeOffset nowUtc)
    {
        if (target is null || string.IsNullOrWhiteSpace(target.BaseUrl))
            return Decision(NexaExternalTestOwnedTargetStatus.MissingTarget, target, ["external test-owned target missing"], allows: false);
        if (!Uri.TryCreate(target.BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            return Decision(NexaExternalTestOwnedTargetStatus.PolicyRejected, target, ["target base URL must be HTTP/HTTPS"], allows: false);

        var reasons = new List<string>();
        if (!target.ExplicitlyTestOwned || target.OwnershipProofMode == NexaExternalTargetOwnershipProofMode.None || string.IsNullOrWhiteSpace(target.ApprovalRef))
            reasons.Add("explicit test-owned approval required");
        if (target.ValidUntilUtc is not null && target.ValidUntilUtc <= nowUtc)
            return Decision(NexaExternalTestOwnedTargetStatus.Expired, target, ["target approval expired"], allows: false);
        if (target.AllowedHosts.Count == 0 || !target.AllowedHosts.Contains(uri.Host))
            reasons.Add("host must be explicitly allowlisted");
        if (target.DeniedHosts.Contains(uri.Host) || target.DeniedHosts.Any(denied => uri.Host.Contains(denied, StringComparison.OrdinalIgnoreCase)))
            reasons.Add("host is denied");
        if (SensitiveHostTokens.Any(token => uri.Host.Contains(token, StringComparison.OrdinalIgnoreCase)) ||
            target.DataSensitivityProfile is NexaExternalTargetDataSensitivityProfile.FinancialFiscalGovernmentBlocked or NexaExternalTargetDataSensitivityProfile.SensitiveBlocked)
            return Decision(NexaExternalTestOwnedTargetStatus.BlockedSensitiveSurface, target, ["financial fiscal government or sensitive host blocked"], allows: false);
        if (target.AllowedPaths.Count == 0)
            reasons.Add("allowed read-only paths required");
        if (target.DeniedPaths.Any(path => target.AllowedPaths.Contains(path)))
            reasons.Add("allowed paths cannot include denied paths");
        if (target.AllowedMethods.Count == 0 || target.AllowedMethods.Any(method => !ReadOnlyMethods.Contains(method)) || target.DeniedMethods.Any(method => !MutatingMethods.Contains(method)))
            reasons.Add("only HTTP GET/HEAD read-only methods are allowed");
        if (target.CredentialPolicy != NexaExternalTargetCredentialPolicy.NoCredentials)
            reasons.Add("real or synthetic login is not allowed for this proof harness");
        if (target.SubmitPolicy != NexaExternalTargetSubmitPolicy.ReadOnlyNoSubmit)
            reasons.Add("submit/mutation policy must be read-only no-submit");
        if (target.DataSensitivityProfile is NexaExternalTargetDataSensitivityProfile.PersonalDataBlocked)
            reasons.Add("real personal data is blocked");
        if (target.EvidencePolicy != NexaExternalTargetEvidencePolicy.MetadataOnlyRedacted)
            reasons.Add("evidence policy must be metadata-only redacted");

        if (reasons.Count > 0)
        {
            var ownershipOnly = reasons.All(reason => reason.Contains("test-owned", StringComparison.OrdinalIgnoreCase));
            return Decision(ownershipOnly ? NexaExternalTestOwnedTargetStatus.OwnershipUnverified : NexaExternalTestOwnedTargetStatus.PolicyRejected, target, reasons, allows: false);
        }

        return Decision(NexaExternalTestOwnedTargetStatus.ApprovedReadOnlyTestOwned, target, ["approved read-only test-owned target"], allows: true);
    }

    private static NexaExternalTestOwnedTargetDecision Decision(NexaExternalTestOwnedTargetStatus status, NexaExternalTestOwnedTarget? target, IReadOnlyList<string> reasons, bool allows) =>
        new(status, RedactTarget(target), reasons.Select(BrowserCredentialRedactor.Redact).ToArray(), allows, Redacted: true);

    private static NexaExternalTestOwnedTarget? RedactTarget(NexaExternalTestOwnedTarget? target) =>
        target is null
            ? null
            : target with
            {
                BaseUrl = RedactUrl(target.BaseUrl),
                ComplianceNotes = BrowserCredentialRedactor.Redact(target.ComplianceNotes),
                OperatorOwner = BrowserCredentialRedactor.Redact(target.OperatorOwner),
                ApprovalRef = BrowserCredentialRedactor.Redact(target.ApprovalRef)
            };

    internal static string RedactUrl(string? url)
    {
        var redacted = BrowserCredentialRedactor.Redact(url);
        if (!Uri.TryCreate(redacted, UriKind.Absolute, out var uri))
            return redacted;
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}{(string.IsNullOrEmpty(uri.Query) ? "" : "?[REDACTED_QUERY]")}";
    }
}

public sealed class NexaExternalProofHarness
{
    private readonly NexaExternalTestOwnedTargetEvaluator _targetEvaluator = new();
    private readonly NexaOperatorBlockerExplanationService _explanations = new();

    public NexaExternalProofHarnessDecision Evaluate(NexaExternalProofHarnessRequest request, DateTimeOffset nowUtc)
    {
        if (!request.OptInEnabled)
        {
            var targetDecision = _targetEvaluator.Evaluate(request.Target, nowUtc);
            return Decision(NexaExternalProofHarnessDecisionKind.SkippedNoOptIn, targetDecision, ["external proof harness opt-in missing"], NexaOperatorBlockerScenario.SkippedTestsBlockExternalLive, canExecute: false);
        }

        var decision = _targetEvaluator.Evaluate(request.Target, nowUtc);
        if (decision.Status == NexaExternalTestOwnedTargetStatus.MissingTarget)
            return Decision(NexaExternalProofHarnessDecisionKind.BlockedNoTarget, decision, decision.ReasonCodes, NexaOperatorBlockerScenario.MissingTestOwnedExternalTarget, canExecute: false);
        if (!decision.AllowsReadOnlyProof || request.Target is null)
            return Decision(NexaExternalProofHarnessDecisionKind.BlockedPolicyViolation, decision, decision.ReasonCodes, NexaOperatorBlockerScenario.CorePermissionMissing, canExecute: false);

        var reasons = new List<string>();
        if (!request.Target.AllowedHosts.Contains(request.RequestedHost))
            reasons.Add("requested host not allowlisted");
        if (!request.Target.AllowedPaths.Contains(request.RequestedPath) || request.Target.DeniedPaths.Contains(request.RequestedPath))
            reasons.Add("requested path not allowlisted for read-only proof");
        if (!request.Target.AllowedMethods.Contains(request.RequestedMethod) || request.Target.DeniedMethods.Contains(request.RequestedMethod))
            reasons.Add("requested method rejected before execution");
        if (request.WouldCaptureBodies)
            reasons.Add("body capture is blocked");
        if (request.WouldCaptureSensitiveHeaderValues)
            reasons.Add("sensitive header value capture is blocked");
        if (request.WouldPersistCookies)
            reasons.Add("cookie persistence is blocked");
        if (request.WouldSubmit)
            reasons.Add("submit/pay/sign/delete is blocked");

        if (reasons.Count > 0)
            return Decision(NexaExternalProofHarnessDecisionKind.BlockedPolicyViolation, decision, reasons, NexaOperatorBlockerScenario.IrreversibleActionBlocked, canExecute: false);

        return Decision(NexaExternalProofHarnessDecisionKind.AllowedReadOnlyProof, decision, ["read-only proof may execute against approved test-owned target"], NexaOperatorBlockerScenario.SkippedTestsBlockExternalLive, canExecute: true);
    }

    private NexaExternalProofHarnessDecision Decision(
        NexaExternalProofHarnessDecisionKind kind,
        NexaExternalTestOwnedTargetDecision targetDecision,
        IReadOnlyList<string> reasons,
        NexaOperatorBlockerScenario scenario,
        bool canExecute) =>
        new(kind, targetDecision, reasons.Select(BrowserCredentialRedactor.Redact).ToArray(), _explanations.Explain(scenario, ["external-proof-harness:redacted"]), canExecute, Redacted: true);
}

public sealed class NexaExternalReadOnlyEvidencePackBuilder
{
    public NexaExternalReadOnlyEvidencePack Build(NexaExternalProofHarnessDecision harnessDecision, NexaExternalProofHarnessRequest request, bool runtimeExecuted, bool runtimePassed)
    {
        var status = harnessDecision.Decision switch
        {
            NexaExternalProofHarnessDecisionKind.SkippedNoOptIn => NexaExternalReadOnlyEvidencePackStatus.SkippedNoOptIn,
            NexaExternalProofHarnessDecisionKind.BlockedNoTarget => NexaExternalReadOnlyEvidencePackStatus.BlockedNoTarget,
            NexaExternalProofHarnessDecisionKind.BlockedPolicyViolation => NexaExternalReadOnlyEvidencePackStatus.BlockedPolicyViolation,
            NexaExternalProofHarnessDecisionKind.AllowedReadOnlyProof when !runtimeExecuted => NexaExternalReadOnlyEvidencePackStatus.PreparedButNotExecuted,
            NexaExternalProofHarnessDecisionKind.AllowedReadOnlyProof when runtimeExecuted && runtimePassed => NexaExternalReadOnlyEvidencePackStatus.PassedReadOnlyProof,
            NexaExternalProofHarnessDecisionKind.AllowedReadOnlyProof when runtimeExecuted => NexaExternalReadOnlyEvidencePackStatus.FailedRuntime,
            _ => NexaExternalReadOnlyEvidencePackStatus.PreparedButNotExecuted
        };

        var candidate = status == NexaExternalReadOnlyEvidencePackStatus.PassedReadOnlyProof &&
            harnessDecision.TargetDecision.Status == NexaExternalTestOwnedTargetStatus.ApprovedReadOnlyTestOwned;

        return new NexaExternalReadOnlyEvidencePack(
            $"external-readonly-proof-{Guid.NewGuid():N}",
            harnessDecision.TargetDecision.Target?.TargetId,
            harnessDecision.TargetDecision.Status,
            DateTimeOffset.UtcNow,
            "ChromeCdpExternal",
            ["NavigationReadOnly", "DomReadOnly", "NetworkMetadataOnly", "CoreGoverned"],
            BrowserCredentialRedactor.Redact($"{request.RequestedMethod} {request.RequestedPath}"),
            harnessDecision.CanExecuteReadOnlyNavigation ? "read-only navigation" : "none",
            ["POST", "PUT", "PATCH", "DELETE", "submit", "pay", "sign", "delete", "upload"],
            BrowserCredentialRedactor.Redact(request.RequestedHost),
            BrowserCredentialRedactor.Redact(request.RequestedPath),
            BrowserCredentialRedactor.Redact(request.RequestedMethod),
            "metadata-only; no cookies, bodies, sensitive headers, tokens, or personal data persisted",
            [],
            ["external-proof-harness:redacted"],
            harnessDecision.ReasonCodes,
            harnessDecision.CanExecuteReadOnlyNavigation ? [] : [harnessDecision.Explanation],
            candidate ? "candidate proof passed; M51/M65 still require explicit closure decision" : "external/live remains blocked or deferred",
            status,
            candidate,
            Redacted: true);
    }
}

public static class NexaTestOwnedExternalTargetFixtureFactory
{
    public const string FixtureHost = "nexa-test-owned.fixture.local";

    public static NexaExternalTestOwnedTarget Create() =>
        new(
            "fixture-test-owned-readonly",
            $"https://{FixtureHost}/landing",
            NexaExternalTargetOwnershipProofMode.OperatorAttestation,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { FixtureHost },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "afip.gob.ar", "bank.example.invalid", "payments.example.invalid" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/landing", "/products", "/document", "/report", "/disabled-form", "/blocked-login", "/blocked-checkout", "/blocked-delete" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/submit", "/checkout/confirm", "/delete/confirm" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GET" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" },
            NexaExternalTargetCredentialPolicy.NoCredentials,
            NexaExternalTargetSubmitPolicy.ReadOnlyNoSubmit,
            NexaExternalTargetDataSensitivityProfile.SyntheticPublic,
            NexaExternalTargetEvidencePolicy.MetadataOnlyRedacted,
            "fixture-approved synthetic target only; not external/live proof",
            DateTimeOffset.UtcNow.AddDays(30),
            "operator-fixture",
            "approval:fixture-synthetic-readonly",
            ExplicitlyTestOwned: true);
}

public sealed class NexaSyntheticExternalScenarioCatalogService
{
    public NexaSyntheticExternalScenarioCatalog CreateDefault() =>
        new(
            [
                Scenario("synthetic-landing", NexaSyntheticExternalScenarioKind.LandingReadOnly, "/landing", ["read-dom", "read-title"], [], NexaSyntheticExternalScenarioSensitivity.SyntheticOnly, "metadata-only read evidence", null),
                Scenario("synthetic-products", NexaSyntheticExternalScenarioKind.ProductListReadOnly, "/products", ["read-dom", "read-list"], [], NexaSyntheticExternalScenarioSensitivity.SyntheticOnly, "metadata-only product list evidence", null),
                Scenario("synthetic-document", NexaSyntheticExternalScenarioKind.DocumentReadOnly, "/document", ["read-dom", "read-document-summary"], [], NexaSyntheticExternalScenarioSensitivity.SyntheticOnly, "metadata-only document summary evidence", null),
                Scenario("synthetic-report", NexaSyntheticExternalScenarioKind.TableReportReadOnly, "/report", ["read-dom", "read-table"], [], NexaSyntheticExternalScenarioSensitivity.SyntheticOnly, "metadata-only table evidence", null),
                Scenario("synthetic-disabled-form", NexaSyntheticExternalScenarioKind.DisabledFormBlocked, "/disabled-form", ["read-dom"], ["submit"], NexaSyntheticExternalScenarioSensitivity.SyntheticOnly, "blocked submit explanation required", NexaOperatorBlockerScenario.IrreversibleActionBlocked),
                Scenario("synthetic-login-blocked", NexaSyntheticExternalScenarioKind.LoginBlocked, "/blocked-login", ["read-dom"], ["enter-credentials", "login"], NexaSyntheticExternalScenarioSensitivity.CredentialSurfaceBlocked, "credential blocker explanation required", NexaOperatorBlockerScenario.RealCredentialsBlocked),
                Scenario("synthetic-checkout-blocked", NexaSyntheticExternalScenarioKind.CheckoutPaymentBlocked, "/blocked-checkout", ["read-dom"], ["checkout", "pay", "submit"], NexaSyntheticExternalScenarioSensitivity.PaymentSurfaceBlocked, "payment blocker explanation required", NexaOperatorBlockerScenario.RealBillingBlocked),
                Scenario("synthetic-delete-blocked", NexaSyntheticExternalScenarioKind.DestructiveActionBlocked, "/blocked-delete", ["read-dom"], ["delete", "sign", "mutate"], NexaSyntheticExternalScenarioSensitivity.DestructiveSurfaceBlocked, "destructive action blocker explanation required", NexaOperatorBlockerScenario.IrreversibleActionBlocked)
            ],
            UsesInternet: false,
            UsesRealCustomerData: false,
            Redacted: true);

    private static NexaSyntheticExternalScenario Scenario(
        string id,
        NexaSyntheticExternalScenarioKind kind,
        string path,
        IReadOnlyList<string> allowed,
        IReadOnlyList<string> denied,
        NexaSyntheticExternalScenarioSensitivity sensitivity,
        string evidence,
        NexaOperatorBlockerScenario? blocker) =>
        new(id, kind, path, allowed, denied, sensitivity, evidence, blocker, UsesRealContent: false);
}

public sealed class NexaProofDryRunBinding
{
    private readonly NexaExternalProofHarness _harness = new();
    private readonly NexaExternalReadOnlyEvidencePackBuilder _evidence = new();
    private readonly NexaOperatorBlockerExplanationService _explanations = new();

    public NexaProofDryRunResult Run(NexaExternalTestOwnedTarget fixtureTarget, NexaSyntheticExternalScenario scenario)
    {
        var blocked = scenario.ExpectedDeniedActions.Count > 0;
        var request = new NexaExternalProofHarnessRequest(
            OptInEnabled: true,
            fixtureTarget,
            NexaTestOwnedExternalTargetFixtureFactory.FixtureHost,
            scenario.Path,
            "GET",
            WouldCaptureBodies: false,
            WouldCaptureSensitiveHeaderValues: false,
            WouldPersistCookies: false,
            WouldSubmit: blocked,
            "operator-fixture");
        var harnessDecision = _harness.Evaluate(request, DateTimeOffset.UtcNow);
        if (blocked && scenario.ExpectedBlockerExplanation is not null)
        {
            var explanation = _explanations.Explain(scenario.ExpectedBlockerExplanation.Value, ["proof-dry-run:redacted"]);
            harnessDecision = harnessDecision with
            {
                Explanation = explanation,
                CanExecuteReadOnlyNavigation = false
            };
        }

        var pack = _evidence.Build(harnessDecision, request, runtimeExecuted: false, runtimePassed: false);
        var status = blocked || harnessDecision.Decision == NexaExternalProofHarnessDecisionKind.BlockedPolicyViolation
            ? NexaProofDryRunStatus.DryRunBlockedByPolicy
            : NexaProofDryRunStatus.DryRunEvidenceGenerated;
        return new NexaProofDryRunResult(
            $"proof-dry-run-{scenario.ScenarioId}",
            status,
            scenario,
            harnessDecision,
            pack,
            ExecutedNetwork: false,
            ClosesM51M65: false,
            Redacted: true);
    }
}

public sealed class NexaTargetBindingReadinessEvaluator
{
    public const string RecommendedDomain = "nexa-lab.nodalos.com.ar";

    public NexaTargetBindingConfig CreateDefault(
        NexaTargetBindingDnsMode dnsMode = NexaTargetBindingDnsMode.Unknown,
        NexaTargetBindingVerificationStatus verificationStatus = NexaTargetBindingVerificationStatus.NotConfigured) =>
        new(
            RecommendedDomain,
            $"https://{RecommendedDomain}",
            "/ownership",
            "/health",
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { RecommendedDomain },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "/",
                "/products",
                "/document",
                "/report",
                "/disabled-form",
                "/blocked-login",
                "/blocked-checkout",
                "/blocked-destructive-action",
                "/health",
                "/ownership"
            },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/api", "/login", "/checkout/submit", "/submit", "/delete", "/pay" },
            NexaTargetBindingDeploymentProvider.VercelHobbyLab,
            dnsMode,
            verificationStatus);

    public NexaTargetBindingDecision Evaluate(NexaTargetBindingConfig config)
    {
        var reasons = new List<string>();
        if (!string.Equals(config.ExpectedDomain, RecommendedDomain, StringComparison.OrdinalIgnoreCase))
            reasons.Add("expected domain must be the approved nexa-lab.nodalos.com.ar subdomain");
        if (config.AllowedHosts.Count != 1 || !config.AllowedHosts.Contains(config.ExpectedDomain) || config.AllowedHosts.Contains("nodalos.com.ar"))
            reasons.Add("allowed hosts must include only the expected lab subdomain");
        if (!config.AllowedPaths.Contains(config.ExpectedHealthPath) || !config.AllowedPaths.Contains(config.ExpectedOwnershipPath))
            reasons.Add("health and ownership paths are required");
        if (config.DnsMode == NexaTargetBindingDnsMode.Unknown)
            reasons.Add("DNS mode is unknown");
        if (config.VerificationStatus != NexaTargetBindingVerificationStatus.OwnershipVerified)
            reasons.Add("HTTPS and ownership verification are not complete");

        var allowed = reasons.Count == 0 &&
            config.DeploymentProvider == NexaTargetBindingDeploymentProvider.VercelHobbyLab &&
            config.ExpectedBaseUrl == $"https://{config.ExpectedDomain}";

        return new NexaTargetBindingDecision(
            config,
            reasons.Count == 0 ? ["domain binding is candidate-ready for future opt-in live proof"] : reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            CandidateLiveProofAllowed: allowed,
            ExecutesNetwork: false,
            Redacted: true);
    }
}

public sealed class NexaLiveProofSafetyGate
{
    private readonly NexaTargetBindingReadinessEvaluator _binding = new();
    private readonly NexaExternalTestOwnedTargetEvaluator _target = new();
    private readonly NexaOperatorBlockerExplanationService _explanations = new();

    public NexaLiveProofSafetyGateDecision Evaluate(NexaLiveProofSafetyGateRequest request, DateTimeOffset nowUtc)
    {
        if (request.Binding is null)
            return Decision(NexaLiveProofSafetyGateStatus.LiveProofNotConfigured, ["target binding is not configured"], NexaOperatorBlockerScenario.MissingTestOwnedExternalTarget);

        var bindingDecision = _binding.Evaluate(request.Binding);
        if (request.Binding.DnsMode == NexaTargetBindingDnsMode.Unknown || request.Binding.VerificationStatus is NexaTargetBindingVerificationStatus.DnsPending or NexaTargetBindingVerificationStatus.VercelPending)
            return Decision(NexaLiveProofSafetyGateStatus.DnsPending, bindingDecision.ReasonCodes, NexaOperatorBlockerScenario.MissingTestOwnedExternalTarget);
        if (request.Binding.VerificationStatus == NexaTargetBindingVerificationStatus.NotConfigured)
            return Decision(NexaLiveProofSafetyGateStatus.HttpsPending, bindingDecision.ReasonCodes, NexaOperatorBlockerScenario.MissingTestOwnedExternalTarget);
        if (request.Binding.VerificationStatus == NexaTargetBindingVerificationStatus.HttpsReady)
            return Decision(NexaLiveProofSafetyGateStatus.OwnershipPending, ["ownership endpoint is not verified"], NexaOperatorBlockerScenario.MissingTestOwnedExternalTarget);
        if (!bindingDecision.CandidateLiveProofAllowed)
            return Decision(NexaLiveProofSafetyGateStatus.DnsPending, bindingDecision.ReasonCodes, NexaOperatorBlockerScenario.MissingTestOwnedExternalTarget);

        var targetDecision = _target.Evaluate(request.Target, nowUtc);
        if (targetDecision.Status == NexaExternalTestOwnedTargetStatus.BlockedSensitiveSurface)
            return Decision(NexaLiveProofSafetyGateStatus.BlockedSensitiveSurface, targetDecision.ReasonCodes, NexaOperatorBlockerScenario.IrreversibleActionBlocked);
        if (!targetDecision.AllowsReadOnlyProof || request.Target is null)
            return Decision(NexaLiveProofSafetyGateStatus.TargetPolicyRejected, targetDecision.ReasonCodes, NexaOperatorBlockerScenario.CorePermissionMissing);
        if (!request.HarnessOptInEnabled)
            return Decision(NexaLiveProofSafetyGateStatus.HarnessOptInMissing, ["external live proof harness opt-in is missing"], NexaOperatorBlockerScenario.SkippedTestsBlockExternalLive);
        if (request.OperatorApprovalRequired && string.IsNullOrWhiteSpace(request.OperatorApprovalRef))
            return Decision(NexaLiveProofSafetyGateStatus.OperatorApprovalMissing, ["operator approval reference is required"], NexaOperatorBlockerScenario.CorePermissionMissing);

        var violations = new List<string>();
        if (!request.Target.AllowedHosts.Contains(request.RequestedHost) || !request.Binding.AllowedHosts.Contains(request.RequestedHost))
            violations.Add("requested host is not allowlisted by target and binding");
        if (!request.Target.AllowedPaths.Contains(request.RequestedPath) || !request.Binding.AllowedPaths.Contains(request.RequestedPath) || request.Binding.DeniedPaths.Contains(request.RequestedPath))
            violations.Add("requested path is not allowlisted for read-only proof");
        if (!string.Equals(request.RequestedMethod, "GET", StringComparison.OrdinalIgnoreCase) || !request.Target.AllowedMethods.Contains(request.RequestedMethod))
            violations.Add("only GET read-only proof is allowed");
        if (request.WouldUseCredentials || request.WouldPersistPersonalCookies || request.WouldCaptureSensitiveHeaderValues || request.WouldCaptureBodies ||
            request.WouldSubmit || request.WouldMutate || request.WouldUsePaymentOrCheckoutOrRealLogin)
            violations.Add("credentials, cookies, bodies, sensitive headers, submit, mutation, payment, checkout, and real login are blocked");
        if (!request.EvidencePackReady)
            violations.Add("evidence pack readiness is required before live proof");

        if (violations.Count > 0)
            return Decision(NexaLiveProofSafetyGateStatus.BlockedSensitiveSurface, violations, NexaOperatorBlockerScenario.IrreversibleActionBlocked);

        return Decision(
            NexaLiveProofSafetyGateStatus.ReadyForReadOnlyLiveProof,
            ["ready for future opt-in read-only live proof; proof has not executed and M51/M65 remain blocked"],
            NexaOperatorBlockerScenario.SkippedTestsBlockExternalLive,
            ready: true);
    }

    private NexaLiveProofSafetyGateDecision Decision(
        NexaLiveProofSafetyGateStatus status,
        IReadOnlyList<string> reasons,
        NexaOperatorBlockerScenario scenario,
        bool ready = false) =>
        new(
            status,
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            _explanations.Explain(scenario, ["live-proof-safety-gate:redacted"]),
            ready,
            ClosesM51M65: false,
            ExecutesNetwork: false,
            Redacted: true);
}
