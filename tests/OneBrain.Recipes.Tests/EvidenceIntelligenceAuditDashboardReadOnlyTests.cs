using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Evidence;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("EvidenceIntelligence")]
[TestCategory("EvidenceIntelligenceAuditDashboardReadOnly")]
public sealed class EvidenceIntelligenceAuditDashboardReadOnlyTests
{
    [TestMethod]
    public void AuditDashboard_IsReadOnlyDeterministicAndFixtureSafe()
    {
        var first = EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture();
        var second = EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(first.ReadOnly);
        Assert.IsTrue(first.Deterministic);
        Assert.IsTrue(first.FixtureSafe);
        Assert.IsTrue(first.NoSideEffectProof.Passes);
        Assert.IsFalse(first.HasProductActions);
        Assert.AreEqual(first.ReadOnlySummary, second.ReadOnlySummary);
        Assert.AreEqual("READ_ONLY_AUDIT_SAFE_FIXTURE_NO_ACTIONS", first.Mode);
        Assert.AreEqual("EvidenceIntelligenceTimelineExportReadOnlyPresenter.CreateFixture", first.SourceLabel);
    }

    [TestMethod]
    public void AuditDashboard_ContainsMinimumExpectedCards()
    {
        var dashboard = EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture();
        var cardIds = dashboard.Cards.Select(card => card.CardId).ToHashSet(StringComparer.Ordinal);

        Assert.AreEqual(20, dashboard.Cards.Count);
        CollectionAssert.IsSubsetOf(
            new[]
            {
                "executive-audit-summary",
                "phase-c-readiness-summary",
                "eil-persistence-design",
                "read-store-scaffold-disabled",
                "write-store-scaffold-disabled",
                "redaction-at-write-hostile-coverage",
                "dry-run-migration-plan",
                "schema-compatibility-guards",
                "evidence-timeline-export-preview",
                "runtime-live-gate",
                "release-commercial-no-go",
                "provider-cloud-disabled",
                "filesystem-db-durable-disabled",
                "migration-runner-disabled",
                "raw-payload-secret-exclusion",
                "blockers-list",
                "warnings-list",
                "documented-debt-list",
                "no-side-effect-proof",
                "next-safe-step"
            }.ToList(),
            cardIds.ToList());
        Assert.IsTrue(dashboard.Cards.All(card => card.AllowedActionsCount == 0));
        Assert.IsTrue(dashboard.Cards.All(card => card.NoSideEffectProof.Passes));
    }

    [TestMethod]
    public void AuditDashboard_ShowsPhaseCAndEilPersistenceStatuses()
    {
        var dashboard = EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture();
        var text = dashboard.ReadOnlySummary;

        Assert.AreEqual("72-82%", dashboard.PhaseCReadinessRange);
        StringAssert.Contains(text, "EIL persistence design");
        StringAssert.Contains(text, "Read store scaffold disabled");
        StringAssert.Contains(text, "Write store scaffold disabled");
        StringAssert.Contains(text, "Redaction-at-write hostile fixture coverage");
        StringAssert.Contains(text, "Dry-run migration plan");
        StringAssert.Contains(text, "Schema compatibility guards");
        StringAssert.Contains(text, "Evidence timeline export preview");
        Assert.IsTrue(dashboard.TimelineExport.PersistenceStatus.FailClosed);
        Assert.IsTrue(dashboard.TimelineExport.ReadStoreStatus.FailClosed);
        Assert.IsTrue(dashboard.TimelineExport.WriteStoreStatus.FailClosed);
    }

    [TestMethod]
    public void AuditDashboard_ShowsRuntimeReleaseProviderFilesystemAndMigrationGates()
    {
        var dashboard = EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture();

        Assert.AreEqual("0%", dashboard.RuntimeLiveReadiness);
        Assert.AreEqual("NO-GO", dashboard.ReleaseCommercialDecision);
        Assert.IsTrue(dashboard.Gates.All(gate => !gate.RuntimeAllowed));
        Assert.IsTrue(dashboard.Gates.All(gate => !gate.ReleaseAllowed));
        Assert.IsTrue(dashboard.Cards.Any(card => card.CardId == "provider-cloud-disabled" && card.Status == EvidenceIntelligenceAuditDashboardCardStatus.Disabled));
        Assert.IsTrue(dashboard.Cards.Any(card => card.CardId == "filesystem-db-durable-disabled" && card.Status == EvidenceIntelligenceAuditDashboardCardStatus.Disabled));
        Assert.IsTrue(dashboard.Cards.Any(card => card.CardId == "migration-runner-disabled" && card.Status == EvidenceIntelligenceAuditDashboardCardStatus.Disabled));
        Assert.IsTrue(dashboard.Cards.Any(card => card.CardId == "runtime-live-gate" && card.Status == EvidenceIntelligenceAuditDashboardCardStatus.Blocked));
        Assert.IsTrue(dashboard.Cards.Any(card => card.CardId == "release-commercial-no-go" && card.Status == EvidenceIntelligenceAuditDashboardCardStatus.NoGo));
    }

    [TestMethod]
    public void AuditDashboard_ModelsBlockersWarningsDebtAndNextSafeStep()
    {
        var dashboard = EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(dashboard.Blockers.Count >= 4);
        Assert.IsTrue(dashboard.Warnings.Count >= 3);
        Assert.IsTrue(dashboard.DocumentedDebt.Count >= 4);
        StringAssert.Contains(dashboard.NextSafeStep, "closeout audit");
        Assert.IsTrue(dashboard.Cards.Any(card => card.Status == EvidenceIntelligenceAuditDashboardCardStatus.Blocked));
        Assert.IsTrue(dashboard.Cards.Any(card => card.Status == EvidenceIntelligenceAuditDashboardCardStatus.Warning));
        Assert.IsTrue(dashboard.Cards.Any(card => card.Status == EvidenceIntelligenceAuditDashboardCardStatus.Deferred));
    }

    [TestMethod]
    public void AuditDashboard_DoesNotExposeProductActionsExportOrProductionClaim()
    {
        var dashboard = EvidenceIntelligenceAuditDashboardReadOnlyPresenter.CreateFixture();
        var text = dashboard.ReadOnlySummary;

        Assert.IsFalse(dashboard.HasProductActions);
        Assert.IsFalse(dashboard.NoSideEffectProof.ProductActionButtonExposed);
        Assert.IsFalse(dashboard.NoSideEffectProof.ProductActionCommandExposed);
        Assert.IsFalse(dashboard.NoSideEffectProof.ExportFileCreated);
        Assert.IsFalse(dashboard.NoSideEffectProof.FilesystemWriteAttempted);
        Assert.IsFalse(text.Contains("sk-", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("Bearer" + " ", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("AKIA", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("ghp_", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("production" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("export " + "completed", StringComparison.OrdinalIgnoreCase));
    }
}
