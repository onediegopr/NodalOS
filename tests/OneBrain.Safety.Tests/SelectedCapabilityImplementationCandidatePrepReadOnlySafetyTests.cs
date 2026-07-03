using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class SelectedCapabilityImplementationCandidatePrepReadOnlySafetyTests
{
    [TestMethod]
    public void PrepPacket_RemainsBlockedPendingUserGo()
    {
        var packet = SelectedCapabilityImplementationCandidatePrepReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.PassesPrepOnlySafetyProof);
        Assert.AreEqual("DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL", packet.SelectedCapability);
        Assert.AreEqual("BLOCKED_PENDING_USER_GO_FOR_IMPLEMENTATION", packet.CandidateStatus);
        Assert.AreEqual("IMPLEMENTATION_CANDIDATE_PREPARED_BUT_BLOCKED_PENDING_USER_GO", packet.MaximumDecisionAllowed);
        Assert.AreEqual("BLOCKED_NOT_EXECUTABLE", packet.BlockedFutureImplementationPrompt.Status);
        Assert.AreEqual("REQUIRED_BEFORE_ANY_ENABLEMENT", packet.PostImplementationAuditPrompt.RequiredBeforeEnablement);
    }

    [TestMethod]
    public void PrepPacket_KeepsAllImplementationAndRuntimeCountsZero()
    {
        var counts = SelectedCapabilityImplementationCandidatePrepReadOnlyPresenter.CreateFixture().Counts;

        Assert.IsTrue(counts.AllZero);
        Assert.AreEqual(0, counts.DurableAuditTrailRealEnabledCount);
        Assert.AreEqual(0, counts.AppendWriteEnabledCount);
        Assert.AreEqual(0, counts.RuntimeEnabledCount);
        Assert.AreEqual(0, counts.ExecutionEnabledCount);
        Assert.AreEqual(0, counts.MutationEnabledCount);
        Assert.AreEqual(0, counts.ExportEnabledCount);
        Assert.AreEqual(0, counts.ServiceRegistrationCount);
        Assert.AreEqual(0, counts.CommandHandlerCount);
        Assert.AreEqual(0, counts.ProductActionCount);
        Assert.AreEqual(0, counts.FilesystemOutputCount);
        Assert.AreEqual(0, counts.DbMigrationCount);
        Assert.AreEqual(0, counts.NetworkProviderCallCount);
        Assert.AreEqual(0, counts.ReleaseCommercialReadyCount);
    }

    [TestMethod]
    public void PrepPacket_DefinesNegativeTestsBeforeCode()
    {
        var packet = SelectedCapabilityImplementationCandidatePrepReadOnlyPresenter.CreateFixture();
        var testIds = packet.RequiredNegativeTests.Select(test => test.TestId).ToList();

        CollectionAssert.Contains(testIds, "no-append-without-user-go");
        CollectionAssert.Contains(testIds, "no-append-without-external-audit");
        CollectionAssert.Contains(testIds, "no-append-without-scope-lock");
        CollectionAssert.Contains(testIds, "no-service-registration");
        CollectionAssert.Contains(testIds, "no-command-handlers");
        CollectionAssert.Contains(testIds, "no-write-outside-isolated-future-audit-path");
        CollectionAssert.Contains(testIds, "fail-closed-missing-gate");
        CollectionAssert.Contains(testIds, "fail-closed-missing-audit");
        CollectionAssert.Contains(testIds, "fail-closed-missing-user-go");
        CollectionAssert.Contains(testIds, "release-commercial-no-go");
        Assert.IsTrue(packet.RequiredNegativeTests.All(test => test.RequiredBeforeImplementation && !test.ImplementedNow));
    }

    [TestMethod]
    public void PrepPacket_MapsFutureFilesAndProhibitedActivationSurfaces()
    {
        var entries = SelectedCapabilityImplementationCandidatePrepReadOnlyPresenter.CreateFixture().ModuleFileCandidateMap;

        Assert.IsTrue(entries.Any(entry => entry.Kind == CandidatePrepMapEntryKind.FutureCandidateFile));
        Assert.IsTrue(entries.Any(entry => entry.Kind == CandidatePrepMapEntryKind.FutureTestFile));
        Assert.IsTrue(entries.Any(entry => entry.Kind == CandidatePrepMapEntryKind.FutureDocFile));
        Assert.IsTrue(entries.Any(entry => entry.Kind == CandidatePrepMapEntryKind.ExistingReadOnlyPattern));
        Assert.IsTrue(entries.Any(entry => entry.Kind == CandidatePrepMapEntryKind.ProhibitedForFirstImplementation && entry.Path.Contains("ServiceCollection", StringComparison.Ordinal)));
        Assert.IsTrue(entries.Any(entry => entry.Kind == CandidatePrepMapEntryKind.ProhibitedForFirstImplementation && entry.Path.Contains("CommandHandler", StringComparison.Ordinal)));
        Assert.IsTrue(entries.Any(entry => entry.Kind == CandidatePrepMapEntryKind.ProhibitedForFirstImplementation && entry.Path.Contains("Migrations", StringComparison.Ordinal)));
        Assert.IsTrue(entries.All(entry => entry.SideEffectsProhibited.Count > 0));
    }

    [TestMethod]
    public void PrepPacket_PublicApiDoesNotExposeRealCapabilityMethods()
    {
        var forbiddenPrefixes = new[]
        {
            "Run",
            "Execute",
            "Export",
            "Mutate",
            "Register",
            "AddService",
            "CreateCommandHandler",
            "Write",
            "Save",
            "Delete",
            "Redact",
            "Retain",
            "Scan",
            "Append"
        };

        var presenterMethods = typeof(SelectedCapabilityImplementationCandidatePrepReadOnlyPresenter)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(method => method.Name)
            .Where(name => name != nameof(SelectedCapabilityImplementationCandidatePrepReadOnlyPresenter.CreateFixture))
            .ToList();

        var packetMethods = typeof(SelectedCapabilityImplementationCandidatePrepReadOnlyPacket)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => method.Name)
            .Where(name => !name.StartsWith("get_", StringComparison.Ordinal))
            .Where(name => name is not nameof(object.Equals) and not nameof(object.GetHashCode) and not nameof(object.ToString))
            .ToList();

        foreach (var name in presenterMethods.Concat(packetMethods))
        {
            Assert.IsFalse(forbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)), name);
        }
    }
}
