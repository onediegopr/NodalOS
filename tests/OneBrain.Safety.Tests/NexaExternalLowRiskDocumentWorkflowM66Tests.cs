using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaExternalLowRiskDocumentWorkflowM66Tests
{
    [TestMethod]
    public void NexaExternalLowRiskDocumentWorkflowRequiresTargetReadiness()
    {
        var decision = Evaluate(Request(targetReady: false));

        Assert.AreEqual(NexaExternalLowRiskDocumentWorkflowDecisionKind.PreparedButBlockedByM65, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "external low-risk target readiness required");
    }

    [TestMethod]
    public void NexaExternalLowRiskDocumentWorkflowBlocksWhenM65Blocked()
    {
        var decision = Evaluate(Request(targetReady: false));

        Assert.AreEqual(NexaExternalLowRiskDocumentWorkflowDecisionKind.PreparedButBlockedByM65, decision.Decision);
    }

    [TestMethod]
    public void NexaExternalLowRiskDocumentWorkflowRequiresSafeDownload()
    {
        var decision = Evaluate(Request(safeDownload: false));

        Assert.AreEqual(NexaExternalLowRiskDocumentWorkflowDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "safe download required");
    }

    [TestMethod]
    public void NexaExternalLowRiskDocumentWorkflowRequiresSafeUpload()
    {
        var decision = Evaluate(Request(safeUpload: false));

        Assert.AreEqual(NexaExternalLowRiskDocumentWorkflowDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "safe upload required");
    }

    [TestMethod]
    public void NexaExternalLowRiskDocumentWorkflowRequiresApproval()
    {
        var decision = Evaluate(Request(approval: false));

        Assert.AreEqual(NexaExternalLowRiskDocumentWorkflowDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "approval required");
    }

    [TestMethod]
    public void NexaExternalLowRiskDocumentWorkflowBlocksSensitiveDocuments()
    {
        var decision = Evaluate(Request(sensitiveDocs: true));

        Assert.AreEqual(NexaExternalLowRiskDocumentWorkflowDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "sensitive external documents blocked");
    }

    [TestMethod]
    public void NexaExternalLowRiskDocumentWorkflowAuditIsRedacted()
    {
        var decision = Evaluate(Request());

        Assert.IsTrue(decision.Redacted);
        Assert.IsFalse(BrowserCredentialRedactor.ContainsSecret(NexaLeakHardeningSerialization.ToSafeJson(decision)));
    }

    [TestMethod]
    public void BrowserRuntimePhaseGatePassesWhenExternalWorkflowPreparedButBlockedByM65()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(State());

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Passed, report.Status);
        CollectionAssert.Contains(report.PassedChecks.ToList(), "private trial external preparation safe");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenExternalWorkflowRunsWithoutTargetReadiness()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(State() with { ExternalWorkflowRunsWithoutTargetReadiness = true });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "private trial external preparation safe");
    }

    private static NexaExternalLowRiskDocumentWorkflowDecision Evaluate(NexaExternalLowRiskDocumentWorkflowRequest request) =>
        new NexaExternalLowRiskDocumentWorkflowEvaluator().Evaluate(
            new NexaExternalLowRiskDocumentWorkflowPolicy(RequireTargetReadiness: true, RequireSafeDownload: true, RequireSafeUpload: true, RequireApproval: true, AllowSensitiveDocuments: false, RequireAudit: true),
            request);

    private static NexaExternalLowRiskDocumentWorkflowRequest Request(
        bool targetReady = true,
        bool safeDownload = true,
        bool safeUpload = true,
        bool approval = true,
        bool sensitiveDocs = false) =>
        new(
            "external-doc-workflow",
            new NexaExternalLowRiskTargetReadiness(Configured: targetReady, Reachable: targetReady, SemanticProofVerified: targetReady, MetadataOnly: true, CookiesPersisted: false, SensitiveHeaderValuesCaptured: false, BrowserCleanupConfirmed: true, Redacted: true),
            safeDownload,
            safeUpload,
            approval,
            sensitiveDocs,
            AuditRedacted: true);

    private static BrowserRuntimeObservedState State() =>
        BrowserVaultThreatLifecycleM56M57Tests.StateForM60() with
        {
            PrivateLocalApiDiagnosticsDefined = true,
            EmailSandboxProviderDefined = true,
            BillingSandboxProviderDefined = true,
            PrivateTrialSimulationSafe = true,
            PrivateTrialRealBillingEnabled = false,
            PrivateTrialRealEmailEnabled = false,
            PrivateTrialEnablesSensitiveRealPilotByDefault = false,
            ExternalLowRiskTargetSetupDefined = true,
            ExternalLowRiskTargetLiveValidated = false,
            ExternalLowRiskDocumentWorkflowPrepared = true,
            ExternalLowRiskDocumentWorkflowBlockedWithoutTarget = true
        };
}
