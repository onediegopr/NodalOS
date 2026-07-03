using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class DurableAuditTrailAppendOnlyMinimalTests
{
    [TestMethod]
    public void MinimalLedger_AppendsTwoEventsWithHashChain()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();
        var policy = Policy(temp.Path);

        var first = ledger.Append(policy, Request("approval-001"));
        var second = ledger.Append(policy, Request("approval-002"));
        var verification = ledger.VerifyFile(first.LedgerFile!);

        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Appended, first.Decision);
        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Appended, second.Decision);
        Assert.AreEqual(1, first.Entry!.SequenceNumber);
        Assert.AreEqual(2, second.Entry!.SequenceNumber);
        Assert.AreEqual(first.Entry.EventHash, second.Entry.PreviousHash);
        Assert.IsTrue(File.Exists(first.LedgerFile));
        Assert.IsTrue(verification.Valid);
        Assert.AreEqual(2, verification.EntryCount);
        Assert.AreEqual(second.Entry.EventHash, verification.LastHash);
    }

    [TestMethod]
    public void MinimalLedger_PersistsJsonlWithoutProductActionOrReleaseReadiness()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();

        var result = ledger.Append(Policy(temp.Path), Request("approval-001"));
        var persisted = File.ReadAllText(result.LedgerFile!);

        Assert.AreEqual(1, result.AppendWriteCount);
        Assert.AreEqual(1, result.PersistedEventCount);
        Assert.IsFalse(result.ProductActionAllowed);
        Assert.IsFalse(result.NetworkAllowed);
        Assert.IsFalse(result.DbMigrationAllowed);
        Assert.IsFalse(result.CommandHandlerRegistered);
        Assert.IsFalse(result.ReleaseCommercialReady);
        Assert.IsTrue(persisted.Contains("approval.reviewed", StringComparison.Ordinal));
        Assert.IsTrue(persisted.Contains("approval-001", StringComparison.Ordinal));
    }

    private static DurableAuditTrailAppendOnlyMinimalPolicy Policy(string root) =>
        new(Enabled: true, StorageRoot: root);

    private static DurableAuditTrailAppendOnlyMinimalRequest Request(string approvalReference) =>
        new(
            EventKind: DurableAuditTrailAppendOnlyMinimal.SupportedEventKind,
            ActorReference: "human-operator:fixture",
            ApprovalReference: approvalReference,
            EvidenceReferences:
            [
                "docs/qa/durable-audit-trail-fixture/report.md"
            ],
            Metadata: new Dictionary<string, string>
            {
                ["decision"] = "approved-for-minimal-append-only-test"
            });

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"nodal-durable-audit-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
