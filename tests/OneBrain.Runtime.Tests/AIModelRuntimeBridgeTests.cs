using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.AI;
using OneBrain.Core.Models;

namespace OneBrain.Runtime.Tests;

[TestClass]
public sealed class AIModelRuntimeBridgeTests
{
    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public async Task BridgeUsesExistingProfileSelectionThenRotatesAuthorizedEnvironmentKeys()
    {
        var values = ConfiguredProfiles();
        var profiles = AIModelConfiguration.LoadOfficialProfiles(values);
        var catalog = AIModelRuntimeCatalogFactory.Build(profiles);
        var secrets = new EnvironmentSecretReferenceStore(values);
        var calls = 0;
        var runtimeRouter = new PolicyAwareModelRouter(
            catalog,
            secrets,
            new DelegateExecutor((context, _) =>
            {
                calls++;
                var credential = Encoding.UTF8.GetString(context.CredentialBytes.Span);
                Assert.IsTrue(credential is "fixture-standard-primary" or "fixture-standard-fallback");
                return ValueTask.FromResult(calls == 1
                    ? ModelAttemptResult.FromHttpStatus(429, "rate limited")
                    : ModelAttemptResult.Succeeded("ok", estimatedCost: 0.01m));
            }));
        var bridge = new AIModelRuntimeBridge(runtimeRouter);

        var result = await bridge.ExecuteAsync(
            new AIModelRoutingRequest(
                TaskText: "Prepare a structured fixture summary",
                Capability: AIModelCapabilities.StandardTask,
                RiskLevel: AIRiskLevels.Medium,
                RequiresVision: false,
                IsAmbiguous: false,
                IsIrreversible: false,
                EstimatedCostUsd: 0.01m,
                EstimatedCalls: 1,
                Environment: "fixture",
                Profile: "runtime-test"),
            new AIModelRoutingPolicy(profiles, []),
            new ModelExecutionRequest("corr-bridge", "mission-fixture", "step-fixture"));

        Assert.IsTrue(result.Success);
        Assert.AreEqual(AIProfileIds.StandardTask, result.LegacyRouting.Profile?.ProfileId);
        Assert.AreEqual("MODEL_ROUTE_FALLBACK_SUCCEEDED", result.Decision);
        Assert.AreEqual(2, calls);
        Assert.IsFalse(result.RequiresOperatorIntervention);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void BridgeBlocksCloudProfileWhenMissionAuthorizationIsLocalOnly()
    {
        var values = ConfiguredProfiles();
        var profiles = AIModelConfiguration.LoadOfficialProfiles(values);
        var catalog = AIModelRuntimeCatalogFactory.Build(profiles);
        var runtimeRouter = new PolicyAwareModelRouter(
            catalog,
            new EnvironmentSecretReferenceStore(values),
            new DelegateExecutor((_, _) => throw new AssertFailedException("Executor must not run.")));
        var bridge = new AIModelRuntimeBridge(runtimeRouter);

        var result = bridge.Plan(
            new AIModelRoutingRequest(
                "local-only fixture",
                AIModelCapabilities.StandardTask,
                AIRiskLevels.Medium,
                false,
                false,
                false,
                0.01m,
                1,
                "fixture",
                "runtime-test"),
            new AIModelRoutingPolicy(profiles, []),
            AIModelRuntimeAuthorization.Default with
            {
                LocalOnly = true,
                CloudAllowed = false,
                MaximumPrivacyClass = ModelPrivacyClass.LocalOnly
            });

        Assert.IsFalse(result.Success);
        Assert.AreEqual("MODEL_RUNTIME_PLAN_BLOCKED", result.Decision);
        Assert.IsTrue(result.RequiresOperatorIntervention);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public async Task EnvironmentAndCompositeStoresResolveOpaqueReferencesWithoutPersistingSecrets()
    {
        var environment = new EnvironmentSecretReferenceStore(new Dictionary<string, string?>
        {
            ["NODAL_TEST_KEY"] = "fixture-secret-value"
        });
        using var ephemeral = new EphemeralSecretReferenceStore();
        using var composite = new CompositeSecretReferenceStore(
            new Dictionary<string, ISecretReferenceStore>
            {
                [EnvironmentSecretReferenceStore.StoreId] = environment,
                ["ephemeral"] = ephemeral
            },
            defaultStoreId: "ephemeral");

        using var environmentLease = await composite.OpenAsync(
            new SecretReference(EnvironmentSecretReferenceStore.StoreId, "NODAL_TEST_KEY"));
        var ephemeralReference = await composite.StoreAsync("temporary", Encoding.UTF8.GetBytes("temporary-secret"));
        using var ephemeralLease = await composite.OpenAsync(ephemeralReference);

        Assert.IsNotNull(environmentLease);
        Assert.AreEqual("fixture-secret-value", Encoding.UTF8.GetString(environmentLease.Bytes.Span));
        Assert.IsNotNull(ephemeralLease);
        Assert.AreEqual("temporary-secret", Encoding.UTF8.GetString(ephemeralLease.Bytes.Span));
        Assert.AreEqual("environment:[REDACTED]", new SecretReference("environment", "NODAL_TEST_KEY").ToString());
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void ConfiguredSecretMaskNeverRevealsPrefixOrSuffix()
    {
        const string fixtureMaterial = "fixture-credential-material-never-render";

        var masked = AIModelConfiguration.MaskSecret(fixtureMaterial);

        Assert.AreEqual("[configured]", masked);
        Assert.IsFalse(masked.Contains("fixture-credential", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(masked.Contains("never-render", StringComparison.OrdinalIgnoreCase));
    }

    private static Dictionary<string, string?> ConfiguredProfiles() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["OB_AI_STANDARD_TASK_PROVIDER"] = "openai",
        ["OB_AI_STANDARD_TASK_MODEL"] = "fixture-standard",
        ["OB_AI_STANDARD_TASK_API_KEY"] = "fixture-standard-primary",
        ["OB_AI_STANDARD_TASK_FALLBACK_PROFILE"] = AIProfileIds.CheapIntent,
        ["OB_AI_CHEAP_INTENT_PROVIDER"] = "openai",
        ["OB_AI_CHEAP_INTENT_MODEL"] = "fixture-cheap",
        ["OB_AI_CHEAP_INTENT_API_KEY"] = "fixture-standard-fallback",
        ["OB_AI_CRITICAL_REASONER_PROVIDER"] = "mock",
        ["OB_AI_CRITICAL_REASONER_MODEL"] = "fixture-critical",
        ["OB_AI_VISION_VERIFIER_PROVIDER"] = "mock",
        ["OB_AI_VISION_VERIFIER_MODEL"] = "fixture-vision"
    };

    private sealed class DelegateExecutor(
        Func<ModelAttemptContext, CancellationToken, ValueTask<ModelAttemptResult>> execute) : IModelAttemptExecutor
    {
        public ValueTask<ModelAttemptResult> ExecuteAsync(
            ModelAttemptContext context,
            CancellationToken cancellationToken) =>
            execute(context, cancellationToken);
    }
}
