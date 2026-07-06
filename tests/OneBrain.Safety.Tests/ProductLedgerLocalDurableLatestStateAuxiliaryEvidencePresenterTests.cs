using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenterTests
{
    [TestMethod]
    public void AuxiliaryEvidence_ValidReaderCandidateProducesNotAuthorityEvidence()
    {
        var presenter = new ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter(SafeOptions());
        var result = presenter.Present(ValidRequest());

        Assert.AreEqual(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceDecision.PresentedAuxiliaryEvidenceNotAuthority, result.Decision);
        Assert.AreEqual(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceState.AuxiliaryEvidenceVisibleNotAuthority, result.State);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.IsTrue(result.ReadOnly);
        Assert.IsTrue(result.AuxiliaryEvidenceOnly);
        Assert.IsTrue(result.Validation.CandidateValidated);
        Assert.IsTrue(result.Validation.ManifestHashValid);
        Assert.IsTrue(result.Validation.SnapshotHashValid);
        Assert.IsTrue(result.Validation.SafeRelativePathsOnly);
        Assert.IsTrue(result.Validation.StaleAware);
        Assert.IsTrue(result.Validation.NotAuthority);
        Assert.IsFalse(result.Authority);
        Assert.IsFalse(result.LiveAuthority);
        Assert.IsFalse(result.ProductAuthority);
        Assert.IsFalse(result.ReadPrecedence);
        Assert.IsFalse(result.LatestPointer);
        Assert.IsFalse(result.LatestPointerOverwrite);
        Assert.IsFalse(result.ProductionAllowed);
        Assert.IsFalse(result.PublicProductAllowed);
        Assert.IsFalse(result.ShellAllowed);
        Assert.IsFalse(result.CommandExecutionAllowed);
        Assert.IsFalse(result.ProviderCloudNetworkAvailable);
        Assert.IsFalse(result.DbMigrationAvailable);
        Assert.IsFalse(result.KmsWormExternalTrustAvailable);
        Assert.IsFalse(result.BrowserCdpWcuOcrRecipesLiveAvailable);
        Assert.IsFalse(result.PilotRunAvailable);
        Assert.IsFalse(result.ReleaseCommercialReady);
        Assert.IsFalse(result.ComplianceCustody);
        Assert.IsFalse(result.CloudBackedDurability);
        Assert.AreEqual(ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter.Classification, result.Classification);
        CollectionAssert.Contains(result.SourceEvidenceRefs.ToArray(), "product-ledger-durable-latest-state-auxiliary-evidence-visible-not-authority");
    }

    [TestMethod]
    public void AuxiliaryEvidence_BlocksAuthorityPrecedencePointerAndUnsafeCandidateClaims()
    {
        var cases = new (Func<ProductLedgerLocalDurableLatestStateReaderCandidateResult, ProductLedgerLocalDurableLatestStateReaderCandidateResult> Mutate, ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker Blocker)[]
        {
            (candidate => candidate with { Decision = ProductLedgerLocalDurableLatestStateReaderCandidateDecision.Rejected }, ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateNotValidated),
            (candidate => candidate with { Authority = true }, ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsAuthority),
            (candidate => candidate with { LiveAuthority = true }, ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsLiveAuthority),
            (candidate => candidate with { ProductAuthority = true }, ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsProductAuthority),
            (candidate => candidate with { ReadPrecedence = true }, ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsReadPrecedence),
            (candidate => candidate with { LatestPointer = true }, ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsLatestPointer),
            (candidate => candidate with { LatestPointerOverwrite = true }, ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsLatestPointerOverwrite),
            (candidate => candidate with { PublicProductAllowed = true }, ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsPublicProduct),
            (candidate => candidate with { CommandExecutionAllowed = true }, ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsCommandExecution),
            (candidate => candidate with { ProviderCloudNetworkAvailable = true }, ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsProviderCloudNetwork),
            (candidate => candidate with { SourceEvidenceRefs = [] }, ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.MissingEvidenceRefs),
            (candidate => candidate with { SourceManifestRelativePath = "unsafe/secret=hidden.json" }, ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.UnsafeMetadata)
        };

        foreach (var testCase in cases)
        {
            var presenter = new ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter(SafeOptions());
            var result = presenter.Present(ValidRequest() with { SourceReaderCandidate = testCase.Mutate(ValidCandidate()) });

            Assert.AreEqual(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceDecision.Rejected, result.Decision, testCase.Blocker.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Blocker, string.Join(",", result.Blockers));
            Assert.IsFalse(result.Authority);
            Assert.IsFalse(result.ReadPrecedence);
            Assert.IsFalse(result.LatestPointer);
            Assert.IsFalse(result.PublicProductAllowed);
        }
    }

    [TestMethod]
    public void AuxiliaryEvidence_BlocksQueryHeaderAndUnsafeOptions()
    {
        var query = new ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter(SafeOptions())
            .Present(ValidRequest() with { QueryOverridePresent = true });
        var header = new ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter(SafeOptions())
            .Present(ValidRequest() with { HeaderOverridePresent = true });
        var unsafeOptions = new ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter(
            SafeOptions() with { AllowsAuthority = true })
            .Present(ValidRequest());

        CollectionAssert.Contains(query.Blockers.ToArray(), ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.QueryOverrideRejected);
        CollectionAssert.Contains(header.Blockers.ToArray(), ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.HeaderOverrideRejected);
        CollectionAssert.Contains(unsafeOptions.Blockers.ToArray(), ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.BoundaryRejected);
    }

    [TestMethod]
    public void AuxiliaryEvidence_SurfaceDomShowsAuxiliaryNotAuthorityState()
    {
        var evidence = new ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter(SafeOptions())
            .Present(ValidRequest());
        var html = new ProductLedgerLocalDevRoutePreview()
            .Render(
                ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest(),
                ProductLedgerOperatorSurfaceReadModelSource.FixtureSafe,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                ValidCandidate(),
                evidence)
            .HtmlSnapshot;

        StringAssert.Contains(html, "data-testid=\"product-ledger-durable-latest-state-auxiliary-evidence-state\"");
        StringAssert.Contains(html, "data-state=\"AuxiliaryEvidenceVisibleNotAuthority\"");
        StringAssert.Contains(html, "data-auxiliary-evidence-only=\"true\"");
        StringAssert.Contains(html, "data-authority=\"false\"");
        StringAssert.Contains(html, "data-read-precedence=\"false\"");
        StringAssert.Contains(html, "data-latest-pointer=\"false\"");
        StringAssert.Contains(html, "data-stale-aware=\"true\"");
        StringAssert.Contains(html, ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter.Classification);
        StringAssert.Contains(html, "auxiliary evidence only not authority no read precedence no latest pointer stale-aware not public/product Development-only");
        Assert.IsFalse(html.Contains("data-executable=\"true\"", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void AuxiliaryEvidence_SourceHasNoForbiddenActivation()
    {
        var source = ReadRepoFile("src", "OneBrain.Core", "Approval", "ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter.cs");
        foreach (var fragment in new[]
        {
            "File.ReadAllText",
            "File.WriteAllText",
            "File.WriteAllBytes",
            "File.AppendAllText",
            "File.Delete",
            "Directory.GetFiles",
            "Directory.EnumerateFiles",
            "Process.Start",
            "ShellExecute",
            "System.Diagnostics.Process",
            "HttpClient",
            "WebSocket",
            "DbContext",
            "MigrationBuilder",
            "KmsClient",
            "WormStore",
            "PilotRecipeExecutor",
            "PilotRecipeExecutionGate.Evaluate(",
            "ReadPrecedence: true",
            "LatestPointer: true",
            "LatestPointerOverwrite: true",
            "ProductAuthority: true",
            "ProductionAllowed: true",
            "PublicProductAllowed: true",
            "ReleaseCommercialReady: true",
            "ComplianceCustody: true",
            "CloudBackedDurability: true"
        })
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }

        StringAssert.Contains(source, "LOCAL_INTERNAL_DEV_ONLY_AUXILIARY_EVIDENCE_NOT_PRECEDENCE_NOT_AUTHORITY");
        StringAssert.Contains(source, "AUXILIARY_EVIDENCE_ONLY");
        StringAssert.Contains(source, "NO_READ_PRECEDENCE");
        StringAssert.Contains(source, "NO_LATEST_POINTER");
    }

    private static ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceOptions SafeOptions() =>
        new(
            ExplicitAuxiliaryEvidenceBoundary: true,
            AllowsReadPrecedence: false,
            AllowsLatestPointer: false,
            AllowsLatestPointerOverwrite: false,
            AllowsAuthority: false,
            AllowsProductAuthority: false,
            AllowsPublicProduct: false,
            AllowsProductionRoute: false,
            AllowsShellOrSubprocess: false,
            AllowsCommandExecution: false,
            AllowsNetwork: false,
            AllowsDb: false,
            AllowsKmsWormExternalTrust: false,
            AllowsReleaseCommercial: false);

    private static ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceRequest ValidRequest() =>
        new(
            ExplicitAuxiliaryEvidenceScope: true,
            DevelopmentMode: true,
            LocalMode: true,
            InternalMode: true,
            SourceReaderCandidate: ValidCandidate(),
            QueryOverridePresent: false,
            HeaderOverridePresent: false);

    private static ProductLedgerLocalDurableLatestStateReaderCandidateResult ValidCandidate() =>
        ProductLedgerLocalDurableLatestStateReaderCandidateResult.Pending with
        {
            Decision = ProductLedgerLocalDurableLatestStateReaderCandidateDecision.ValidatedCandidateNotAuthority,
            State = ProductLedgerLocalDurableLatestStateReaderCandidateState.CandidateValidatedNotAuthority,
            Blockers = [],
            CandidateId = "durable-latest-state-reader-candidate.test",
            CreatedAtUtc = DateTimeOffset.Parse("2026-07-06T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            SourceManifestId = "latest-state-manifest.test",
            SourceManifestRelativePath = "docs/test-output/product-ledger/latest-state-manifests/manifest.test.json",
            SourceManifestHash = new string('a', 64),
            SourceManifestHashPrefix = new string('a', 12),
            SourceManifestCheckpointHash = new string('b', 64),
            SourceManifestCheckpointHashPrefix = new string('b', 12),
            SourceSnapshotIds = ["latest-state-snapshot.test"],
            SourceSnapshotRelativePaths = ["docs/test-output/product-ledger/latest-state-snapshots/snapshot.test.json"],
            SourceSnapshotHashes = [new string('c', 64)],
            SourceSnapshotHashPrefixes = [new string('c', 12)],
            SourceEvidenceRefs =
            [
                "product-ledger-durable-latest-state-reader-candidate-validated-not-authority",
                "docs/qa/product-ledger-durable-latest-state-reader-candidate/report.md"
            ],
            Validation = new ProductLedgerLocalDurableLatestStateReaderCandidateValidation(
                ManifestSchemaValid: true,
                ManifestHashValid: true,
                ManifestCheckpointValid: true,
                SnapshotSchemaValid: true,
                SnapshotHashValid: true,
                SnapshotCheckpointValid: true,
                SafeRelativePathsOnly: true,
                StaleAware: true,
                TamperDetected: false,
                CorruptionDetected: false,
                NotAuthority: true),
            ValidationState = "candidate-validated-not-authority",
            StaleState = "stale-aware-historical-evidence",
            TamperState = "tamper-not-detected",
            CorruptionState = "corruption-not-detected",
            Classification = ProductLedgerLocalDurableLatestStateReaderCandidateValidator.Classification,
            NegativeFlags = ProductLedgerLocalDurableLatestStateReaderCandidateValidator.NegativeFlags
        };

    private static string ReadRepoFile(params string[] segments) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(segments).ToArray()));

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
