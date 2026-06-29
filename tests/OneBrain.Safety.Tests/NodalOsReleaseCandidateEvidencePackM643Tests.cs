using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ReleaseCandidateEvidencePack")]
[TestCategory("M643")]
public sealed class NodalOsReleaseCandidateEvidencePackM643Tests
{
    private const string ReportPath = "docs/reports/m643-release-candidate-evidence-pack.md";
    private const string EvidencePackPath = "artifacts/agent-operations/m643/release-candidate-evidence-pack.json";
    private const string ClosureGatePath = "artifacts/agent-operations/m643/public-release-closure-gate.json";
    private const string DisabledProofPath = "artifacts/agent-operations/m643/disabled-runtime-provider-proof.json";
    private const string PackagingReviewPath = "artifacts/agent-operations/m643/packaging-signing-store-review.json";
    private const string GoNoGoPath = "artifacts/agent-operations/m643/m643-go-no-go.json";

    private const string ManifestPath = "browser-extension/onebrain-chrome-lab/manifest.json";
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string ServiceWorkerPath = "browser-extension/onebrain-chrome-lab/service_worker.js";
    private const string ContentScriptPath = "browser-extension/onebrain-chrome-lab/content_script.js";
    private const string RecipeCorePath = "browser-extension/onebrain-chrome-lab/recipe_core.js";

    private const string BridgeOptionsPath = "src/OneBrain.ChromeLab.Bridge/ChromeLabOptions.cs";
    private const string BridgeProtocolPath = "src/OneBrain.ChromeLab.Bridge/ChromeLabProtocol.cs";
    private const string BridgeSecurityPath = "src/OneBrain.ChromeLab.Bridge/ChromeLabSecurity.cs";
    private const string BridgeSelfTestPath = "src/OneBrain.ChromeLab.Bridge/ChromeLabSelfTest.cs";
    private const string BridgeOpenAiPath = "src/OneBrain.ChromeLab.Bridge/OpenAiAgentClient.cs";
    private const string BridgeProgramPath = "src/OneBrain.ChromeLab.Bridge/Program.cs";
    private const string BridgeProjectPath = "src/OneBrain.ChromeLab.Bridge/OneBrain.ChromeLab.Bridge.csproj";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FullPath(string relativePath) =>
        Path.Combine(RepoRoot(), relativePath);

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(File.ReadAllText(FullPath(relativePath)));

