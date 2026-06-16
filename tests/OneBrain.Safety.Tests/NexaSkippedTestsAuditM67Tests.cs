using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class NexaSkippedTestsAuditM67Tests
{
    [TestMethod]
    public void NexaSkippedTestsAuditReportMatchesKnownSkippedCount() =>
        Assert.AreEqual(29, Report().Items.Count);

    [TestMethod]
    public void NexaSkippedTestsAuditReportIncludesExternalLowRiskTargetSkips()
    {
        var names = Names();

        CollectionAssert.Contains(names, "NexaExternalLowRiskTargetLiveVerifiesSemanticProof");
        CollectionAssert.Contains(names, "NexaExternalLowRiskTargetLiveCapturesMetadataOnly");
        CollectionAssert.Contains(names, "NexaExternalLowRiskTargetLiveCleansBrowser");
    }

    [TestMethod]
    public void NexaSkippedTestsAuditReportIncludesExternalReadOnlySkips() =>
        Assert.IsTrue(Report().Items.Count(item => item.TestName.StartsWith("BrowserExternalReadOnlyLive", StringComparison.Ordinal)) >= 5);

    [TestMethod]
    public void NexaSkippedTestsAuditReportIncludesCdpLiveSkips() =>
        Assert.IsTrue(Report().Items.Count(item => item.Category == NexaSkippedTestCategory.CdpLiveOptIn) >= 9);

    [TestMethod]
    public void NexaSkippedTestsAuditReportIncludesSensitiveSimulationSkips() =>
        Assert.IsTrue(Report().Items.Count(item => item.Category == NexaSkippedTestCategory.SensitiveSimulationOptIn) >= 4);

    [TestMethod]
    public void NexaSkippedTestsAuditReportMarksBlockingSkips() =>
        Assert.IsTrue(Report().Items.Any(item => item.Category == NexaSkippedTestCategory.ExternalTargetBlocked && item.RecommendedAction.Contains("Configure", StringComparison.Ordinal)));

    [TestMethod]
    public void NexaSkippedTestsAuditReportListsRequiredEnvVars() =>
        Assert.IsTrue(Report().Items.All(item => !string.IsNullOrWhiteSpace(item.OptInEnvironmentVariable)));

    [TestMethod]
    public void NexaSkippedTestsAuditReportDoesNotClaimSkippedTestsPassed() =>
        Assert.IsFalse(Report().Items.Any(item => item.Reason.Contains("passed", StringComparison.OrdinalIgnoreCase)));

    private static NexaSkippedTestsAuditReport Report() => new NexaSkippedTestsAuditReporter().CreateReport();

    private static List<string> Names() => Report().Items.Select(item => item.TestName).ToList();
}
