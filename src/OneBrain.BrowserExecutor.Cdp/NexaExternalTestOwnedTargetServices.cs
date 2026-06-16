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
    public NexaExternalReadOnlyEvidencePack Build(
        NexaExternalProofHarnessDecision harnessDecision,
        NexaExternalProofHarnessRequest request,
        bool runtimeExecuted,
        bool runtimePassed,
        NexaExternalProofProbeKind probeKind = NexaExternalProofProbeKind.ModeledFake)
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
        var tooling = ToolingFor(probeKind);
        var capabilities = CapabilitiesFor(probeKind);

        return new NexaExternalReadOnlyEvidencePack(
            $"external-readonly-proof-{Guid.NewGuid():N}",
            harnessDecision.TargetDecision.Target?.TargetId,
            harnessDecision.TargetDecision.Status,
            DateTimeOffset.UtcNow,
            tooling,
            capabilities,
            BrowserCredentialRedactor.Redact($"{request.RequestedMethod} {request.RequestedPath}"),
            harnessDecision.CanExecuteReadOnlyNavigation ? "read-only navigation" : "none",
            ["POST", "PUT", "PATCH", "DELETE", "submit", "pay", "sign", "delete", "upload"],
            BrowserCredentialRedactor.Redact(request.RequestedHost),
            BrowserCredentialRedactor.Redact(request.RequestedPath),
            BrowserCredentialRedactor.Redact(request.RequestedMethod),
            "response body fetched transiently for safety scan; body not persisted; redacted metadata and safety summary only; no cookies, sensitive headers, tokens, or personal data persisted",
            [],
            ["external-proof-harness:redacted"],
            harnessDecision.ReasonCodes,
            harnessDecision.CanExecuteReadOnlyNavigation ? [] : [harnessDecision.Explanation],
            candidate ? "candidate proof passed; M51/M65 still require explicit closure decision" : "external/live remains blocked or deferred",
            status,
            candidate,
            Redacted: true,
            probeKind,
            probeKind == NexaExternalProofProbeKind.ModeledFake ? NexaExternalEvidencePersistenceStatus.NotPersistedModeled : NexaExternalEvidencePersistenceStatus.NotPersisted,
            tooling);
    }

    private static string ToolingFor(NexaExternalProofProbeKind probeKind) =>
        probeKind switch
        {
            NexaExternalProofProbeKind.RealHttpClient => "HttpReadOnlyExternal",
            NexaExternalProofProbeKind.RealChromeCdp => "ChromeCdpExternalReadOnly",
            _ => "ModeledFake"
        };

    private static IReadOnlyList<string> CapabilitiesFor(NexaExternalProofProbeKind probeKind) =>
        probeKind switch
        {
            NexaExternalProofProbeKind.RealHttpClient => ["HttpGetReadOnly", "NetworkMetadataOnly", "CoreGoverned"],
            NexaExternalProofProbeKind.RealChromeCdp => ["BrowserNavigationReadOnly", "DomSnapshotReadOnly", "PageMetadataReadOnly", "NetworkMetadataRedacted", "CoreGoverned"],
            _ => ["ModeledReadOnly", "CoreGoverned"]
        };
}

