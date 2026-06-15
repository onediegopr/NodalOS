using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class BrowserVaultThreatLifecycleM56M57Tests
{
    [TestMethod]
    public void BrowserVaultThreatTestBlocksCompanionSecretRetrieval() =>
        AssertBlocked(Threat(BrowserVaultThreatActorKind.Companion));

    [TestMethod]
    public void BrowserVaultThreatTestBlocksSupportSecretRetrieval() =>
        AssertBlocked(Threat(BrowserVaultThreatActorKind.Support));

    [TestMethod]
    public void BrowserVaultThreatTestBlocksAdminDashboardRawSecret() =>
        AssertBlocked(Threat(BrowserVaultThreatActorKind.AdminDashboard, raw: true));

    [TestMethod]
    public void BrowserVaultThreatTestBlocksPublicApiSecretLeak() =>
        AssertBlocked(Threat(BrowserVaultThreatActorKind.PublicApi, dto: true));

    [TestMethod]
    public void BrowserVaultThreatTestBlocksDiagnosticsSecretLeak() =>
        AssertBlocked(Threat(BrowserVaultThreatActorKind.Diagnostics, dto: true));

    [TestMethod]
    public void BrowserVaultThreatTestBlocksAuditExportSecretLeak() =>
        AssertBlocked(Threat(BrowserVaultThreatActorKind.AuditExport, dto: true));

    [TestMethod]
    public void BrowserVaultThreatTestBlocksCrossTenantSecretRetrieval() =>
        AssertBlocked(Threat(BrowserVaultThreatActorKind.Core) with { TargetTenantId = "tenant-other" });

    [TestMethod]
    public void BrowserVaultThreatTestBlocksUnauthorizedWorkerSecretRetrieval() =>
        AssertBlocked(Threat(BrowserVaultThreatActorKind.Core) with { WorkerAuthorized = false });

    [TestMethod]
    public void BrowserVaultThreatTestBlocksWithoutProductiveVaultEntitlement() =>
        AssertBlocked(Threat(BrowserVaultThreatActorKind.Core) with { ProductiveVaultEntitlement = false });

    [TestMethod]
    public void BrowserVaultThreatTestBlocksWhenGateFails() =>
        AssertBlocked(Threat(BrowserVaultThreatActorKind.Core) with { GatePassed = false });

    [TestMethod]
    public void BrowserVaultThreatTestCoreOnlyHandleIsNotPublicDto()
    {
        var decision = new BrowserVaultThreatEvaluator().Evaluate(Threat(BrowserVaultThreatActorKind.Core), Reference());

        Assert.AreEqual(BrowserVaultThreatDecisionKind.AllowedCoreOnly, decision.Decision);
        Assert.IsNotNull(decision.Handle);
        Assert.IsFalse(decision.Handle.PublicDto);
        Assert.IsFalse(decision.RawSecretExposed);
    }

    [TestMethod]
    public void BrowserVaultThreatTestSecretHandleIsNotSerializable()
    {
        var handle = new BrowserVaultThreatEvaluator().Evaluate(Threat(BrowserVaultThreatActorKind.Core), Reference()).Handle!;

        Assert.IsTrue(handle.CoreOnly);
        Assert.IsFalse(handle.Serializable);
        Assert.IsFalse(handle.Exportable);
    }

    [TestMethod]
    public void BrowserVaultRotationRequiresPolicyAndApproval()
    {
        var decision = Lifecycle().EvaluateRotation(RotationPolicy(), RotationRequest() with { PolicyPresent = false, ApprovalPresent = false });

        Assert.AreEqual(BrowserVaultLifecycleDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "rotation policy missing");
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "rotation owner/admin approval missing");
    }

    [TestMethod]
    public void BrowserVaultRotationDoesNotExposeOldOrNewSecret()
    {
        var decision = Lifecycle().EvaluateRotation(RotationPolicy(), RotationRequest());

        Assert.AreEqual(BrowserVaultLifecycleDecisionKind.Allowed, decision.Decision);
        Assert.IsFalse(decision.OldSecretExposed);
        Assert.IsFalse(decision.NewSecretExposed);
    }

    [TestMethod]
    public void BrowserVaultRecoveryFailsClosedWithoutProvider()
    {
        var decision = Lifecycle().EvaluateRecovery(RecoveryPolicy(), RecoveryRequest() with { ProviderAvailable = false });

        Assert.AreEqual(BrowserVaultLifecycleDecisionKind.FailClosed, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "recovery provider unavailable");
    }

    [TestMethod]
    public void BrowserVaultRecoveryRequiresOwnerApproval()
    {
        var decision = Lifecycle().EvaluateRecovery(RecoveryPolicy(), RecoveryRequest() with { ApprovalPresent = false });

        Assert.AreEqual(BrowserVaultLifecycleDecisionKind.FailClosed, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "recovery owner/admin approval missing");
    }

    [TestMethod]
    public void BrowserVaultRecoveryAuditDoesNotContainSecret()
    {
        var decision = Lifecycle().EvaluateRecovery(RecoveryPolicy(), RecoveryRequest());
        var json = NexaLeakHardeningSerialization.ToSafeJson(decision);

        Assert.AreEqual(BrowserVaultLifecycleDecisionKind.Allowed, decision.Decision);
        Assert.IsFalse(decision.SecretExposed);
        Assert.IsFalse(json.Contains("synthetic-os-backed-password", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserVaultExportDisabledByDefault()
    {
        var decision = Lifecycle().EvaluateExport(new BrowserVaultExportPolicy(BrowserVaultExportMode.Disabled, true, true, true, false), ExportRequest());

        Assert.AreEqual(BrowserVaultLifecycleDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "vault export disabled by default");
    }

    [TestMethod]
    public void BrowserVaultExportRequiresEnterprisePolicy()
    {
        var decision = Lifecycle().EvaluateExport(new BrowserVaultExportPolicy(BrowserVaultExportMode.ManifestOnly, true, true, true, false), ExportRequest() with { ConfigurationProfile = NexaConfigurationProfileKind.LocalSandbox });

        Assert.AreEqual(BrowserVaultLifecycleDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "vault export requires enterprise controlled policy");
    }

    [TestMethod]
    public void BrowserVaultExportNeverCleartext()
    {
        var decision = Lifecycle().EvaluateExport(new BrowserVaultExportPolicy(BrowserVaultExportMode.Cleartext, false, false, false, true), ExportRequest());

        Assert.IsTrue(decision.CleartextBlocked);
        Assert.IsFalse(decision.RawSecretExposed);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "cleartext vault export blocked");
    }

    [TestMethod]
    public void BrowserVaultExportManifestDoesNotContainSecret()
    {
        var decision = Lifecycle().EvaluateExport(new BrowserVaultExportPolicy(BrowserVaultExportMode.ManifestOnly, true, true, true, false), ExportRequest());
        var json = NexaLeakHardeningSerialization.ToSafeJson(decision.Manifest);

        Assert.AreEqual(BrowserVaultLifecycleDecisionKind.DesignOnly, decision.Decision);
        Assert.IsFalse(decision.Manifest.ContainsRawSecret);
        Assert.IsFalse(json.Contains("synthetic-os-backed-password", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BrowserVaultExportBlocksWithoutEncryptionPolicy()
    {
        var decision = Lifecycle().EvaluateExport(new BrowserVaultExportPolicy(BrowserVaultExportMode.ManifestOnly, true, true, true, false), ExportRequest() with { EncryptionPolicyPresent = false });

        Assert.AreEqual(BrowserVaultLifecycleDecisionKind.Blocked, decision.Decision);
        CollectionAssert.Contains(decision.ReasonCodes.ToList(), "vault export encryption policy missing");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGatePassesWithVaultThreatAndLifecyclePolicies()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(State());

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Passed, report.Status);
        CollectionAssert.Contains(report.PassedChecks.ToList(), "vault threat boundary safe");
        CollectionAssert.Contains(report.PassedChecks.ToList(), "vault lifecycle policy safe");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsWhenVaultExportCleartextEnabled()
    {
        var report = BrowserVaultMinimalM23Tests.GateReport(State() with { VaultExportCleartextBlocked = false });

        Assert.AreEqual(BrowserRuntimePhaseCloseStatus.Failed, report.Status);
        CollectionAssert.Contains(report.FailedChecks.ToList(), "vault lifecycle policy safe");
    }

    private static void AssertBlocked(BrowserVaultThreatRequest request)
    {
        var decision = new BrowserVaultThreatEvaluator().Evaluate(request, Reference());

        Assert.IsTrue(decision.BlocksAccess);
        Assert.IsFalse(decision.RawSecretExposed);
    }

    private static BrowserVaultThreatRequest Threat(BrowserVaultThreatActorKind actor, bool raw = false, bool dto = false) =>
        new("vault-threat-request", actor, "tenant-local", "tenant-local", "worker-local", WorkerAuthorized: true, ProductiveVaultEntitlement: true, GatePassed: true, AttemptsRawSecretAccess: raw, AttemptsPublicDtoSecret: dto, AttemptsSerialization: false);

    private static BrowserSecretReference Reference() =>
        new("synthetic-secret-reference", BrowserSecretKind.Password, BrowserSecretScope.Temporary, "owner-local", "portal-local", DateTimeOffset.UtcNow, "synthetic secret reference");

    private static BrowserVaultLifecyclePolicyEvaluator Lifecycle() => new();

    private static BrowserVaultRotationPolicy RotationPolicy() =>
        new(RotationEnabled: true, RequirePolicy: true, RequireAudit: true, RequireOwnerOrAdminApproval: true, ExposeOldSecret: false, ExposeNewSecret: false);

    private static BrowserVaultRotationPolicyRequest RotationRequest() =>
        new("vault-rotation-request", Reference(), NexaRole.Owner, PolicyPresent: true, ApprovalPresent: true, AuditEnabled: true, "rotate synthetic reference");

    private static BrowserVaultRecoveryPolicy RecoveryPolicy() =>
        new(RecoveryEnabled: true, RequireOwnerOrAdminApproval: true, RequireLocalMachineUserBinding: true, FailClosedWhenProviderUnavailable: true, AuditWithoutValue: true);

    private static BrowserVaultRecoveryRequest RecoveryRequest() =>
        new("vault-recovery-request", Reference(), NexaRole.Owner, ApprovalPresent: true, ProviderAvailable: true, LocalMachineUserBindingPresent: true, "recover synthetic reference");

    private static BrowserVaultExportRequest ExportRequest() =>
        new("vault-export-request", Reference(), NexaConfigurationProfileKind.EnterpriseControlled, NexaRole.Owner, StrongApprovalPresent: true, EncryptionPolicyPresent: true, "manifest only export");

    private static BrowserRuntimeObservedState State() =>
        NexaLocalProductShellM48Tests.SafeState() with
        {
            LeakHardeningCompleted = true,
            SkippedTestsAuditCompleted = true,
            PrivatePreviewLocalAllowed = true,
            PrivatePreviewLocalSafe = true,
            M51ExternalProofDeferred = true,
            PrivatePreviewFeedbackLoopDefined = true,
            PrivatePreviewFeedbackLoopLeaksSecrets = false,
            VaultThreatTestsPassed = true,
            VaultThreatTestsMissing = false,
            VaultRotationPolicyDefined = true,
            VaultRecoveryPolicyDefined = true,
            VaultExportPolicyDefined = true,
            VaultExportCleartextBlocked = true,
            VaultRecoveryFailsClosed = true,
            VaultRotationExposesSecret = false
        };
}