    private static string Sha256Hex(string relativePath)
    {
        var bytes = File.ReadAllBytes(FullPath(relativePath));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    [TestMethod]
    public void ReportExists() =>
        Assert.IsTrue(File.Exists(FullPath(ReportPath)), ReportPath);

    [TestMethod]
    public void EvidencePackExists() =>
        Assert.IsTrue(File.Exists(FullPath(EvidencePackPath)), EvidencePackPath);

    [TestMethod]
    public void PublicReleaseClosureGateExists() =>
        Assert.IsTrue(File.Exists(FullPath(ClosureGatePath)), ClosureGatePath);

    [TestMethod]
    public void DisabledRuntimeProviderProofExists() =>
        Assert.IsTrue(File.Exists(FullPath(DisabledProofPath)), DisabledProofPath);

    [TestMethod]
    public void PackagingSigningStoreReviewExists() =>
        Assert.IsTrue(File.Exists(FullPath(PackagingReviewPath)), PackagingReviewPath);

    [TestMethod]
    public void GoNoGoExists() =>
        Assert.IsTrue(File.Exists(FullPath(GoNoGoPath)), GoNoGoPath);

    [TestMethod]
    public void EvidencePackIsReadyButPublicReleaseRemainsBlocked()
    {
        using var doc = ReadJson(EvidencePackPath);
        Assert.AreEqual("M643", doc.RootElement.GetProperty("milestone").GetString());
        Assert.AreEqual("RELEASE_CANDIDATE_EVIDENCE_PACK_READY", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(doc.RootElement.GetProperty("evidencePackReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void EvidencePackIncludesCleanRuntimeDevtoolsEvidence()
    {
        using var doc = ReadJson(EvidencePackPath);
        var clean = doc.RootElement.GetProperty("cleanEvidence");
        Assert.AreEqual("pass", clean.GetProperty("runtimeTabEvidence").GetString());
        Assert.AreEqual("pass", clean.GetProperty("serviceWorkerDevtoolsEvidence").GetString());
        Assert.AreEqual("pass", clean.GetProperty("bridgeLiveness").GetString());
        Assert.AreEqual("no", clean.GetProperty("cspViolationsObserved").GetString());
        Assert.AreEqual("no", clean.GetProperty("errConnectionRefusedObserved").GetString());
        Assert.AreEqual("no", clean.GetProperty("invalidTokenObserved").GetString());
        Assert.AreEqual("no", clean.GetProperty("policyViolation1008Observed").GetString());
        Assert.AreEqual("no", clean.GetProperty("bridgeWebSocketErrorRepeatedObserved").GetString());
        Assert.AreEqual("no", clean.GetProperty("webSocketReconnectingObserved").GetString());
    }

    [TestMethod]
    public void PublicReleaseClosureGateBlocksWhenRequiredBlockersRemainOpen()
    {
        using var doc = ReadJson(ClosureGatePath);
        Assert.AreEqual("PUBLIC_RELEASE_NO_GO_BLOCKERS_OPEN", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("allCriticalBlockersClosed").GetBoolean());
        Assert.AreEqual("NO-GO", doc.RootElement.GetProperty("closureGateResult").GetString());

        var blockers = doc.RootElement.GetProperty("blockerClosure").EnumerateArray()
            .Where(item => item.GetProperty("blocksPublicRelease").GetBoolean())
            .Select(item => item.GetProperty("id").GetString())
            .ToArray();

        CollectionAssert.Contains(blockers, "HOST_PERMISSIONS_FORMAL_JUSTIFICATION");
        CollectionAssert.Contains(blockers, "PROVIDER_RUNTIME_FINAL_GATE");
        CollectionAssert.Contains(blockers, "PACKAGING_SIGNING_STORE_REVIEW");
    }

    [TestMethod]
    public void RuntimeProviderProofKeepsSensitiveCapabilitiesDisabled()
    {
        using var doc = ReadJson(DisabledProofPath);
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeProductiveEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeChangesAllowed").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerReleaseReady").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("browserAutomationEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("capabilityUnlockEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void PackagingSigningStoreReviewRemainsIncompleteAndBlocksPublicRelease()
    {
        using var doc = ReadJson(PackagingReviewPath);
        Assert.AreEqual("PACKAGING_SIGNING_STORE_REVIEW_INCOMPLETE", doc.RootElement.GetProperty("decision").GetString());
        Assert.IsFalse(doc.RootElement.GetProperty("packagingReviewComplete").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("signingReviewComplete").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("storeListingReviewComplete").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("privacyDisclosureReviewComplete").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("supportDisclosureReviewComplete").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("rollbackPlanComplete").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseReady").GetBoolean());
    }

    [TestMethod]
    public void GoNoGoKeepsPublicReleaseAndSensitivePathsBlocked()
    {
        using var doc = ReadJson(GoNoGoPath);
        Assert.IsTrue(doc.RootElement.GetProperty("readyForReleaseCandidateEvidencePack").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("readyForReleaseEvidenceGate").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForReleasePublic").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForJsChanges").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForRuntimeChanges").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForProviderCloud").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForFilesystem").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("readyForBrowserAutomation").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("runtimeEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("providerCloudEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("filesystemEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("publicReleaseEnabled").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("productFilesModified").GetBoolean());
    }

    [TestMethod]
    public void ManifestJsonUnchanged()
    {
        Assert.AreEqual("76859A171D0FBC585E96253D0F269AEF4A54DCFD5F704DE9DB92D26EB1AEDDFD", Sha256Hex(ManifestPath));
    }

    [TestMethod]
    public void SidepanelHtmlUnchanged()
    {
        Assert.AreEqual("A8E95DC5772C5B55EFE29A35D57A038B44F11432D2FEA23739553CB5C7C835A9", Sha256Hex(SidepanelHtmlPath));
    }

    [TestMethod]
    public void SidepanelCssUnchanged()
    {
        Assert.AreEqual("C6DA9402E2A859DB8C598F417A6F362B6B819E734F11F7B95DBA5957DE620182", Sha256Hex(SidepanelCssPath));
    }

    [TestMethod]
    public void SidepanelJsUnchanged()
    {
        Assert.AreEqual("9063CDDD2FBE020FB3EDD8EEC9591356DA8B1B54774F3666D0B9E2E76217E6A2", Sha256Hex(SidepanelJsPath));
    }

    [TestMethod]
    public void ServiceWorkerUnchanged()
    {
        Assert.AreEqual("B65E0385EC96F0E96DCB3493311372A3B307C53E732235C7B3093AFE2DC39859", Sha256Hex(ServiceWorkerPath));
    }

    [TestMethod]
    public void ContentScriptUnchanged()
    {
        Assert.AreEqual("E1042E37DC884BA8B088DC7CB4D805BC5BFC72820C78DB632D520B6AD4477186", Sha256Hex(ContentScriptPath));
    }

    [TestMethod]
    public void RecipeCoreUnchanged()
    {
        Assert.AreEqual("DEA70FD162CE2F94ED29D35CD2C919AD2D62DA1810D46F49DD0CEBF63399C5F8", Sha256Hex(RecipeCorePath));
    }

    [TestMethod]
    public void BridgeSourceUnchanged()
    {
        Assert.AreEqual("F61164907EE4FB35AA8C0E3CA4A13A98986258B9AB4EDD1469A9D146AF81646A", Sha256Hex(BridgeOptionsPath));
        Assert.AreEqual("C62BBBEA97ACA6B15D72F25415F9E94E20FE9D460903AEB9292A107DCF2AFE68", Sha256Hex(BridgeProtocolPath));
        Assert.AreEqual("5E933A21C501487523C60197F02E0D68CE6D0F5865DC9401F9337EBA41B58B37", Sha256Hex(BridgeSecurityPath));
        Assert.AreEqual("8CF05765736DEF46584022F1E33E22CD4E85A3C08A952048D425FBBC1EFE5309", Sha256Hex(BridgeSelfTestPath));
        Assert.AreEqual("CAACC05544B9E41F7606BD17ECAD81FB36842869BC34E46F8596D4D62EB5B5C4", Sha256Hex(BridgeOpenAiPath));
        Assert.AreEqual("B9FB617E6B2FD1393EC4E1DD7B90AF91E44B26EBD4D70DA87416A5B46531E21E", Sha256Hex(BridgeProgramPath));
        Assert.AreEqual("E00407FA4BC6DCB8ADBB677E5FE4AF41FE7BDB967C5827EBF54569E6DF32E85D", Sha256Hex(BridgeProjectPath));
    }
}