public sealed class NexaExternalEvidenceLedgerPersistence
{
    public NexaExternalReadOnlyEvidencePack PersistIfEligible(NexaExternalReadOnlyEvidencePack pack, BrowserPersistentAuditLedger ledger)
    {
        if (pack.ProbeKind == NexaExternalProofProbeKind.ModeledFake)
            return pack with { PersistenceStatus = NexaExternalEvidencePersistenceStatus.NotPersistedModeled };
        if (pack.Status != NexaExternalReadOnlyEvidencePackStatus.PassedReadOnlyProof ||
            pack.TargetApprovalStatus != NexaExternalTestOwnedTargetStatus.ApprovedReadOnlyTestOwned ||
            !pack.Redacted ||
            ContainsUnsafeMaterial(pack))
            return pack with { PersistenceStatus = NexaExternalEvidencePersistenceStatus.PersistenceFailed };

        var ledgerEvent = BrowserPersistentAuditLedger.Create(
            BrowserAuditLedgerEventKind.NetworkCaptureRecorded,
            "external-readonly-proof",
            "external-proof-persist",
            pack.ProofId,
            "external-test-owned-profile",
            "external-readonly-session",
            null,
            null,
            null,
            "PassedReadOnlyProofPersisted",
            "redacted external read-only proof evidence persisted; response body not persisted",
            new Dictionary<string, string>
            {
                ["proofId"] = pack.ProofId,
                ["targetId"] = pack.TargetId ?? "unknown",
                ["probeKind"] = pack.ProbeKind.ToString(),
                ["tooling"] = pack.Tooling,
                ["host"] = pack.VisitedHost ?? "unknown",
                ["path"] = pack.VisitedPath ?? "unknown",
                ["method"] = pack.Method,
                ["status"] = pack.Status.ToString(),
                ["redactionSummary"] = pack.RedactionSummary,
                ["bodyPolicy"] = "body transiently scanned and not persisted"
            });
        var appended = ledger.Append(ledgerEvent);
        var seal = ledger.HeadSeal;

        return pack with
        {
            PersistenceStatus = NexaExternalEvidencePersistenceStatus.PersistedRedactedLedger,
            LedgerRef = appended.EventId,
            LedgerSequence = appended.Integrity.SequenceNumber,
            LedgerHash = appended.Integrity.EventHash,
            PersistedAtUtc = appended.CreatedAtUtc,
            LogRefs = pack.LogRefs.Concat([$"ledgerRef:{appended.EventId}", $"ledgerSequence:{appended.Integrity.SequenceNumber}", $"ledgerHash:{appended.Integrity.EventHash}", $"headSeal:{seal.LastEventHash}"]).ToArray()
        };
    }

    private static bool ContainsUnsafeMaterial(NexaExternalReadOnlyEvidencePack pack)
    {
        var serialized = System.Text.Json.JsonSerializer.Serialize(pack);
        return serialized.Contains("opaque-token-value-123456789", StringComparison.Ordinal) ||
            serialized.Contains("synthetic-cookie-session-value", StringComparison.Ordinal) ||
            serialized.Contains("synthetic-api-key-value", StringComparison.Ordinal) ||
            serialized.Contains("synthetic-bearer-token", StringComparison.Ordinal) ||
            serialized.Contains("set-cookie", StringComparison.OrdinalIgnoreCase) ||
            serialized.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
            serialized.Contains("<body", StringComparison.OrdinalIgnoreCase);
    }
}

public static class NexaTestOwnedExternalTargetFixtureFactory
{
    public const string FixtureHost = "nodal-os-test-owned.fixture.local";

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
    public const string RecommendedDomain = "lab.nodalos.com.ar";
    public const string LegacyDeactivatedDomain = "nexalab.nodalos.com.ar";

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
            reasons.Add("expected domain must be the approved lab.nodalos.com.ar subdomain");
        if (config.AllowedHosts.Count != 1 || !config.AllowedHosts.Contains(config.ExpectedDomain) || config.AllowedHosts.Contains("nodalos.com.ar") || config.AllowedHosts.Contains(LegacyDeactivatedDomain))
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

public sealed class NexaHttpClientReadOnlyProbe(HttpClient? httpClient = null) : INexaReadOnlyHttpProbe
{
    private readonly HttpClient _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

