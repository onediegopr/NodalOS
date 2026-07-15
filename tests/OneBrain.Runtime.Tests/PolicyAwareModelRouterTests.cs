using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Models;
using OneBrain.Core.Runtime;

namespace OneBrain.Runtime.Tests;

[TestClass]
public sealed class PolicyAwareModelRouterTests
{
    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void PlanPrefersAuthorizedLocalModelForLocalAlias()
    {
        using var secrets = new EphemeralSecretReferenceStore();
        var catalog = CreateCatalog(Array.Empty<SecretReference>());
        var router = new PolicyAwareModelRouter(catalog, secrets, new DelegateExecutor((_, _) =>
            ValueTask.FromResult(ModelAttemptResult.Succeeded())));

        var plan = router.Plan(Request(localOnly: true, cloudAllowed: false));

        Assert.IsTrue(plan.IsRoutable);
        Assert.AreEqual("local", plan.Candidates[0].Provider.ProviderId);
        Assert.AreEqual(ModelProviderKind.Local, plan.Candidates[0].Provider.Kind);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public async Task ExecuteRotatesAuthorizedKeysWithoutOperatorInterruption()
    {
        using var secrets = new EphemeralSecretReferenceStore();
        var first = await secrets.StoreAsync("primary", Encoding.UTF8.GetBytes("fixture-key-one"));
        var second = await secrets.StoreAsync("secondary", Encoding.UTF8.GetBytes("fixture-key-two"));
        var catalog = CreateCloudOnlyCatalog([first, second]);
        var calls = 0;
        var router = new PolicyAwareModelRouter(
            catalog,
            secrets,
            new DelegateExecutor((_, _) => ValueTask.FromResult(
                ++calls == 1
                    ? ModelAttemptResult.FromHttpStatus(429, "rate limited")
                    : ModelAttemptResult.Succeeded("ok", estimatedCost: 0.01m))));

        var result = await router.ExecuteAsync(Request(), Execution());

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.RequiresOperatorIntervention);
        Assert.AreEqual("MODEL_ROUTE_FALLBACK_SUCCEEDED", result.Decision);
        Assert.AreEqual(2, calls);
        Assert.AreEqual(2, result.Attempts.Count);
        Assert.AreEqual("configured", result.Attempts[0].CredentialSlot);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public async Task ExecuteFallsBackAcrossAuthorizedProviders()
    {
        using var secrets = new EphemeralSecretReferenceStore();
        var keyA = await secrets.StoreAsync("a", Encoding.UTF8.GetBytes("provider-a-secret"));
        var keyB = await secrets.StoreAsync("b", Encoding.UTF8.GetBytes("provider-b-secret"));
        var catalog = CreateTwoCloudProviderCatalog(keyA, keyB);
        var calls = new List<string>();
        var router = new PolicyAwareModelRouter(
            catalog,
            secrets,
            new DelegateExecutor((context, _) =>
            {
                calls.Add(context.Candidate.Provider.ProviderId);
                return ValueTask.FromResult(context.Candidate.Provider.ProviderId == "cloud-a"
                    ? ModelAttemptResult.FromHttpStatus(503, "provider unavailable")
                    : ModelAttemptResult.Succeeded("fallback-ok", estimatedCost: 0.02m));
            }));

        var result = await router.ExecuteAsync(Request(), Execution());

        Assert.IsTrue(result.Success);
        Assert.AreEqual("cloud-b", result.SelectedCandidate?.Provider.ProviderId);
        CollectionAssert.AreEqual(new[] { "cloud-a", "cloud-b" }, calls);
        Assert.IsFalse(result.RequiresOperatorIntervention);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public async Task UserCancellationStopsFallbackImmediately()
    {
        using var secrets = new EphemeralSecretReferenceStore();
        var key = await secrets.StoreAsync("a", Encoding.UTF8.GetBytes("provider-secret"));
        var catalog = CreateCloudOnlyCatalog([key]);
        var calls = 0;
        var router = new PolicyAwareModelRouter(
            catalog,
            secrets,
            new DelegateExecutor(async (_, token) =>
            {
                calls++;
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return ModelAttemptResult.Succeeded();
            }));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var result = await router.ExecuteAsync(Request(), Execution(), cancellationToken: cancellation.Token);

        Assert.IsTrue(result.Cancelled);
        Assert.IsFalse(result.Success);
        Assert.AreEqual("CANCELLED_BY_OPERATOR", result.Decision);
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public async Task FullChainExhaustionIsExplicitAndBounded()
    {
        using var secrets = new EphemeralSecretReferenceStore();
        var first = await secrets.StoreAsync("primary", Encoding.UTF8.GetBytes("first-secret"));
        var second = await secrets.StoreAsync("secondary", Encoding.UTF8.GetBytes("second-secret"));
        var catalog = CreateCloudOnlyCatalog([first, second]);
        var router = new PolicyAwareModelRouter(
            catalog,
            secrets,
            new DelegateExecutor((_, _) => ValueTask.FromResult(
                ModelAttemptResult.FromHttpStatus(500, "upstream failed"))));
        var policy = ModelFallbackPolicy.Default with { MaximumAttempts = 2, MaximumFallbackDepth = 1 };

        var result = await router.ExecuteAsync(Request(), Execution(), policy);

        Assert.IsFalse(result.Success);
        Assert.AreEqual("MODEL_ROUTE_FALLBACK_EXHAUSTED", result.Decision);
        Assert.AreEqual(2, result.Attempts.Count);
        Assert.IsTrue(result.RequiresOperatorIntervention);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public void CostPolicyAndPrivacyPolicyFilterBeforeExecution()
    {
        using var secrets = new EphemeralSecretReferenceStore();
        var catalog = CreateCatalog(Array.Empty<SecretReference>());
        var router = new PolicyAwareModelRouter(catalog, secrets, new DelegateExecutor((_, _) =>
            throw new AssertFailedException("Executor must not run.")));
        var request = Request(localOnly: false, cloudAllowed: true) with
        {
            MaximumInputCostPerMillion = 0,
            MaximumOutputCostPerMillion = 0,
            MaximumPrivacyClass = ModelPrivacyClass.LocalOnly,
            AllowedProviderIds = new[] { "cloud" }
        };

        var plan = router.Plan(request);

        Assert.IsFalse(plan.IsRoutable);
        Assert.AreEqual("NO_COMPATIBLE_MODEL_WITHIN_AUTHORIZED_POLICY", plan.BlockedReason);
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public async Task SecretsAreAbsentFromAttemptsSignalsAndSafeMessages()
    {
        using var secrets = new EphemeralSecretReferenceStore();
        const string rawCredential = "sk-fixture-1234";
        var reference = await secrets.StoreAsync("primary", Encoding.UTF8.GetBytes(rawCredential));
        var catalog = CreateCloudOnlyCatalog([reference]);
        var observer = new RecordingObserver();
        var router = new PolicyAwareModelRouter(
            catalog,
            secrets,
            new DelegateExecutor((context, _) =>
            {
                Assert.AreEqual(rawCredential, Encoding.UTF8.GetString(context.CredentialBytes.Span));
                return ValueTask.FromResult(ModelAttemptResult.Failed(
                    ModelAttemptFailureKind.NonRecoverable,
                    $"api_key={rawCredential}"));
            }),
            observer: observer);

        var result = await router.ExecuteAsync(Request(), Execution());
        var projection = string.Join("|", result.Attempts.Select(attempt => attempt.SafeMessage)) +
            string.Join("|", observer.Signals.SelectMany(signal => signal.Dimensions.Values));

        Assert.IsFalse(projection.Contains(rawCredential, StringComparison.Ordinal));
        Assert.IsFalse(reference.ToString().Contains(reference.SecretId, StringComparison.Ordinal));
    }

    [TestMethod]
    [TestCategory("AiriSelectiveRuntime")]
    public async Task WindowsDpapiStoreKeepsPlaintextOutOfPersistedBytes()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("DPAPI validation is Windows-only.");

        var directory = Path.Combine(Path.GetTempPath(), "nodal-dpapi-test-" + Guid.NewGuid().ToString("N"));
        var store = new WindowsDpapiSecretReferenceStore(directory);
        var secret = Encoding.UTF8.GetBytes("dpapi-private-secret");
        try
        {
            var reference = await store.StoreAsync("test", secret);
            var persisted = File.ReadAllBytes(Directory.GetFiles(directory).Single());
            Assert.IsFalse(Encoding.UTF8.GetString(persisted).Contains("dpapi-private-secret", StringComparison.Ordinal));
            using var lease = await store.OpenAsync(reference);
            Assert.IsNotNull(lease);
            CollectionAssert.AreEqual(secret, lease.Bytes.ToArray());
            Assert.IsTrue(await store.DeleteAsync(reference));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static ModelRouteRequest Request(bool localOnly = false, bool cloudAllowed = true) =>
        new(
            "fast-default",
            ModelCapabilities.Chat,
            1000,
            localOnly,
            cloudAllowed,
            cloudAllowed ? ModelPrivacyClass.AuthorizedCloud : ModelPrivacyClass.LocalOnly,
            20,
            20,
            5,
            Array.Empty<string>(),
            PreferSpeed: true,
            PreferQuality: false);

    private static ModelExecutionRequest Execution() => new("corr-1", "mission-1", "step-1");

    private static ModelCatalog CreateCatalog(IReadOnlyList<SecretReference> cloudKeys)
    {
        var providers = new[]
        {
            new ModelProviderDefinition(
                "local", "Local", ModelProviderKind.Local, new Uri("http://127.0.0.1:11434"),
                false, Array.Empty<SecretReference>(), ModelProviderState.Ready, 95,
                ModelPrivacyClass.LocalOnly, new[] { "local" }),
            new ModelProviderDefinition(
                "cloud", "Cloud", ModelProviderKind.Cloud, new Uri("https://example.invalid"),
                cloudKeys.Count > 0, cloudKeys, ModelProviderState.Ready, 90,
                ModelPrivacyClass.AuthorizedCloud, new[] { "global" })
        };
        var models = new[]
        {
            new ModelDefinition(
                "local-fast", "local", "local-fast", 8192,
                ModelCapabilities.Chat | ModelCapabilities.StructuredOutput,
                ModelPrivacyClass.LocalOnly, 0, 0, 75, 65, true),
            new ModelDefinition(
                "cloud-fast", "cloud", "cloud-fast", 32768,
                ModelCapabilities.Chat | ModelCapabilities.StructuredOutput,
                ModelPrivacyClass.AuthorizedCloud, 1, 2, 95, 80, true)
        };
        var aliases = new[] { new LogicalModelAlias("fast-default", ModelCapabilities.Chat, true, 0, 0) };
        return new ModelCatalog(providers, models, aliases);
    }

    private static ModelCatalog CreateCloudOnlyCatalog(IReadOnlyList<SecretReference> keys)
    {
        var provider = new ModelProviderDefinition(
            "cloud", "Cloud", ModelProviderKind.Cloud, new Uri("https://example.invalid"),
            true, keys, ModelProviderState.Ready, 95,
            ModelPrivacyClass.AuthorizedCloud, new[] { "global" });
        var model = new ModelDefinition(
            "cloud-fast", "cloud", "cloud-fast", 32768,
            ModelCapabilities.Chat | ModelCapabilities.StructuredOutput,
            ModelPrivacyClass.AuthorizedCloud, 1, 2, 95, 80, true);
        return new ModelCatalog([provider], [model], [new LogicalModelAlias("fast-default", ModelCapabilities.Chat, false, 0, 0)]);
    }

    private static ModelCatalog CreateTwoCloudProviderCatalog(SecretReference keyA, SecretReference keyB)
    {
        var providers = new[]
        {
            new ModelProviderDefinition(
                "cloud-a", "Cloud A", ModelProviderKind.Cloud, new Uri("https://a.invalid"),
                true, [keyA], ModelProviderState.Ready, 100,
                ModelPrivacyClass.AuthorizedCloud, new[] { "global" }),
            new ModelProviderDefinition(
                "cloud-b", "Cloud B", ModelProviderKind.Cloud, new Uri("https://b.invalid"),
                true, [keyB], ModelProviderState.Ready, 90,
                ModelPrivacyClass.AuthorizedCloud, new[] { "global" })
        };
        var models = new[]
        {
            new ModelDefinition("a", "cloud-a", "a", 32000, ModelCapabilities.Chat,
                ModelPrivacyClass.AuthorizedCloud, 1, 1, 100, 80, true),
            new ModelDefinition("b", "cloud-b", "b", 32000, ModelCapabilities.Chat,
                ModelPrivacyClass.AuthorizedCloud, 1, 1, 90, 80, true)
        };
        return new ModelCatalog(providers, models, [new LogicalModelAlias("fast-default", ModelCapabilities.Chat, false, 0, 0)]);
    }

    private sealed class DelegateExecutor(
        Func<ModelAttemptContext, CancellationToken, ValueTask<ModelAttemptResult>> execute) : IModelAttemptExecutor
    {
        public ValueTask<ModelAttemptResult> ExecuteAsync(ModelAttemptContext context, CancellationToken cancellationToken) =>
            execute(context, cancellationToken);
    }

    private sealed class RecordingObserver : IRuntimeSignalObserver
    {
        public List<RuntimeSignal> Signals { get; } = [];
        public void TryObserve(RuntimeSignal signal) => Signals.Add(signal);
    }
}
