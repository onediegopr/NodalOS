using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class FirstRealCapabilityCandidateScopeProposalReadOnlySafetyTests
{
    [TestMethod]
    public void ScopeProposal_SelectsExactlyOneCandidateWithoutImplementationApproval()
    {
        var packet = FirstRealCapabilityCandidateScopeProposalReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.PassesReadOnlySafetyProof);
        Assert.IsTrue(packet.ExactlyOneSelected);
        Assert.AreEqual("DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL_SCOPE_PROPOSAL_READ_ONLY", packet.SelectedCandidateId);
        Assert.AreEqual(FirstCapabilityCandidateDecision.SelectedScopeProposalReadOnly, packet.SelectedCandidate.Decision);
        Assert.AreEqual("SAFE_TO_PREPARE_EXTERNAL_AUDIT_FOR_SELECTED_CANDIDATE", packet.Decision);
        Assert.AreEqual("BLOCKED_NOT_EXECUTABLE", packet.FutureImplementationPromptStatus);
        Assert.IsFalse(packet.NoGoStatus.SafeToImplementNow);
        Assert.IsFalse(packet.NoGoStatus.ImplementationPromptExecutableNow);
    }

    [TestMethod]
    public void ScopeProposal_KeepsAllRealCapabilityCountsAtZero()
    {
        var counts = FirstRealCapabilityCandidateScopeProposalReadOnlyPresenter.CreateFixture().Counts;

        Assert.IsTrue(counts.AllZero);
        Assert.AreEqual(0, counts.RuntimeEnabledCount);
        Assert.AreEqual(0, counts.ExecutionEnabledCount);
        Assert.AreEqual(0, counts.MutationEnabledCount);
        Assert.AreEqual(0, counts.ExportEnabledCount);
        Assert.AreEqual(0, counts.BrowserCdpLiveEnabledCount);
        Assert.AreEqual(0, counts.WcuOcrLiveEnabledCount);
        Assert.AreEqual(0, counts.RecipesExecutionEnabledCount);
        Assert.AreEqual(0, counts.ServiceRegistrationCount);
        Assert.AreEqual(0, counts.CommandHandlerCount);
        Assert.AreEqual(0, counts.ProductActionCount);
        Assert.AreEqual(0, counts.FilesystemOutputCount);
        Assert.AreEqual(0, counts.NetworkProviderCallCount);
        Assert.AreEqual(0, counts.ReleaseCommercialReadyCount);
    }

    [TestMethod]
    public void ScopeProposal_RequiresAuditsGatesAndNegativeTestsBeforeAnyImplementation()
    {
        var packet = FirstRealCapabilityCandidateScopeProposalReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.RequiredGates.Count >= 17);
        Assert.IsTrue(packet.RequiredGates.All(gate => gate.RequiredBeforeImplementation && !gate.SatisfiedNow && gate.BlocksImplementation));
        Assert.IsTrue(packet.RequiredNegativeTests.Count >= 20);
        Assert.IsTrue(packet.RequiredNegativeTests.All(test => test.RequiredBeforeImplementation && !test.ImplementedNow));
        Assert.IsTrue(packet.ExternalAuditRequirements.All(audit => audit.RequiredBeforeImplementation));
        Assert.IsTrue(packet.ExternalAuditRequirements.All(audit => audit.RequiredAfterImplementationBeforeEnablement));
        Assert.IsTrue(packet.ExternalAuditRequirements.All(audit => !audit.SatisfiedNow));
    }

    [TestMethod]
    public void ScopeProposal_DefersHighRiskBrowserWcuRecipesAndExportCandidates()
    {
        var packet = FirstRealCapabilityCandidateScopeProposalReadOnlyPresenter.CreateFixture();

        AssertDeferred(packet, "BROWSER_CDP_SAFE_RUNTIME_SCOPE_PROPOSAL_READ_ONLY", FirstCapabilityCandidateDecision.NoGoHighRisk);
        AssertDeferred(packet, "WCU_OCR_SAFE_RUNTIME_SCOPE_PROPOSAL_READ_ONLY", FirstCapabilityCandidateDecision.NoGoHighRisk);
        AssertDeferred(packet, "RECIPES_EXECUTION_SAFE_RUNTIME_SCOPE_PROPOSAL_READ_ONLY", FirstCapabilityCandidateDecision.NoGoHighRisk);
        AssertDeferred(packet, "PHYSICAL_EXPORT_CONTROLLED_SCOPE_PROPOSAL_READ_ONLY", FirstCapabilityCandidateDecision.NotFirstCandidate);
        AssertDeferred(packet, "REDACTION_RUNTIME_MINIMAL_SCOPE_PROPOSAL_READ_ONLY", FirstCapabilityCandidateDecision.NotFirstCandidate);
        AssertDeferred(packet, "RETENTION_DELETION_RUNTIME_SCOPE_PROPOSAL_READ_ONLY", FirstCapabilityCandidateDecision.NoGoHighRisk);
        AssertDeferred(packet, "MUTATION_STORE_MINIMAL_SCOPE_PROPOSAL_READ_ONLY", FirstCapabilityCandidateDecision.FutureCandidateBlocked);
    }

    [TestMethod]
    public void ScopeProposal_PublicApiDoesNotExposeRealCapabilityMethods()
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

        var presenterMethods = typeof(FirstRealCapabilityCandidateScopeProposalReadOnlyPresenter)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(method => method.Name)
            .Where(name => name != nameof(FirstRealCapabilityCandidateScopeProposalReadOnlyPresenter.CreateFixture))
            .ToList();

        var packetMethods = typeof(FirstRealCapabilityCandidateScopeProposalReadOnlyPacket)
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

    private static void AssertDeferred(
        FirstRealCapabilityCandidateScopeProposalReadOnlyPacket packet,
        string candidateId,
        FirstCapabilityCandidateDecision expectedDecision)
    {
        var candidate = packet.CandidateMatrix.Single(item => item.CandidateId == candidateId);

        Assert.AreEqual(expectedDecision, candidate.Decision);
        Assert.IsFalse(candidate.SuitableAsFirstCandidate);
        Assert.IsTrue(candidate.KeepsImplementationBlocked);
    }
}
