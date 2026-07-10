using System.Net;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.ChromeLab.Bridge;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class ChromeLabBridgeSecurityTests
{
    [TestMethod]
    public void TokenAuthenticationAcceptsHeaderAndBearerAndRejectsWrongToken()
    {
        var options = BridgeOptions();
        var headerContext = Context("/debug");
        headerContext.Request.Headers[ChromeLabBridgeSecurity.TokenHeaderName] = "test-token";
        Assert.IsTrue(ChromeLabBridgeSecurity.IsAuthorized(headerContext, options));

        var bearerContext = Context("/debug");
        bearerContext.Request.Headers.Authorization = "Bearer test-token";
        Assert.IsTrue(ChromeLabBridgeSecurity.IsAuthorized(bearerContext, options));

        var rejectedContext = Context("/debug");
        rejectedContext.Request.Headers[ChromeLabBridgeSecurity.TokenHeaderName] = "wrong-token";
        Assert.IsFalse(ChromeLabBridgeSecurity.IsAuthorized(rejectedContext, options));
    }

    [TestMethod]
    public void OriginPolicyUsesExactHostsPortsAndConfiguredExtensionIds()
    {
        var original = Environment.GetEnvironmentVariable(
            ChromeLabBridgeSecurity.AllowedExtensionIdsEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(
                ChromeLabBridgeSecurity.AllowedExtensionIdsEnvironmentVariable,
                "abcdefghijklmnopabcdefghijklmnop");

            var options = BridgeOptions();
            Assert.IsTrue(ChromeLabBridgeSecurity.IsOriginAllowed("http://localhost:8787", options));
            Assert.IsTrue(ChromeLabBridgeSecurity.IsOriginAllowed("http://127.0.0.1:8787", options));
            Assert.IsFalse(ChromeLabBridgeSecurity.IsOriginAllowed("http://localhost.evil:8787", options));
            Assert.IsFalse(ChromeLabBridgeSecurity.IsOriginAllowed("http://localhost:8788", options));
            Assert.IsTrue(ChromeLabBridgeSecurity.IsOriginAllowed(
                "chrome-extension://abcdefghijklmnopabcdefghijklmnop", options));
            Assert.IsFalse(ChromeLabBridgeSecurity.IsOriginAllowed(
                "chrome-extension://ponmlkjihgfedcbaponmlkjihgfedcba", options));
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                ChromeLabBridgeSecurity.AllowedExtensionIdsEnvironmentVariable,
                original);
        }
    }

    [TestMethod]
    public void OriginPolicyAllowsPrivateLanOnlyWhenExplicitlyEnabled()
    {
        Assert.IsFalse(ChromeLabBridgeSecurity.IsOriginAllowed(
            "http://192.168.1.20:8787", BridgeOptions()));
        Assert.IsTrue(ChromeLabBridgeSecurity.IsOriginAllowed(
            "http://192.168.1.20:8787", BridgeOptions(allowLan: true)));
        Assert.IsFalse(ChromeLabBridgeSecurity.IsOriginAllowed(
            "http://8.8.8.8:8787", BridgeOptions(allowLan: true)));
    }

    [TestMethod]
    public void MiddlewareReplacesThePermissiveProgramCorsPolicyWithExactOriginRules()
    {
        var original = Environment.GetEnvironmentVariable(
            ChromeLabBridgeSecurity.AllowedExtensionIdsEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(
                ChromeLabBridgeSecurity.AllowedExtensionIdsEnvironmentVariable,
                "abcdefghijklmnopabcdefghijklmnop");
            var corsOptions = new CorsOptions();
            corsOptions.AddDefaultPolicy(policy => policy.SetIsOriginAllowed(_ => true));
            _ = Middleware(_ => Task.CompletedTask, BridgeOptions(), corsOptions);

            var policy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);
            Assert.IsNotNull(policy);
            Assert.IsTrue(policy.IsOriginAllowed("http://localhost:8787"));
            Assert.IsFalse(policy.IsOriginAllowed("http://localhost.evil:8787"));
            Assert.IsTrue(policy.IsOriginAllowed(
                "chrome-extension://abcdefghijklmnopabcdefghijklmnop"));
            Assert.IsFalse(policy.IsOriginAllowed(
                "chrome-extension://ponmlkjihgfedcbaponmlkjihgfedcba"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                ChromeLabBridgeSecurity.AllowedExtensionIdsEnvironmentVariable,
                original);
        }
    }

    [TestMethod]
    public async Task MiddlewareRejectsUnauthenticatedControlEndpointAndAcceptsValidToken()
    {
        var nextCalled = false;
        var options = BridgeOptions();
        var middleware = Middleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        var rejected = Context("/debug");
        await middleware.InvokeAsync(rejected, options);
        Assert.AreEqual(StatusCodes.Status401Unauthorized, rejected.Response.StatusCode);
        Assert.IsFalse(nextCalled);
        Assert.AreEqual("no-store", rejected.Response.Headers.CacheControl.ToString());

        var accepted = Context("/debug");
        accepted.Request.Headers[ChromeLabBridgeSecurity.TokenHeaderName] = "test-token";
        await middleware.InvokeAsync(accepted, options);
        Assert.IsTrue(nextCalled);
    }

    [TestMethod]
    public async Task MiddlewareKeepsStealthAndPairingDisabledByDefault()
    {
        var nextCalled = false;
        var options = BridgeOptions();
        var middleware = Middleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        var stealth = Context("/ws/stealth");
        stealth.Request.Headers[ChromeLabBridgeSecurity.TokenHeaderName] = "test-token";
        await middleware.InvokeAsync(stealth, options);
        Assert.AreEqual(StatusCodes.Status401Unauthorized, stealth.Response.StatusCode);
        Assert.IsFalse(nextCalled);

        var original = Environment.GetEnvironmentVariable(
            ChromeLabBridgeSecurity.EnableLocalPairingEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(
                ChromeLabBridgeSecurity.EnableLocalPairingEnvironmentVariable,
                null);
            var pairing = Context("/pairing/local-token");
            await middleware.InvokeAsync(pairing, options);
            Assert.AreEqual(StatusCodes.Status404NotFound, pairing.Response.StatusCode);
            Assert.IsFalse(nextCalled);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                ChromeLabBridgeSecurity.EnableLocalPairingEnvironmentVariable,
                original);
        }
    }

    [TestMethod]
    public void FatalProtocolErrorsCloseAuthenticationFailuresOnly()
    {
        Assert.IsTrue(ChromeLabBridgeSecurity.IsFatalProtocolError(
            "{\"type\":\"protocol.error\",\"error\":\"authentication_required\"}"));
        Assert.IsTrue(ChromeLabBridgeSecurity.IsFatalProtocolError(
            "{\"type\":\"protocol.error\",\"error\":\"invalid_token\"}"));
        Assert.IsFalse(ChromeLabBridgeSecurity.IsFatalProtocolError(
            "{\"type\":\"protocol.error\",\"error\":\"malformed_json\"}"));
    }

    private static ChromeLabBridgeSecurityMiddleware Middleware(
        RequestDelegate next,
        ChromeLabOptions options,
        CorsOptions? corsOptions = null) =>
        new(
            next,
            Microsoft.Extensions.Options.Options.Create(corsOptions ?? new CorsOptions()),
            options);

    private static ChromeLabOptions BridgeOptions(bool allowLan = false, bool stealthEnabled = false) =>
        new()
        {
            Host = "127.0.0.1",
            Port = 8787,
            ConnectionToken = "test-token",
            AllowLan = allowLan,
            StealthEnabled = stealthEnabled
        };

    private static DefaultHttpContext Context(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = HttpMethods.Get;
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        return context;
    }
}
