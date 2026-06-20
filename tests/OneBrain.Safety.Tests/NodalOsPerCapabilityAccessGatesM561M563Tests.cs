using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("PerCapabilityAccessGate")]
[TestCategory("SyntheticFailureModes")]
[TestCategory("ConsentEnforcementPreview")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsPerCapabilityAccessGatesM561M563Tests
{
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
        "s" + "k-"
    ];

    private readonly NodalOsPerCapabilityAccessGateService gateService = new();
    private readonly NodalOsPerCapabilityAccessGateJsonSerializer gateSerializer = new();
    private readonly NodalOsSyntheticFailureModesService failureService = new();
    private readonly NodalOsSyntheticFailureModesJsonSerializer failureSerializer = new();
    private readonly NodalOsConsentEnforcementPreviewService consentService = new();
    private readonly NodalOsConsentEnforcementPreviewJsonSerializer consentSerializer = new();

    [TestMethod]
    public void CapabilityGates_AreDisabledContractOnlyAndFailClosed()
    {
        foreach (var gate in Gates())
        {
            Assert.IsTrue(gate.DisabledByDefault);
            Assert.IsTrue(gate.RequiresExplicitFutureEnablement);
            Assert.IsTrue(gate.RequiresUserConsent);
            Assert.IsTrue(gate.FailClosed);
            Assert.IsTrue(gate.IsContractOnly);
        }

        AssertSafeOutput(gateSerializer.SerializeGates(Gates()));
    }

    [TestMethod]
    public void GateDecisions_CannotAuthorizeOrAccessOperationalCapabilities()
    {
        foreach (var decision in Decisions())
        {
            Assert.IsFalse(decision.GateEnabled);
            Assert.IsFalse(decision.ReadyForRealUse);
            Assert.IsFalse(decision.CanAuthorizeCapability);
            Assert.IsFalse(decision.CanAccessFilesystem);
            Assert.IsFalse(decision.CanReadContent);
            Assert.IsFalse(decision.CanFingerprintContent);
            Assert.IsFalse(decision.CanBuildRepresentation);
            Assert.IsFalse(decision.CanSendToLlm);
            Assert.IsFalse(decision.CanSendToCloud);
        }

        AssertSafeOutput(gateSerializer.SerializeDecisions(Decisions()));
    }

    [TestMethod]
    public void CapabilityDependencyMatrix_ModelsRequiredDependencies()
    {
        var matrix = gateService.CreateDependencyMatrix();

        AssertDepends(matrix, NodalOsOperationalCapability.DirectoryListing, NodalOsOperationalCapability.PathCanonicalization);
        AssertDepends(matrix, NodalOsOperationalCapability.FileRead, NodalOsOperationalCapability.PathCanonicalization);
        AssertDepends(matrix, NodalOsOperationalCapability.FileRead, NodalOsOperationalCapability.DirectoryListing);
        AssertDepends(matrix, NodalOsOperationalCapability.FileHash, NodalOsOperationalCapability.FileRead);
        AssertDepends(matrix, NodalOsOperationalCapability.Indexing, NodalOsOperationalCapability.SecretDetection);
        AssertDepends(matrix, NodalOsOperationalCapability.RepresentationBuild, NodalOsOperationalCapability.Indexing);

        var runtime = matrix.Dependencies.Single(d => d.Capability == NodalOsOperationalCapability.RuntimeExecution);
        Assert.AreEqual(0, runtime.DependsOn.Count);
        Assert.IsTrue(runtime.PolicyDependenciesRedacted.Any(p => p.Contains("Positive execution gate", StringComparison.OrdinalIgnoreCase)));
        AssertSafeOutput(gateSerializer.SerializeDependencyMatrix(matrix));
    }

    [TestMethod]
    public void SyntheticFailureModes_AreSyntheticOnlyAndNonOperational()
    {
        var matrix = FailureMatrix();

        foreach (var mode in matrix.FailureModes)
        {
            Assert.IsTrue(mode.IsSyntheticOnly);
            Assert.IsFalse(mode.UsesRealFilesystem);
            Assert.IsFalse(mode.PerformsRealOperation);
        }

        AssertSafeOutput(failureSerializer.SerializeMatrix(matrix));
    }

    [TestMethod]
    public void SyntheticFailureModeMatrix_CoversAllCategoriesAndFailsClosed()
    {
        var matrix = FailureMatrix();
        var categories = matrix.FailureModes.Select(m => m.Scenario).ToHashSet();

        foreach (var category in Enum.GetValues<NodalOsSyntheticFailureCategory>())
            Assert.IsTrue(categories.Contains(category), $"Missing failure category: {category}");

        Assert.AreEqual(100m, matrix.CoveragePercent);
        Assert.IsTrue(matrix.ReadyForSyntheticFailureReview);
        Assert.IsFalse(matrix.ReadyForRealFailureHandling);
        Assert.IsFalse(matrix.ReadyForRealFilesystemAccess);
        Assert.IsFalse(matrix.ReadyForRealScan);
        Assert.IsTrue(matrix.Behavior.FailClosed);
        Assert.IsTrue(matrix.Behavior.DoesNotRetryAutomatically);
        Assert.IsTrue(matrix.Behavior.DoesNotEscalateToRuntime);
    }

    [TestMethod]
    public void ConsentEnforcementPreview_IsNoOpAndCannotAuthorizeOrPersist()
    {
        var preview = ConsentPreview();

        Assert.IsTrue(preview.IsPreviewOnly);
        Assert.IsTrue(preview.IsNoOp);
        Assert.IsFalse(preview.UsesRealFilesystem);
        Assert.IsFalse(preview.EnforcesConsentOnRealOperation);
        Assert.IsFalse(preview.CanAuthorizeRealCapability);
        Assert.IsFalse(preview.CanPersistConsent);
        Assert.IsFalse(preview.CanBypassConsent);
        AssertSafeOutput(consentSerializer.SerializePreview(preview));
    }

    [TestMethod]
    public void ConsentRules_RequireConsentScopeFreshnessAndFailClosed()
    {
        foreach (var rule in ConsentPreview().Rules)
        {
            Assert.IsTrue(rule.ConsentRequired);
            Assert.IsTrue(rule.ConsentScopeRequired);
            Assert.IsTrue(rule.ConsentFreshnessRequired);
            Assert.IsTrue(rule.RevocationSupportedInFuture);
            Assert.IsTrue(rule.NarrowScopeRequiredForSensitiveCapability);
            Assert.IsTrue(rule.UserFacingExplanationRequired);
            Assert.IsTrue(rule.FailClosedIfMissing);
        }
    }

    [TestMethod]
    public void ConsentPreviewResult_AllowsSyntheticReviewOnly()
    {
        var result = ConsentPreview().Result;

        Assert.AreEqual(NodalOsConsentEnforcementMode.PreviewOnly, result.ConsentEnforcementMode);
        Assert.IsTrue(result.ReadyForSyntheticConsentReview);
        Assert.IsFalse(result.ReadyForProductiveConsentEnforcement);
        Assert.IsFalse(result.ReadyForRealFilesystemAccess);
        Assert.IsFalse(result.ReadyForRealScan);
        Assert.IsFalse(result.ReadyForIndexing);
        Assert.IsFalse(result.ReadyForRepresentationBuild);
        Assert.IsFalse(result.ReadyForLlmContext);
    }

    [TestMethod]
    public void ConsentReviewOptions_AreNoOpAndNonAuthorizing()
    {
        foreach (var option in Enum.GetValues<NodalOsConsentReviewOption>())
        {
            var result = consentService.ApplyOption(option);

            Assert.IsTrue(result.IsNoOp);
            Assert.IsFalse(result.MutatesState);
            Assert.IsFalse(result.AuthorizesRealCapability);
            Assert.IsFalse(result.PersistsConsent);
            AssertSafeOutput(consentSerializer.SerializeOption(result));
        }
    }

    [TestMethod]
    public void StaticPreviewArtifact_HasNoExternalScriptsOrNetworkResources()
    {
        var html = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m563", "per-capability-access-gates-preview.html"));

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_DeclareGatesFailuresAndConsentPreviewBlocked()
    {
        var gates = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m563", "per-capability-access-gates.json"));
        var failures = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m563", "synthetic-failure-modes.json"));
        var consent = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m563", "consent-enforcement-preview.json"));

        AssertContains(gates, "\"disabledByDefault\": true");
        AssertContains(gates, "\"gateEnabled\": false");
        AssertContains(failures, "\"readyForSyntheticFailureReview\": true");
        AssertContains(failures, "\"failClosed\": true");
        AssertContains(consent, "\"isPreviewOnly\": true");
        AssertContains(consent, "\"canAuthorizeRealCapability\": false");
        AssertSafeOutput(gates + failures + consent);
    }

    [TestMethod]
    public void Boundary_NewPerCapabilityFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
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

    private IReadOnlyList<NodalOsCapabilityAccessGate> Gates() => gateService.CreateGates();
    private IReadOnlyList<NodalOsCapabilityGateDecision> Decisions() => Gates().Select(gateService.Decide).ToArray();
    private NodalOsSyntheticFailureModeMatrix FailureMatrix() => failureService.CreateMatrix();
    private NodalOsConsentEnforcementPreview ConsentPreview() => consentService.CreatePreview(Gates(), FailureMatrix());

    private static void AssertDepends(NodalOsCapabilityDependencyMatrix matrix, NodalOsOperationalCapability capability, NodalOsOperationalCapability dependency)
    {
        var item = matrix.Dependencies.Single(d => d.Capability == capability);
        Assert.IsTrue(item.DependsOn.Contains(dependency), $"{capability} missing dependency {dependency}");
    }

    private static string NewSource()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsPerCapabilityAccessGateContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsSyntheticFailureModesContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsConsentEnforcementPreviewContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsPerCapabilityAccessGateServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsSyntheticFailureModesServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsConsentEnforcementPreviewServices.cs")
        };

        return string.Join(Environment.NewLine, files.Select(TextStore.ReadAllText));
    }

    private static void AssertSafeOutput(string value)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(value, marker);

        AssertDoesNotContain(value, "NEXA");
        AssertDoesNotContain(value, "NODRIX");
        AssertDoesNotContain(value, "HOTEP");
    }

    private static void AssertContains(string value, string expected) => StringAssert.Contains(value, expected);

    private static void AssertDoesNotContain(string value, string forbidden) =>
        Assert.IsFalse(value.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Unexpected content: {forbidden}");

    private static string PathFor(params string[] segments) => Path.Combine([FindRepoRoot(), .. segments]);

    private static string FindRepoRoot()
    {
        var current = Path.GetFullPath(AppContext.BaseDirectory);
        while (!string.IsNullOrEmpty(current) && !TextStore.Exists(Path.Combine(current, "OneBrain.slnx")))
            current = Path.GetDirectoryName(current) ?? string.Empty;

        return string.IsNullOrEmpty(current) ? throw new InvalidOperationException("Repository root not found.") : current;
    }
}

