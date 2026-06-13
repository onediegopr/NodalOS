using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;
using OneBrain.Core.Execution;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class SafeClickMigrationReportBuilderTests
{
    [TestMethod]
    public void MigrationSummaryCountsEligible()
    {
        var report = SafeClickMigrationReportBuilder.Build(
        [
            CreateReadiness(eligible: true, reason: SafeClickMigrationReadinessReason.Ready),
            CreateReadiness(eligible: false, reason: SafeClickMigrationReadinessReason.ApprovalV2)
        ]);

        Assert.AreEqual(2, report.Summary.TotalSafeClicks);
        Assert.AreEqual(1, report.Summary.EligibleForFsm);
        Assert.AreEqual(1, report.Summary.NotEligibleForFsm);
    }

    [TestMethod]
    public void MigrationSummaryCountsApprovalV2()
    {
        var report = SafeClickMigrationReportBuilder.Build(
        [
            CreateReadiness(false, SafeClickMigrationReadinessReason.ApprovalV2, approvalV2: 1)
        ]);

        Assert.AreEqual(1, report.Summary.ApprovalV2);
        Assert.AreEqual(1, report.Summary.BlockingReasons[SafeClickMigrationReadinessReason.ApprovalV2]);
    }

    [TestMethod]
    public void MigrationSummaryCountsWeakIdentity()
    {
        var report = SafeClickMigrationReportBuilder.Build(
        [
            CreateReadiness(false, SafeClickMigrationReadinessReason.WeakIdentity, weakIdentity: 1)
        ]);

        Assert.AreEqual(1, report.Summary.WeakIdentity);
    }

    [TestMethod]
    public void MigrationSummaryCountsMissingTargetObserve()
    {
        var report = SafeClickMigrationReportBuilder.Build(
        [
            CreateReadiness(false, SafeClickMigrationReadinessReason.MissingTargetObserve, targetObserveMissing: 1)
        ]);

        Assert.AreEqual(1, report.Summary.TargetObserveMissing);
    }

    [TestMethod]
    public void MigrationSummaryCountsRuntimeIdChanged()
    {
        var report = SafeClickMigrationReportBuilder.Build(
        [
            CreateReadiness(false, SafeClickMigrationReadinessReason.RuntimeIdChanged, runtimeIdChanged: 1)
        ]);

        Assert.AreEqual(1, report.Summary.RuntimeIdChanged);
    }

    [TestMethod]
    public void MigrationSummaryCountsLegacyFallback()
    {
        var report = SafeClickMigrationReportBuilder.Build(
        [
            CreateReadiness(false, SafeClickMigrationReadinessReason.WouldUseLegacyFallback, wouldUseUnsafeFallback: 1)
        ]);

        Assert.AreEqual(1, report.Summary.WouldUseUnsafeFallback);
    }

    [TestMethod]
    public void MigrationReportIsDeterministic()
    {
        SafeClickShadowReadiness[] items =
        [
            CreateReadiness(false, SafeClickMigrationReadinessReason.RuntimeIdChanged, runtimeIdChanged: 1),
            CreateReadiness(false, SafeClickMigrationReadinessReason.ApprovalV2, approvalV2: 1),
            CreateReadiness(true, SafeClickMigrationReadinessReason.Ready)
        ];

        var left = SafeClickMigrationReportBuilder.Build(items);
        var right = SafeClickMigrationReportBuilder.Build(items);

        Assert.AreEqual(left.Json, right.Json);
        Assert.AreEqual(left.Markdown, right.Markdown);
    }

    private static SafeClickShadowReadiness CreateReadiness(
        bool eligible,
        SafeClickMigrationReadinessReason reason,
        int approvalV2 = 0,
        int weakIdentity = 0,
        int targetObserveMissing = 0,
        int runtimeIdChanged = 0,
        int wouldUseUnsafeFallback = 0)
    {
        return new SafeClickShadowReadiness(
            Success: eligible,
            Blocked: !eligible,
            Reason: reason == SafeClickMigrationReadinessReason.Ready ? "Ready" : reason.ToString(),
            ReadinessReason: reason,
            ProjectedState: eligible ? StepState.Bound : StepState.Blocked,
            IdentityStrength: weakIdentity > 0 ? IdentityStrength.Weak : IdentityStrength.Strong,
            IdentitySource: "web-uia",
            HasTargetObserve: targetObserveMissing == 0,
            HasApprovalV3: approvalV2 == 0,
            HasRuntimeId: runtimeIdChanged == 0,
            RuntimeIdentityMatch: runtimeIdChanged > 0 ? RuntimeIdentityMatch.Different : RuntimeIdentityMatch.Same,
            WouldUseUnsafeFallback: wouldUseUnsafeFallback > 0,
            WouldRequireLegacy: !eligible,
            EligibleForFsm: eligible,
            Metrics: new SafeClickMigrationMetrics(
                TotalClicks: 1,
                EligibleForFsm: eligible ? 1 : 0,
                NotEligibleForFsm: eligible ? 0 : 1,
                ApprovalV3Strong: approvalV2 == 0 && weakIdentity == 0 ? 1 : 0,
                ApprovalV2: approvalV2,
                WeakIdentity: weakIdentity,
                TargetObservePresent: targetObserveMissing == 0 ? 1 : 0,
                TargetObserveMissing: targetObserveMissing,
                RuntimeIdPresent: runtimeIdChanged == 0 ? 1 : 0,
                RuntimeIdMissing: 0,
                RuntimeIdStable: eligible ? 1 : 0,
                RuntimeIdChanged: runtimeIdChanged,
                RuntimeIdUnknown: 0,
                UsesElClick: 0,
                UsesUiaActionExecutor: 0,
                InvokePatternAvailable: 1,
                InvokePatternUnavailable: 0,
                InvokePatternUnknown: 0,
                WouldRequireLegacy: eligible ? 0 : 1,
                WouldUseUnsafeFallback: wouldUseUnsafeFallback,
                WebUiaEligible: eligible ? 1 : 0,
                DesktopUiaObservable: 0,
                DesktopUiaStrong: 0,
                DesktopUiaWeak: 0,
                DesktopMissingIdentity: 0),
            Reasons: eligible ? ["Ready"] : [reason.ToString()]);
    }
}
