using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("DisabledPathJailPrototypeGate")]
[TestCategory("SyntheticCanonicalization")]
[TestCategory("NoMutationProof")]
[TestCategory("ProjectUnderstandingImplementationBoundary")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsDisabledPathJailGateM555M557Tests
{
    private static readonly string[] ForbiddenNames = ["Nexa", "NEXA", "NODRIX", "HOTEP"];
    private static readonly string[] SensitiveMarkers =
    [
        "Bear" + "er ",
        "Authorization:",
        "Cook" + "ie:",
        "password",
        "raw " + "secret",
        "api" + "_key",
        "access" + "_token",
        "refresh" + "_token",
        "private key",
        "s" + "k-",
        "connection string"
    ];

    private readonly NodalOsDisabledPathJailPrototypeGateService gateService = new();
    private readonly NodalOsDisabledPathJailPrototypeGateJsonSerializer gateSerializer = new();
    private readonly NodalOsSyntheticCanonicalizationCasesJsonSerializer casesSerializer = new();
    private readonly NodalOsNoMutationProofContractService proofService = new();
    private readonly NodalOsNoMutationProofContractJsonSerializer proofSerializer = new();

    [TestMethod]
    public void DisabledPathJailPrototypeGate_IsDisabledByDefaultAndNonOperational()
    {
        var gate = Gate();

        Assert.IsTrue(gate.DisabledByDefault);
        Assert.IsTrue(gate.RequiresExplicitFutureEnablement);
        Assert.IsTrue(gate.RequiresAuditBeforeEnablement);
        Assert.IsTrue(gate.RequiresConsentBeforeEnablement);
        Assert.IsTrue(gate.RequiresNoMutationProofBeforeEnablement);
        Assert.IsFalse(gate.UsesRealFilesystem);
        Assert.IsFalse(gate.PerformsRealCanonicalization);
        Assert.IsFalse(gate.PerformsDirectoryListing);
        Assert.IsFalse(gate.PerformsFileRead);
        Assert.IsFalse(gate.PerformsFileHash);
        Assert.IsFalse(gate.CanAuthorizeRealScan);
        Assert.IsFalse(gate.CanAuthorizeFilesystemAccess);
        AssertSafeOutput(gateSerializer.SerializeGate(gate));
    }

    [TestMethod]
    public void DisabledPathJailPrototypeGateDecision_KeepsAllRealReadinessBlocked()
    {
        var decision = gateService.Decide(Gate());

        Assert.IsTrue(decision.PrototypeGateCreated);
        Assert.IsFalse(decision.PrototypeGateEnabled);
        Assert.IsTrue(decision.ReadyForDisabledPrototypeReview);
        Assert.IsFalse(decision.ReadyForRealPathJail);
        Assert.IsFalse(decision.ReadyForRealFilesystemAccess);
        Assert.IsFalse(decision.ReadyForRealScan);
        Assert.IsFalse(decision.ReadyForDirectoryListing);
        Assert.IsFalse(decision.ReadyForFileRead);
        Assert.IsFalse(decision.ReadyForFileHash);
        Assert.IsFalse(decision.ReadyForIndexing);
        Assert.IsFalse(decision.ReadyForRepresentationBuild);
        Assert.IsFalse(decision.ReadyForLlmContext);
        AssertSafeOutput(gateSerializer.SerializeDecision(decision));
    }

    [TestMethod]
    public void SyntheticCanonicalizationCases_AreSyntheticOnly()
    {
        foreach (var item in Matrix().Cases)
        {
            Assert.IsTrue(item.IsSyntheticOnly);
            Assert.IsFalse(item.UsesRealFilesystem);
            Assert.IsFalse(item.PerformsRealCanonicalization);
        }

        AssertSafeOutput(casesSerializer.SerializeMatrix(Matrix()));
    }

    [TestMethod]
    public void SyntheticCanonicalizationMatrix_CoversAllRequiredGroups()
    {
        var matrix = Matrix();
        var groups = matrix.Cases.Select(c => c.Group).ToHashSet();

        foreach (var group in Enum.GetValues<NodalOsSyntheticCanonicalizationCaseGroup>())
            Assert.IsTrue(groups.Contains(group), $"Missing group: {group}");

        Assert.AreEqual(100m, matrix.CoveragePercent);
        Assert.AreEqual(0, matrix.MissingCaseGroups.Count);
        Assert.IsTrue(matrix.ReadyForSyntheticCanonicalizationReview);
        Assert.IsFalse(matrix.ReadyForRealCanonicalization);
        Assert.IsFalse(matrix.ReadyForRealPathJail);
        Assert.IsFalse(matrix.ReadyForFilesystemAccess);
    }

    [TestMethod]
    public void SyntheticCanonicalizationMatrix_CannotResolveOrAuthorizeOperationalBehavior()
    {
        var matrix = Matrix();

        Assert.IsFalse(matrix.CanResolveRealPath);
        Assert.IsFalse(matrix.CanAccessFilesystem);
        Assert.IsFalse(matrix.CanReadDirectory);
        Assert.IsFalse(matrix.CanReadFile);
        Assert.IsFalse(matrix.CanHashFile);
        Assert.IsFalse(matrix.CanAuthorizeScan);
    }

    [TestMethod]
    public void NoMutationProofContract_DisablesAllMutationOperations()
    {
        var contract = Proof();

        Assert.IsTrue(contract.IsContractOnly);
        Assert.IsFalse(contract.UsesRealFilesystem);
        Assert.IsFalse(contract.PerformsMutation);
        Assert.IsFalse(contract.PerformsFileWrite);
        Assert.IsFalse(contract.PerformsFileDelete);
        Assert.IsFalse(contract.PerformsFileMove);
        Assert.IsFalse(contract.PerformsDirectoryCreate);
        Assert.IsFalse(contract.PerformsPermissionChange);
        Assert.IsFalse(contract.PerformsMetadataWrite);
        Assert.IsFalse(contract.PerformsLocking);
        Assert.IsTrue(contract.RequiresRuntimeAuditBeforeRealUse);
        Assert.IsTrue(contract.ForbiddenOperationsRedacted.Count >= 14);
        AssertSafeOutput(proofSerializer.SerializeContract(contract));
    }

    [TestMethod]
    public void NoMutationProofResult_IsNecessaryButNotSufficient()
    {
        var result = proofService.Evaluate(Proof());

        Assert.IsTrue(result.ContractDeclaresNoMutation);
        Assert.IsTrue(result.ReadyForSyntheticNoMutationReview);
        Assert.IsFalse(result.ReadyForRealNoMutationGuarantee);
        Assert.IsFalse(result.ReadyForRealFilesystemAccess);
        Assert.IsFalse(result.ReadyForRealScan);
        Assert.IsTrue(result.NecessaryButNotSufficientForFutureOperationalScan);
        AssertSafeOutput(proofSerializer.SerializeResult(result));
    }

    [TestMethod]
    public void StaticPreviewArtifact_HasNoExternalScriptsOrNetworkResources()
    {
        var html = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m557", "disabled-path-jail-prototype-gate-preview.html"));

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_KeepGateDisabledAndNoMutationContractOnly()
    {
        var gate = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m557", "disabled-path-jail-prototype-gate.json"));
        var cases = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m557", "synthetic-canonicalization-cases.json"));
        var proof = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m557", "no-mutation-proof-contract.json"));

        AssertContains(gate, "\"disabledByDefault\": true");
        AssertContains(gate, "\"prototypeGateEnabled\": false");
        AssertContains(cases, "\"readyForSyntheticCanonicalizationReview\": true");
        AssertContains(cases, "\"readyForRealCanonicalization\": false");
        AssertContains(proof, "\"isContractOnly\": true");
        AssertContains(proof, "\"contractDeclaresNoMutation\": true");
        AssertContains(proof, "\"necessaryButNotSufficientForFutureOperationalScan\": true");
        AssertSafeOutput(gate + cases + proof);
    }

    [TestMethod]
    public void Boundary_NewDisabledPathJailGateFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
    {
        var source = NewSource();

        AssertDoesNotContain(source, "OneBrain." + "BrowserExecutor" + ".Cdp");
        AssertDoesNotContain(source, "Http" + "Client");
        AssertDoesNotContain(source, "Client" + "WebSocket");
        AssertDoesNotContain(source, "Process" + ".Start");
        AssertDoesNotContain(source, "System.Diagnostics." + "Process");
        AssertDoesNotContain(source, "Background" + "Service");
        AssertDoesNotContain(source, "Task.Run");
        AssertDoesNotContain(source, "File" + ".Read");
        AssertDoesNotContain(source, "File" + ".Write");
        AssertDoesNotContain(source, "File" + ".Delete");
        AssertDoesNotContain(source, "File" + ".Move");
        AssertDoesNotContain(source, "Directory" + ".");
        AssertDoesNotContain(source, "File" + "Info");
        AssertDoesNotContain(source, "Directory" + "Info");
    }

    private NodalOsDisabledPathJailPrototypeGate Gate() => gateService.CreateGate();

    private static NodalOsSyntheticCanonicalizationMatrix Matrix() =>
        NodalOsSyntheticCanonicalizationCasesFixtures.Matrix();

    private NodalOsNoMutationProofContract Proof() =>
        proofService.CreateContract(Gate(), Matrix());

    private static string NewSource()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsDisabledPathJailPrototypeGateContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsSyntheticCanonicalizationCasesContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsNoMutationProofContractContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsDisabledPathJailPrototypeGateServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsSyntheticCanonicalizationCasesServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsNoMutationProofContractServices.cs")
        };

        return string.Join(Environment.NewLine, files.Select(TextStore.ReadAllText));
    }

    private static void AssertSafeOutput(string value)
    {
        foreach (var name in ForbiddenNames)
            AssertDoesNotContain(value, name);

        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(value, marker);
    }

    private static void AssertContains(string value, string expected) =>
        StringAssert.Contains(value, expected);

    private static void AssertDoesNotContain(string value, string forbidden) =>
        Assert.IsFalse(value.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Unexpected content: {forbidden}");

    private static string PathFor(params string[] segments) =>
        Path.Combine([FindRepoRoot(), .. segments]);

    private static string FindRepoRoot()
    {
        var current = Path.GetFullPath(AppContext.BaseDirectory);
        while (!string.IsNullOrEmpty(current) && !TextStore.Exists(Path.Combine(current, "OneBrain.slnx")))
            current = Path.GetDirectoryName(current) ?? string.Empty;

        return string.IsNullOrEmpty(current) ? throw new InvalidOperationException("Repository root not found.") : current;
    }
}

