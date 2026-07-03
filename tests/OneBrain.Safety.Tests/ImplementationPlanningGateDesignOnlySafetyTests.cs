using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ImplementationPlanningGateDesignOnlySafetyTests
{
    [TestMethod]
    public void PlanningGate_IsDesignOnlyAndKeepsCanonicalPausedState()
    {
        var packet = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();

        Assert.AreEqual(ImplementationPlanningGateStatus.DesignOnly, packet.Status);
        Assert.AreEqual("PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION_NO_MUTATION_NO_PHYSICAL_EXPORT_NO_REDACTION_RUNTIME", packet.CanonicalState);
        StringAssert.Contains(packet.Mode, "DESIGN_ONLY");
        Assert.IsTrue(packet.PassesDesignOnlySafetyProof);
    }

    [TestMethod]
    public void PlanningGate_DoesNotApproveAnyCandidateForImplementation()
    {
        var packet = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.CandidateMatrix.Count >= 9);
        Assert.IsTrue(packet.NoCandidateApprovedForImplementation);
        Assert.IsFalse(packet.CandidateMatrix.Any(candidate => candidate.ApprovedForImplementation));
        Assert.IsTrue(packet.CandidateMatrix.All(candidate => candidate.IsStillBlocked));
        Assert.IsFalse(packet.CandidateMatrix.Any(candidate => candidate.Decision == ImplementationCapabilityCandidateDecision.ApprovedForImplementation));
    }

    [TestMethod]
    public void PlanningGate_RecommendedCandidateRemainsBlockedByAudit()
    {
        var packet = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();

        Assert.AreEqual("DURABLE_AUDIT_TRAIL_APPEND_ONLY_IMPLEMENTATION_PLANNING_DESIGN_ONLY", packet.RecommendedFutureCandidateId);
        Assert.AreEqual("FUTURE_CANDIDATE_BLOCKED_BY_AUDIT", packet.RecommendedFutureCandidateStatus);
        Assert.AreEqual(ImplementationCapabilityCandidateDecision.FutureCandidateBlockedByAudit, packet.RecommendedFutureCandidate.Decision);
        StringAssert.Contains(packet.RecommendedFutureCandidate.WhyNotNow, "external audit");
        Assert.IsTrue(packet.RecommendedFutureCandidate.RequiredExternalAudits.Count >= 2);
    }

    [TestMethod]
    public void PlanningGate_RequiresExternalAuditUserGoAndNegativeTestsBeforeSensitiveCapabilities()
    {
        var packet = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();
        var gates = packet.GateMatrix.Select(gate => gate.GateId).ToList();

        CollectionAssert.Contains(gates, "explicit-user-go");
        CollectionAssert.Contains(gates, "external-audit-before-implementation");
        CollectionAssert.Contains(gates, "negative-tests-before-code");
        CollectionAssert.Contains(gates, "scope-isolation");
        CollectionAssert.Contains(gates, "final-external-audit-before-enablement");
        CollectionAssert.Contains(gates, "release-commercial-no-go");
        Assert.IsTrue(packet.GateMatrix.All(gate => gate.RequiredBeforeImplementation));
        Assert.IsTrue(packet.GateMatrix.All(gate => !gate.SatisfiedNow));
        Assert.IsTrue(packet.GateMatrix.All(gate => gate.BlocksImplementation));
        Assert.IsTrue(packet.NegativeTestRequirements.All(test => test.RequiredBeforeImplementation));
        Assert.IsTrue(packet.NegativeTestRequirements.All(test => !test.ImplementedNow));
    }

    [TestMethod]
    public void PlanningGate_KeepsAllEnabledCountsAtZero()
    {
        var counts = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture().Counts;

        Assert.IsTrue(counts.AllZero);
        Assert.AreEqual(0, counts.RuntimeEnabledCount);
        Assert.AreEqual(0, counts.ExecutionEnabledCount);
        Assert.AreEqual(0, counts.MutationEnabledCount);
        Assert.AreEqual(0, counts.ExportEnabledCount);
        Assert.AreEqual(0, counts.RedactionRuntimeEnabledCount);
        Assert.AreEqual(0, counts.RetentionDeletionEnabledCount);
        Assert.AreEqual(0, counts.ServiceRegistrationCount);
        Assert.AreEqual(0, counts.CommandHandlerCount);
        Assert.AreEqual(0, counts.ProductActionCount);
        Assert.AreEqual(0, counts.FilesystemOutputCount);
        Assert.AreEqual(0, counts.NetworkProviderCallCount);
    }

    [TestMethod]
    public void PlanningGate_KeepsAllRealCapabilitiesNoGo()
    {
        var status = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture().NoGoCapabilityStatus;

        Assert.IsTrue(status.AllNoGo);
        Assert.IsFalse(status.CanOpenRuntimeNow);
        Assert.IsFalse(status.CanExecuteNow);
        Assert.IsFalse(status.CanMutateNow);
        Assert.IsFalse(status.CanExportNow);
        Assert.IsFalse(status.CanRunRedactionRuntimeNow);
        Assert.IsFalse(status.CanRunSecretPiiScanNow);
        Assert.IsFalse(status.CanRunRetentionDeletionNow);
        Assert.IsFalse(status.CanRegisterServiceNow);
        Assert.IsFalse(status.CanCreateCommandHandlerNow);
        Assert.IsFalse(status.CanUseProductIoNow);
        Assert.IsFalse(status.CanUseProviderNetworkNow);
        Assert.AreEqual("NO-GO", status.ReleaseCommercialReadiness);
    }

    [TestMethod]
    public void PlanningGate_PublicApiDoesNotExposeExecutionExportMutationOrRegistrationMethods()
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
            "Scan"
        };

        var methodNames = typeof(ImplementationPlanningGateDesignOnlyPresenter)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(method => method.Name)
            .Where(name => name != nameof(ImplementationPlanningGateDesignOnlyPresenter.CreateFixture))
            .ToList();

        var packetMethods = typeof(ImplementationPlanningGateDesignOnly)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => method.Name)
            .Where(name => !name.StartsWith("get_", StringComparison.Ordinal))
            .Where(name => name is not nameof(object.Equals) and not nameof(object.GetHashCode) and not nameof(object.ToString))
            .ToList();

        foreach (var name in methodNames.Concat(packetMethods))
        {
            Assert.IsFalse(forbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)), name);
        }
    }
}
