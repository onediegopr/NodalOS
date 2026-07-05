using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
[TestCategory("ProductLedgerIntegrationPropertyPack")]
public sealed class ProductLedgerIntegrationPropertyTestPackTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [TestMethod]
    public void IntegrationPack_AppendsVerifiesExportsAndLinksLocalEvidence()
    {
        using var fixture = LedgerFixture.Create();
        var writer = new ProductLedgerPathLocalOnlyActiveWriter();
        var activation = writer.Activate(ReadyActivationRequest(fixture));

        var first = writer.Append(ReadyAppendRequest(activation, HashFor("entry-001")));
        var second = writer.Append(
            ReadyAppendRequest(activation, HashFor("entry-002")) with
            {
                EvidenceMetadata = new Dictionary<string, string>
                {
                    ["authority"] = "local-only-policy-bound",
                    ["operator.email"] = "operator@example.test",
                    ["authorization"] = "Bearer SYNTHETIC-fixture-token-not-real",
                    ["fingerprint"] = "AbCDefGhIjKLmnopQRStuvWXyz1234567890+/="
                }
            });
        var third = writer.Append(
            ReadyAppendRequest(activation, HashFor("entry-003")) with
            {
                EvidenceMetadata = new Dictionary<string, string>
                {
                    ["authority"] = "local-only-policy-bound",
                    ["redaction"] = "redacted-before-persistence",
                    ["failure"] = "replay-rollback-evidence",
                    ["note"] = "local-caf\u00e9-fixture",
                    ["format"] = "```local```"
                }
            });

        AssertAppended(first);
        AssertAppended(second);
        AssertAppended(third);
        AssertNoRuntimeOrExternal(first);
        AssertNoRuntimeOrExternal(second);
        AssertNoRuntimeOrExternal(third);

        var entries = writer.ReadVerified(activation);
        Assert.AreEqual(3, entries.Count);
        Assert.AreEqual(1, entries.Count(entry => entry.PreviousEntryHash == "GENESIS"));
        Assert.AreEqual(entries[0].EntryHash, entries[1].PreviousEntryHash);
        Assert.AreEqual(entries[1].EntryHash, entries[2].PreviousEntryHash);
        Assert.AreEqual("true", entries[1].EvidenceMetadata["redaction.applied"]);
        Assert.IsTrue(entries[1].EvidenceMetadata.Values.Any(value => value == "redacted-sensitive"));

        var checkpoint = ReadCheckpoint(activation.ActiveCheckpointFilePath!);
        Assert.AreEqual(3, checkpoint.HeadSequence);
        Assert.AreEqual(entries[^1].EntryHash, checkpoint.HeadEntryHash);
        Assert.AreEqual(HashLedger(entries), checkpoint.LedgerHash);

        var acceptance = new ProductLedgerOperatorAcceptanceLocalOnlyMatrix().Build(
            ProductLedgerOperatorAcceptanceLocalOnlyMatrix.ReadyRequest());
        Assert.AreEqual(ProductLedgerOperatorAcceptanceMatrixDecision.ReadyLocalOnly, acceptance.Decision);
        Assert.IsTrue(acceptance.Rows.Any(row => row.EvidenceRefs.Contains("bounded-local-report-export")));
        Assert.IsFalse(acceptance.ReleaseCommercialReady);

        var visualEvidence = new ProductLedgerLocalDevVisualQaEvidence().Evaluate(
            ProductLedgerLocalDevVisualQaEvidence.CreateDefaultFixtureRequest());
        Assert.AreEqual(ProductLedgerLocalDevVisualQaEvidenceDecision.EvidenceReady, visualEvidence.Decision);
        Assert.IsFalse(visualEvidence.RealScreenshotCaptured);
        Assert.IsFalse(visualEvidence.BrowserCdpProductiveUsed);

        var reportContent = "{" +
            "\"kind\":\"ledger-diagnostic\"," +
            "\"candidateId\":\"ledger-candidate-001\"," +
            $"\"headSequence\":{checkpoint.HeadSequence}," +
            $"\"headEntryHash\":\"{checkpoint.HeadEntryHash}\"," +
            $"\"ledgerHash\":\"{checkpoint.LedgerHash}\"," +
            "\"scope\":\"internal local bounded\"," +
            "\"evidence\":[\"operator matrix\",\"static fixture screenshot\",\"post write verification\"]" +
            "}";
        var export = new ProductLedgerLocalReportExportService().Export(ReadyExportRequest(fixture, reportContent));

        Assert.AreEqual(ProductLedgerLocalReportExportDecision.ExportedBoundedLocal, export.Decision);
        Assert.IsTrue(export.PostWriteHashVerified);
        Assert.IsTrue(File.Exists(export.Evidence!.CanonicalReportFilePath));
        StringAssert.Contains(File.ReadAllText(export.Evidence.CanonicalReportFilePath), checkpoint.HeadEntryHash);
        AssertGeneratedArtifactsDoNotContain(fixture.AllowedRoot, SensitiveCorpus());
    }

    [TestMethod]
    public void PropertyPack_MetadataCorpusRedactsRejectsBoundsAndRemainsDeterministic()
    {
        using var fixture = LedgerFixture.Create();
        var writer = new ProductLedgerPathLocalOnlyActiveWriter();
        var activation = writer.Activate(ReadyActivationRequest(fixture));

        var accepted = new[]
        {
            new Dictionary<string, string> { ["note"] = "local-caf\u00e9-fixture" },
            new Dictionary<string, string> { ["format"] = "```local```" },
            new Dictionary<string, string> { ["json"] = "{\"kind\":\"local\",\"safe\":true}" },
            new Dictionary<string, string> { ["authorization"] = "Bearer SYNTHETIC-fixture-token-not-real" },
            new Dictionary<string, string> { ["api_key"] = "api_key=SYNTHETIC-not-real" },
            new Dictionary<string, string> { ["connection"] = "Server=local;User Id=fixture;Password=SYNTHETIC-not-real;" },
            new Dictionary<string, string> { ["fingerprint"] = "AbCDefGhIjKLmnopQRStuvWXyz1234567890+/=" },
            new Dictionary<string, string> { ["operator.email"] = "operator@example.test" }
        };

        foreach (var metadata in accepted)
        {
            var firstGuard = ProductLedgerPathLocalOnlyMetadataGuard.Evaluate(metadata);
            var secondGuard = ProductLedgerPathLocalOnlyMetadataGuard.Evaluate(metadata);
            Assert.IsTrue(firstGuard.Allowed, string.Join(",", metadata.Keys));
            Assert.IsTrue(secondGuard.Allowed, string.Join(",", metadata.Keys));
            CollectionAssert.AreEqual(
                firstGuard.SafeMetadata.ToArray(),
                secondGuard.SafeMetadata.ToArray());
            Assert.IsTrue(firstGuard.SafeMetadata.Count <= ProductLedgerPathLocalOnlyMetadataGuard.MaxPersistedMetadataFields);
            Assert.IsTrue(firstGuard.SafeMetadata.Values.All(value => value.Length <= ProductLedgerPathLocalOnlyMetadataGuard.MaxMetadataValueLength));
            AssertNoSensitiveValues(firstGuard.SafeMetadata, metadata.Values);

            var append = writer.Append(
                ReadyAppendRequest(activation, HashFor(string.Join("|", metadata.Select(pair => pair.Key + pair.Value)))) with
                {
                    EvidenceMetadata = metadata
                });
            AssertAppended(append);
        }

        var rejected = new Dictionary<IReadOnlyDictionary<string, string>, ProductLedgerPathLocalOnlyBlocker>
        {
            [new Dictionary<string, string> { [""] = "safe" }] = ProductLedgerPathLocalOnlyBlocker.UnsafeEvidenceMetadata,
            [new Dictionary<string, string> { ["Ref"] = "one", ["ref"] = "two" }] = ProductLedgerPathLocalOnlyBlocker.UnsafeEvidenceMetadata,
            [new Dictionary<string, string> { ["path"] = @"C:\Users\fixture\ledger.txt" }] = ProductLedgerPathLocalOnlyBlocker.UnsafeEvidenceMetadata,
            [new Dictionary<string, string> { ["unc"] = @"\\server\share\ledger.txt" }] = ProductLedgerPathLocalOnlyBlocker.UnsafeEvidenceMetadata,
            [new Dictionary<string, string> { ["url"] = "https://local.invalid/ledger" }] = ProductLedgerPathLocalOnlyBlocker.UnsafeEvidenceMetadata,
            [new Dictionary<string, string> { ["raw.value"] = "redacted" }] = ProductLedgerPathLocalOnlyBlocker.UnsafeEvidenceMetadata,
            [new Dictionary<string, string> { ["summary"] = "raw payload should not persist" }] = ProductLedgerPathLocalOnlyBlocker.UnsafeEvidenceMetadata,
            [new Dictionary<string, string> { ["control"] = "safe\u0001value" }] = ProductLedgerPathLocalOnlyBlocker.UnsafeEvidenceMetadata,
            [new Dictionary<string, string> { ["huge"] = new string('x', 129) }] = ProductLedgerPathLocalOnlyBlocker.RetentionLimitExceeded,
            [Enumerable.Range(0, 13).ToDictionary(index => $"field{index:00}", _ => "safe")] = ProductLedgerPathLocalOnlyBlocker.RetentionLimitExceeded,
            [new Dictionary<string, string> { ["claim"] = "WORM KMS external trust compliance custody" }] = ProductLedgerPathLocalOnlyBlocker.RetentionLimitExceeded
        };

        var checkpointBeforeRejected = File.ReadAllText(activation.ActiveCheckpointFilePath!);
        foreach (var testCase in rejected)
        {
            var guard = ProductLedgerPathLocalOnlyMetadataGuard.Evaluate(testCase.Key);
            Assert.IsFalse(guard.Allowed);
            CollectionAssert.Contains(guard.Blockers.ToArray(), testCase.Value);

            var append = writer.Append(
                ReadyAppendRequest(activation, HashFor("rejected-" + string.Join("|", testCase.Key.Select(pair => pair.Key + pair.Value)))) with
                {
                    EvidenceMetadata = testCase.Key
                });
            Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.Blocked, append.Decision);
            CollectionAssert.Contains(append.Blockers.ToArray(), testCase.Value);
            Assert.IsFalse(string.Join(",", append.Blockers).Contains("SYNTHETIC-not-real", StringComparison.OrdinalIgnoreCase));
        }

        Assert.AreEqual(checkpointBeforeRejected, File.ReadAllText(activation.ActiveCheckpointFilePath!));
        writer.ReadVerified(activation);
        AssertGeneratedArtifactsDoNotContain(fixture.AllowedRoot, SensitiveCorpus());
    }

    [TestMethod]
    public void TamperPack_DetectsLedgerCheckpointSequenceHashMetadataAndMalformedLines()
    {
        AssertInvalidAfterTamper((ledgerPath, _, entries) =>
        {
            var entry = entries[0] with { SafePayloadHash = new string('f', 64) };
            File.WriteAllText(ledgerPath, JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine, Encoding.UTF8);
        });
        AssertInvalidAfterTamper((_, checkpointPath, entries) =>
            WriteCheckpoint(checkpointPath, new ProductLedgerPathLocalOnlyCheckpoint(99, entries[^1].EntryHash, HashLedger(entries), ProductLedgerPathLocalOnlyActiveWriter.ActiveLocalOnlyStatus)));
        AssertInvalidAfterTamper((ledgerPath, _, _) =>
            File.WriteAllText(ledgerPath, File.ReadLines(ledgerPath).First() + Environment.NewLine, Encoding.UTF8));
        AssertInvalidAfterTamper((ledgerPath, _, entries) =>
        {
            var duplicate = entries[1] with { Sequence = 1 };
            File.WriteAllText(ledgerPath, string.Join(Environment.NewLine, [JsonSerializer.Serialize(entries[0], JsonOptions), JsonSerializer.Serialize(duplicate, JsonOptions)]) + Environment.NewLine, Encoding.UTF8);
        });
        AssertInvalidAfterTamper((ledgerPath, _, entries) =>
        {
            var mismatch = entries[1] with { PreviousEntryHash = new string('0', 64) };
            File.WriteAllText(ledgerPath, string.Join(Environment.NewLine, [JsonSerializer.Serialize(entries[0], JsonOptions), JsonSerializer.Serialize(mismatch, JsonOptions)]) + Environment.NewLine, Encoding.UTF8);
        });
        AssertInvalidAfterTamper((ledgerPath, _, entries) =>
        {
            var tampered = entries[0] with { EvidenceMetadata = new Dictionary<string, string>(entries[0].EvidenceMetadata) { ["redaction.applied"] = "true" } };
            File.WriteAllText(ledgerPath, JsonSerializer.Serialize(tampered, JsonOptions) + Environment.NewLine, Encoding.UTF8);
        });
        AssertInvalidAfterTamper((ledgerPath, _, entries) =>
        {
            var tampered = entries[0] with { EvidenceMetadata = new Dictionary<string, string>(entries[0].EvidenceMetadata) { ["redaction.field01"] = "redacted-value" } };
            File.WriteAllText(ledgerPath, JsonSerializer.Serialize(tampered, JsonOptions) + Environment.NewLine, Encoding.UTF8);
        }, includeSensitiveEntry: true);
        AssertInvalidAfterTamper((_, checkpointPath, entries) =>
            WriteCheckpoint(checkpointPath, new ProductLedgerPathLocalOnlyCheckpoint(entries.Count - 1, entries[^2].EntryHash, HashLedger(entries), ProductLedgerPathLocalOnlyActiveWriter.ActiveLocalOnlyStatus)));
        AssertInvalidAfterTamper((ledgerPath, _, _) =>
            File.AppendAllText(ledgerPath, "{partial", Encoding.UTF8));
        AssertInvalidAfterTamper((ledgerPath, _, _) =>
            File.WriteAllText(ledgerPath, "{not-json" + Environment.NewLine, Encoding.UTF8));
    }

    [TestMethod]
    public void StaticWriteSurface_ProductLedgerCoreApprovalFilesRemainAllowlistedAndLocalOnly()
    {
        var approvalRoot = Path.Combine(RepoRoot(), "src", "OneBrain.Core", "Approval");
        var allowlistedWriteFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ApprovalArtifactWriter.cs",
            "DurableAuditTrailAppendOnlyMinimal.cs",
            "ProductLedgerLocalReportExportService.cs",
            "ProductLedgerPathLocalOnlyActiveWriter.cs",
            "ProductLedgerPathLocalTempWriterTestOnly.cs"
        };
        var writeApiFragments = new[]
        {
            "File.WriteAllText",
            "File.WriteAllBytes",
            "File.AppendAllText",
            "File.AppendAllLines",
            "File.Create"
        };

        foreach (var file in Directory.EnumerateFiles(approvalRoot, "*.cs"))
        {
            var source = File.ReadAllText(file);
            if (writeApiFragments.Any(fragment => source.Contains(fragment, StringComparison.Ordinal)))
            {
                CollectionAssert.Contains(allowlistedWriteFiles.ToArray(), Path.GetFileName(file), file);
            }
        }

        foreach (var file in Directory.EnumerateFiles(approvalRoot, "ProductLedger*.cs"))
        {
            var source = File.ReadAllText(file);
            foreach (var fragment in new[]
            {
                "File.Delete",
                "Directory.Delete",
                "Process.Start",
                "HttpClient",
                "WebSocket",
                "DbContext",
                "MigrationBuilder",
                "KmsClient",
                "WormStore"
            })
            {
                Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), $"{fragment} in {Path.GetFileName(file)}");
            }
        }
    }

    private static void AssertInvalidAfterTamper(
        Action<string, string, IReadOnlyList<ProductLedgerPathLocalOnlyEntry>> tamper,
        bool includeSensitiveEntry = false)
    {
        using var fixture = LedgerFixture.Create();
        var writer = new ProductLedgerPathLocalOnlyActiveWriter();
        var activation = writer.Activate(ReadyActivationRequest(fixture));
        var firstRequest = includeSensitiveEntry
            ? ReadyAppendRequest(activation, HashFor("tamper-sensitive")) with
            {
                EvidenceMetadata = new Dictionary<string, string>
                {
                    ["authority"] = "local-only-policy-bound",
                    ["authorization"] = "Bearer SYNTHETIC-fixture-token-not-real"
                }
            }
            : ReadyAppendRequest(activation, HashFor("tamper-001"));
        AssertAppended(writer.Append(firstRequest));
        AssertAppended(writer.Append(ReadyAppendRequest(activation, HashFor("tamper-002"))));

        var entries = writer.ReadVerified(activation);
        tamper(activation.ActiveLedgerFilePath!, activation.ActiveCheckpointFilePath!, entries);

        try
        {
            writer.ReadVerified(activation);
            Assert.Fail("Tampered local-only ledger should fail closed.");
        }
        catch (InvalidDataException)
        {
        }

        var appendAfterTamper = writer.Append(ReadyAppendRequest(activation, HashFor("after-tamper")));
        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.Blocked, appendAfterTamper.Decision);
        CollectionAssert.Contains(appendAfterTamper.Blockers.ToArray(), ProductLedgerPathLocalOnlyBlocker.ExistingLedgerInvalid);
        AssertNoRuntimeOrExternal(appendAfterTamper);
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

    private static ProductLedgerPathLocalOnlyAppendRequest ReadyAppendRequest(
        ProductLedgerPathLocalOnlyActivationResult activation,
        string safePayloadHash) =>
        new(
            ActivationResult: activation,
            SafePayloadHash: safePayloadHash,
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

    private static ProductLedgerLocalReportExportRequest ReadyExportRequest(LedgerFixture fixture, string reportContent) =>
        new(
            AllowedRootPath: fixture.AllowedRoot,
            ReportFilePath: Path.Combine(fixture.AllowedRoot, "reports", "product-ledger-integration-report.json"),
            ReportContent: reportContent,
            EvidenceMetadata: new Dictionary<string, string>
            {
                ["operator"] = "internal-local-only",
                ["redaction"] = "redacted-before-export",
                ["boundary"] = "canonicalized",
                ["checkpoint"] = "verified"
            },
            ExplicitInternalLocalOnlyBoundedExportScope: true,
            HasOperatorInternalEvidence: true,
            HasRedactionBeforePersistenceEvidence: true,
            HasSafeContentEvidence: true,
            HasResolvedReparsePointEvidence: true,
            HasTocTouMitigationEvidence: true,
            HardlinkOrMountAliasRiskUnresolved: false,
            AllowOverwriteExisting: false,
            RequestsPublicUiAction: false,
            RequestsDestructiveAction: false,
            RequestsProductCommandHandler: false,
            RequestsProductiveServiceRegistration: false,
            ClaimsProviderCloudNetwork: false,
            ClaimsDbMigration: false,
            ClaimsKmsWormExternalTrust: false,
            ClaimsBrowserCdpWcuOcrRecipesLive: false,
            ClaimsReleaseCommercial: false,
            ClaimsExternalTelemetryOrSync: false,
            ClaimsBillingLicensingCloud: false,
            ClaimsExternalExport: false,
            ClaimsUnboundedPhysicalExport: false);

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

    private static void AssertAppended(ProductLedgerPathLocalOnlyAppendResult result)
    {
        Assert.AreEqual(ProductLedgerPathLocalOnlyWriterDecision.AppendedLocalOnly, result.Decision);
        Assert.IsNotNull(result.Entry);
        Assert.AreEqual(0, result.Blockers.Count);
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

    private static void AssertNoSensitiveValues(
        IReadOnlyDictionary<string, string> persistedMetadata,
        IEnumerable<string> originalValues)
    {
        var serialized = JsonSerializer.Serialize(persistedMetadata, JsonOptions);
        foreach (var value in originalValues)
        {
            if (SensitiveCorpus().Any(secret => value.Contains(secret, StringComparison.OrdinalIgnoreCase))
                || value.Contains("Bearer ", StringComparison.OrdinalIgnoreCase)
                || value.Contains('@', StringComparison.Ordinal))
            {
                Assert.IsFalse(serialized.Contains(value, StringComparison.OrdinalIgnoreCase), value);
            }
        }
    }

    private static void AssertGeneratedArtifactsDoNotContain(string root, IEnumerable<string> forbiddenValues)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file, Encoding.UTF8);
            foreach (var forbidden in forbiddenValues)
            {
                Assert.IsFalse(text.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"{Path.GetFileName(file)} leaked {forbidden}");
            }
        }
    }

    private static IReadOnlyList<string> SensitiveCorpus() =>
    [
        "SYNTHETIC-fixture-token-not-real",
        "SYNTHETIC-not-real",
        "operator@example.test",
        "AbCDefGhIjKLmnopQRStuvWXyz1234567890+/="
    ];

    private static ProductLedgerPathLocalOnlyCheckpoint ReadCheckpoint(string checkpointPath) =>
        JsonSerializer.Deserialize<ProductLedgerPathLocalOnlyCheckpoint>(
            File.ReadAllText(checkpointPath, Encoding.UTF8),
            JsonOptions) ?? throw new InvalidDataException("checkpoint missing");

    private static void WriteCheckpoint(string checkpointPath, ProductLedgerPathLocalOnlyCheckpoint checkpoint) =>
        File.WriteAllText(checkpointPath, JsonSerializer.Serialize(checkpoint, JsonOptions), Encoding.UTF8);

    private static string HashLedger(IReadOnlyList<ProductLedgerPathLocalOnlyEntry> entries)
    {
        var material = string.Join("\n", entries.Select(entry => $"{entry.Sequence}:{entry.EntryHash}"));
        return Sha256(material);
    }

    private static string HashFor(string value) => Sha256("safe-payload:" + value);

    private static string Sha256(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
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
            var allowedRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-integration-property-tests", Guid.NewGuid().ToString("N"));
            var ledgerRoot = Path.Combine(allowedRoot, "active-ledger");
            Directory.CreateDirectory(ledgerRoot);
            return new LedgerFixture(allowedRoot, ledgerRoot);
        }

        public void Dispose()
        {
            var baseRoot = Path.Combine(RepoRoot(), ".tmp-product-ledger-integration-property-tests");
            if (AllowedRoot.StartsWith(baseRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(AllowedRoot))
            {
                Directory.Delete(AllowedRoot, recursive: true);
            }
        }
    }
}
