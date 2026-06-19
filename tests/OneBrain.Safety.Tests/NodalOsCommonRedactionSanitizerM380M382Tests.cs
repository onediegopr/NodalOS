using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CommonRedaction")]
[TestCategory("Redaction")]
[TestCategory("Sanitizer")]
[TestCategory("RunReport")]
[TestCategory("RecipeManifest")]
[TestCategory("AgentProgressReporting")]
[TestCategory("StepLibrary")]
public sealed class NodalOsCommonRedactionSanitizerM380M382Tests
{
    private static readonly string RepoRoot = AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Debug") ||
                                              AppDomain.CurrentDomain.BaseDirectory.Contains("bin\\Release")
        ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."))
        : AppDomain.CurrentDomain.BaseDirectory;

    private readonly NodalOsSensitiveContentClassifier classifier = new();
    private readonly NodalOsRedactionService redaction = new();

    [TestMethod]
    public void RedactField_RedactsCookie()
    {
        var result = redaction.RedactField("cookie", "session=fake-session-value; Path=/");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
        Assert.IsTrue(result.Matches.Any(match => match.Kind == NodalOsSensitiveContentKind.Cookie));
    }

    [TestMethod]
    public void RedactField_RedactsSetCookie()
    {
        var result = redaction.RedactField("set-cookie", "session=fake-session-value; HttpOnly");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
        Assert.IsTrue(result.Matches.Any(match => match.Kind == NodalOsSensitiveContentKind.SetCookie));
    }

    [TestMethod]
    public void RedactField_RedactsAuthorizationBearer()
    {
        var result = redaction.RedactField("authorization", "Bearer fakeBearerToken1234567890");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
        Assert.IsTrue(result.Matches.Any(match => match.Kind == NodalOsSensitiveContentKind.AuthorizationHeader));
    }

    [TestMethod]
    public void RedactField_RedactsBasicAuth()
    {
        var result = redaction.RedactField("authorization", "Basic ZmFrZTpmYWtlMTIzNDU2");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
        Assert.IsTrue(result.Matches.Any(match => match.Kind == NodalOsSensitiveContentKind.BasicAuth));
    }

    [TestMethod]
    public void RedactField_RedactsPassword()
    {
        var result = redaction.RedactField("password", "fake-password-value");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
        Assert.IsTrue(result.Matches.Any(match => match.Kind == NodalOsSensitiveContentKind.Password));
    }

    [TestMethod]
    public void RedactField_RedactsSecret()
    {
        var result = redaction.RedactField("secret", "fake-secret-value");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
        Assert.IsTrue(result.Matches.Any(match => match.Kind == NodalOsSensitiveContentKind.Secret));
    }

    [TestMethod]
    public void RedactField_RedactsApiKey()
    {
        var result = redaction.RedactField("api_key", "fake-api-key-value");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
        Assert.IsTrue(result.Matches.Any(match => match.Kind == NodalOsSensitiveContentKind.ApiKey));
    }

    [TestMethod]
    public void RedactField_RedactsAccessToken()
    {
        var result = redaction.RedactField("access_token", "fake-access-token-value");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
        Assert.IsTrue(result.Matches.Any(match => match.Kind == NodalOsSensitiveContentKind.AccessToken));
    }

    [TestMethod]
    public void RedactField_RedactsRefreshToken()
    {
        var result = redaction.RedactField("refresh_token", "fake-refresh-token-value");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
        Assert.IsTrue(result.Matches.Any(match => match.Kind == NodalOsSensitiveContentKind.RefreshToken));
    }

    [TestMethod]
    public void RedactField_RedactsIdToken()
    {
        var result = redaction.RedactField("id_token", "fake-id-token-value");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
        Assert.IsTrue(result.Matches.Any(match => match.Kind == NodalOsSensitiveContentKind.IdToken));
    }

    [TestMethod]
    public void RedactValue_RedactsBearerTokenPattern()
    {
        var result = redaction.RedactValue("Authorization: Bearer fakeBearerToken1234567890");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
        Assert.IsTrue(result.Matches.Any(match => match.Kind == NodalOsSensitiveContentKind.BearerToken));
    }

    [TestMethod]
    public void RedactValue_RedactsJwtLikeToken()
    {
        var result = redaction.RedactValue("eyJhbGciOiJub25lIn0.eyJzdWIiOiJmYWtlIn0.signaturefake");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
        Assert.IsTrue(result.Matches.Any(match => match.Kind == NodalOsSensitiveContentKind.JwtLikeToken));
    }

    [TestMethod]
    public void RedactValue_RedactsQueryStringAccessToken()
    {
        var result = redaction.RedactValue("https://example.invalid/callback?access_token=fake-access-token-123&state=ok");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
        Assert.IsTrue(result.Matches.Any(match => match.Kind == NodalOsSensitiveContentKind.GenericToken));
    }

    [TestMethod]
    public void RedactDictionary_PreservesFieldNamesAndRedactsValues()
    {
        var result = redaction.RedactDictionary(new Dictionary<string, string>
        {
            ["authorization"] = "Bearer fakeBearerToken1234567890",
            ["safe-note"] = "public fixture note"
        });

        Assert.IsTrue(result.WasRedacted);
        Assert.IsTrue(result.Values.ContainsKey("authorization"));
        Assert.IsTrue(result.Values.ContainsKey("safe-note"));
        Assert.AreEqual("[REDACTED]", result.Values["authorization"]);
        Assert.AreEqual("public fixture note", result.Values["safe-note"]);
    }

