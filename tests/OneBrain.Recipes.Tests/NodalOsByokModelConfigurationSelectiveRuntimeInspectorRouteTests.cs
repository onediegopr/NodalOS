using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Core.Models;
using OneBrain.Core.Models;
using OneBrain.Pilot;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ByokModelConfiguration")]
[TestCategory("MvpVerticalSlice")]
[TestCategory("AiriSelectiveRuntime")]
public sealed class NodalOsByokModelConfigurationSelectiveRuntimeInspectorRouteTests
{
    [TestMethod]
    public async Task ByokRoutesPersistOpaqueConfigurationTestRealRouteAndProjectMissionControlWithoutSecretLeak()
    {
        using var fixture = Fixture.Create();
        using var secrets = new EphemeralSecretReferenceStore();
        const string rawKey = "route-private-model-credential";
        const string providerContent = "ROUTE_CONNECTION_OK";
        var provider = new DelegateHandler((request, _) =>
        {
            Assert.AreEqual("Bearer", request.Headers.Authorization?.Scheme);
            Assert.AreEqual(rawKey, request.Headers.Authorization?.Parameter);
            Assert.AreEqual("/v1/chat/completions", request.RequestUri?.AbsolutePath);
            return Task.FromResult(Json(HttpStatusCode.OK, providerContent));
        });
        var service = fixture.Service(secrets, provider);

        await using var app = BuildApp(service);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        var address = ServerAddress(app);
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(address) };

