using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ImplementationPlanningGateDesignOnlyTests
{
    [TestMethod]
    public void PlanningGate_FixtureIsDeterministicAndDesignOnly()
    {
        var first = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();
        var second = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();

        Assert.AreEqual("nodal-os.implementation-planning-gate.design-only.fixture.v1", first.PacketId);
        Assert.AreEqual(first.PacketId, second.PacketId);
        Assert.AreEqual(first.GeneratedAtUtc, second.GeneratedAtUtc);
        Assert.AreEqual(first.Mode, second.Mode);
        Assert.AreEqual(first.CandidateMatrix.Count, second.CandidateMatrix.Count);
        Assert.AreEqual(first.GateMatrix.Count, second.GateMatrix.Count);
        Assert.IsTrue(first.PassesDesignOnlySafetyProof);
        Assert.IsTrue(second.PassesDesignOnlySafetyProof);
    }

    [TestMethod]
    public void PlanningGate_EvaluatesRequiredFutureCapabilityCandidates()
    {
        var packet = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();
        var candidateIds = packet.CandidateMatrix.Select(candidate => candidate.CandidateId).ToList();

        CollectionAssert.Contains(candidateIds, "APPROVAL_EXECUTION_MINIMAL_IMPLEMENTATION_PLANNING_DESIGN_ONLY");
        CollectionAssert.Contains(candidateIds, "PHYSICAL_EXPORT_CONTROLLED_IMPLEMENTATION_PLANNING_DESIGN_ONLY");
        CollectionAssert.Contains(candidateIds, "REDACTION_RUNTIME_MINIMAL_IMPLEMENTATION_PLANNING_DESIGN_ONLY");
        CollectionAssert.Contains(candidateIds, "RETENTION_DELETION_RUNTIME_MINIMAL_IMPLEMENTATION_PLANNING_DESIGN_ONLY");
        CollectionAssert.Contains(candidateIds, "BROWSER_CDP_SAFE_RUNTIME_PLANNING_DESIGN_ONLY");
        CollectionAssert.Contains(candidateIds, "WCU_OCR_SAFE_RUNTIME_PLANNING_DESIGN_ONLY");
        CollectionAssert.Contains(candidateIds, "RECIPES_EXECUTION_SAFE_RUNTIME_PLANNING_DESIGN_ONLY");
        CollectionAssert.Contains(candidateIds, "DURABLE_AUDIT_TRAIL_APPEND_ONLY_IMPLEMENTATION_PLANNING_DESIGN_ONLY");
        CollectionAssert.Contains(candidateIds, "MUTATION_STORE_MINIMAL_IMPLEMENTATION_PLANNING_DESIGN_ONLY");
    }

    [TestMethod]
    public void PlanningGate_RanksDurableAuditTrailAsFutureCandidateButNotApproved()
    {
        var packet = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();
        var recommended = packet.RecommendedFutureCandidate;

        Assert.AreEqual(1, recommended.RecommendedOrder);
        Assert.AreEqual(ImplementationCapabilityCandidateDecision.FutureCandidateBlockedByAudit, recommended.Decision);
        Assert.IsFalse(recommended.ApprovedForImplementation);
        Assert.IsTrue(recommended.IsStillBlocked);
        Assert.AreEqual(ImplementationRiskLevel.Medium, recommended.TechnicalRisk);
        Assert.AreEqual(ImplementationRiskLevel.Medium, recommended.SafetyRisk);
        Assert.AreEqual(ImplementationRiskLevel.Medium, recommended.DataRisk);
    }

    [TestMethod]
    public void PlanningGate_AllCandidatesRequirePreconditionsAuditsAndRollbackProof()
    {
        var packet = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.CandidateMatrix.All(candidate => candidate.RequiredPreconditions.Any(item => item.Contains("explicit user GO", StringComparison.Ordinal))));
        Assert.IsTrue(packet.CandidateMatrix.All(candidate => candidate.RequiredPreconditions.Any(item => item.Contains("external audit", StringComparison.Ordinal))));
        Assert.IsTrue(packet.CandidateMatrix.All(candidate => candidate.RequiredNegativeTests.Count >= 5));
        Assert.IsTrue(packet.CandidateMatrix.All(candidate => candidate.RequiredExternalAudits.Count >= 2));
        Assert.IsTrue(packet.CandidateMatrix.All(candidate => candidate.RequiredRollbackNoSideEffectProof.Count >= 3));
        Assert.IsTrue(packet.CandidateMatrix.All(candidate => !string.IsNullOrWhiteSpace(candidate.RequiredIsolatedScope)));
        Assert.IsTrue(packet.CandidateMatrix.All(candidate => !string.IsNullOrWhiteSpace(candidate.WhyNotNow)));
    }

    [TestMethod]
    public void PlanningGate_GatesCoverFailureModesAuditAndReleaseNoGo()
    {
        var packet = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();
        var gateIds = packet.GateMatrix.Select(gate => gate.GateId).ToList();

        CollectionAssert.Contains(gateIds, "rollback-no-side-effect-plan");
        CollectionAssert.Contains(gateIds, "evidence-audit-trail-plan");
        CollectionAssert.Contains(gateIds, "failure-mode-fail-closed");
        CollectionAssert.Contains(gateIds, "overclaim-scan");
        CollectionAssert.Contains(gateIds, "final-external-audit-before-enablement");
        CollectionAssert.Contains(gateIds, "release-commercial-no-go");
        Assert.IsTrue(packet.GateMatrix.All(gate => !string.IsNullOrWhiteSpace(gate.EvidenceRequired)));
    }

    [TestMethod]
    public void PlanningGate_HardeningRequirementsKeepBrowserWcuAndRecipesFutureOnly()
    {
        var packet = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();

        AssertFutureOnlyHardening(packet.BrowserCdpNegativeRequirements, "Browser/CDP", "system browser", "CDP live connection");
        AssertFutureOnlyHardening(packet.WcuOcrNegativeRequirements, "WCU/OCR", "real screen capture without gate", "OCR over real data");
        AssertFutureOnlyHardening(packet.RecipesNegativeRequirements, "Recipes", "recipe action runner", "background execution");
    }

    [TestMethod]
    public void PlanningGate_NegativeTestsCoverBrowserWcuAndRecipesNoSideEffects()
    {
        var packet = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();
        var negativeTestIds = packet.NegativeTestRequirements.Select(test => test.TestId).ToList();

        CollectionAssert.Contains(negativeTestIds, "browser-cdp-live");
        CollectionAssert.Contains(negativeTestIds, "wcu-ocr-live");
        CollectionAssert.Contains(negativeTestIds, "recipes-real-execution");
        Assert.IsTrue(packet.NegativeTestRequirements
            .Where(test => test.TestId is "browser-cdp-live" or "wcu-ocr-live" or "recipes-real-execution")
            .All(test => test.RequiredBeforeImplementation && !test.ImplementedNow));
        Assert.AreEqual(0, packet.Counts.BrowserCdpLiveEnabledCount);
        Assert.AreEqual(0, packet.Counts.WcuOcrLiveEnabledCount);
        Assert.AreEqual(0, packet.Counts.RecipesExecutionEnabledCount);
        Assert.IsFalse(packet.NoGoCapabilityStatus.CanUseBrowserCdpLiveNow);
        Assert.IsFalse(packet.NoGoCapabilityStatus.CanUseWcuOcrLiveNow);
        Assert.IsFalse(packet.NoGoCapabilityStatus.CanExecuteRecipesNow);
    }

    [TestMethod]
    public void PlanningGate_EvidenceLinksAreDocumentationOnly()
    {
        var packet = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();

        Assert.IsTrue(packet.EvidenceLinks.Count >= 5);
        Assert.IsTrue(packet.EvidenceLinks.All(link => link.StartsWith("docs/", StringComparison.Ordinal)));
        CollectionAssert.Contains(packet.EvidenceLinks.ToList(), "docs/qa/nodal-os-read-only-reentry-decision-packet/report.md");
        CollectionAssert.Contains(packet.EvidenceLinks.ToList(), "docs/decision-log.md");
    }

    [TestMethod]
    public void PlanningGate_WordingDoesNotClaimImplementationReadiness()
    {
        var packet = ImplementationPlanningGateDesignOnlyPresenter.CreateFixture();
        var text = string.Join(
            "\n",
            packet.Mode,
            packet.RecommendedFutureCandidateStatus,
            packet.HumanOperatorRecommendation,
            string.Join("\n", packet.Blockers),
            string.Join("\n", packet.Warnings),
            string.Join("\n", packet.CandidateMatrix.Select(candidate => $"{candidate.CandidateName} {candidate.WhyNotNow} {candidate.Decision}")),
            string.Join("\n", packet.BrowserCdpNegativeRequirements.Select(requirement => $"{requirement.Capability} {requirement.CandidateBlockedReason}")),
            string.Join("\n", packet.WcuOcrNegativeRequirements.Select(requirement => $"{requirement.Capability} {requirement.CandidateBlockedReason}")),
            string.Join("\n", packet.RecipesNegativeRequirements.Select(requirement => $"{requirement.Capability} {requirement.CandidateBlockedReason}")));

        var forbidden = new[]
        {
            "approved for implementation",
            "implementation ready",
            "runtime enabled",
            "execution enabled",
            "mutation enabled",
            "export enabled",
            "redaction runtime enabled",
            "retention enabled",
            "deletion enabled",
            "production ready",
            "release ready",
            "commercial ready"
        };

        foreach (var term in forbidden)
        {
            Assert.IsFalse(text.Contains(term, StringComparison.OrdinalIgnoreCase), term);
        }
    }

    private static void AssertFutureOnlyHardening(
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
        Assert.IsTrue(requirements.Any(requirement => requirement.ForbiddenUntilApproved.Contains(firstForbidden)));
        Assert.IsTrue(requirements.Any(requirement => requirement.ForbiddenUntilApproved.Contains(secondForbidden)));
        Assert.IsTrue(requirements.Any(requirement => requirement.ForbiddenUntilApproved.Contains("service registration")));
        Assert.IsTrue(requirements.Any(requirement => requirement.ForbiddenUntilApproved.Contains("command handler")));
    }
}
