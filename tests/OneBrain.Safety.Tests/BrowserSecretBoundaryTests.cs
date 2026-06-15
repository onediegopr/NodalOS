using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserSecretBoundaryTests
{
    [TestMethod]
    public async Task BrowserSecretNullVaultDeniesEverythingByDefault()
    {
        var vault = new NullBrowserSecretVault();
        var result = await vault.RequestAccessAsync(Request(Reference(BrowserSecretKind.Password)), BrowserSecretAccessPolicy.DenyAll);

        Assert.AreEqual(BrowserSecretAccessDecisionKind.Denied, result.Decision.Decision);
        Assert.IsFalse(result.Decision.AllowsAccess);
        Assert.AreEqual(1, vault.AuditEvents.Count);
        Assert.IsTrue(result.Validate().IsValid);
    }

    [TestMethod]
    public async Task BrowserSecretInMemoryVaultAllowsOnlySyntheticSecretsUnderPolicy()
    {
        var vault = new InMemoryTestOnlySecretVault();
        var reference = vault.StoreSyntheticSecret(BrowserSecretKind.ApiKey, BrowserSecretScope.Temporary, "test-owner", "fixture", "synthetic://api-key");
        var policy = Policy(Set(BrowserSecretKind.ApiKey), Set(BrowserSecretScope.Temporary));

        var allowed = await vault.RequestAccessAsync(Request(reference, BrowserSecretUsageIntent.ReadOnlyReference), policy);
        var denied = await vault.RequestAccessAsync(Request(Reference(BrowserSecretKind.ApiKey), BrowserSecretUsageIntent.ReadOnlyReference), policy);

        Assert.AreEqual(BrowserSecretAccessDecisionKind.Allowed, allowed.Decision.Decision);
        Assert.AreEqual(BrowserSecretAccessDecisionKind.Denied, denied.Decision.Decision);
        Assert.IsNotNull(allowed.Reference);
        Assert.IsTrue(allowed.Validate().IsValid);
        Assert.IsFalse(allowed.Diagnostic.Contains("synthetic://", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task BrowserSecretInMemoryVaultReturnsCanonicalReferenceAndRejectsAlteredMetadata()
    {
        var vault = new InMemoryTestOnlySecretVault();
        var reference = vault.StoreSyntheticSecret(BrowserSecretKind.ApiKey, BrowserSecretScope.Temporary, "test-owner", "fixture", "synthetic://api-key");
        var altered = reference with { Scope = BrowserSecretScope.Runtime, RedactedLabel = "ApiKey Runtime [REDACTED]" };
        var policy = Policy(Set(BrowserSecretKind.ApiKey), Set(BrowserSecretScope.Temporary));

        var allowed = await vault.RequestAccessAsync(Request(reference), policy);
        var denied = await vault.RequestAccessAsync(Request(altered), policy);

        Assert.AreEqual(BrowserSecretAccessDecisionKind.Allowed, allowed.Decision.Decision);
        Assert.AreEqual(reference, allowed.Reference);
        Assert.AreEqual(BrowserSecretAccessDecisionKind.Denied, denied.Decision.Decision);
        Assert.IsNull(denied.Reference);
    }

    [TestMethod]
    public void BrowserSecretInMemoryVaultRejectsNonSyntheticValues()
    {
        var vault = new InMemoryTestOnlySecretVault();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            vault.StoreSyntheticSecret(BrowserSecretKind.Password, BrowserSecretScope.Temporary, "test-owner", "fixture", "password=real"));
    }

    [TestMethod]
    public void BrowserSecretEvaluatorDoesNotExposeProductiveForcedAllowed()
    {
        var method = typeof(BrowserSecretAccessPolicyEvaluator).GetMethod(nameof(BrowserSecretAccessPolicyEvaluator.Decide), [typeof(BrowserSecretAccessRequest), typeof(BrowserSecretAccessPolicy)]);

        Assert.IsNotNull(method);
        Assert.IsNull(typeof(BrowserSecretAccessPolicyEvaluator).GetMethods().SingleOrDefault(methodInfo =>
            methodInfo.Name == nameof(BrowserSecretAccessPolicyEvaluator.Decide) &&
            methodInfo.GetParameters().Any(parameter => parameter.Name == "forcedDecision")));
    }

    [TestMethod]
    public void BrowserSecretLikeIdentifiersFailClosedAndAuditIsRedacted()
    {
        var request = Request(Reference(BrowserSecretKind.ApiKey) with { SecretId = "token=raw-secret-value" }) with
        {
            CorrelationId = "cookie=session-value",
            RequestId = "header.payload.signature"
        };

        var decision = new BrowserSecretAccessPolicyEvaluator().Decide(request, Policy(Set(BrowserSecretKind.ApiKey), Set(BrowserSecretScope.Temporary)));

        Assert.AreEqual(BrowserSecretAccessDecisionKind.FailClosed, decision.Decision);
        Assert.IsFalse(decision.AuditEvent.SecretId.Contains("raw-secret-value", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(decision.AuditEvent.Validate().Errors.Any(error => error.Contains("SecretId", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(decision.AuditEvent.RedactedSummary));
    }

    [TestMethod]
    public void BrowserSecretSafeIdentifiersRemainValid()
    {
        var request = Request(Reference(BrowserSecretKind.ApiKey));

        Assert.IsTrue(request.Validate().IsValid);
    }

    [TestMethod]
    public void BrowserSecretUnknownSecretFailsClosed()
    {
        var decision = new BrowserSecretAccessPolicyEvaluator().Decide(
            Request(Reference(BrowserSecretKind.UnknownSensitiveSecret), BrowserSecretUsageIntent.Unknown),
            Policy(Set(BrowserSecretKind.UnknownSensitiveSecret), Set(BrowserSecretScope.Runtime)));

        Assert.AreEqual(BrowserSecretAccessDecisionKind.FailClosed, decision.Decision);
        Assert.IsTrue(decision.BlocksAccess);
    }

    [TestMethod]
    public void BrowserSecretPasswordAccessRequiresHumanAndCookieIsDeniedByDefault()
    {
        var evaluator = new BrowserSecretAccessPolicyEvaluator();
        var password = evaluator.Decide(Request(Reference(BrowserSecretKind.Password), BrowserSecretUsageIntent.FillCredential), Policy(Set(BrowserSecretKind.Password), Set(BrowserSecretScope.Person)));
        var cookie = evaluator.Decide(Request(Reference(BrowserSecretKind.SessionCookie), BrowserSecretUsageIntent.AttachCookie), Policy(Set(BrowserSecretKind.SessionCookie), Set(BrowserSecretScope.Session)));

        Assert.AreEqual(BrowserSecretAccessDecisionKind.RequiresHuman, password.Decision);
        Assert.AreEqual(BrowserSecretAccessDecisionKind.Denied, cookie.Decision);
    }

    [TestMethod]
    public void BrowserSecretAccessGeneratesRedactedAuditEvent()
    {
        var request = Request(Reference(BrowserSecretKind.AccessToken), BrowserSecretUsageIntent.ReadOnlyReference) with
        {
            Reason = BrowserCredentialRedactor.Redact("access_" + "token=synthetic-value")
        };
        var decision = new BrowserSecretAccessPolicyEvaluator().Decide(request, Policy(Set(BrowserSecretKind.AccessToken), Set(BrowserSecretScope.Runtime)));

        Assert.IsTrue(decision.AuditEvent.RedactionApplied);
        Assert.IsTrue(decision.AuditEvent.Validate().IsValid);
        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(decision.AuditEvent.RedactedSummary));
    }

    [TestMethod]
    public void BrowserSecretValuesDoNotAppearInDiagnosticsEvidenceHandoffOrCompanionProtocol()
    {
        var raw = "client_" + "secret=synthetic-value";
        var redacted = BrowserCredentialRedactor.Redact(raw);
        var evidence = new BrowserEvidence(
            EvidenceId: "evidence-secret",
            RunId: "run-1",
            StepId: null,
            ActionId: null,
            VerificationId: null,
            TargetContext: null,
            EvidenceType: BrowserEvidenceType.Log,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Summary: redacted,
            PayloadRef: null,
            InlinePayload: redacted,
            RedactionApplied: true,
            SensitivityLevel: BrowserSensitivityLevel.Secret);
        var request = HandoffRequest(redacted);
        var presentation = new BrowserHumanHandoffCompanionAdapter().CreatePresentation(request);
        var companionEvent = new BrowserHumanHandoffUiEvent(
            BrowserHumanHandoffUiEventKind.HandoffUserCompleted,
            request.RequestId,
            request.Context.RunId,
            request.Context.ActionId,
            request.Context.CorrelationId,
            BrowserHumanHandoffCompanionAdapter.RuntimeKind,
            BrowserHumanHandoffCompanionAdapter.CompanionSource,
            Authoritative: false,
            VerificationStatus: BrowserVerificationStatus.Verified,
            EvidenceRefs: [],
            ProofRefs: [],
            Redacted: true,
            Diagnostics: redacted);

        Assert.IsTrue(evidence.Validate().IsValid);
        Assert.IsTrue(request.Validate().IsValid);
        Assert.IsTrue(presentation.Validate().IsValid);
        Assert.IsTrue(companionEvent.Validate().IsValid);
        Assert.IsFalse(evidence.InlinePayload!.Contains("synthetic-value", StringComparison.Ordinal));
        Assert.IsFalse(presentation.Instruction.Contains("synthetic-value", StringComparison.Ordinal));
        Assert.IsFalse(companionEvent.Diagnostics.Contains("synthetic-value", StringComparison.Ordinal));
        Assert.IsFalse(companionEvent.CanMarkSuccess);
    }

    [TestMethod]
    public async Task BrowserSecretHandoffUserCompletedDoesNotCreateOrStoreSecret()
    {
        var vault = new NullBrowserSecretVault();
        var target = Target();
        var action = Action(target);
        var boundary = new BrowserCredentialBoundaryDetector().EvaluateAction(action);
        var request = new BrowserHumanHandoffCoordinator().CreateRequest(boundary, action, "corr-1", "profile-1", "session-1");
        var decision = new BrowserHumanHandoffCoordinator().TryResume(request, BrowserHumanHandoffStatus.UserCompleted, target, Session(), null, DateTimeOffset.UtcNow);

        Assert.IsFalse(decision.Success);
        Assert.AreEqual(0, vault.AuditEvents.Count);
        await Task.CompletedTask;
    }

    private static BrowserSecretAccessPolicy Policy(IReadOnlySet<BrowserSecretKind> kinds, IReadOnlySet<BrowserSecretScope> scopes) =>
        new(
            DenyByDefault: true,
            AllowedKinds: kinds,
            AllowedScopes: scopes,
            AllowCredentialFill: false,
            AllowCookieAccess: false,
            RequiresHumanForPassword: true,
            RequiresApprovalForSensitiveSubmit: true,
            AllowSyntheticTestSecretsOnly: true);

    private static IReadOnlySet<T> Set<T>(params T[] values) where T : notnull => new HashSet<T>(values);

    private static BrowserSecretReference Reference(BrowserSecretKind kind) =>
        new(
            SecretId: $"secret-ref-{Guid.NewGuid():N}",
            Kind: kind,
            Scope: BrowserSecretScope.Temporary,
            Owner: "test-owner",
            Portal: "fixture",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            RedactedLabel: $"{kind}:[REDACTED]");

    private static BrowserSecretAccessRequest Request(BrowserSecretReference reference, BrowserSecretUsageIntent intent = BrowserSecretUsageIntent.ReadOnlyReference) =>
        new(
            RequestId: $"secret-request-{Guid.NewGuid():N}",
            RunId: "run-1",
            ActionId: "action-1",
            CorrelationId: "corr-1",
            ProfileId: "profile-1",
            SessionId: "session-1",
            Secret: reference,
            Intent: intent,
            RequestedAtUtc: DateTimeOffset.UtcNow,
            Reason: "synthetic fixture access");

    private static BrowserHumanHandoffRequest HandoffRequest(string safeText)
    {
        var target = Target(title: safeText);
        return new BrowserHumanHandoffRequest(
            RequestId: "handoff-secret",
            Status: BrowserHumanHandoffStatus.Created,
            Reason: BrowserHumanHandoffReason.PasswordRequired,
            Context: new BrowserHumanHandoffContext("run-1", "action-1", "corr-1", "profile-1", "session-1", target, target.FrameId, BrowserCredentialRedactor.Redact("https://local.test/login?token=synthetic"), safeText, true),
            ResumeToken: new BrowserHumanHandoffResumeToken("resume-1", "handoff-secret", "corr-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(10)),
            Instruction: new BrowserHumanHandoffInstruction("manual intervention required", ["continue"], ["verified after observe"]),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(10),
            RedactionApplied: true,
            EvidenceRefs: ["handoff-evidence"],
            ProofRefs: ["handoff-proof"]);
    }

    private static BrowserAction Action(BrowserTargetContext target) =>
        new(
            ActionId: "action-1",
            IdempotencyKey: "idem-1",
            RunId: "run-1",
            StepId: "step-1",
            TargetContext: target,
            FrameId: target.FrameId,
            ActionType: BrowserActionType.Click,
            Target: new BrowserActionTarget("login", "#login", "login", null),
            Input: null,
            ExpectedOutcome: new BrowserExpectedOutcome("manual login", null, "home", null),
            RiskClass: BrowserRiskClass.Critical,
            TimeoutMs: 8000,
            RequiresApproval: true,
            CreatedAtUtc: DateTimeOffset.UtcNow);

    private static BrowserSessionDescriptor Session() =>
        new(ManagedBrowserSessionId.New(), BrowserProfileId.New(), "test", "corr-1", BrowserSessionState.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(10), BrowserProfileCleanupPolicy.DeleteOnClose);

    private static BrowserTargetContext Target(string title = "Login fixture")
    {
        const string targetId = "target-1";
        const string frameId = "main";
        return new BrowserTargetContext(
            RunId: "run-1",
            BrowserId: "browser-1",
            BrowserSessionId: "session-1",
            BrowserContextId: null,
            WindowId: null,
            TargetId: targetId,
            PageId: targetId,
            TabId: null,
            FrameId: frameId,
            ParentFrameId: null,
            Url: new Uri("file:///fixture/login-form.html"),
            Title: title,
            Generation: 1,
            LivenessToken: BrowserTargetContext.CreateLivenessToken(targetId, frameId, 1),
            ObservedAtUtc: DateTimeOffset.UtcNow,
            IsActive: true,
            IsVisible: true,
            IsUserFacing: true,
            ReadyState: "complete",
            Source: BrowserTargetSource.Fixture);
    }
}
