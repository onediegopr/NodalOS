using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class DurableAuditTrailAppendOnlyMinimalSafetyTests
{
    [TestMethod]
    public void MinimalLedger_FailsClosedWhenDisabledOrEventKindUnexpected()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();

        var disabled = ledger.Append(new DurableAuditTrailAppendOnlyMinimalPolicy(false, temp.Path), Request());
        var unexpected = ledger.Append(Policy(temp.Path), Request() with { EventKind = "runtime.executed" });

        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, disabled.Decision);
        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, unexpected.Decision);
        CollectionAssert.Contains(disabled.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.Disabled);
        CollectionAssert.Contains(unexpected.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.UnexpectedEventKind);
        AssertNoSideEffects(disabled);
        AssertNoSideEffects(unexpected);
    }

    [TestMethod]
    public void MinimalLedger_RejectsRawPayloadAndSecretLikeContent()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();

        var rawPayload = ledger.Append(Policy(temp.Path), Request() with { RawPayload = "{\"secret\":\"raw\"}" });
        var secretLike = ledger.Append(Policy(temp.Path), Request() with { ActorReference = "token=raw-secret" });

        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, rawPayload.Decision);
        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, secretLike.Decision);
        CollectionAssert.Contains(rawPayload.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.RawPayloadRejected);
        CollectionAssert.Contains(secretLike.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.SecretLikeContentRejected);
        AssertNoSideEffects(rawPayload);
        AssertNoSideEffects(secretLike);
    }

    [TestMethod]
    public void MinimalLedger_FailsClosedOutsideLocalTestStorageBoundaryByDefault()
    {
        var ledger = new DurableAuditTrailAppendOnlyMinimal();
        var outsideTemp = System.IO.Path.GetFullPath("durable-audit-trail-not-temp");

        var result = ledger.Append(new DurableAuditTrailAppendOnlyMinimalPolicy(true, outsideTemp), Request());

        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.StorageRootOutsideLocalTestBoundary);
        AssertNoSideEffects(result);
    }

    [TestMethod]
    public void MinimalLedger_DetectsTamperedExistingLedgerAndRefusesFurtherAppend()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();
        var first = ledger.Append(Policy(temp.Path), Request());

        var text = File.ReadAllText(first.LedgerFile!);
        File.WriteAllText(first.LedgerFile!, text.Replace("approval-001", "approval-999", StringComparison.Ordinal));

        var verification = ledger.VerifyFile(first.LedgerFile!);
        var second = ledger.Append(Policy(temp.Path), Request() with { ApprovalReference = "approval-002" });

        Assert.IsFalse(verification.Valid);
        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, second.Decision);
        CollectionAssert.Contains(second.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.ExistingLedgerIntegrityFailed);
        AssertNoSideEffects(second);
    }

    [TestMethod]
    public void MinimalLedger_PublicSurfaceDoesNotRegisterCommandsOrRuntimeExecution()
    {
        var forbiddenNames = new[]
        {
            "Register",
            "CommandHandler",
            "Execute",
            "RunProductAction",
            "Network",
            "Migrate"
        };

        var methodNames = typeof(DurableAuditTrailAppendOnlyMinimal)
            .GetMethods()
            .Where(method => method.DeclaringType == typeof(DurableAuditTrailAppendOnlyMinimal))
            .Select(method => method.Name)
            .ToArray();

        foreach (var forbidden in forbiddenNames)
        {
            Assert.IsFalse(methodNames.Any(name => name.Contains(forbidden, StringComparison.OrdinalIgnoreCase)), forbidden);
        }
    }

    private static DurableAuditTrailAppendOnlyMinimalPolicy Policy(string root) =>
        new(Enabled: true, StorageRoot: root);

    private static DurableAuditTrailAppendOnlyMinimalRequest Request() =>
        new(
            EventKind: DurableAuditTrailAppendOnlyMinimal.SupportedEventKind,
            ActorReference: "human-operator:fixture",
            ApprovalReference: "approval-001",
            EvidenceReferences:
            [
                "docs/qa/durable-audit-trail-fixture/report.md"
            ]);

    private static void AssertNoSideEffects(DurableAuditTrailAppendOnlyMinimalResult result)
    {
        Assert.AreEqual(0, result.AppendWriteCount);
        Assert.AreEqual(0, result.PersistedEventCount);
        Assert.IsFalse(result.ProductActionAllowed);
        Assert.IsFalse(result.NetworkAllowed);
        Assert.IsFalse(result.DbMigrationAllowed);
        Assert.IsFalse(result.CommandHandlerRegistered);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"nodal-durable-audit-safety-{Guid.NewGuid():N}");
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