    public async Task<NexaReadOnlyHttpProbeResult> GetAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var headerNames = response.Headers.Select(header => header.Key)
            .Concat(response.Content.Headers.Select(header => header.Key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new NexaReadOnlyHttpProbeResult(
            (int)response.StatusCode,
            BrowserCredentialRedactor.Redact(text),
            headerNames,
            CapturedCookies: false,
            CapturedBodies: false,
            CapturedSensitiveHeaderValues: false);
    }
}

public sealed class NexaHttpsOwnershipVerifier(INexaReadOnlyHttpProbe? probe = null)
{
    public static NexaHttpsOwnershipVerificationRequest DefaultRequest(bool optInLiveNetwork = false) =>
        new(
            $"https://{NexaTargetBindingReadinessEvaluator.RecommendedDomain}",
            "/health/",
            "/ownership/",
            ["NODAL OS", "test-owned", "read-only", "no-real-users", "no-real-credentials", "no-real-payments", "no-submit"],
            "Vercel",
            "Shift Evidence",
            "lab",
            optInLiveNetwork);

    private readonly INexaReadOnlyHttpProbe _probe = probe ?? new NexaHttpClientReadOnlyProbe();

    public async Task<NexaHttpsOwnershipVerificationResult> VerifyAsync(NexaHttpsOwnershipVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var healthUrl = Combine(request.ExpectedBaseUrl, request.RequiredHealthPath);
        var ownershipUrl = Combine(request.ExpectedBaseUrl, request.RequiredOwnershipPath);
        var evidence = new List<string>
        {
            $"baseUrl:{BrowserCredentialRedactor.Redact(request.ExpectedBaseUrl)}",
            $"healthUrl:{BrowserCredentialRedactor.Redact(healthUrl)}",
            $"ownershipUrl:{BrowserCredentialRedactor.Redact(ownershipUrl)}",
            "dnsMode:delegated-to-vercel",
            $"provider:{request.DeploymentProvider}",
            $"scope:{request.DeploymentScope}",
            $"project:{request.DeploymentProject}",
            "restrictions:no-real-users/no-real-credentials/no-real-payments/no-submit/read-only"
        };

        if (!request.OptInLiveNetwork)
            return Result(NexaHttpsOwnershipVerificationStatus.NotChecked, request, healthUrl, ownershipUrl, null, null, evidence, ["live HTTPS ownership verification opt-in missing"], restrictions: false, candidate: false, executed: false);
        if (!Uri.TryCreate(request.ExpectedBaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
            return Result(NexaHttpsOwnershipVerificationStatus.HttpsReady, request, healthUrl, ownershipUrl, null, null, evidence, ["expected base URL must be HTTPS"], restrictions: false, candidate: false, executed: false);
        if (!string.Equals(baseUri.Host, NexaTargetBindingReadinessEvaluator.RecommendedDomain, StringComparison.OrdinalIgnoreCase))
            return Result(NexaHttpsOwnershipVerificationStatus.VerificationFailed, request, healthUrl, ownershipUrl, null, null, evidence, ["host does not match configured test-owned target"], restrictions: false, candidate: false, executed: false);

        try
        {
            var health = await _probe.GetAsync(new Uri(healthUrl), cancellationToken).ConfigureAwait(false);
            if (health.StatusCode != 200)
                return Result(NexaHttpsOwnershipVerificationStatus.VerificationFailed, request, healthUrl, ownershipUrl, health.StatusCode, null, evidence, ["health endpoint did not return HTTP 200"], restrictions: false, candidate: false, executed: true);
            if (UnsafeCapture(health))
                return Result(NexaHttpsOwnershipVerificationStatus.VerificationFailed, request, healthUrl, ownershipUrl, health.StatusCode, null, evidence, ["health probe captured unsafe material"], restrictions: false, candidate: false, executed: true);

            var ownership = await _probe.GetAsync(new Uri(ownershipUrl), cancellationToken).ConfigureAwait(false);
            if (ownership.StatusCode != 200)
                return Result(NexaHttpsOwnershipVerificationStatus.VerificationFailed, request, healthUrl, ownershipUrl, health.StatusCode, ownership.StatusCode, evidence, ["ownership endpoint did not return HTTP 200"], restrictions: false, candidate: false, executed: true);
            if (UnsafeCapture(ownership))
                return Result(NexaHttpsOwnershipVerificationStatus.VerificationFailed, request, healthUrl, ownershipUrl, health.StatusCode, ownership.StatusCode, evidence, ["ownership probe captured unsafe material"], restrictions: false, candidate: false, executed: true);

            var combined = $"{health.RedactedText}\n{ownership.RedactedText}";
            var missing = request.ExpectedProjectMetadata
                .Where(token => !ContainsMetadataToken(combined, token))
                .ToArray();
            if (missing.Length > 0)
                return Result(NexaHttpsOwnershipVerificationStatus.MetadataMismatch, request, healthUrl, ownershipUrl, health.StatusCode, ownership.StatusCode, evidence, missing.Select(token => $"metadata missing: {token}").ToArray(), restrictions: false, candidate: false, executed: true);

            return Result(NexaHttpsOwnershipVerificationStatus.VerifiedTestOwnedReadOnlyTarget, request, healthUrl, ownershipUrl, health.StatusCode, ownership.StatusCode, evidence, ["verified test-owned read-only target"], restrictions: true, candidate: true, executed: true);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return Result(NexaHttpsOwnershipVerificationStatus.VerificationFailed, request, healthUrl, ownershipUrl, null, null, evidence, [$"verification failed: {ex.GetType().Name}"], restrictions: false, candidate: false, executed: true);
        }
    }

    private static bool UnsafeCapture(NexaReadOnlyHttpProbeResult result) =>
        result.CapturedCookies ||
        result.CapturedBodies ||
        result.CapturedSensitiveHeaderValues ||
        ContainsUnsafeProofMaterial(result.RedactedText);

    private static bool ContainsUnsafeProofMaterial(string value) =>
        value.Contains("opaque-token", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("api-key", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("bearer ", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("refresh-token", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("session-cookie", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("set-cookie", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsMetadataToken(string text, string token)
    {
        var redactedToken = BrowserCredentialRedactor.Redact(token);
        return text.Contains(token, StringComparison.OrdinalIgnoreCase) ||
            text.Contains(redactedToken, StringComparison.OrdinalIgnoreCase);
    }

    private static NexaHttpsOwnershipVerificationResult Result(
        NexaHttpsOwnershipVerificationStatus status,
        NexaHttpsOwnershipVerificationRequest request,
        string healthUrl,
        string ownershipUrl,
        int? healthStatus,
        int? ownershipStatus,
        IReadOnlyList<string> evidence,
        IReadOnlyList<string> reasons,
        bool restrictions,
        bool candidate,
        bool executed) =>
        new(
            status,
            BrowserCredentialRedactor.Redact(request.ExpectedBaseUrl),
            BrowserCredentialRedactor.Redact(healthUrl),
            BrowserCredentialRedactor.Redact(ownershipUrl),
            healthStatus,
            ownershipStatus,
            evidence.Select(BrowserCredentialRedactor.Redact).ToArray(),
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            restrictions,
            candidate,
            ClosesM51M65: false,
            ExecutedNetwork: executed,
            Redacted: true);

    private static string Combine(string baseUrl, string path) =>
        $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
}

public sealed class NexaFirstReadOnlyLiveProofRunner(INexaReadOnlyHttpProbe? probe = null)
{
    private static readonly string[] AllowedRoutes = ["/", "/health/", "/ownership/", "/products/", "/document/", "/report/"];
    private static readonly string[] DeniedRoutes = ["/disabled-form/", "/blocked-login/", "/blocked-checkout/", "/blocked-destructive-action/"];

    private readonly INexaReadOnlyHttpProbe _probe = probe ?? new NexaHttpClientReadOnlyProbe();
    private readonly NexaHttpsOwnershipVerifier _verifier = new(probe);
    private readonly NexaExternalProofHarness _harness = new();
    private readonly NexaExternalReadOnlyEvidencePackBuilder _evidence = new();
    private readonly NexaExternalEvidenceLedgerPersistence _persistence = new();
    private readonly NexaLiveProofSafetyGate _gate = new();
    private readonly NexaOperatorBlockerExplanationService _explanations = new();
    private readonly NexaExternalProofProbeKind _probeKind = probe is null || probe is NexaHttpClientReadOnlyProbe
        ? NexaExternalProofProbeKind.RealHttpClient
        : NexaExternalProofProbeKind.ModeledFake;

    public Task<NexaFirstReadOnlyLiveProofResult> RunAsync(bool optIn, bool executeNetwork, CancellationToken cancellationToken = default) =>
        RunAsync(optIn, executeNetwork, ledger: null, cancellationToken);

    public async Task<NexaFirstReadOnlyLiveProofResult> RunAsync(bool optIn, bool executeNetwork, BrowserPersistentAuditLedger? ledger, CancellationToken cancellationToken = default)
    {
        var target = CreateLiveTarget();
        var binding = new NexaTargetBindingReadinessEvaluator().CreateDefault(
            NexaTargetBindingDnsMode.DelegatedToVercel,
            NexaTargetBindingVerificationStatus.OwnershipVerified);
        var verification = executeNetwork
            ? await _verifier.VerifyAsync(NexaHttpsOwnershipVerifier.DefaultRequest(optIn), cancellationToken).ConfigureAwait(false)
            : ModeledVerification(optIn);
        var safety = _gate.Evaluate(GateRequest(binding, target, optIn), DateTimeOffset.UtcNow);
        var request = new NexaExternalProofHarnessRequest(optIn, target, NexaTargetBindingReadinessEvaluator.RecommendedDomain, "/", "GET", false, false, false, false, "operator-live-proof");
        var harness = _harness.Evaluate(request, DateTimeOffset.UtcNow);

        if (!optIn)
        {
            var skippedPack = _evidence.Build(harness, request, runtimeExecuted: false, runtimePassed: false, _probeKind);
            return Build(NexaFirstReadOnlyLiveProofStatus.SkippedNoOptIn, verification, safety, skippedPack, [], [], [safety.Explanation], executed: false);
        }

        if (verification.Status != NexaHttpsOwnershipVerificationStatus.VerifiedTestOwnedReadOnlyTarget)
        {
            var failedPack = _evidence.Build(harness, request, runtimeExecuted: false, runtimePassed: false, _probeKind);
            return Build(NexaFirstReadOnlyLiveProofStatus.BlockedVerificationFailed, verification, safety, failedPack, [], [], [safety.Explanation], executed: verification.ExecutedNetwork);
        }

        if (!safety.ReadyForReadOnlyLiveProof || !harness.CanExecuteReadOnlyNavigation)
        {
            var blockedPack = _evidence.Build(harness, request, runtimeExecuted: false, runtimePassed: false, _probeKind);
            return Build(NexaFirstReadOnlyLiveProofStatus.BlockedPolicyViolation, verification, safety, blockedPack, [], DeniedRoutes, [safety.Explanation], executed: verification.ExecutedNetwork);
        }

        if (!executeNetwork)
        {
            var allowedPack = _evidence.Build(harness, request, runtimeExecuted: false, runtimePassed: false, _probeKind);
            return Build(NexaFirstReadOnlyLiveProofStatus.CandidateRunnerAllowed, verification, safety, allowedPack, AllowedRoutes, DeniedRoutes, DeniedExplanations(), executed: false);
        }

        foreach (var route in AllowedRoutes)
        {
            var url = new Uri($"{NexaHttpsOwnershipVerifier.DefaultRequest(true).ExpectedBaseUrl}{route}");
            var probeResult = await _probe.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (probeResult.StatusCode != 200 || probeResult.CapturedCookies || probeResult.CapturedBodies || probeResult.CapturedSensitiveHeaderValues || ContainsUnsafeProofMaterial(probeResult.RedactedText))
            {
                var failedPack = _evidence.Build(harness, request, runtimeExecuted: true, runtimePassed: false, _probeKind);
                return Build(NexaFirstReadOnlyLiveProofStatus.FailedRuntime, verification, safety, failedPack, AllowedRoutes, DeniedRoutes, DeniedExplanations(), executed: true);
            }
        }

        var pack = _evidence.Build(harness, request, runtimeExecuted: true, runtimePassed: true, _probeKind) with
        {
            LogRefs = ["provider:Vercel", "scope:Shift Evidence", "project:lab", $"domain:{NexaTargetBindingReadinessEvaluator.RecommendedDomain}", "routes:/,/health/,/ownership/,/products/,/document/,/report/"]
        };
        if (ledger is not null)
            pack = _persistence.PersistIfEligible(pack, ledger);
        return Build(NexaFirstReadOnlyLiveProofStatus.PassedReadOnlyProof, verification, safety, pack, AllowedRoutes, DeniedRoutes, DeniedExplanations(), executed: true);
    }

    public static NexaExternalTestOwnedTarget CreateLiveTarget() =>
        new(
            "nodal-os-vercel-test-owned-readonly",
            $"https://{NexaTargetBindingReadinessEvaluator.RecommendedDomain}/",
            NexaExternalTargetOwnershipProofMode.RepositoryControlledDeployment,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { NexaTargetBindingReadinessEvaluator.RecommendedDomain },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "afip.gob.ar", "bank.example.invalid", "banco.example.invalid", "gov.example.invalid" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/", "/health/", "/ownership/", "/products/", "/document/", "/report/" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/disabled-form/", "/blocked-login/", "/blocked-checkout/", "/blocked-destructive-action/", "/api", "/submit", "/pay", "/delete" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" },
            NexaExternalTargetCredentialPolicy.NoCredentials,
            NexaExternalTargetSubmitPolicy.ReadOnlyNoSubmit,
            NexaExternalTargetDataSensitivityProfile.LowRiskSynthetic,
            NexaExternalTargetEvidencePolicy.MetadataOnlyRedacted,
            "Vercel Shift Evidence project lab, synthetic read-only target",
            DateTimeOffset.UtcNow.AddDays(30),
            "Shift Evidence",
            "approval:lab-vercel-readonly",
            ExplicitlyTestOwned: true);

    private static NexaLiveProofSafetyGateRequest GateRequest(NexaTargetBindingConfig binding, NexaExternalTestOwnedTarget target, bool optIn) =>
        new(binding, target, optIn, NexaTargetBindingReadinessEvaluator.RecommendedDomain, "/", "GET", false, false, false, false, false, false, false, true, false, "approval:lab-vercel-readonly");

    private static NexaHttpsOwnershipVerificationResult ModeledVerification(bool optIn)
    {
        var request = NexaHttpsOwnershipVerifier.DefaultRequest(optInLiveNetwork: false);
        var healthUrl = $"{request.ExpectedBaseUrl}/health/";
        var ownershipUrl = $"{request.ExpectedBaseUrl}/ownership/";
        return new NexaHttpsOwnershipVerificationResult(
            optIn ? NexaHttpsOwnershipVerificationStatus.VerifiedTestOwnedReadOnlyTarget : NexaHttpsOwnershipVerificationStatus.NotChecked,
            request.ExpectedBaseUrl,
            healthUrl,
            ownershipUrl,
            optIn ? 200 : null,
            optIn ? 200 : null,
            [
                $"baseUrl:{request.ExpectedBaseUrl}",
                $"healthUrl:{healthUrl}",
                $"ownershipUrl:{ownershipUrl}",
                "provider:Vercel",
                "scope:Shift Evidence",
                "project:lab",
                "mode:modeled-no-network"
            ],
            optIn ? ["modeled HTTPS ownership verification; live network not executed"] : ["live HTTPS ownership verification opt-in missing"],
            RestrictionsConfirmed: optIn,
            EnablesCandidateLiveProof: optIn,
            ClosesM51M65: false,
            ExecutedNetwork: false,
            Redacted: true);
    }

    private IReadOnlyList<NexaOperatorBlockerExplanation> DeniedExplanations() =>
    [
        _explanations.Explain(NexaOperatorBlockerScenario.IrreversibleActionBlocked, ["live-proof-denied-route:redacted"]),
        _explanations.Explain(NexaOperatorBlockerScenario.RealCredentialsBlocked, ["live-proof-denied-route:redacted"]),
        _explanations.Explain(NexaOperatorBlockerScenario.RealBillingBlocked, ["live-proof-denied-route:redacted"])
    ];

    private static NexaFirstReadOnlyLiveProofResult Build(
        NexaFirstReadOnlyLiveProofStatus status,
        NexaHttpsOwnershipVerificationResult verification,
        NexaLiveProofSafetyGateDecision safety,
        NexaExternalReadOnlyEvidencePack pack,
        IReadOnlyList<string> routes,
        IReadOnlyList<string> deniedRoutes,
        IReadOnlyList<NexaOperatorBlockerExplanation> explanations,
        bool executed) =>
        new(status, verification, safety, pack, routes, deniedRoutes, explanations, executed, Redacted: true);

    private static bool ContainsUnsafeProofMaterial(string value) =>
        value.Contains("opaque-token", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("api-key", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("bearer ", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("refresh-token", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("session-cookie", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("set-cookie", StringComparison.OrdinalIgnoreCase);
}

public static class NodalOsExternalLiveProofOptIn
{
    public const string CurrentEnvironmentVariable = "NODAL_OS_EXTERNAL_LIVE_PROOF_OPT_IN";
    public const string LegacyEnvironmentVariable = "NEXA_EXTERNAL_LIVE_PROOF_OPT_IN";

    public static bool IsEnabled(Func<string, string?>? getEnvironmentVariable = null)
    {
        var read = getEnvironmentVariable ?? Environment.GetEnvironmentVariable;
        return IsTrue(read(CurrentEnvironmentVariable)) || IsTrue(read(LegacyEnvironmentVariable));
    }

    private static bool IsTrue(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}

public sealed class NexaM51M65ClosureCandidateReviewer
{
    public NexaM51M65ClosureCandidateReview Review(NexaFirstReadOnlyLiveProofResult proof)
    {
        var violations = new List<string>();
        if (!proof.Redacted || !proof.EvidencePack.Redacted)
            violations.Add("proof or evidence pack is not redacted");
        if (proof.EvidencePack.RedactionSummary.Contains("cookie", StringComparison.OrdinalIgnoreCase) && !proof.EvidencePack.RedactionSummary.Contains("no cookies", StringComparison.OrdinalIgnoreCase))
            violations.Add("cookie material detected");
        if (proof.EvidencePack.PolicyDecisions.Any(IsSensitivePolicyText))
            violations.Add("secret-like policy decision detected");
        if (proof.EvidencePack.ProbeKind == NexaExternalProofProbeKind.ModeledFake)
            violations.Add("modeled fake probe cannot support closure candidate");
        if (proof.EvidencePack.ProbeKind == NexaExternalProofProbeKind.RealHttpClient &&
            (proof.EvidencePack.Tooling != "HttpReadOnlyExternal" ||
             proof.EvidencePack.RuntimeProvider != "HttpReadOnlyExternal" ||
             proof.EvidencePack.RuntimeCapabilities.Contains("ChromeCdpExternal") ||
             proof.EvidencePack.RuntimeCapabilities.Contains("DomReadOnly") ||
             proof.EvidencePack.RuntimeCapabilities.Contains("NavigationReadOnly")))
            violations.Add("HttpClient proof tooling/capabilities are not honest");
        if (proof.EvidencePack.ProbeKind is NexaExternalProofProbeKind.RealHttpClient or NexaExternalProofProbeKind.RealChromeCdp &&
            (string.IsNullOrWhiteSpace(proof.EvidencePack.LedgerRef) ||
             proof.EvidencePack.PersistenceStatus != NexaExternalEvidencePersistenceStatus.PersistedRedactedLedger))
            violations.Add("real proof requires persisted redacted ledger evidence");

        var decision = proof.Status switch
        {
            NexaFirstReadOnlyLiveProofStatus.SkippedNoOptIn => NexaM51M65ClosureCandidateReviewDecision.LiveProofSkippedNoOptIn,
            NexaFirstReadOnlyLiveProofStatus.BlockedVerificationFailed => NexaM51M65ClosureCandidateReviewDecision.LiveProofFailed,
            NexaFirstReadOnlyLiveProofStatus.FailedRuntime => NexaM51M65ClosureCandidateReviewDecision.LiveProofFailed,
            NexaFirstReadOnlyLiveProofStatus.PassedReadOnlyProof when
                proof.ExecutedNetwork &&
                proof.Verification.Status == NexaHttpsOwnershipVerificationStatus.VerifiedTestOwnedReadOnlyTarget &&
                proof.EvidencePack.Status == NexaExternalReadOnlyEvidencePackStatus.PassedReadOnlyProof &&
                proof.EvidencePack.CandidateForM51M65Closure &&
                proof.EvidencePack.ProbeKind is NexaExternalProofProbeKind.RealHttpClient or NexaExternalProofProbeKind.RealChromeCdp &&
                !string.IsNullOrWhiteSpace(proof.EvidencePack.LedgerRef) &&
                proof.EvidencePack.PersistenceStatus == NexaExternalEvidencePersistenceStatus.PersistedRedactedLedger &&
                violations.Count == 0 => NexaM51M65ClosureCandidateReviewDecision.CandidateCloseM51AndM65,
            NexaFirstReadOnlyLiveProofStatus.PassedReadOnlyProof => NexaM51M65ClosureCandidateReviewDecision.LiveProofPassedButReviewRequired,
            NexaFirstReadOnlyLiveProofStatus.CandidateRunnerAllowed => NexaM51M65ClosureCandidateReviewDecision.DoNotClose,
            _ => NexaM51M65ClosureCandidateReviewDecision.NoLiveProofExecuted
        };

        if (violations.Count > 0)
            decision = NexaM51M65ClosureCandidateReviewDecision.DoNotClose;

        if (decision == NexaM51M65ClosureCandidateReviewDecision.CandidateCloseM51AndM65)
            decision = NexaM51M65ClosureCandidateReviewDecision.CandidateCloseM51Only;

        var canCandidate = decision == NexaM51M65ClosureCandidateReviewDecision.CandidateCloseM51Only;
        return new NexaM51M65ClosureCandidateReview(
            proof.EvidencePack.ProofId,
            proof.EvidencePack.TargetId ?? "unknown",
            NexaTargetBindingReadinessEvaluator.RecommendedDomain,
            "Vercel",
            "Shift Evidence/lab",
            proof.Verification.Status,
            proof.Status,
            proof.RoutesTested,
            proof.DeniedRoutesTested,
            proof.EvidencePack.Status,
            proof.Redacted && proof.EvidencePack.Redacted,
            violations.Select(BrowserCredentialRedactor.Redact).ToArray(),
            proof.BlockerExplanations,
            canCandidate ? "candidate close M51 after explicit review acceptance" : "do not close M51",
            canCandidate ? "M65 deferred; requires dedicated browser/CDP or auth-target evidence" : "M65 deferred; requires dedicated evidence",
            decision,
            PublicSaasStillDisabled: true,
            RealBillingStillDisabled: true,
            RealEmailStillDisabled: true,
            RealCredentialsStillBlocked: true,
            SensitiveSurfacesStillBlocked: true,
            Redacted: true);
    }

    private static bool IsSensitivePolicyText(string value) =>
        BrowserCredentialRedactor.ContainsSecret(value) ||
        value.Contains("token", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("cookie", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("secret", StringComparison.OrdinalIgnoreCase);
}
