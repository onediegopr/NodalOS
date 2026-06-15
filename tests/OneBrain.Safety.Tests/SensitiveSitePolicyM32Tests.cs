using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class SensitiveSitePolicyM32Tests
{
    [TestMethod]
    public void SensitiveSitePolicyClassifiesFiscalAsCritical() =>
        Assert.AreEqual(SensitiveSiteRiskLevel.Critical, SensitiveSitePolicyEvaluator.DefaultRisk(SensitiveSiteCategory.Fiscal));

    [TestMethod]
    public void SensitiveSitePolicyClassifiesBankingAsCritical() =>
        Assert.AreEqual(SensitiveSiteRiskLevel.Critical, SensitiveSitePolicyEvaluator.DefaultRisk(SensitiveSiteCategory.Banking));

    [TestMethod]
    public void SensitiveSitePolicyClassifiesErpAsHighOrCritical() =>
        Assert.IsTrue(SensitiveSitePolicyEvaluator.DefaultRisk(SensitiveSiteCategory.ERP) is SensitiveSiteRiskLevel.HighRisk or SensitiveSiteRiskLevel.Critical);

    [TestMethod]
    public void SensitiveSitePolicyBlocksUnknownDomain()
    {
        var decision = Evaluate("https://unknown.example.test", SensitiveSiteActionKind.ReadOnlyView, classifications: new Dictionary<string, SensitiveSiteClassification>());

        Assert.AreEqual(SensitiveSiteDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), SensitiveSiteReason.UnknownDomain);
    }

    [TestMethod]
    public void SensitiveSitePolicyBlocksIrreversibleActionByDefault() =>
        AssertProhibited(SensitiveSiteActionKind.Delete, SensitiveSiteReason.IrreversibleActionBlocked);

    [TestMethod]
    public void SensitiveSitePolicyBlocksPaymentByDefault() =>
        AssertProhibited(SensitiveSiteActionKind.Pay, SensitiveSiteReason.PaymentBlocked);

    [TestMethod]
    public void SensitiveSitePolicyBlocksSubmitByDefault() =>
        AssertProhibited(SensitiveSiteActionKind.Submit, SensitiveSiteReason.SubmitBlocked);

    [TestMethod]
    public void SensitiveSitePolicyBlocksSigningByDefault() =>
        AssertProhibited(SensitiveSiteActionKind.Sign, SensitiveSiteReason.SigningBlocked);

    [TestMethod]
    public void SensitiveSitePolicyRequiresApprovalForReadOnlyCriticalSite()
    {
        var decision = Evaluate("https://fiscal.example.test", SensitiveSiteActionKind.ReadOnlyView, approvals: []);

        Assert.AreEqual(SensitiveSiteDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), SensitiveSiteReason.SensitiveSiteRequiresApproval);
    }

    [TestMethod]
    public void SensitiveSitePolicyRequiresApprovalForDocumentDownload()
    {
        var decision = Evaluate("https://fiscal.example.test", SensitiveSiteActionKind.DownloadDocument, approvals: []);

        Assert.AreEqual(SensitiveSiteDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), SensitiveSiteReason.SensitiveSiteRequiresApproval);
    }

    [TestMethod]
    public void SensitiveSitePolicyRequiresApprovalForDocumentUpload()
    {
        var decision = Evaluate("https://fiscal.example.test", SensitiveSiteActionKind.UploadDocument, approvals: []);

        Assert.AreEqual(SensitiveSiteDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), SensitiveSiteReason.SensitiveSiteRequiresApproval);
    }

    [TestMethod]
    public void SensitiveSitePolicyRequiresSafeDownloadForSensitiveDownload()
    {
        var decision = Evaluate("https://fiscal.example.test", SensitiveSiteActionKind.DownloadDocument, contextMutation: c => c with { SafeDownloadAvailable = false });

        Assert.AreEqual(SensitiveSiteDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), SensitiveSiteReason.SensitiveDownloadRequiresSafeDownload);
    }

    [TestMethod]
    public void SensitiveSitePolicyRequiresSafeUploadForSensitiveUpload()
    {
        var decision = Evaluate("https://fiscal.example.test", SensitiveSiteActionKind.UploadDocument, contextMutation: c => c with { SafeUploadAvailable = false });

        Assert.AreEqual(SensitiveSiteDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), SensitiveSiteReason.SensitiveUploadRequiresSafeUpload);
    }

    [TestMethod]
    public void SensitiveSitePolicyBlocksProfileRaw()
    {
        var decision = Evaluate("https://fiscal.example.test", SensitiveSiteActionKind.ReadOnlyView, contextMutation: c => c with { ProfileState = BrowserRuntimeProfileState.RawUserProfileActive });

        Assert.AreEqual(SensitiveSiteDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), SensitiveSiteReason.ProfileRawBlocked);
    }

    [TestMethod]
    public void SensitiveSitePolicyBlocksProductiveReplay()
    {
        var decision = Evaluate("https://fiscal.example.test", SensitiveSiteActionKind.ReadOnlyView, contextMutation: c => c with { ReplayState = BrowserRuntimeReplayState.ProductiveActive });

        Assert.AreEqual(SensitiveSiteDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), SensitiveSiteReason.ProductiveReplayBlocked);
    }

    [TestMethod]
    public void SensitiveSitePolicyBlocksProductiveRecorder()
    {
        var decision = Evaluate("https://fiscal.example.test", SensitiveSiteActionKind.ReadOnlyView, contextMutation: c => c with { RecorderState = BrowserRuntimeRecorderState.ProductiveActive });

        Assert.AreEqual(SensitiveSiteDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), SensitiveSiteReason.ProductiveRecorderBlocked);
    }

    [TestMethod]
    public void SensitiveSitePolicyDoesNotAuditSecretsCookiesBodies()
    {
        var decision = Evaluate("https://fiscal.example.test?access_token=opaque", SensitiveSiteActionKind.ReadOnlyView);
        var auditText = decision.AuditEvent.ToString()!;

        Assert.IsTrue(decision.AuditRequirement.ExcludeSecretsCookiesBodies);
        Assert.IsFalse(auditText.Contains("opaque", StringComparison.Ordinal));
        Assert.IsFalse(auditText.Contains("cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(auditText.Contains("body", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void SensitiveSitePolicyProducesReasonCodes()
    {
        var decision = Evaluate("https://fiscal.example.test", SensitiveSiteActionKind.Submit);

        Assert.IsTrue(decision.ReasonCodes.Count > 0);
    }

    [TestMethod]
    public void SensitiveSitePolicyReportsWhatRemainsBlocked()
    {
        var decision = Evaluate("https://fiscal.example.test", SensitiveSiteActionKind.ReadOnlyView);

        CollectionAssert.Contains(decision.WhatRemainsBlocked.ToList(), "submit");
        CollectionAssert.Contains(decision.WhatRemainsBlocked.ToList(), "pay");
        CollectionAssert.Contains(decision.WhatRemainsBlocked.ToList(), "sign");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateAllowsSensitiveSitesPolicyDefined()
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true));

        Assert.IsTrue(report.Passed, string.Join("; ", report.FailedChecks));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsSensitiveRealPilotBeforeM33() =>
        AssertGateFails(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { SensitiveSiteRealPilotActive = true });

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsSensitiveSubmitEnabled() =>
        AssertGateFails(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { SensitiveSiteSubmitEnabled = true });

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsSensitivePaymentEnabled() =>
        AssertGateFails(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { SensitiveSitePaymentEnabled = true });

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsSensitiveSigningEnabled() =>
        AssertGateFails(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { SensitiveSiteSigningEnabled = true });

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsSensitiveReplayProductive() =>
        AssertGateFails(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { ReplayState = BrowserRuntimeReplayState.ProductiveActive });

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsSensitiveRecorderProductive() =>
        AssertGateFails(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true) with { RecorderState = BrowserRuntimeRecorderState.ProductiveActive });

    private static void AssertProhibited(SensitiveSiteActionKind action, SensitiveSiteReason reason)
    {
        var decision = Evaluate("https://fiscal.example.test", action);

        Assert.AreEqual(SensitiveSiteDecisionKind.Prohibited, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), reason);
    }

    private static void AssertGateFails(BrowserRuntimeObservedState state)
    {
        using var temp = BrowserVaultMinimalM23Tests.TempDir();
        var report = BrowserSafeDownloadM26TestAccess.PhaseReport(temp.Path, state);
        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "sensitive sites policy defined");
    }

    private static SensitiveSitePolicyDecision Evaluate(string url, SensitiveSiteActionKind action, IReadOnlyDictionary<string, SensitiveSiteClassification>? classifications = null, IReadOnlyList<string>? approvals = null, Func<SensitiveSitePolicyContext, SensitiveSitePolicyContext>? contextMutation = null)
    {
        var context = Context();
        context = contextMutation?.Invoke(context) ?? context;
        var request = new SensitiveSitePolicyRequest(
            "run-sensitive",
            "action-sensitive",
            "corr-sensitive",
            new Uri(url),
            action,
            DataSensitive: true,
            approvals ?? ["approval-sensitive"],
            ["evidence-sensitive"],
            context);
        return new SensitiveSitePolicyEvaluator().Evaluate(request, SensitiveSitePolicy.Default(classifications ?? Classifications()));
    }

    private static SensitiveSitePolicyContext Context() =>
        new(
            UserConsentValid: true,
            ApprovalProvided: SensitiveSiteHumanApprovalRequirement.SingleApprovalRequired,
            BrowserVaultMinimalM23Tests.GateReport(BrowserVaultMinimalM23Tests.SafeState(vaultMinimal: true)),
            BrowserRuntimeProfileState.UserProfileControlledWithConsent,
            BrowserRuntimeVaultState.MinimalSandboxActive,
            BrowserRuntimeReplayState.SafeModeReadOnlyActive,
            BrowserRuntimeRecorderState.ReadOnlyPrototypeActive,
            BrowserNetworkCaptureMode.MetadataOnly,
            RequestBodyCaptureSupported: false,
            ResponseBodyCaptureSupported: false,
            SensitiveHeaderValueCaptureSupported: false,
            SafeDownloadAvailable: true,
            SafeUploadAvailable: true);

    private static IReadOnlyDictionary<string, SensitiveSiteClassification> Classifications() =>
        new Dictionary<string, SensitiveSiteClassification>(StringComparer.OrdinalIgnoreCase)
        {
            ["fiscal.example.test"] = new("fiscal.example.test", SensitiveSiteCategory.Fiscal, SensitiveSiteRiskLevel.Critical, TestOnlySimulation: true),
            ["bank.example.test"] = new("bank.example.test", SensitiveSiteCategory.Banking, SensitiveSiteRiskLevel.Critical, TestOnlySimulation: true),
            ["erp.example.test"] = new("erp.example.test", SensitiveSiteCategory.ERP, SensitiveSiteRiskLevel.HighRisk, TestOnlySimulation: true)
        };
}

