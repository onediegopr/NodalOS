using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Evidence;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("EvidenceIntelligence")]
[TestCategory("EvidenceIntelligenceTimelineExportReadOnly")]
public sealed class EvidenceIntelligenceTimelineExportReadOnlyTests
{
    [TestMethod]
    public void TimelineExportPreview_IsReadOnlyDeterministicAndFixtureSafe()
    {
        var first = EvidenceIntelligenceTimelineExportReadOnlyPresenter.CreateFixture();
        var second = EvidenceIntelligenceTimelineExportReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(first.ReadOnly);
        Assert.IsTrue(first.Deterministic);
        Assert.IsFalse(first.PhysicalExportEnabled);
        Assert.IsTrue(first.NoSideEffectProof.Passes);
        Assert.AreEqual(first.CopyReadyPreview, second.CopyReadyPreview);
        Assert.AreEqual("READ_ONLY_FIXTURE_SAFE_NO_EXPORT_FILE", first.Manifest.Mode);
        Assert.AreEqual("EvidenceIntelligenceReadOnlyUiMount.CreateFixture", first.Manifest.SourceLabel);
        Assert.IsFalse(first.Manifest.PhysicalExportEnabled);
        Assert.IsTrue(first.Manifest.CopyReady);
    }

    [TestMethod]
    public void TimelineExportPreview_ContainsMinimumExpectedSections()
    {
        var export = EvidenceIntelligenceTimelineExportReadOnlyPresenter.CreateFixture();
        var sectionIds = export.Sections.Select(section => section.SectionId).ToHashSet(StringComparer.Ordinal);

        Assert.AreEqual(20, export.Sections.Count);
        CollectionAssert.IsSubsetOf(
            new[]
            {
                "executive-summary",
                "evidence-index-summary",
                "timeline-events",
                "claims-and-evidence-links",
                "action-scan-results",
                "contradictions-and-risks",
                "typed-evidence-graph-summary",
                "readiness-matrix",
                "safe-next-step",
                "human-actions-required",
                "persistence-capability-status",
                "read-store-scaffold-status",
                "write-store-scaffold-status",
                "redaction-at-write-hostile-coverage",
                "dry-run-migration-plan-status",
                "schema-compatibility-guard-status",
                "export-blockers",
                "export-warnings",
                "no-side-effect-proof",
                "deferred-capabilities"
            }.ToList(),
            sectionIds.ToList());
        Assert.IsTrue(export.Sections.All(section => section.ExportableInReadOnlyPreview));
        Assert.IsTrue(export.Sections.All(section => !section.RealExportOccurred));
        Assert.IsTrue(export.Sections.All(section => section.NoSideEffectProof.Passes));
    }

    [TestMethod]
    public void TimelineExportPreview_IncludesEvidenceClaimsActionsReadinessAndGraph()
    {
        var export = EvidenceIntelligenceTimelineExportReadOnlyPresenter.CreateFixture();
        var text = export.CopyReadyPreview;

        StringAssert.Contains(text, "Evidence index summary");
        StringAssert.Contains(text, "Timeline events");
        StringAssert.Contains(text, "Claims and evidence links");
        StringAssert.Contains(text, "Action scan results");
        StringAssert.Contains(text, "Contradictions and risks");
        StringAssert.Contains(text, "Typed evidence graph summary");
        StringAssert.Contains(text, "Readiness matrix");
        StringAssert.Contains(text, "Safe next step");
        Assert.IsTrue(export.Timeline.Count >= 5);
        Assert.IsTrue(export.Manifest.IncludedEvidenceRefs.Count > 0);
    }

    [TestMethod]
    public void TimelineExportPreview_IncludesPersistenceScaffoldMigrationAndSchemaStatus()
    {
        var export = EvidenceIntelligenceTimelineExportReadOnlyPresenter.CreateFixture();
        var text = export.CopyReadyPreview;

        StringAssert.Contains(text, "Persistence capability status");
        StringAssert.Contains(text, "Read store scaffold status");
        StringAssert.Contains(text, "Write store scaffold status");
        StringAssert.Contains(text, "Redaction-at-write hostile coverage");
        StringAssert.Contains(text, "Dry-run migration plan status");
        StringAssert.Contains(text, "Schema compatibility guard status");
        Assert.IsTrue(export.PersistenceStatus.FailClosed);
        Assert.IsTrue(export.ReadStoreStatus.FailClosed);
        Assert.IsTrue(export.WriteStoreStatus.FailClosed);
        Assert.IsTrue(export.DryRunMigrationStatus.FailClosed);
        Assert.IsTrue(export.SchemaCompatibilityStatus.FailClosed);
        Assert.IsFalse(export.PersistenceStatus.DurableStoreEnabled);
        Assert.IsFalse(export.ReadStoreStatus.FilesystemReadEnabled);
        Assert.IsFalse(export.WriteStoreStatus.FilesystemWriteEnabled);
        Assert.IsFalse(export.DryRunMigrationStatus.MigrationExecutionEnabled);
        Assert.IsFalse(export.SchemaCompatibilityStatus.ServiceRegistrationEnabled);
    }

    [TestMethod]
    public void TimelineExportPreview_ExcludesRawPayloadSecretsAndPhysicalExport()
    {
        var export = EvidenceIntelligenceTimelineExportReadOnlyPresenter.CreateFixture();
        var text = export.CopyReadyPreview;

        Assert.IsFalse(text.Contains("sk-", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("Bearer" + " ", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("AKIA", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("ghp_", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("raw payload", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("production" + "-ready", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(export.NoSideEffectProof.ExportFileCreated);
        Assert.IsFalse(export.NoSideEffectProof.FilesystemWriteAttempted);
        CollectionAssert.Contains(export.Manifest.ExcludedContentClasses.ToList(), "secret-like content");
        CollectionAssert.Contains(export.Manifest.ExcludedContentClasses.ToList(), "sensitive-never-persist fields");
    }

    [TestMethod]
    public void TimelineExportPreview_ModelsWarningsBlockersAndNoSideEffects()
    {
        var export = EvidenceIntelligenceTimelineExportReadOnlyPresenter.CreateFixture();

        Assert.IsTrue(export.Warnings.Count >= 3);
        Assert.IsTrue(export.Blockers.Count >= 3);
        CollectionAssert.Contains(export.Blockers.ToList(), "Physical export to filesystem is disabled.");
        Assert.IsTrue(export.Sections.Any(section => section.Status == EvidenceIntelligenceTimelineExportSectionStatus.Warning));
        Assert.IsTrue(export.Sections.Any(section => section.Status == EvidenceIntelligenceTimelineExportSectionStatus.Blocked));
        Assert.IsTrue(export.Sections.Any(section => section.Status == EvidenceIntelligenceTimelineExportSectionStatus.Deferred));
        Assert.IsFalse(export.NoSideEffectProof.FilesystemReadAttempted);
        Assert.IsFalse(export.NoSideEffectProof.DatabaseTouched);
        Assert.IsFalse(export.NoSideEffectProof.MigrationRunnerStarted);
        Assert.IsFalse(export.NoSideEffectProof.MigrationExecuted);
        Assert.IsFalse(export.NoSideEffectProof.ProviderCloudTouched);
        Assert.IsFalse(export.NoSideEffectProof.RuntimeTouched);
        Assert.IsFalse(export.NoSideEffectProof.ProductWriteFallbackUsed);
    }
}