        using var formResponse = await client.GetAsync(
            NodalOsByokModelConfigurationEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var formHtml = await formResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        var token = ExtractToken(formHtml);
        Assert.AreEqual(HttpStatusCode.OK, formResponse.StatusCode);
        StringAssert.Contains(formHtml, "data-nodal-os=\"byok-model-configuration\"");
        StringAssert.Contains(formHtml, "data-configured=\"false\"");
        StringAssert.Contains(formResponse.Headers.GetValues("Content-Security-Policy").Single(), "form-action 'self'");
        Assert.IsFalse(formHtml.Contains("<script", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(formHtml, "<option value=\"OpenAiCompatibleLocal\" selected>");

        using var configure = CreatePost(
            new Uri(address),
            NodalOsByokModelConfigurationEndpointMapper.HtmlRoute,
            token,
            ConfigurationForm(rawKey));
        using var configuredResponse = await client.SendAsync(
            configure,
            TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Redirect, configuredResponse.StatusCode);
        Assert.AreEqual(NodalOsByokModelConfigurationEndpointMapper.HtmlRoute, configuredResponse.Headers.Location?.OriginalString);

        using var configuredJsonResponse = await client.GetAsync(
            NodalOsByokModelConfigurationEndpointMapper.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var configuredJson = await configuredJsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using (var configuredDocument = JsonDocument.Parse(configuredJson))
        {
            var root = configuredDocument.RootElement;
            Assert.IsTrue(root.GetProperty("configured").GetBoolean());
            Assert.IsTrue(root.GetProperty("persisted").GetBoolean());
            Assert.IsFalse(root.GetProperty("connected").GetBoolean());
            Assert.IsTrue(root.GetProperty("secretsExcluded").GetBoolean());
            Assert.IsFalse(root.GetProperty("productAuthorityGranted").GetBoolean());
            Assert.AreEqual("primary-provider", root.GetProperty("primary").GetProperty("providerId").GetString());
            Assert.AreEqual("ephemeral", root.GetProperty("primary").GetProperty("credentialStoreId").GetString());
        }
        AssertNoSecret(configuredJson, rawKey, providerContent);

        using var configuredPageResponse = await client.GetAsync(
            NodalOsByokModelConfigurationEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var configuredPage = await configuredPageResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        token = ExtractToken(configuredPage);
        StringAssert.Contains(configuredPage, "data-configured=\"true\"");
        StringAssert.Contains(configuredPage, "Probar conexión real");
        var configForm = Regex.Match(
            configuredPage,
            "<form class=\"config\".*?</form>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            TimeSpan.FromSeconds(1));
        Assert.IsTrue(configForm.Success);
        Assert.IsFalse(configForm.Value.Contains("action=\"/models/test\"", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(configForm.Value.Contains("action=\"/models/clear\"", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(
            3,
            Regex.Matches(configuredPage, "<form\\b", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)).Count);
        AssertNoSecret(configuredPage, rawKey, providerContent);

        using var testRequest = CreatePost(
            new Uri(address),
            NodalOsByokModelConfigurationEndpointMapper.TestRoute,
            token,
            new Dictionary<string, string>());
        using var testedResponse = await client.SendAsync(
            testRequest,
            TestContext.CancellationTokenSource.Token);
        var testedPage = await testedResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.OK, testedResponse.StatusCode);
        StringAssert.Contains(testedPage, "data-connected=\"true\"");
        StringAssert.Contains(testedPage, "data-real-provider-call=\"true\"");
        StringAssert.Contains(testedPage, "data-network-used=\"true\"");
        StringAssert.Contains(testedPage, "data-secrets-excluded=\"true\"");
        AssertNoSecret(testedPage, rawKey, providerContent);
        Assert.AreEqual(1, provider.Calls);

        using var testedJsonResponse = await client.GetAsync(
            NodalOsByokModelConfigurationEndpointMapper.JsonRoute,
            TestContext.CancellationTokenSource.Token);
        var testedJson = await testedJsonResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token);
        using (var testedDocument = JsonDocument.Parse(testedJson))
        {
            var root = testedDocument.RootElement;
            Assert.IsTrue(root.GetProperty("connected").GetBoolean());
            Assert.IsTrue(root.GetProperty("connectionTested").GetBoolean());
            Assert.IsTrue(root.GetProperty("realProviderCallAttempted").GetBoolean());
            Assert.AreEqual(1, root.GetProperty("attemptCount").GetInt32());
            Assert.AreEqual("primary-provider", root.GetProperty("selectedProviderId").GetString());
            Assert.AreEqual("primary-model", root.GetProperty("selectedModelId").GetString());
            Assert.AreEqual(64, root.GetProperty("responseSha256").GetString()?.Length);
            Assert.IsTrue(root.GetProperty("evidenceRefs").GetArrayLength() > 0);
            Assert.IsTrue(root.GetProperty("timeline").GetArrayLength() >= 3);
        }
        AssertNoSecret(testedJson, rawKey, providerContent);

        var control = await MissionControlProductShellEndpointMapper.BuildSnapshotAsync(
            cancellationToken: TestContext.CancellationTokenSource.Token,
            byokModelConfigurationService: service);
        Assert.IsTrue(control.ByokConfigured);
        Assert.IsTrue(control.ModelConnectionVerified);
        Assert.AreEqual("standard_task", control.LogicalModel);
        Assert.AreEqual("primary-provider", control.ActiveProvider);
        Assert.AreEqual("primary-model", control.ActiveModel);
        Assert.IsTrue(control.NetworkUsed);
        Assert.IsTrue(control.ExternalIoUsed);
        Assert.IsFalse(control.ProductAuthorityGranted);
        Assert.IsTrue(control.EvidenceRefs.Any(value => value.Contains("byok-model-connection", StringComparison.Ordinal)));
        var controlHtml = MissionControlProductShellHtmlRenderer.Render(control);
        StringAssert.Contains(controlHtml, "data-byok-configured=\"true\"");
        StringAssert.Contains(controlHtml, "data-model-connection-verified=\"true\"");
        StringAssert.Contains(controlHtml, "/models/config");
        AssertNoSecret(controlHtml, rawKey, providerContent);

        var metadata = await File.ReadAllTextAsync(fixture.MetadataPath, TestContext.CancellationTokenSource.Token);
        AssertNoSecret(metadata, rawKey, providerContent);
        StringAssert.Contains(metadata, "responseSha256");
    }

    [TestMethod]
    public async Task ByokPostRejectsMismatchedTokenAndOriginWithoutPersistingCredential()
    {
        using var fixture = Fixture.Create();
        using var secrets = new EphemeralSecretReferenceStore();
        var service = fixture.Service(secrets, new DelegateHandler((_, _) =>
            throw new AssertFailedException("Provider must not be called.")));
        await using var app = BuildApp(service);
        await app.StartAsync(TestContext.CancellationTokenSource.Token);
        var address = ServerAddress(app);
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(address) };

        using var formResponse = await client.GetAsync(
            NodalOsByokModelConfigurationEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        var token = ExtractToken(await formResponse.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token));
        using var wrongToken = CreatePost(
            new Uri(address),
            NodalOsByokModelConfigurationEndpointMapper.HtmlRoute,
            new string('0', token.Length),
            ConfigurationForm("wrong-token-key"));
        using var wrongTokenResponse = await client.SendAsync(wrongToken, TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Forbidden, wrongTokenResponse.StatusCode);
        Assert.IsFalse(File.Exists(fixture.MetadataPath));

        using var refreshed = await client.GetAsync(
            NodalOsByokModelConfigurationEndpointMapper.HtmlRoute,
            TestContext.CancellationTokenSource.Token);
        token = ExtractToken(await refreshed.Content.ReadAsStringAsync(TestContext.CancellationTokenSource.Token));
        using var wrongOrigin = CreatePost(
            new Uri(address),
            NodalOsByokModelConfigurationEndpointMapper.HtmlRoute,
            token,
            ConfigurationForm("wrong-origin-key"),
            origin: "http://example.invalid");
        using var wrongOriginResponse = await client.SendAsync(wrongOrigin, TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(HttpStatusCode.Forbidden, wrongOriginResponse.StatusCode);
        Assert.IsFalse(File.Exists(fixture.MetadataPath));
    }

    [TestMethod]
    public void ByokModelBoundaryIsLoopbackOnly()
    {
        Assert.IsFalse(NodalOsByokModelConfigurationEndpointMapper.IsRequestAllowed(null));
        Assert.IsFalse(NodalOsByokModelConfigurationEndpointMapper.IsRequestAllowed(IPAddress.Parse("192.0.2.55")));
        Assert.IsTrue(NodalOsByokModelConfigurationEndpointMapper.IsRequestAllowed(IPAddress.Loopback));
    }

    public TestContext TestContext { get; set; } = null!;

    private static WebApplication BuildApp(NodalOsByokModelConfigurationService service)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        NodalOsByokModelConfigurationEndpointMapper.MapNodalOsByokModelConfiguration(
            app,
            app.Environment,
            () => service);
        return app;
    }

    private static Dictionary<string, string> ConfigurationForm(string key) => new()
    {
        ["primaryProviderId"] = "primary-provider",
        ["primaryDisplayName"] = "Primary Provider",
        ["primaryProviderType"] = "OpenAiCompatibleLocal",
        ["primaryEndpoint"] = "http://127.0.0.1:5511/v1",
        ["primaryModelId"] = "primary-model",
        ["primaryApiKey"] = key,
        ["maximumTotalCostUsd"] = "1",
        ["perAttemptTimeoutSeconds"] = "5",
        ["primaryInputCostPerMillion"] = "1",
        ["primaryOutputCostPerMillion"] = "2"
    };

    private static HttpRequestMessage CreatePost(
        Uri baseUri,
        string route,
        string token,
        Dictionary<string, string> form,
        string? origin = null)
    {
        form[NodalOsByokModelConfigurationEndpointMapper.TokenField] = token;
        var request = new HttpRequestMessage(HttpMethod.Post, route)
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.TryAddWithoutValidation("Origin", origin ?? baseUri.GetLeftPart(UriPartial.Authority));
        return request;
    }

    private static string ExtractToken(string html)
    {
        var match = Regex.Match(
            html,
            $"name=\"{NodalOsByokModelConfigurationEndpointMapper.TokenField}\" value=\"(?<token>[0-9a-f]+)\"",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));
        Assert.IsTrue(match.Success, "BYOK model request token was not rendered.");
        return match.Groups["token"].Value;
    }

    private static string ServerAddress(WebApplication app)
    {
        var feature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        var address = feature?.Addresses.SingleOrDefault();
        Assert.IsFalse(string.IsNullOrWhiteSpace(address), "Server address was not assigned.");
        return address!;
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string content)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    choices = new[] { new { message = new { content } } },
                    usage = new { prompt_tokens = 9, completion_tokens = 3 }
                }),
                Encoding.UTF8,
                "application/json")
        };
        return response;
    }

    private static void AssertNoSecret(string value, params string[] forbidden)
    {
        foreach (var item in forbidden)
            Assert.IsFalse(value.Contains(item, StringComparison.Ordinal), $"Sensitive value leaked: {item}");
    }

    private sealed class DelegateHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            return response(request, cancellationToken);
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
            "nodal-os-byok-model-route-tests",
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
                if (!Directory.Exists(Root))
                    return;
                foreach (var path in Directory.GetFiles(Root, "*", SearchOption.AllDirectories))
                    File.SetAttributes(path, FileAttributes.Normal);
                Directory.Delete(Root, recursive: true);
            }
            catch
            {
            }
        }
    }
}
