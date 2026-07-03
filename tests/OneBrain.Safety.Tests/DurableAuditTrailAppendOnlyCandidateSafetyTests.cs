using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class DurableAuditTrailAppendOnlyCandidateSafetyTests
{
    [TestMethod]
    public void Candidate_FailsClosedWithoutUserGoAuditOrScopeLock()
    {
        var missingUserGo = DurableAuditTrailAppendOnlyCandidate.Evaluate(
            DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest() with
            {
                Gates = DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest().Gates with { UserExplicitGo = false }
            });
        var missingAudit = DurableAuditTrailAppendOnlyCandidate.Evaluate(
            DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest() with
            {
                Gates = DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest().Gates with { PreImplementationExternalAuditGo = false }
            });
        var missingScope = DurableAuditTrailAppendOnlyCandidate.Evaluate(
            DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest() with
            {
                Gates = DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest().Gates with { ScopeLocked = false }
            });

        Assert.AreEqual(DurableAuditTrailAppendOnlyCandidateDecision.Blocked, missingUserGo.Decision);
        Assert.AreEqual(DurableAuditTrailAppendOnlyCandidateDecision.Blocked, missingAudit.Decision);
        Assert.AreEqual(DurableAuditTrailAppendOnlyCandidateDecision.Blocked, missingScope.Decision);
        CollectionAssert.Contains(missingUserGo.BlockReasons.ToArray(), DurableAuditTrailAppendOnlyCandidateBlockReason.MissingUserExplicitGo);
        CollectionAssert.Contains(missingAudit.BlockReasons.ToArray(), DurableAuditTrailAppendOnlyCandidateBlockReason.MissingPreImplementationExternalAuditGo);
        CollectionAssert.Contains(missingScope.BlockReasons.ToArray(), DurableAuditTrailAppendOnlyCandidateBlockReason.MissingScopeLock);
        AssertNoSideEffectCounts(missingUserGo.Counts);
        AssertNoSideEffectCounts(missingAudit.Counts);
        AssertNoSideEffectCounts(missingScope.Counts);
    }

    [TestMethod]
    public void Candidate_RejectsUnexpectedScopeAndEventKind()
    {
        var unexpectedScope = DurableAuditTrailAppendOnlyCandidate.Evaluate(
            DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest() with { TargetScopeId = "workspace.wide.audit-trail" });
        var unexpectedEvent = DurableAuditTrailAppendOnlyCandidate.Evaluate(
            DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest() with { EventKind = "runtime.executed" });

        Assert.AreEqual(DurableAuditTrailAppendOnlyCandidateDecision.Blocked, unexpectedScope.Decision);
        Assert.AreEqual(DurableAuditTrailAppendOnlyCandidateDecision.Blocked, unexpectedEvent.Decision);
        CollectionAssert.Contains(unexpectedScope.BlockReasons.ToArray(), DurableAuditTrailAppendOnlyCandidateBlockReason.UnexpectedTargetScope);
        CollectionAssert.Contains(unexpectedEvent.BlockReasons.ToArray(), DurableAuditTrailAppendOnlyCandidateBlockReason.UnexpectedEventKind);
        Assert.IsNull(unexpectedScope.Preview);
        Assert.IsNull(unexpectedEvent.Preview);
        AssertNoSideEffectCounts(unexpectedScope.Counts);
        AssertNoSideEffectCounts(unexpectedEvent.Counts);
    }

    [TestMethod]
    public void Candidate_CreatesOnlyNonPersistedEnvelopePreviewWhenAllGatesPass()
    {
        var result = DurableAuditTrailAppendOnlyCandidate.Evaluate(DurableAuditTrailAppendOnlyCandidate.CreateFixtureRequest());

        Assert.AreEqual(DurableAuditTrailAppendOnlyCandidateDecision.CandidateAcceptedNoWrite, result.Decision);
        Assert.IsNotNull(result.Preview);
        Assert.AreEqual("DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL", result.Preview!.CapabilityId);
        Assert.IsFalse(result.Preview.AppendWritten);
        Assert.IsFalse(result.Preview.EventPersisted);
        Assert.IsFalse(result.Preview.EnablementAllowed);
        Assert.IsTrue(result.RequiresPostImplementationExternalAuditBeforeEnablement);
        Assert.IsFalse(result.SafeToEnableNow);
        Assert.IsTrue(result.NoSideEffectProof);
        AssertNoSideEffectCounts(result.Counts);
    }

    [TestMethod]
    public void Candidate_PublicApiDoesNotExposeRuntimeWriteOrRegistrationMethods()
    {
        var forbiddenNames = new[]
        {
            "Append",
            "Write",
            "Persist",
            "Save",
            "Delete",
            "Execute",
            "Run",
            "Start",
            "Register",
            "AddService",
            "CreateCommandHandler",
            "Export",
            "Redact",
            "Retain",
            "Scan"
        };

        var methodNames = typeof(DurableAuditTrailAppendOnlyCandidate)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(method => method.Name)
            .ToArray();

        foreach (var forbidden in forbiddenNames)
        {
            Assert.IsFalse(methodNames.Any(name => name.Contains(forbidden, StringComparison.OrdinalIgnoreCase)), forbidden);
        }
    }

    private static void AssertNoSideEffectCounts(DurableAuditTrailAppendOnlyCandidateCounts counts)
    {
        Assert.AreEqual(0, counts.DurableAuditTrailRealEnabledCount);
        Assert.AreEqual(0, counts.AppendWriteCount);
        Assert.AreEqual(0, counts.PersistedEventCount);
        Assert.AreEqual(0, counts.RuntimeInvocationCount);
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
}