    [TestMethod]
    public void RedactionMatch_DoesNotContainRawSecret()
    {
        var result = redaction.RedactField("authorization", "Bearer fakeRawSecretToken1234567890");
        var serializedMatches = JsonSerializer.Serialize(result.Matches);

        Assert.IsFalse(serializedMatches.Contains("fakeRawSecretToken", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serializedMatches.Contains("1234567890", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Classifier_DoesNotRedactSecretary()
    {
        Assert.IsFalse(classifier.ContainsSensitiveContent("secretary"));
    }

    [TestMethod]
    public void Classifier_DoesNotRedactPassenger()
    {
        Assert.IsFalse(classifier.ContainsSensitiveContent("passenger"));
    }

    [TestMethod]
    public void Classifier_DoesNotRedactTokenizationStrategy()
    {
        Assert.IsFalse(classifier.ContainsSensitiveContent("tokenization strategy"));
    }

    [TestMethod]
    public void Classifier_DoesNotRedactCookiePolicyDocument()
    {
        Assert.IsFalse(classifier.ContainsSensitiveContent("cookie policy document"));
    }

    [TestMethod]
    public void RunReportSanitizer_DelegatesToCommonRedaction()
    {
        Assert.IsFalse(NodalOsRunReportSanitizer.IsSafeValue("Authorization: Bearer fakeBearerToken1234567890"));
        Assert.AreEqual("[REDACTED]", NodalOsRunReportSanitizer.Sanitize("Authorization: Bearer fakeBearerToken1234567890"));
    }

    [TestMethod]
    public void RecipeManifestSanitizerOrValidator_DelegatesToCommonRedaction()
    {
        var manifest = NodalOsRecipeManifestFixtures.ReadOnlyRecipe() with
        {
            EvidenceRequirements = ["run-report", "access_token=fake-access-token-123"]
        };

        var result = new NodalOsRecipeManifestValidator().Validate(manifest);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(string.Join(" ", result.Errors), "secret-like");
    }

    [TestMethod]
    public void AgentProgressReportSanitizer_DelegatesToCommonRedaction()
    {
        var report = new NodalOsAgentProgressReportBuilder().CreateProgress(
            "report-redaction-001",
            "mission-redaction-001",
            "task-redaction-001",
            "Authorization: Bearer fakeBearerToken1234567890",
            [NodalOsAgentProgressReportFixtures.Evidence("evidence-redaction-001")]);

        var sanitized = NodalOsAgentProgressReportSanitizer.Sanitize(report);

        Assert.IsFalse(NodalOsAgentProgressReportSanitizer.IsSafe(report));
        Assert.IsTrue(NodalOsAgentProgressReportSanitizer.IsSafe(sanitized));
        Assert.AreEqual("[REDACTED]", sanitized.Summary);
    }

    [TestMethod]
    public void StepLibrarySanitizer_DelegatesToCommonRedaction()
    {
        Assert.IsTrue(NodalOsStepLibrarySanitizer.ContainsSecretLikeContent("Authorization: Bearer fakeBearerToken1234567890"));
        Assert.AreEqual("[REDACTED]", NodalOsStepLibrarySanitizer.SanitizeLabelOrDescription("Authorization: Bearer fakeBearerToken1234567890"));
    }

    [TestMethod]
    public void ExistingSanitizerBehavior_RemainsCompatible()
    {
        var legacyUnsafeValues = new[]
        {
            "authorization token leaked",
            "bearer fakeBearerToken1234567890",
            "password=fake-password",
            "secret=fake-secret",
            "api_key=fake-key",
            "access_token=fake-access",
            "refresh_token=fake-refresh",
            "id_token=fake-id",
            "set-cookie: session=fake"
        };

        foreach (var value in legacyUnsafeValues)
            Assert.IsFalse(NodalOsRunReportSanitizer.IsSafeValue(value), value);
    }

    [TestMethod]
    public void RedactionResult_SerializesToJson()
    {
        var result = redaction.RedactField("authorization", "Bearer fakeBearerToken1234567890");
        var json = JsonSerializer.Serialize(result);
        var roundTrip = JsonSerializer.Deserialize<NodalOsRedactionResult>(json);

        Assert.IsNotNull(roundTrip);
        Assert.IsTrue(roundTrip.WasRedacted);
        Assert.AreEqual("[REDACTED]", roundTrip.Value);
    }

    [TestMethod]
    public void NoUiOrRuntimeActionsIntroduced()
    {
        var artifactPath = Path.Combine(
            RepoRoot,
            "artifacts",
            "core",
            "m382",
            "common-redaction-sanitizer-summary.json");
        var json = File.ReadAllText(artifactPath);

        StringAssert.Contains(json, "\"noUiImplemented\": true");
        StringAssert.Contains(json, "\"noRuntimeBehaviorChange\": true");
        StringAssert.Contains(json, "\"noRecipeExecutionImplemented\": true");
        StringAssert.Contains(json, "\"noOrchestrationApiImplemented\": true");
    }
}
