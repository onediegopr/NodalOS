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
    public void MinimalLedger_RejectsSecretLikeContentVariants()
    {
        var variants = new[]
        {
            "password=value",
            "token=value",
            "secret=value",
            "Authorization: Bearer value",
            "Cookie: session=value",
            "-----BEGIN PRIVATE KEY-----",
            "api_key=value",
            "apikey=value",
            "api-key=value",
            "Bearer abcdefghijklmnopqrstuvwxyz",
            "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJmaXh0dXJlIn0.signature123",
            "ghp_abcdefghijklmnopqrstuvwxyz123456",
            "github_pat_abcdefghijklmnopqrstuvwxyz123456",
            "sk-proj-abcdefghijklmnopqrstuvwxyz",
            "User ID=fixture;Password=fixture",
            "AccountKey=fixture",
            "SharedAccessKey=fixture",
            "DefaultEndpointsProtocol=https",
            "-----BEGIN RSA PRIVATE KEY-----",
            "-----BEGIN OPENSSH PRIVATE KEY-----"
        };

        foreach (var secretLikeContent in variants)
        {
            using var temp = new TempDirectory();
            var ledger = new DurableAuditTrailAppendOnlyMinimal();

            var result = ledger.Append(
                Policy(temp.Path),
                Request() with
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["audit-note"] = secretLikeContent
                    }
                });

            Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, result.Decision, secretLikeContent);
            CollectionAssert.Contains(result.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.SecretLikeContentRejected);
            AssertNoSideEffects(result);
        }
    }

    [TestMethod]
    public void MinimalLedger_DoesNotRejectBenignTaskLikeTextAsOpenAiKey()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();

        var result = ledger.Append(
            Policy(temp.Path),
            Request() with
            {
                Metadata = new Dictionary<string, string>
                {
                    ["note"] = "task-local-fixture"
                }
            });

        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Appended, result.Decision);
        Assert.AreEqual(1, result.AppendWriteCount);
    }

    [TestMethod]
    public void MinimalLedger_FailsClosedOutsideLocalTestStorageBoundaryByDefault()
    {
        var ledger = new DurableAuditTrailAppendOnlyMinimal();
        var outsideTemp = System.IO.Path.Combine(
            Directory.GetCurrentDirectory(),
            $"durable-audit-trail-not-temp-{Guid.NewGuid():N}");

        var result = ledger.Append(new DurableAuditTrailAppendOnlyMinimalPolicy(true, outsideTemp), Request());

        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.StorageRootOutsideLocalTestBoundary);
        AssertNoSideEffects(result);
        Assert.IsFalse(Directory.Exists(outsideTemp));
    }

    [TestMethod]
    public void MinimalLedger_VerifyFileFailsClosedOutsideLocalTestBoundary()
    {
        var ledger = new DurableAuditTrailAppendOnlyMinimal();
        var outsideTemp = System.IO.Path.Combine(
            Directory.GetCurrentDirectory(),
            $"durable-audit-trail-product-path-{Guid.NewGuid():N}",
            "durable-audit-trail.append-only.jsonl");

        var verification = ledger.VerifyFile(outsideTemp);

        Assert.IsFalse(verification.Valid);
        CollectionAssert.Contains(verification.Errors.ToArray(), "ledger_outside_local_test_boundary");
        Assert.AreEqual(0, verification.EntryCount);
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
    public void MinimalLedger_DetectsMalformedLedgerJsonAndRefusesFurtherAppend()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();
        var ledgerFile = System.IO.Path.Combine(temp.Path, "durable-audit-trail.append-only.jsonl");
        File.WriteAllText(ledgerFile, "{not-json");

        var verification = ledger.VerifyFile(ledgerFile);
        var result = ledger.Append(Policy(temp.Path), Request());

        Assert.IsFalse(verification.Valid);
        CollectionAssert.Contains(verification.Errors.ToArray(), "malformed_json_line:1");
        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.ExistingLedgerIntegrityFailed);
        AssertNoSideEffects(result);
    }

    [TestMethod]
    public void MinimalLedger_DetectsInvalidEntryShapeAndRefusesFurtherAppend()
    {
        var cases = new Dictionary<string, string>
        {
            ["{}"] = "invalid_entry_shape_line:1",
            [ValidEntryJsonWithout(",\"eventHash\":\"hash-fixture\"")] = "missing_event_hash_line:1",
            [ValidEntryJsonWithout("\"previousHash\":\"genesis\",")] = "missing_previous_hash_line:1",
            [ValidEntryJsonWithout("\"sequenceNumber\":1,")] = "invalid_sequence_line:1",
            [ValidEntryJson().Replace("\"sequenceNumber\":1", "\"sequenceNumber\":0", StringComparison.Ordinal)] = "invalid_sequence_line:1",
            [ValidEntryJson().Replace("\"eventHash\":\"hash-fixture\"", "\"eventHash\":null", StringComparison.Ordinal)] = "missing_event_hash_line:1",
            [ValidEntryJson().Replace("\"previousHash\":\"genesis\"", "\"previousHash\":null", StringComparison.Ordinal)] = "missing_previous_hash_line:1",
            [ValidEntryJson().Replace("\"eventHash\":\"hash-fixture\"", "\"eventHash\":\"\"", StringComparison.Ordinal)] = "empty_event_hash_line:1",
            [ValidEntryJson().Replace("\"previousHash\":\"genesis\"", "\"previousHash\":\"   \"", StringComparison.Ordinal)] = "empty_previous_hash_line:1",
            ["{\"unsupported\":true}"] = "invalid_entry_shape_line:1"
        };

        foreach (var testCase in cases)
        {
            using var temp = new TempDirectory();
            var ledger = new DurableAuditTrailAppendOnlyMinimal();
            var ledgerFile = LedgerFile(temp.Path);
            File.WriteAllText(ledgerFile, testCase.Key);

            var verification = ledger.VerifyFile(ledgerFile);
            var result = ledger.Append(Policy(temp.Path), Request());

            Assert.IsFalse(verification.Valid, testCase.Value);
            CollectionAssert.Contains(verification.Errors.ToArray(), testCase.Value);
            Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, result.Decision, testCase.Value);
            CollectionAssert.Contains(result.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.ExistingLedgerIntegrityFailed);
            AssertNoSideEffects(result);
        }
    }

    [TestMethod]
    public void MinimalLedger_DetectsSequenceGapDuplicateReorderAndHashMismatches()
    {
        AssertCorruptedValidLedgerRejected(lines =>
        {
            lines[1] = lines[1].Replace("\"sequenceNumber\":2", "\"sequenceNumber\":3", StringComparison.Ordinal);
        }, "sequence_mismatch:3");

        AssertCorruptedValidLedgerRejected(lines =>
        {
            lines[1] = lines[1].Replace("\"sequenceNumber\":2", "\"sequenceNumber\":1", StringComparison.Ordinal);
        }, "sequence_mismatch:1");

        AssertCorruptedValidLedgerRejected(lines =>
        {
            (lines[0], lines[1]) = (lines[1], lines[0]);
        }, "sequence_mismatch:2");

        AssertCorruptedValidLedgerRejected(lines =>
        {
            lines[1] = lines[1].Replace("\"previousHash\":\"", "\"previousHash\":\"broken-", StringComparison.Ordinal);
        }, "previous_hash_mismatch:2");

        AssertCorruptedValidLedgerRejected(lines =>
        {
            lines[0] = lines[0].Replace("\"eventHash\":\"", "\"eventHash\":\"broken-", StringComparison.Ordinal);
        }, "event_hash_mismatch:1");
    }

    [TestMethod]
    public void MinimalLedger_FailsClosedForEmptyOrWhitespaceLedgerLinesButAllowsNormalTrailingNewline()
    {
        using var trailingNewline = new TempDirectory();
        var trailingLedger = new DurableAuditTrailAppendOnlyMinimal();
        var first = trailingLedger.Append(Policy(trailingNewline.Path), Request());

        Assert.IsTrue(File.ReadAllText(first.LedgerFile!).EndsWith(Environment.NewLine, StringComparison.Ordinal));
        Assert.IsTrue(trailingLedger.VerifyFile(first.LedgerFile!).Valid);

        AssertCorruptedValidLedgerRejected(lines => lines.Insert(1, string.Empty), "empty_line:2");
        AssertCorruptedValidLedgerRejected(lines => lines.Insert(1, "   "), "whitespace_line:2");
    }

    [TestMethod]
    public void MinimalLedger_SequentialAppendsRemainConsistentUnderLocalLedgerLock()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();
        for (var i = 1; i <= 5; i++)
        {
            var result = ledger.Append(Policy(temp.Path), Request() with { ApprovalReference = $"approval-{i:000}" });
            Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Appended, result.Decision);
            Assert.AreEqual(i, result.Entry!.SequenceNumber);
        }

        var verification = ledger.VerifyFile(LedgerFile(temp.Path));

        Assert.IsTrue(verification.Valid);
        Assert.AreEqual(5, verification.EntryCount);
        Assert.AreEqual(5, verification.LastSequenceNumber);
    }

    [TestMethod]
    public void MinimalLedger_ConcurrentLocalTestAppendsRemainAppendOnlyAndValid()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();
        var appendTasks = Enumerable.Range(1, 16)
            .Select(i => Task.Run(() => ledger.Append(Policy(temp.Path), Request() with { ApprovalReference = $"approval-{i:000}" })))
            .ToArray();

        Task.WaitAll(appendTasks);
        var results = appendTasks.Select(task => task.Result).ToArray();
        var verification = ledger.VerifyFile(LedgerFile(temp.Path));
        var lines = File.ReadAllLines(LedgerFile(temp.Path));
        var entries = lines
            .Select(line => System.Text.Json.JsonSerializer.Deserialize<DurableAuditTrailAppendOnlyMinimalEntry>(
                line,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!)
            .ToArray();

        Assert.IsTrue(results.All(result => result.Decision == DurableAuditTrailAppendOnlyMinimalDecision.Appended));
        Assert.IsTrue(results.All(result => result.AppendWriteCount == 1));
        Assert.IsTrue(results.All(result => result.PersistedEventCount == 1));
        Assert.IsTrue(verification.Valid);
        Assert.AreEqual(16, verification.EntryCount);
        CollectionAssert.AreEqual(Enumerable.Range(1, 16).Select(i => (long)i).ToArray(), entries.Select(entry => entry.SequenceNumber).ToArray());
        Assert.AreEqual(16, entries.Select(entry => entry.EventId).Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public void MinimalLedger_PublicSurfaceDoesNotRegisterCommandsOrRuntimeExecution()
    {
        var forbiddenNames = new[]
        {
            "Register",
            "AddCommandHandler",
            "ICommandHandler",
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

    [TestMethod]
    public void MinimalLedger_SourceContainsNoRuntimeRegistrationHandlersProductActionsOrExternalProviders()
    {
        var sourcePath = System.IO.Path.Combine(
            FindRepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "DurableAuditTrailAppendOnlyMinimal.cs");
        var source = File.ReadAllText(sourcePath);
        var forbiddenFragments = new[]
        {
            "AddSingleton",
            "AddScoped",
            "AddTransient",
            "IHostedService",
            "MapPost",
            "MapGet",
            "AddCommandHandler",
            "ICommandHandler",
            "ICommand",
            "RunProductAction",
            "ProductActionButton",
            "HttpClient",
            "WebSocket",
            "DbContext",
            "MigrationBuilder",
            "SaveChanges",
            "Browser",
            "CDP",
            "WCU",
            "OCR",
            "RecipeExecution",
            "ReleaseReady = true",
            "CommercialReady = true",
            "ReleaseCommercialReady: true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
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

    private static string LedgerFile(string root) =>
        System.IO.Path.Combine(root, "durable-audit-trail.append-only.jsonl");

    private static string ValidEntryJson() =>
        """
        {"sequenceNumber":1,"eventId":"audit-trail-fixture","eventKind":"approval.reviewed","createdAtUtc":"2026-01-01T00:00:00+00:00","actorReference":"human-operator:fixture","approvalReference":"approval-001","evidenceReferences":["docs/qa/durable-audit-trail-fixture/report.md"],"metadata":{},"previousHash":"genesis","eventHash":"hash-fixture"}
        """;

    private static string ValidEntryJsonWithout(string fragment) =>
        ValidEntryJson().Replace(fragment, string.Empty, StringComparison.Ordinal);

    private static void AssertCorruptedValidLedgerRejected(Action<List<string>> corrupt, string expectedError)
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();
        ledger.Append(Policy(temp.Path), Request() with { ApprovalReference = "approval-001" });
        ledger.Append(Policy(temp.Path), Request() with { ApprovalReference = "approval-002" });
        var ledgerFile = LedgerFile(temp.Path);
        var lines = File.ReadAllLines(ledgerFile).ToList();
        corrupt(lines);
        File.WriteAllLines(ledgerFile, lines);

        var verification = ledger.VerifyFile(ledgerFile);
        var result = ledger.Append(Policy(temp.Path), Request() with { ApprovalReference = "approval-003" });

        Assert.IsFalse(verification.Valid, expectedError);
        CollectionAssert.Contains(verification.Errors.ToArray(), expectedError);
        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, result.Decision, expectedError);
        CollectionAssert.Contains(result.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.ExistingLedgerIntegrityFailed);
        AssertNoSideEffects(result);
    }

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

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(System.IO.Path.Combine(
                directory.FullName,
                "src",
                "OneBrain.Core",
                "Approval",
                "DurableAuditTrailAppendOnlyMinimal.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
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
