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
    public void MinimalLedger_FailsClosedForNullReferenceFieldsAndMalformedMetadataWithoutThrowing()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();

        var nullActor = ledger.Append(Policy(temp.Path), Request() with { ActorReference = null! });
        var nullApproval = ledger.Append(Policy(temp.Path), Request() with { ApprovalReference = null! });
        var nullEvidenceElement = ledger.Append(Policy(temp.Path), Request() with { EvidenceReferences = [null!] });
        var nullEvidenceList = ledger.Append(Policy(temp.Path), Request() with { EvidenceReferences = null! });
        var nullMetadataValue = ledger.Append(
            Policy(temp.Path),
            Request() with { Metadata = new Dictionary<string, string> { ["note"] = null! } });
        var blankMetadataKey = ledger.Append(
            Policy(temp.Path),
            Request() with { Metadata = new Dictionary<string, string> { ["   "] = "value" } });

        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, nullActor.Decision);
        CollectionAssert.Contains(nullActor.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.MissingActorReference);
        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, nullApproval.Decision);
        CollectionAssert.Contains(nullApproval.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.MissingApprovalReference);
        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, nullEvidenceElement.Decision);
        CollectionAssert.Contains(nullEvidenceElement.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.MissingEvidenceReference);
        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, nullEvidenceList.Decision);
        CollectionAssert.Contains(nullEvidenceList.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.MissingEvidenceReference);
        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, nullMetadataValue.Decision);
        CollectionAssert.Contains(nullMetadataValue.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.MalformedMetadata);
        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, blankMetadataKey.Decision);
        CollectionAssert.Contains(blankMetadataKey.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.MalformedMetadata);

        AssertNoSideEffects(nullActor);
        AssertNoSideEffects(nullApproval);
        AssertNoSideEffects(nullEvidenceElement);
        AssertNoSideEffects(nullEvidenceList);
        AssertNoSideEffects(nullMetadataValue);
        AssertNoSideEffects(blankMetadataKey);
        Assert.IsFalse(File.Exists(LedgerFile(temp.Path)));
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
    public void Stage2TestOnly_FailsClosedWithoutExplicitTestFeatureFlag()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();
        var gates = new[]
        {
            null,
            new DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate(false, null, RedactionProof()),
            new DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate(true, null, RedactionProof()),
            new DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate(true, string.Empty, RedactionProof()),
            new DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate(true, "true", RedactionProof()),
            new DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate(true, "enabled", RedactionProof()),
            new DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate(true, "enabled:product", RedactionProof())
        };

        foreach (var gate in gates)
        {
            var result = ledger.AppendStage2TestOnly(Policy(temp.Path), Request(), gate);

            Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, result.Decision, gate?.FeatureFlagValue);
            AssertNoSideEffects(result);
        }

        Assert.IsFalse(File.Exists(LedgerFile(temp.Path)));
    }

    [TestMethod]
    public void Stage2TestOnly_FailsClosedWithoutRedactionBeforePersistenceProof()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();
        var flag = "enabled:test-only";
        var gates = new[]
        {
            new DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate(true, flag, null),
            new DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate(true, flag, RedactionProof() with { PolicyReference = "" }),
            new DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate(true, flag, RedactionProof() with { FieldClassificationCompleted = false }),
            new DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate(true, flag, RedactionProof() with { RedactionCompleted = false }),
            new DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate(true, flag, RedactionProof() with { CompletedBeforePersistence = false }),
            new DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate(true, flag, RedactionProof() with { Succeeded = false })
        };

        foreach (var gate in gates)
        {
            var result = ledger.AppendStage2TestOnly(Policy(temp.Path), Request(), gate);

            Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, result.Decision);
            CollectionAssert.Contains(result.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.MissingRedactionBeforePersistenceProof);
            AssertNoSideEffects(result);
        }

        Assert.IsFalse(File.Exists(LedgerFile(temp.Path)));
    }

    [TestMethod]
    public void Stage2TestOnly_RejectsProductLedgerPathEvenWhenUnderTemp()
    {
        var productLikeRoot = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"nodal-product-ledger-{Guid.NewGuid():N}");
        var ledger = new DurableAuditTrailAppendOnlyMinimal();

        var result = ledger.AppendStage2TestOnly(Policy(productLikeRoot), Request(), Stage2Gate());

        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.ProductLedgerPathRejected);
        AssertNoSideEffects(result);
        Assert.IsFalse(Directory.Exists(productLikeRoot));
    }

    [TestMethod]
    public void Stage2TestOnly_PreservesBaseEmptyStorageRootRejection()
    {
        var ledger = new DurableAuditTrailAppendOnlyMinimal();

        var result = ledger.AppendStage2TestOnly(new DurableAuditTrailAppendOnlyMinimalPolicy(true, string.Empty), Request(), Stage2Gate());

        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.EmptyStorageRoot);
        CollectionAssert.DoesNotContain(result.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.ProductLedgerPathRejected);
        AssertNoSideEffects(result);
    }

    [TestMethod]
    public void Stage2TestOnly_RejectsSecretLikeDataBeforeAnyPersistence()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();

        var result = ledger.AppendStage2TestOnly(
            Policy(temp.Path),
            Request() with
            {
                Metadata = new Dictionary<string, string>
                {
                    ["browser-cdp-payload"] = "Authorization: Bearer live-token"
                }
            },
            Stage2Gate());

        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.RejectReasons.ToArray(), DurableAuditTrailAppendOnlyMinimalRejectReason.SecretLikeContentRejected);
        AssertNoSideEffects(result);
        Assert.IsFalse(File.Exists(LedgerFile(temp.Path)));
    }

    [TestMethod]
    public void Stage2TestOnly_AppendsWithoutOverwritingDeletingOrTruncatingExistingEvents()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();

        var first = ledger.AppendStage2TestOnly(Policy(temp.Path), Request() with { ApprovalReference = "approval-stage2-001" }, Stage2Gate());
        var firstLine = File.ReadAllLines(first.LedgerFile!).Single();
        var firstLength = new FileInfo(first.LedgerFile!).Length;
        var second = ledger.AppendStage2TestOnly(Policy(temp.Path), Request() with { ApprovalReference = "approval-stage2-002" }, Stage2Gate());
        var lines = File.ReadAllLines(first.LedgerFile!);
        var secondLength = new FileInfo(first.LedgerFile!).Length;
        var verification = ledger.VerifyFile(first.LedgerFile!);

        Assert.AreEqual(DurableAuditTrailAppendOnlyMinimalDecision.Appended, second.Decision);
        Assert.AreEqual(2, lines.Length);
        Assert.AreEqual(firstLine, lines[0]);
        Assert.IsTrue(secondLength > firstLength);
        Assert.IsTrue(lines[1].Contains("approval-stage2-002", StringComparison.Ordinal));
        Assert.IsTrue(verification.Valid);
        Assert.AreEqual(2, verification.EntryCount);
        Assert.AreEqual(2, verification.LastSequenceNumber);
    }

    [TestMethod]
    public void Stage2TestOnly_ConcurrentLocalTempAppendsRemainAppendOnlyAndValid()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();
        var appendTasks = Enumerable.Range(1, 32)
            .Select(i => Task.Run(() => ledger.AppendStage2TestOnly(
                Policy(temp.Path),
                Request() with { ApprovalReference = $"approval-stage2-{i:000}" },
                Stage2Gate())))
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
        Assert.AreEqual(32, verification.EntryCount);
        CollectionAssert.AreEqual(Enumerable.Range(1, 32).Select(i => (long)i).ToArray(), entries.Select(entry => entry.SequenceNumber).ToArray());
        Assert.AreEqual(32, entries.Select(entry => entry.EventId).Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public void Stage2TestOnly_VerifyFileRepeatedReadsDoNotMutateLedger()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();
        ledger.AppendStage2TestOnly(Policy(temp.Path), Request() with { ApprovalReference = "approval-stage2-001" }, Stage2Gate());
        ledger.AppendStage2TestOnly(Policy(temp.Path), Request() with { ApprovalReference = "approval-stage2-002" }, Stage2Gate());
        var ledgerFile = LedgerFile(temp.Path);
        var beforeText = File.ReadAllText(ledgerFile);
        var beforeLength = new FileInfo(ledgerFile).Length;

        var firstRead = ledger.VerifyFile(ledgerFile);
        var secondRead = ledger.VerifyFile(ledgerFile);
        var afterText = File.ReadAllText(ledgerFile);
        var afterLength = new FileInfo(ledgerFile).Length;

        Assert.IsTrue(firstRead.Valid);
        Assert.IsTrue(secondRead.Valid);
        Assert.AreEqual(firstRead.EntryCount, secondRead.EntryCount);
        Assert.AreEqual(firstRead.LastHash, secondRead.LastHash);
        Assert.AreEqual(beforeLength, afterLength);
        Assert.AreEqual(beforeText, afterText);
    }

    [TestMethod]
    public void Stage2TestOnly_LocalHashChainDoesNotOverclaimTailDeletionEvidenceWithoutCheckpoint()
    {
        using var temp = new TempDirectory();
        var ledger = new DurableAuditTrailAppendOnlyMinimal();
        ledger.AppendStage2TestOnly(Policy(temp.Path), Request() with { ApprovalReference = "approval-stage2-001" }, Stage2Gate());
        ledger.AppendStage2TestOnly(Policy(temp.Path), Request() with { ApprovalReference = "approval-stage2-002" }, Stage2Gate());
        var ledgerFile = LedgerFile(temp.Path);
        var originalVerification = ledger.VerifyFile(ledgerFile);
        var originalLines = File.ReadAllLines(ledgerFile);

        File.WriteAllLines(ledgerFile, originalLines.Take(1));
        var afterTailDeletion = ledger.VerifyFile(ledgerFile);

        Assert.IsTrue(originalVerification.Valid);
        Assert.AreEqual(2, originalVerification.EntryCount);
        Assert.IsTrue(afterTailDeletion.Valid);
        Assert.AreEqual(1, afterTailDeletion.EntryCount);
        Assert.AreNotEqual(originalVerification.LastHash, afterTailDeletion.LastHash);
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

    private static DurableAuditTrailAppendOnlyMinimalStage2TestOnlyGate Stage2Gate() =>
        new(true, "enabled:test-only", RedactionProof());

    private static DurableAuditTrailAppendOnlyMinimalRedactionProof RedactionProof() =>
        new(
            PolicyReference: "stage2-test-only-redaction-policy.v1",
            FieldClassificationCompleted: true,
            RedactionCompleted: true,
            CompletedBeforePersistence: true,
            Succeeded: true);

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
