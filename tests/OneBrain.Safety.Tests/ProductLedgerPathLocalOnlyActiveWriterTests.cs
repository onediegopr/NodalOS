using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPathLocalOnlyActiveWriterTests
{
    [TestMethod]
    public void LocalOnlyActiveWriter_FailsClosedByDefault()
    {
        var writer = new ProductLedgerPathLocalOnlyActiveWriter();

        var activation = writer.Activate(null);
        var append = writer.Append(null);

        Assert.AreEqual(ProductLedgerPathLocalOnlyActivationDecision.Rejected, activation.Decision);
        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.Rejected, append.Decision);
        CollectionAssert.Contains(activation.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.MissingRequest);
        CollectionAssert.Contains(append.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.MissingRequest);
        AssertNoRuntimeOrExternal(activation);
        AssertNoRuntimeOrExternal(append);
    }

    [TestMethod]
    public void LocalOnlyActivation_BlocksMissingFailedCandidateEvidenceAndRuntime()
    {
        using var fixture = LedgerFixture.Create();
        var ready = ReadyActivationRequest(fixture);
        var cases = new Dictionary<ProductLedgerPathLocalOnlyActivationRequest, ProductLedgerPathLocalOnlyBlocker>
        {
            [ready with { PersistedCandidateResult = null }] = ProductLedgerPathLocalOnlyBlocker.MissingPersistedCandidateResult,
            [ready with { PersistedCandidateResult = new ProductLedgerPathPersistedCandidateRegistry().Persist(null) }] = ProductLedgerPathLocalOnlyBlocker.FailedPersistedCandidateResult,
            [ready with { ExplicitLocalOnlyActivationMode = false }] = ProductLedgerPathLocalOnlyBlocker.MissingExplicitLocalOnlyActivationMode,
            [ready with { HasAuthorityEvidence = false }] = ProductLedgerPathLocalOnlyBlocker.MissingAuthorityEvidence,
            [ready with { HasRedactionBeforePersistenceEvidence = false }] = ProductLedgerPathLocalOnlyBlocker.MissingRedactionBeforePersistenceEvidence,
            [ready with { HasFailureReplayRollbackEvidence = false }] = ProductLedgerPathLocalOnlyBlocker.MissingFailureReplayRollbackEvidence,
            [ready with { HasRetentionEvidence = false }] = ProductLedgerPathLocalOnlyBlocker.MissingRetentionEvidence,
            [ready with { LocalRuntimeFlagDefaultOff = false }] = ProductLedgerPathLocalOnlyBlocker.RuntimeFlagNotDefaultOff,
            [ready with { ClaimsLocalTempAsProductLedgerPath = true }] = ProductLedgerPathLocalOnlyBlocker.LocalTempClaimedAsProductLedgerPath
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathLocalOnlyActiveWriter().Activate(testCase.Key);

            Assert.AreEqual(ProductLedgerPathLocalOnlyActivationDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoRuntimeOrExternal(result);
        }
    }

    [TestMethod]
    public void LocalOnlyActivation_BlocksExternalRuntimeAndReleaseClaims()
    {
        using var fixture = LedgerFixture.Create();
        var ready = ReadyActivationRequest(fixture);
        var cases = new Dictionary<ProductLedgerPathLocalOnlyActivationRequest, ProductLedgerPathLocalOnlyBlocker>
        {
            [ready with { RequestsRuntimeEnablement = true }] = ProductLedgerPathLocalOnlyBlocker.RuntimeEnablementRequested,
            [ready with { RequestsProductServiceRegistration = true }] = ProductLedgerPathLocalOnlyBlocker.ProductServiceRegistrationRequested,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerPathLocalOnlyBlocker.ProductCommandHandlerRequested,
            [ready with { RequestsUiProductAction = true }] = ProductLedgerPathLocalOnlyBlocker.UiProductActionRequested,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerPathLocalOnlyBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsWormKmsExternalTrust = true }] = ProductLedgerPathLocalOnlyBlocker.WormKmsExternalTrustClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerPathLocalOnlyBlocker.DbMigrationClaimed,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerPathLocalOnlyBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ready with { ClaimsReleaseCommercialReadiness = true }] = ProductLedgerPathLocalOnlyBlocker.ReleaseCommercialReadinessClaimed
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathLocalOnlyActiveWriter().Activate(testCase.Key);

            Assert.AreEqual(ProductLedgerPathLocalOnlyActivationDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoRuntimeOrExternal(result);
        }
    }

    [TestMethod]
    public void LocalOnlyActivation_EnablesOnlyBoundedLocalPathAndWriter()
    {
        using var fixture = LedgerFixture.Create();
        var activation = new ProductLedgerPathLocalOnlyActiveWriter().Activate(ReadyActivationRequest(fixture));

        Assert.AreEqual(ProductLedgerPathLocalOnlyActivationDecision.ActivatedLocalOnly, activation.Decision);
        Assert.IsTrue(activation.ProductLedgerPathActive);
        Assert.IsTrue(activation.ProductLedgerWriteAllowed);
        Assert.IsTrue(activation.LocalRuntimeFlagDefaultOff);
        Assert.AreEqual(fixture.LedgerRoot, activation.ActiveLedgerRootPath);
        Assert.IsTrue(activation.ActiveLedgerFilePath!.StartsWith(fixture.LedgerRoot, StringComparison.OrdinalIgnoreCase));
        AssertNoRuntimeOrExternal(activation);
        StringAssert.Contains(activation.StatusText, "ACTIVE_PRODUCT_LEDGER_PATH_LOCAL_ONLY");
        StringAssert.Contains(activation.StatusText, "RUNTIME_FLAG_DEFAULT_OFF");
    }

    [TestMethod]
    public void LocalOnlyWriter_AppendsReadsAndVerifiesBoundedLedger()
    {
        using var fixture = LedgerFixture.Create();
        var writer = new ProductLedgerPathLocalOnlyActiveWriter();
        var activation = writer.Activate(ReadyActivationRequest(fixture));
        var first = writer.Append(ReadyAppendRequest(activation));
        var second = writer.Append(ReadyAppendRequest(activation) with { SafePayloadHash = new string('b', 64) });

        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly, first.Decision);
        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly, second.Decision);
        Assert.IsTrue(first.ProductLedgerPathActive);
        Assert.IsTrue(first.ProductLedgerWriteAllowed);
        AssertNoRuntimeOrExternal(first);
        AssertNoRuntimeOrExternal(second);
        Assert.AreEqual(1, first.Entry!.Sequence);
        Assert.AreEqual(2, second.Entry!.Sequence);
        Assert.AreEqual(first.Entry.EntryHash, second.Entry.PreviousEntryHash);
        Assert.IsTrue(File.Exists(first.ActiveLedgerFilePath));
        Assert.IsTrue(File.Exists(first.ActiveCheckpointFilePath));

        var entries = writer.ReadVerified(activation);
        Assert.AreEqual(2, entries.Count);
        Assert.AreEqual(new string('a', 64), entries[0].SafePayloadHash);
        Assert.AreEqual(new string('b', 64), entries[1].SafePayloadHash);
    }

    [TestMethod]
    public void LocalOnlyWriter_BlocksRuntimeExternalAndUnsafePayload()
    {
        using var fixture = LedgerFixture.Create();
        var writer = new ProductLedgerPathLocalOnlyActiveWriter();
        var activation = writer.Activate(ReadyActivationRequest(fixture));
        var ready = ReadyAppendRequest(activation);
        var cases = new Dictionary<ProductLedgerPathLocalOnlyAppendRequest, ProductLedgerPathLocalOnlyBlocker>
        {
            [ready with { ActivationResult = null }] = ProductLedgerPathLocalOnlyBlocker.MissingActivationResult,
            [ready with { RuntimeFlagStillDefaultOff = false }] = ProductLedgerPathLocalOnlyBlocker.RuntimeFlagNotDefaultOff,
            [ready with { SafePayloadHash = "" }] = ProductLedgerPathLocalOnlyBlocker.MissingSafePayloadHash,
            [ready with { SafePayloadHash = "raw-secret" }] = ProductLedgerPathLocalOnlyBlocker.UnsafePayloadHash,
            [ready with { SafePayloadHash = new string('G', 64) }] = ProductLedgerPathLocalOnlyBlocker.UnsafePayloadHash,
            [ready with { EvidenceMetadata = new Dictionary<string, string> { ["payload"] = "bearer redacted" } }] = ProductLedgerPathLocalOnlyBlocker.UnsafeEvidenceMetadata,
            [ready with { EvidenceMetadata = new Dictionary<string, string> { ["path"] = "C:\\Users\\synthetic" } }] = ProductLedgerPathLocalOnlyBlocker.UnsafeEvidenceMetadata,
            [ready with { EvidenceMetadata = new Dictionary<string, string> { ["ref"] = new string('x', 129) } }] = ProductLedgerPathLocalOnlyBlocker.RetentionLimitExceeded,
            [ready with { EvidenceMetadata = Enumerable.Range(0, 13).ToDictionary(index => $"field{index:00}", index => "safe") }] = ProductLedgerPathLocalOnlyBlocker.RetentionLimitExceeded,
            [ready with { EvidenceMetadata = new Dictionary<string, string> { ["retention"] = "keep forever with compliance custody" } }] = ProductLedgerPathLocalOnlyBlocker.RetentionLimitExceeded,
            [ready with { RequestsRuntimeEnablement = true }] = ProductLedgerPathLocalOnlyBlocker.RuntimeEnablementRequested,
            [ready with { RequestsProductServiceRegistration = true }] = ProductLedgerPathLocalOnlyBlocker.ProductServiceRegistrationRequested,
            [ready with { RequestsProductCommandHandler = true }] = ProductLedgerPathLocalOnlyBlocker.ProductCommandHandlerRequested,
            [ready with { RequestsUiProductAction = true }] = ProductLedgerPathLocalOnlyBlocker.UiProductActionRequested,
            [ready with { ClaimsProviderCloudNetwork = true }] = ProductLedgerPathLocalOnlyBlocker.ProviderCloudNetworkClaimed,
            [ready with { ClaimsWormKmsExternalTrust = true }] = ProductLedgerPathLocalOnlyBlocker.WormKmsExternalTrustClaimed,
            [ready with { ClaimsDbMigration = true }] = ProductLedgerPathLocalOnlyBlocker.DbMigrationClaimed,
            [ready with { ClaimsBrowserCdpWcuOcrRecipesLive = true }] = ProductLedgerPathLocalOnlyBlocker.BrowserCdpWcuOcrRecipesLiveClaimed,
            [ready with { ClaimsReleaseCommercialReadiness = true }] = ProductLedgerPathLocalOnlyBlocker.ReleaseCommercialReadinessClaimed
        };

        foreach (var testCase in cases)
        {
            var result = writer.Append(testCase.Key);

            Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.Blocked, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoRuntimeOrExternal(result);
        }
    }

    [TestMethod]
    public void LocalOnlyWriter_RedactsSensitiveMetadataBeforePersistence()
    {
        using var fixture = LedgerFixture.Create();
        var writer = new ProductLedgerPathLocalOnlyActiveWriter();
        var activation = writer.Activate(ReadyActivationRequest(fixture));
        const string bearerValue = "Bearer SYNTHETIC-fixture-token-not-real";
        const string emailValue = "operator@example.test";
        const string highEntropyValue = "AbCDefGhIjKLmnopQRStuvWXyz1234567890+/=";
        var append = writer.Append(
            ReadyAppendRequest(activation) with
            {
                EvidenceMetadata = new Dictionary<string, string>
                {
                    ["authority"] = "local-only-policy-bound",
                    ["api_key"] = "api_key=SYNTHETIC-not-real",
                    ["authorization"] = bearerValue,
                    ["operator.email"] = emailValue,
                    ["client"] = "client_secret_redacted",
                    ["fingerprint"] = highEntropyValue
                }
            });

        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly, append.Decision);
        var serializedLedger = File.ReadAllText(append.ActiveLedgerFilePath!);
        Assert.IsFalse(serializedLedger.Contains("api_key", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serializedLedger.Contains(bearerValue, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serializedLedger.Contains(emailValue, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serializedLedger.Contains(highEntropyValue, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serializedLedger.Contains("client_secret", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(append.Entry!.EvidenceMetadata.ContainsKey("redaction.applied"));
        Assert.AreEqual("true", append.Entry.EvidenceMetadata["redaction.applied"]);
        Assert.AreEqual("bounded-local", append.Entry.EvidenceMetadata["retention.mode"]);
        Assert.IsTrue(append.Entry.EvidenceMetadata.Values.Any(value => value == "redacted-sensitive"));

        var entries = writer.ReadVerified(activation);
        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(append.Entry.EntryHash, entries[0].EntryHash);
    }

    [TestMethod]
    public void LocalOnlyWriter_RedactionRetentionFailureDoesNotPersistOrCorruptLedger()
    {
        using var fixture = LedgerFixture.Create();
        var writer = new ProductLedgerPathLocalOnlyActiveWriter();
        var activation = writer.Activate(ReadyActivationRequest(fixture));
        var first = writer.Append(ReadyAppendRequest(activation));
        var blocked = writer.Append(
            ReadyAppendRequest(activation) with
            {
                SafePayloadHash = new string('b', 64),
                EvidenceMetadata = new Dictionary<string, string> { ["raw.payload"] = "must-not-persist" }
            });

        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly, first.Decision);
        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.Blocked, blocked.Decision);
        CollectionAssert.Contains(blocked.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.UnsafeEvidenceMetadata);
        var serializedLedger = File.ReadAllText(first.ActiveLedgerFilePath!);
        Assert.IsFalse(serializedLedger.Contains("must-not-persist", StringComparison.OrdinalIgnoreCase));

        var entries = writer.ReadVerified(activation);
        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(first.Entry!.EntryHash, entries[0].EntryHash);
    }

    [TestMethod]
    public void LocalOnlyWriter_FailsClosedOnTailDeletionAndCheckpointLoss()
    {
        using var fixture = LedgerFixture.Create();
        var writer = new ProductLedgerPathLocalOnlyActiveWriter();
        var activation = writer.Activate(ReadyActivationRequest(fixture));
        var first = writer.Append(ReadyAppendRequest(activation));
        var second = writer.Append(ReadyAppendRequest(activation) with { SafePayloadHash = new string('b', 64) });
        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly, second.Decision);

        File.WriteAllText(second.ActiveLedgerFilePath!, File.ReadLines(second.ActiveLedgerFilePath!).First() + Environment.NewLine);
        var afterTailDeletion = writer.Append(ReadyAppendRequest(activation) with { SafePayloadHash = new string('c', 64) });
        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.Blocked, afterTailDeletion.Decision);
        CollectionAssert.Contains(afterTailDeletion.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.ExistingLedgerInvalid);

        using var checkpointFixture = LedgerFixture.Create();
        var checkpointActivation = writer.Activate(ReadyActivationRequest(checkpointFixture));
        var checkpointFirst = writer.Append(ReadyAppendRequest(checkpointActivation));
        File.Delete(checkpointFirst.ActiveCheckpointFilePath!);
        var afterCheckpointLoss = writer.Append(ReadyAppendRequest(checkpointActivation) with { SafePayloadHash = new string('d', 64) });
        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.Blocked, afterCheckpointLoss.Decision);
        CollectionAssert.Contains(afterCheckpointLoss.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.ExistingLedgerInvalid);

        AssertNoRuntimeOrExternal(first);
        AssertNoRuntimeOrExternal(afterTailDeletion);
        AssertNoRuntimeOrExternal(afterCheckpointLoss);
    }

    [TestMethod]
    public void LocalOnlyWriter_FailsClosedOnRehashedUnsafeExistingLedgerMetadata()
    {
        using var fixture = LedgerFixture.Create();
        var writer = new ProductLedgerPathLocalOnlyActiveWriter();
        var activation = writer.Activate(ReadyActivationRequest(fixture));
        var append = writer.Append(ReadyAppendRequest(activation));
        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly, append.Decision);

        var unsafeMetadata = new Dictionary<string, string> { ["token"] = "redacted" };
        var unsafeEntryHash = HashEntry(
            append.Entry!.Sequence,
            append.Entry.CandidateId,
            append.Entry.SafePayloadHash,
            unsafeMetadata,
            append.Entry.PreviousEntryHash);
        var unsafeEntry = append.Entry with
        {
            EvidenceMetadata = unsafeMetadata,
            EntryHash = unsafeEntryHash
        };
        File.WriteAllText(
            append.ActiveLedgerFilePath!,
            JsonSerializer.Serialize(unsafeEntry, JsonOptions) + Environment.NewLine,
            Encoding.UTF8);
        WriteCheckpoint(append.ActiveCheckpointFilePath!, unsafeEntry);

        try
        {
            writer.ReadVerified(activation);
            Assert.Fail("ReadVerified should fail closed on rehashed unsafe metadata.");
        }
        catch (InvalidDataException)
        {
        }

        var afterUnsafeRehash = writer.Append(ReadyAppendRequest(activation) with { SafePayloadHash = new string('f', 64) });
        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.Blocked, afterUnsafeRehash.Decision);
        CollectionAssert.Contains(afterUnsafeRehash.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.ExistingLedgerInvalid);
        AssertNoRuntimeOrExternal(afterUnsafeRehash);
    }

    [TestMethod]
    public async Task LocalOnlyWriter_ConcurrentAppendsSameLedgerProduceSequentialVerifiedChain()
    {
        using var fixture = LedgerFixture.Create();
        var writer = new ProductLedgerPathLocalOnlyActiveWriter();
        var activation = writer.Activate(ReadyActivationRequest(fixture));
        var appendCount = 24;

        var results = await Task.WhenAll(Enumerable.Range(0, appendCount).Select(index =>
            Task.Run(() => writer.Append(
                ReadyAppendRequest(activation) with
                {
                    SafePayloadHash = HashForIndex(index),
                    EvidenceMetadata = new Dictionary<string, string>
                    {
                        ["authority"] = "local-only-policy-bound",
                        ["redaction"] = "redacted-before-persistence",
                        ["failure"] = $"replay-rollback-evidence-{index:00}"
                    }
                }))));

        Assert.IsTrue(results.All(result => result.Decision == ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly));
        var entries = writer.ReadVerified(activation);
        Assert.AreEqual(appendCount, entries.Count);
        CollectionAssert.AreEqual(Enumerable.Range(1, appendCount).ToArray(), entries.Select(entry => entry.Sequence).ToArray());
        Assert.AreEqual(1, entries.Count(entry => entry.PreviousEntryHash == "GENESIS"));
        Assert.AreEqual(entries.Count, entries.Select(entry => entry.EntryHash).Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(entries.Count - 1, entries.Skip(1).Select(entry => entry.PreviousEntryHash).Distinct(StringComparer.Ordinal).Count());

        for (var i = 1; i < entries.Count; i++)
        {
            Assert.AreEqual(entries[i - 1].EntryHash, entries[i].PreviousEntryHash);
        }

        Assert.IsTrue(File.Exists(activation.ActiveCheckpointFilePath));
        AssertNoRuntimeOrExternal(results.Last());
    }

    [TestMethod]
    public async Task LocalOnlyWriter_ConcurrentBlockedAppendsDoNotCorruptSuccessfulChain()
    {
        using var fixture = LedgerFixture.Create();
        var writer = new ProductLedgerPathLocalOnlyActiveWriter();
        var activation = writer.Activate(ReadyActivationRequest(fixture));

        var results = await Task.WhenAll(Enumerable.Range(0, 18).Select(index =>
            Task.Run(() =>
            {
                var request = ReadyAppendRequest(activation) with
                {
                    SafePayloadHash = HashForIndex(index),
                    EvidenceMetadata = new Dictionary<string, string>
                    {
                        ["authority"] = "local-only-policy-bound",
                        ["redaction"] = "redacted-before-persistence",
                        ["failure"] = $"replay-rollback-evidence-{index:00}"
                    }
                };
                return index % 3 == 0
                    ? writer.Append(request with { EvidenceMetadata = new Dictionary<string, string> { ["raw.payload"] = $"must-not-persist-{index:00}" } })
                    : writer.Append(request);
            })));

        Assert.AreEqual(6, results.Count(result => result.Decision == ProductLedgerPathLocalOnlyWriterDecision.Blocked));
        Assert.AreEqual(12, results.Count(result => result.Decision == ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly));
        var entries = writer.ReadVerified(activation);
        Assert.AreEqual(12, entries.Count);
        CollectionAssert.AreEqual(Enumerable.Range(1, 12).ToArray(), entries.Select(entry => entry.Sequence).ToArray());
        for (var i = 1; i < entries.Count; i++)
        {
            Assert.AreEqual(entries[i - 1].EntryHash, entries[i].PreviousEntryHash);
        }
    }

    [TestMethod]
    public void LocalOnlyActiveWriter_SourceHasNoRuntimeRegistrationDbCloudKmsOrUiActions()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerPathLocalOnlyActiveWriter.cs"));
        var forbiddenFragments = new[]
        {
            "IService" + "Collection",
            "Add" + "Singleton",
            "Add" + "Scoped",
            "Add" + "Transient",
            "IHosted" + "Service",
            "ICommand" + "Handler",
            "Handle" + "Async(",
            "Control" + "ler",
            "Db" + "Context",
            "Migration" + "Builder",
            "Http" + "Client",
            "Web" + "Socket",
            "Kms" + "Client",
            "Worm" + "Store",
            "ProductRuntimeEnabled:" + " true",
            "ProductServiceRegistrationAllowed:" + " true",
            "ProductCommandHandlersAllowed:" + " true",
            "UiProductActionsAllowed:" + " true",
            "DbProviderCloudNetworkAllowed:" + " true",
            "KmsWormExternalTrustAllowed:" + " true",
            "LiveAutomationAllowed:" + " true",
            "ReleaseCommercialReady:" + " true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }

        StringAssert.Contains(source, "ProductLedgerPathActive: active");
        StringAssert.Contains(source, "ProductLedgerWriteAllowed: active");
        StringAssert.Contains(source, "ProductRuntimeEnabled: false");
        StringAssert.Contains(source, "IsUnderLocalTemp");
        StringAssert.Contains(source, "FailedPersistedCandidateResult");
        StringAssert.Contains(source, "File.AppendAllText");
        StringAssert.Contains(source, "File.WriteAllText");
        StringAssert.Contains(source, "ConcurrentDictionary<string, object>");
        StringAssert.Contains(source, "GetLedgerLock");
        StringAssert.Contains(source, "ProductLedgerPathLocalOnlyMetadataGuard");
    }

    private static ProductLedgerPathLocalOnlyActivationRequest ReadyActivationRequest(LedgerFixture fixture) =>
        new(
            PersistedCandidateResult: ReadyPersistedCandidate(fixture),
            ExplicitLocalOnlyActivationMode: true,
            HasAuthorityEvidence: true,
            HasRedactionBeforePersistenceEvidence: true,
            HasFailureReplayRollbackEvidence: true,
            HasRetentionEvidence: true,
            LocalRuntimeFlagDefaultOff: true,
            RequestsRuntimeEnablement: false,
            RequestsProductServiceRegistration: false,
            RequestsProductCommandHandler: false,
            RequestsUiProductAction: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsWormKmsExternalTrust: false,
            ClaimsDbMigration: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercialReadiness: false,
            ClaimsLocalTempAsProductLedgerPath: false);

    private static ProductLedgerPathLocalOnlyAppendRequest ReadyAppendRequest(ProductLedgerPathLocalOnlyActivationResult activation) =>
        new(
            ActivationResult: activation,
            SafePayloadHash: new string('a', 64),
            EvidenceMetadata: new Dictionary<string, string>
            {
                ["authority"] = "local-only-policy-bound",
                ["redaction"] = "redacted-before-persistence",
                ["failure"] = "replay-rollback-evidence"
            },
            RuntimeFlagStillDefaultOff: true,
            RequestsRuntimeEnablement: false,
            RequestsProductServiceRegistration: false,
            RequestsProductCommandHandler: false,
            RequestsUiProductAction: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsWormKmsExternalTrust: false,
            ClaimsDbMigration: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercialReadiness: false);

    private static ProductLedgerPathPersistedCandidateResult ReadyPersistedCandidate(LedgerFixture fixture)
    {
        var canonicalization = new ProductLedgerPathCanonicalizationValidator().Validate(
            new ProductLedgerPathCanonicalizationRequest(
                CandidatePath: fixture.LedgerRoot,
                AllowedRootPath: fixture.AllowedRoot,
                ExplicitLocalOnlyMode: true,
                NoProductLedgerWriteAssertion: true,
                NoRuntimeEnablementAssertion: true,
                NoReleaseCommercialAssertion: true,
                ClaimsProductLedgerActive: false,
                ClaimsProductReady: false,
                ClaimsExternalTrust: false,
                ClaimsWormKmsCloud: false,
                ClaimsLocalTempAsProductLedgerPath: false,
                HasResolvedReparsePointEvidence: true,
                HasTocTouMitigationEvidence: true,
                HardlinkOrMountAliasRiskUnresolved: false));
        var policy = new ProductLedgerPathActivePolicy().Evaluate(
            new ProductLedgerPathActivePolicyRequest(
                CanonicalizationResult: canonicalization,
                HasCanonicalAllowedBoundaryEvidence: true,
                HasNoUnresolvedReparseSymlinkJunctionRiskEvidence: true,
                HasTocTouMitigationEvidence: true,
                HasRedactionPolicyEvidence: true,
                HasRetentionPolicyEvidence: true,
                HasReplayFailureEvidence: true,
                HasRollbackNonRollbackClassification: true,
                HasAuthorityEvidence: true,
                AuthorityEvidenceIsNonProduct: true,
                TreatsHumanGoAsProductAuthority: false,
                EvidenceReferences: ["docs/qa/product-ledger-path-active-policy-local-only-no-write/report.md"],
                EvidenceReferencesAreStale: false,
                EvidenceReferencesAreInconsistent: false,
                ClaimsLocalTempAsProductLedgerPath: false,
                NoProductWriteAssertion: true,
                NoRuntimeEnablementAssertion: true,
                NoReleaseCommercialAssertion: true,
                NoProviderCloudNetworkAssertion: true,
                NoWormKmsExternalTrustAssertion: true,
                RequestsProductLedgerPathActivation: false,
                RequestsWriterActivation: false,
                RequestsRuntimeEnablement: false,
                RequestsProductServiceRegistration: false,
                RequestsProductCommandHandler: false,
                RequestsUiProductAction: false,
                ClaimsReleaseCommercialReadiness: false,
                ClaimsProviderCloudNetwork: false,
                ClaimsWormKmsExternalTrust: false));
        return new ProductLedgerPathPersistedCandidateRegistry().Persist(
            new ProductLedgerPathPersistedCandidateRequest(
                CandidateId: "ledger-candidate-001",
                ActivePolicyResult: policy,
                CanonicalizationResult: canonicalization,
                EvidenceReferences: ["docs/qa/product-ledger-path-persisted-candidate-local-only-no-write/report.md"],
                ClaimsLocalTempAsProductLedgerPath: false,
                RequestsProductLedgerPathActivation: false,
                RequestsWriterActivation: false,
                RequestsRuntimeEnablement: false,
                RequestsProductServiceRegistration: false,
                RequestsProductCommandHandler: false,
                RequestsUiProductAction: false,
                ClaimsProviderCloudNetwork: false,
                ClaimsWormKmsExternalTrust: false,
                ClaimsReleaseCommercialReadiness: false));
    }

    private static void AssertNoRuntimeOrExternal(ProductLedgerPathLocalOnlyActivationResult result)
    {
        Assert.IsFalse(result.ProductRuntimeEnabled);
        Assert.IsFalse(result.ProductServiceRegistrationAllowed);
        Assert.IsFalse(result.ProductCommandHandlersAllowed);
        Assert.IsFalse(result.UiProductActionsAllowed);
        Assert.IsFalse(result.DbProviderCloudNetworkAllowed);
        Assert.IsFalse(result.KmsWormExternalTrustAllowed);
        Assert.IsFalse(result.LiveAutomationAllowed);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }

    private static void AssertNoRuntimeOrExternal(ProductLedgerPathLocalOnlyAppendResult result)
    {
        Assert.IsFalse(result.ProductRuntimeEnabled);
        Assert.IsFalse(result.ProductServiceRegistrationAllowed);
        Assert.IsFalse(result.ProductCommandHandlersAllowed);
        Assert.IsFalse(result.UiProductActionsAllowed);
        Assert.IsFalse(result.DbProviderCloudNetworkAllowed);
        Assert.IsFalse(result.KmsWormExternalTrustAllowed);
        Assert.IsFalse(result.LiveAutomationAllowed);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                directory.FullName,
                "src",
                "OneBrain.Core",
                "Approval",
                "ProductLedgerPathLocalOnlyActiveWriter.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static void WriteCheckpoint(string checkpointPath, ProductLedgerPathLocalOnlyEntry entry)
    {
        var checkpoint = new ProductLedgerPathLocalOnlyCheckpoint(
            HeadSequence: 1,
            HeadEntryHash: entry.EntryHash,
            LedgerHash: HashLedger(entry),
            StatusText: ProductLedgerPathLocalOnlyActiveWriter.ActiveLocalOnlyStatus);
        File.WriteAllText(checkpointPath, JsonSerializer.Serialize(checkpoint, JsonOptions), Encoding.UTF8);
    }

    private static string HashLedger(ProductLedgerPathLocalOnlyEntry entry)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"1:{entry.EntryHash}"));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string HashEntry(
        int sequence,
        string candidateId,
        string safePayloadHash,
        IReadOnlyDictionary<string, string> metadata,
        string previousHash)
    {
        var metadataText = string.Join(
            "\n",
            metadata.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}={pair.Value}"));
        var material = $"{sequence}\n{candidateId}\n{safePayloadHash.ToLowerInvariant()}\n{metadataText}\n{previousHash}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string HashForIndex(int index)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"safe-payload-{index}"));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed class LedgerFixture : IDisposable
    {
        private LedgerFixture(string allowedRoot, string ledgerRoot)
        {
            AllowedRoot = allowedRoot;
            LedgerRoot = ledgerRoot;
        }

        public string AllowedRoot { get; }

        public string LedgerRoot { get; }

        public static LedgerFixture Create()
        {
            var allowedRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-local-only-tests");
            var ledgerRoot = Path.Combine(allowedRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ledgerRoot);
            return new LedgerFixture(allowedRoot, ledgerRoot);
        }

        public void Dispose()
        {
            if (AllowedRoot.StartsWith(RepoRoot(), StringComparison.OrdinalIgnoreCase) && Directory.Exists(AllowedRoot))
            {
                Directory.Delete(AllowedRoot, recursive: true);
            }
        }
    }
}
