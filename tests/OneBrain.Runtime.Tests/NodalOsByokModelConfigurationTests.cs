using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Models;
using OneBrain.Core.Models;

namespace OneBrain.Runtime.Tests;

[TestClass]
[TestCategory("ByokModelConfiguration")]
[TestCategory("MvpVerticalSlice")]
[TestCategory("AiriSelectiveRuntime")]
public sealed class NodalOsByokModelConfigurationTests
{
    [TestMethod]
    public async Task ConfigurePersistsOnlyOpaqueCredentialReferenceAndClearDeletesIt()
    {
        using var fixture = Fixture.Create();
        using var secrets = new EphemeralSecretReferenceStore();
        const string rawKey = "fixture-private-model-key";
        var service = fixture.Service(secrets, new QueueHandler());

        var configured = await service.ConfigureAsync(Request(primaryApiKey: rawKey));

        Assert.IsTrue(configured.Accepted, string.Join(" | ", configured.Blockers));
        Assert.IsTrue(configured.Configured);
        Assert.IsTrue(configured.Persisted);
        Assert.IsTrue(configured.Primary?.CredentialConfigured == true);
        Assert.AreEqual("ephemeral", configured.Primary?.CredentialStoreId);
        Assert.IsTrue(File.Exists(fixture.MetadataPath));
        var json = await File.ReadAllTextAsync(fixture.MetadataPath);
        Assert.IsFalse(json.Contains(rawKey, StringComparison.Ordinal));
        Assert.IsFalse(JsonSerializer.Serialize(configured).Contains(rawKey, StringComparison.Ordinal));

        var document = JsonSerializer.Deserialize<NodalOsPersistedByokModelConfiguration>(json, JsonOptions());
        Assert.IsNotNull(document?.Primary.CredentialReference);
        using (var lease = await secrets.OpenAsync(document!.Primary.CredentialReference!))
        {
            Assert.IsNotNull(lease);
            Assert.AreEqual(rawKey, Encoding.UTF8.GetString(lease.Bytes.Span));
        }

        var cleared = await service.ClearAsync();
        Assert.IsFalse(cleared.Configured);
        Assert.IsFalse(File.Exists(fixture.MetadataPath));
        Assert.IsNull(await secrets.OpenAsync(document!.Primary.CredentialReference!));
    }

    [TestMethod]
    public async Task ConnectionTestMakesRealHttpRequestAndPersistsOnlyResponseHash()
    {
        using var fixture = Fixture.Create();
        using var secrets = new EphemeralSecretReferenceStore();
        const string rawKey = "fixture-connection-key";
        var handler = new QueueHandler((request, _) =>
        {
            Assert.AreEqual("Bearer", request.Headers.Authorization?.Scheme);
            Assert.AreEqual(rawKey, request.Headers.Authorization?.Parameter);
            return Task.FromResult(Json(HttpStatusCode.OK, "NODAL_OK", promptTokens: 11, completionTokens: 3));
        });
        var service = fixture.Service(secrets, handler);
        var configured = await service.ConfigureAsync(Request(primaryApiKey: rawKey));
        Assert.IsTrue(configured.Accepted);

        var tested = await service.TestConnectionAsync();

        Assert.IsTrue(tested.Accepted, string.Join(" | ", tested.Blockers));
        Assert.IsTrue(tested.Connected);
        Assert.IsTrue(tested.RealProviderCallAttempted);
        Assert.IsTrue(tested.NetworkUsed);
        Assert.AreEqual(1, tested.AttemptCount);
        Assert.AreEqual("primary-provider", tested.SelectedProviderId);
        Assert.AreEqual("primary-model", tested.SelectedModelId);
        Assert.AreEqual(64, tested.ResponseSha256?.Length);
        Assert.IsTrue(tested.EvidenceRefs.Count > 0);
        Assert.IsTrue(tested.Timeline.Count >= 3);
        Assert.AreEqual(1, handler.Calls);

        var persisted = await File.ReadAllTextAsync(fixture.MetadataPath);
        Assert.IsFalse(persisted.Contains(rawKey, StringComparison.Ordinal));
        Assert.IsFalse(persisted.Contains("NODAL_OK", StringComparison.Ordinal));
        StringAssert.Contains(persisted, tested.ResponseSha256!);
    }

    [TestMethod]
    public async Task PreauthorizedFallbackContinuesAutomaticallyWithinLocalPrivacyAndBudget()
    {
        using var fixture = Fixture.Create();
        using var secrets = new EphemeralSecretReferenceStore();
        var handler = new QueueHandler(
            (_, _) => Task.FromResult(Json(HttpStatusCode.ServiceUnavailable, "ignored")),
            (_, _) => Task.FromResult(Json(HttpStatusCode.OK, "FALLBACK_OK", promptTokens: 7, completionTokens: 2)));
        var service = fixture.Service(secrets, handler);
        var configured = await service.ConfigureAsync(Request(
            primaryApiKey: "primary-key",
            enableFallback: true,
            fallbackApiKey: "fallback-key"));
        Assert.IsTrue(configured.Accepted, string.Join(" | ", configured.Blockers));

        var tested = await service.TestConnectionAsync();

        Assert.IsTrue(tested.Connected, string.Join(" | ", tested.Blockers));
        Assert.IsTrue(tested.FallbackApplied);
        Assert.AreEqual("fallback-provider", tested.SelectedProviderId);
        Assert.AreEqual("fallback-model", tested.SelectedModelId);
        Assert.AreEqual(2, tested.AttemptCount);
        Assert.AreEqual(2, handler.Calls);
        Assert.IsTrue(tested.Timeline.Any(value => value.Kind == OneBrain.AgentOperations.Contracts.NodalOsCoreEventKind.WarningRaised));
    }

