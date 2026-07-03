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
        Assert.AreEqual(0, counts.BrowserCdpLiveEnabledCount);
        Assert.AreEqual(0, counts.WcuOcrLiveEnabledCount);
        Assert.AreEqual(0, counts.RecipesExecutionEnabledCount);
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
        Assert.IsFalse(status.CanUseBrowserCdpLiveNow);
        Assert.IsFalse(status.CanUseWcuOcrLiveNow);
        Assert.IsFalse(status.CanExecuteRecipesNow);
        Assert.AreEqual("NO-GO", status.ReleaseCommercialReadiness);
    }

    [TestMethod]
    public void PlanningGate_HardensBrowserCdpWcuOcrAndRecipesNegativeRequirements()
    {
        var packet = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();
        var negativeCapabilities = packet.NegativeTestRequirements.Select(test => test.Capability).ToList();

        CollectionAssert.Contains(negativeCapabilities, "browser/CDP live");
        CollectionAssert.Contains(negativeCapabilities, "WCU/OCR live");
        CollectionAssert.Contains(negativeCapabilities, "recipes real execution");
        Assert.IsTrue(packet.NegativeTestRequirements
            .Where(test => test.Capability is "browser/CDP live" or "WCU/OCR live" or "recipes real execution")
            .All(test => test.RequiredBeforeImplementation && !test.ImplementedNow));

        AssertBlockedHardening(packet.BrowserCdpNegativeRequirements, "Browser/CDP", "CDP live connection", "click, type or submit real action");
        AssertBlockedHardening(packet.WcuOcrNegativeRequirements, "WCU/OCR", "OCR over real data", "clipboard access");
        AssertBlockedHardening(packet.RecipesNegativeRequirements, "Recipes", "recipe action runner", "scheduler");
    }

    [TestMethod]
    public void PlanningGate_KeepsBrowserCdpWcuOcrAndRecipesCandidatesBlocked()
    {
        var packet = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();
        var sensitiveCandidateIds = new[]
        {
            "BROWSER_CDP_SAFE_RUNTIME_PLANNING_DESIGN_ONLY",
            "WCU_OCR_SAFE_RUNTIME_PLANNING_DESIGN_ONLY",
            "RECIPES_EXECUTION_SAFE_RUNTIME_PLANNING_DESIGN_ONLY"
        };

        foreach (var candidateId in sensitiveCandidateIds)
        {
            var candidate = packet.CandidateMatrix.Single(item => item.CandidateId == candidateId);

            Assert.AreEqual(ImplementationCapabilityCandidateDecision.Blocked, candidate.Decision);
            Assert.IsFalse(candidate.ApprovedForImplementation);
            Assert.IsTrue(candidate.IsStillBlocked);
            Assert.IsTrue(candidate.RequiredExternalAudits.Count >= 2);
            Assert.IsTrue(candidate.RequiredPreconditions.Any(item => item.Contains("explicit user GO", StringComparison.Ordinal)));
            Assert.IsTrue(candidate.RequiredPreconditions.Any(item => item.Contains("external audit", StringComparison.Ordinal)));
        }
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

    private static void AssertBlockedHardening(
        IReadOnlyList<ImplementationCapabilityHardeningRequirement> requirements,
        string capability,
        string firstForbidden,
        string secondForbidden)
    {
        Assert.IsTrue(requirements.Count > 0);
        Assert.IsTrue(requirements.All(requirement => requirement.Capability == capability));
        Assert.IsTrue(requirements.All(requirement => requirement.CandidateBlockedReason == "FUTURE_CANDIDATE_BLOCKED_BY_EXTERNAL_AUDIT_AND_USER_GO"));
        Assert.IsTrue(requirements.All(requirement => requirement.RequiredFailClosedBehavior));
        Assert.IsTrue(requirements.All(requirement => requirement.RequiredNoSideEffectProof));
        Assert.IsTrue(requirements.All(requirement => requirement.RequiredNegativeTestsBeforeImplementation));
        Assert.IsTrue(requirements.All(requirement => !requirement.AllowsRealCapabilityNow));
        Assert.IsTrue(requirements.All(requirement => requirement.RequiredExternalAuditScope.Contains("external audit", StringComparison.Ordinal)));
        Assert.IsTrue(requirements.All(requirement => requirement.RequiredUserGoScope.Contains("explicit human GO", StringComparison.Ordinal)));
        Assert.IsTrue(requirements.Any(requirement => requirement.ForbiddenUntilApproved.Contains(firstForbidden)));
        Assert.IsTrue(requirements.Any(requirement => requirement.ForbiddenUntilApproved.Contains(secondForbidden)));
        Assert.IsTrue(requirements.Any(requirement => requirement.ForbiddenUntilApproved.Contains("service registration")));
        Assert.IsTrue(requirements.Any(requirement => requirement.ForbiddenUntilApproved.Contains("command handler")));
        Assert.IsTrue(requirements.Any(requirement => requirement.ForbiddenUntilApproved.Contains("release/commercial readiness")));
    }
}
