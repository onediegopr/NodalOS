using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Execution;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class SafeClickLegacyRetirementReadinessTests
{
    [TestMethod]
    public void RetirementReadinessTrueWhenNoBlockingReasons()
    {
        var readiness = SafeClickLegacyRetirementReadinessEvaluator.Evaluate(
            totalSafeClicks: 1,
            defaultFsmRouted: 1,
            explicitLegacyOptOut: 0,
            legacyPathUsed: 0,
            elClickUsed: 0,
            uiaActionExecutorUsed: 0,
            unsafeFallbackUsed: 0,
            nonCompliantLegacyOptOut: 0,
            desktopExcluded: 0,
            webExcluded: 0,
            allEligibleModeObserved: 1,
            unknownDispatchPathBlocked: 0);

        Assert.IsTrue(readiness.IsReadyForRetirement);
        Assert.AreEqual(0, readiness.BlockingReasons.Count);
    }

    [TestMethod]
    public void RetirementReadinessFalseWhenLegacySignalsExist()
    {
        var readiness = SafeClickLegacyRetirementReadinessEvaluator.Evaluate(
            totalSafeClicks: 1,
            defaultFsmRouted: 0,
            explicitLegacyOptOut: 1,
            legacyPathUsed: 1,
            elClickUsed: 1,
            uiaActionExecutorUsed: 1,
            unsafeFallbackUsed: 1,
            nonCompliantLegacyOptOut: 1,
            desktopExcluded: 0,
            webExcluded: 0,
            allEligibleModeObserved: 0,
            unknownDispatchPathBlocked: 0);

        Assert.IsFalse(readiness.IsReadyForRetirement);
        CollectionAssert.Contains(readiness.BlockingReasons.ToList(), "LegacyPathUsed");
        CollectionAssert.Contains(readiness.BlockingReasons.ToList(), "ElClickUsed");
        CollectionAssert.Contains(readiness.BlockingReasons.ToList(), "UiaActionExecutorUsed");
        CollectionAssert.Contains(readiness.BlockingReasons.ToList(), "UnsafeFallbackUsed");
        CollectionAssert.Contains(readiness.BlockingReasons.ToList(), "NonCompliantLegacyOptOut");
    }

    [TestMethod]
    public void RetirementReadinessReportsUnknownDispatchPath()
    {
        var readiness = SafeClickLegacyRetirementReadinessEvaluator.Evaluate(
            totalSafeClicks: 1,
            defaultFsmRouted: 0,
            explicitLegacyOptOut: 0,
            legacyPathUsed: 0,
            elClickUsed: 0,
            uiaActionExecutorUsed: 0,
            unsafeFallbackUsed: 0,
            nonCompliantLegacyOptOut: 0,
            desktopExcluded: 0,
            webExcluded: 0,
            allEligibleModeObserved: 0,
            unknownDispatchPathBlocked: 1);

        Assert.IsFalse(readiness.IsReadyForRetirement);
        CollectionAssert.Contains(readiness.BlockingReasons.ToList(), "UnknownDispatchPathBlocked");
    }
}