    [TestMethod]
    public async Task OperatorCancellationStopsBeforeFallbackAndIsNotPersistedAsConnectionResult()
    {
        using var fixture = Fixture.Create();
        using var secrets = new EphemeralSecretReferenceStore();
        var handler = new QueueHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return Json(HttpStatusCode.OK, "never");
        });
        var service = fixture.Service(secrets, handler);
        var configured = await service.ConfigureAsync(Request(
            primaryApiKey: "primary-key",
            enableFallback: true,
            fallbackApiKey: "fallback-key"));
        Assert.IsTrue(configured.Accepted);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(80));

        var tested = await service.TestConnectionAsync(cancellation.Token);

        Assert.IsTrue(tested.Cancelled);
        Assert.IsFalse(tested.Connected);
        Assert.AreEqual("CANCELLED_BY_OPERATOR", tested.Decision);
        Assert.AreEqual(1, handler.Calls);
        var rehydrated = await service.GetCurrentAsync();
        Assert.IsFalse(rehydrated.ConnectionTested);
        Assert.AreEqual(NodalOsByokConfigurationState.ReadyForConnectionTest, rehydrated.State);
    }

    [TestMethod]
    public async Task CloudEndpointRequiresHttpsExplicitCloudAuthorizationAndCredential()
    {
        using var fixture = Fixture.Create();
        using var secrets = new EphemeralSecretReferenceStore();
        var service = fixture.Service(secrets, new QueueHandler());
        var invalid = Request(primaryApiKey: null) with
        {
            PrimaryProviderType = NodalOsByokProviderType.OpenAiCompatibleCloud,
            PrimaryEndpoint = "http://example.invalid/v1",
            CloudAllowed = false
        };

        var result = await service.ConfigureAsync(invalid);

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(NodalOsByokConfigurationState.ConfigurationInvalid, result.State);
        Assert.IsTrue(result.Blockers.Any(value => value.Contains("HTTPS", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(File.Exists(fixture.MetadataPath));
    }

    private static NodalOsByokModelConfigurationRequest Request(
        string? primaryApiKey,
        bool enableFallback = false,
        string? fallbackApiKey = null) => new(
        PrimaryProviderId: "primary-provider",
        PrimaryDisplayName: "Primary Provider",
        PrimaryProviderType: NodalOsByokProviderType.OpenAiCompatibleLocal,
        PrimaryEndpoint: "http://127.0.0.1:5511/v1",
        PrimaryModelId: "primary-model",
        PrimaryApiKey: primaryApiKey,
        EnableFallback: enableFallback,
        FallbackProviderId: enableFallback ? "fallback-provider" : null,
        FallbackDisplayName: enableFallback ? "Fallback Provider" : null,
        FallbackProviderType: enableFallback ? NodalOsByokProviderType.OpenAiCompatibleLocal : null,
        FallbackEndpoint: enableFallback ? "http://127.0.0.1:5512/v1" : null,
        FallbackModelId: enableFallback ? "fallback-model" : null,
        FallbackApiKey: fallbackApiKey,
        CloudAllowed: false,
        MaximumTotalCostUsd: 1m,
        PerAttemptTimeoutSeconds: 5,
        PrimaryInputCostPerMillion: 1m,
        PrimaryOutputCostPerMillion: 2m,
        FallbackInputCostPerMillion: 0.5m,
        FallbackOutputCostPerMillion: 1m);

    private static HttpResponseMessage Json(
        HttpStatusCode status,
        string content,
        long promptTokens = 0,
        long completionTokens = 0)
    {
        var response = new HttpResponseMessage(status);
        response.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                choices = new[] { new { message = new { content } } },
                usage = new { prompt_tokens = promptTokens, completion_tokens = completionTokens }
            }),
            Encoding.UTF8,
            "application/json");
        return response;
    }

    private static JsonSerializerOptions JsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed class QueueHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> responses = new();
        public QueueHandler(params Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>[] responses)
        {
            foreach (var response in responses)
                this.responses.Enqueue(response);
        }

        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            if (responses.Count == 0)
                throw new AssertFailedException("Unexpected model HTTP request.");
            return responses.Dequeue()(request, cancellationToken);
        }
    }

    private sealed class Fixture : IDisposable
    {
        private Fixture(string root)
        {
            Root = root;
            MetadataPath = Path.Combine(root, "models", "byok.v1.json");
        }

        public string Root { get; }
        public string MetadataPath { get; }

        public static Fixture Create() => new(Path.Combine(
            Path.GetTempPath(),
            "nodal-os-byok-model-tests",
            Guid.NewGuid().ToString("N")));

        public NodalOsByokModelConfigurationService Service(
            ISecretReferenceStore secrets,
            HttpMessageHandler handler) => new(
            MetadataPath,
            secrets,
            new HttpClient(handler));

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                    Directory.Delete(Root, recursive: true);
            }
            catch
            {
            }
        }
    }
}
