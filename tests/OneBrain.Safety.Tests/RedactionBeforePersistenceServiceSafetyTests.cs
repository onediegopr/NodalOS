using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class RedactionBeforePersistenceServiceSafetyTests
{
    [TestMethod]
    public void RedactionService_RejectsSensitiveCorpusBeforePersistenceWithoutRawLeak()
    {
        var service = new RedactionBeforePersistenceService();
        var sensitiveCases = new Dictionary<string, DurableAuditTrailAppendOnlyMinimalRequest>
        {
            ["secret"] = Request() with { Metadata = new Dictionary<string, string> { ["note"] = "Authorization: Bearer live-token" } },
            ["secret-whitespace"] = Request() with { Metadata = new Dictionary<string, string> { ["note"] = "PASSWORD : spaced-secret" } },
            ["api-key-whitespace"] = Request() with { Metadata = new Dictionary<string, string> { ["note"] = "api key = spaced-secret" } },
            ["email"] = Request() with { Metadata = new Dictionary<string, string> { ["reviewer"] = "person@example.com" } },
            ["email-uppercase"] = Request() with { Metadata = new Dictionary<string, string> { ["reviewer"] = "PERSON+ALIAS@EXAMPLE.COM" } },
            ["windows-path"] = Request() with { EvidenceReferences = [@"C:\Users\person\Documents\private.txt"] },
            ["unc-path"] = Request() with { EvidenceReferences = [@"\\server\share\private.txt"] },
            ["unc-path-leading-space"] = Request() with { EvidenceReferences = [@"  \\server\share\private.txt"] },
            ["raw-payload"] = Request() with { RawPayload = "{\"token\":\"raw-secret\"}" }
        };

        foreach (var testCase in sensitiveCases)
        {
            var result = service.Evaluate(RedactionBeforePersistencePolicy.TestOnly, testCase.Value);
            var rendered = result.ToString();

            Assert.AreEqual(RedactionBeforePersistenceDecision.Rejected, result.Decision, testCase.Key);
            Assert.IsFalse(result.Succeeded, testCase.Key);
            Assert.IsNull(result.SafeRequest, testCase.Key);
            Assert.IsTrue(result.Reasons.Count > 0, testCase.Key);
            Assert.IsFalse(rendered.Contains("live-token", StringComparison.Ordinal), testCase.Key);
            Assert.IsFalse(rendered.Contains("person@example.com", StringComparison.Ordinal), testCase.Key);
            Assert.IsFalse(rendered.Contains("PERSON+ALIAS@EXAMPLE.COM", StringComparison.Ordinal), testCase.Key);
            Assert.IsFalse(rendered.Contains(@"C:\Users\person", StringComparison.Ordinal), testCase.Key);
            Assert.IsFalse(rendered.Contains(@"\\server\share", StringComparison.Ordinal), testCase.Key);
            Assert.IsFalse(rendered.Contains("raw-secret", StringComparison.Ordinal), testCase.Key);
            Assert.IsFalse(rendered.Contains("spaced-secret", StringComparison.Ordinal), testCase.Key);
            Assert.IsTrue(result.Evidence.CompletedBeforePersistence, testCase.Key);
            Assert.IsFalse(result.Evidence.ContainsRawValues, testCase.Key);
        }
    }

    [TestMethod]
    public void RedactionService_FailsClosedForMissingPolicyMalformedMetadataAndReferences()
    {
        var service = new RedactionBeforePersistenceService();
        var malformedCases = new[]
        {
            service.Evaluate(null!, Request()),
            service.Evaluate(new RedactionBeforePersistencePolicy("", "v1"), Request()),
            service.Evaluate(new RedactionBeforePersistencePolicy("redaction-before-persistence.test-only", "unknown"), Request()),
            service.Evaluate(RedactionBeforePersistencePolicy.TestOnly, null!),
            service.Evaluate(RedactionBeforePersistencePolicy.TestOnly, Request() with { Metadata = null }),
            service.Evaluate(RedactionBeforePersistencePolicy.TestOnly, Request() with { Metadata = new Dictionary<string, string> { [""] = "safe" } }),
            service.Evaluate(RedactionBeforePersistencePolicy.TestOnly, Request() with { Metadata = new Dictionary<string, string> { ["safe"] = null! } }),
            service.Evaluate(RedactionBeforePersistencePolicy.TestOnly, Request() with { EvidenceReferences = null! }),
            service.Evaluate(RedactionBeforePersistencePolicy.TestOnly, Request() with { EvidenceReferences = [] }),
            service.Evaluate(RedactionBeforePersistencePolicy.TestOnly, Request() with { EvidenceReferences = [null!] }),
            service.Evaluate(RedactionBeforePersistencePolicy.TestOnly, Request() with { EvidenceReferences = ["https://example.invalid/evidence"] })
        };

        foreach (var result in malformedCases)
        {
            Assert.AreEqual(RedactionBeforePersistenceDecision.Rejected, result.Decision);
            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.SafeRequest);
            Assert.IsTrue(result.Reasons.Count > 0);
            Assert.IsFalse(result.Evidence.ContainsRawValues);
        }
    }

    [TestMethod]
    public void RedactionService_AllowsSafeCandidateWithDeterministicEvidence()
    {
        var service = new RedactionBeforePersistenceService();
        var request = Request();

        var first = service.Evaluate(RedactionBeforePersistencePolicy.TestOnly, request);
        var second = service.Evaluate(RedactionBeforePersistencePolicy.TestOnly, request);

        Assert.AreEqual(RedactionBeforePersistenceDecision.Allowed, first.Decision);
        Assert.IsTrue(first.Succeeded);
        Assert.IsNotNull(first.SafeRequest);
        Assert.AreEqual(request.EventKind, first.SafeRequest!.EventKind);
        Assert.AreEqual(request.ActorReference, first.SafeRequest.ActorReference);
        Assert.AreEqual(request.ApprovalReference, first.SafeRequest.ApprovalReference);
        CollectionAssert.AreEqual(request.EvidenceReferences.ToArray(), first.SafeRequest.EvidenceReferences.ToArray());
        CollectionAssert.AreEquivalent(request.Metadata!.ToArray(), first.SafeRequest.Metadata!.ToArray());
        Assert.IsNull(first.SafeRequest.RawPayload);
        Assert.AreEqual(first.Evidence.CandidateHash, second.Evidence.CandidateHash);
        Assert.AreEqual("redaction-before-persistence.test-only", first.Evidence.PolicyId);
        Assert.AreEqual("v1", first.Evidence.PolicyVersion);
        Assert.IsTrue(first.Evidence.CompletedBeforePersistence);
        Assert.IsFalse(first.Evidence.ContainsRawValues);
    }

    [TestMethod]
    public void RedactionService_PropertyCorpusRejectsSensitiveVariantsInEveryCandidateField()
    {
        var service = new RedactionBeforePersistenceService();
        var sensitiveSamples = new[]
        {
            "Token : variant-secret",
            "SECRET=variant-secret",
            "api-key = variant-secret",
            "Authorization: Bearer variant-secret",
            "fixture.person+alias@example.com",
            @"d:\Users\person\Secrets\approval.txt",
            @"\\server\share\Secrets\approval.txt"
        };

        foreach (var sample in sensitiveSamples)
        {
            var candidates = new Dictionary<string, DurableAuditTrailAppendOnlyMinimalRequest>
            {
                ["actor"] = Request() with { ActorReference = sample },
                ["approval"] = Request() with { ApprovalReference = sample },
                ["evidence"] = Request() with { EvidenceReferences = [sample] },
                ["metadata-key"] = Request() with { Metadata = new Dictionary<string, string> { [sample] = "safe" } },
                ["metadata-value"] = Request() with { Metadata = new Dictionary<string, string> { ["note"] = sample } }
            };

            foreach (var candidate in candidates)
            {
                var result = service.Evaluate(RedactionBeforePersistencePolicy.TestOnly, candidate.Value);
                var rendered = result.ToString();

                Assert.AreEqual(RedactionBeforePersistenceDecision.Rejected, result.Decision, $"{sample}:{candidate.Key}");
                Assert.IsFalse(result.Succeeded, $"{sample}:{candidate.Key}");
                Assert.IsNull(result.SafeRequest, $"{sample}:{candidate.Key}");
                Assert.IsTrue(result.Reasons.Count > 0, $"{sample}:{candidate.Key}");
                Assert.IsFalse(rendered.Contains("variant-secret", StringComparison.Ordinal), $"{sample}:{candidate.Key}");
                Assert.IsFalse(rendered.Contains("fixture.person+alias@example.com", StringComparison.Ordinal), $"{sample}:{candidate.Key}");
                Assert.IsFalse(rendered.Contains("Secrets", StringComparison.Ordinal), $"{sample}:{candidate.Key}");
                Assert.IsFalse(result.Evidence.ContainsRawValues, $"{sample}:{candidate.Key}");
            }
        }
    }

    [TestMethod]
    public void RedactionService_PropertyCorpusAllowsBenignControlText()
    {
        var service = new RedactionBeforePersistenceService();
        var safeControls = new[]
        {
            "approval-tokenization-note",
            "api key rotation planned without value",
            "person at example dot com",
            "docs/qa/redaction-before-persistence/report.md",
            "operator reviewed fixture"
        };

        foreach (var sample in safeControls)
        {
            var result = service.Evaluate(
                RedactionBeforePersistencePolicy.TestOnly,
                Request() with
                {
                    EvidenceReferences = ["docs/qa/redaction-before-persistence/report.md"],
                    Metadata = new Dictionary<string, string>
                    {
                        ["note"] = sample
                    }
                });

            Assert.AreEqual(RedactionBeforePersistenceDecision.Allowed, result.Decision, sample);
            Assert.IsTrue(result.Succeeded, sample);
            Assert.IsNotNull(result.SafeRequest, sample);
            Assert.IsFalse(result.Evidence.ContainsRawValues, sample);
        }
    }

    [TestMethod]
    public void RedactionService_SourceContainsNoRuntimeRegistrationHandlersOrExternalProviders()
    {
        var sourcePath = System.IO.Path.Combine(
            FindRepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "RedactionBeforePersistenceService.cs");
        var source = File.ReadAllText(sourcePath);
        var forbiddenFragments = new[]
        {
            "AddSingleton",
            "AddScoped",
            "AddTransient",
            "IHostedService",
            "MapPost",
            "MapGet",
            "AddCommandHandler",
            "ICommandHandler",
            "ICommand",
            "RunProductAction",
            "ProductActionButton",
            "HttpClient",
            "WebSocket",
            "DbContext",
            "MigrationBuilder",
            "SaveChanges",
            "Browser",
            "CDP",
            "WCU",
            "OCR",
            "RecipeExecution",
            "ReleaseReady = true",
            "CommercialReady = true",
            "ReleaseCommercialReady: true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }
    }

    private static DurableAuditTrailAppendOnlyMinimalRequest Request() =>
        new(
            EventKind: DurableAuditTrailAppendOnlyMinimal.SupportedEventKind,
            ActorReference: "human-operator:fixture",
            ApprovalReference: "approval-001",
            EvidenceReferences:
            [
                "docs/qa/durable-audit-trail-fixture/report.md"
            ],
            Metadata: new Dictionary<string, string>
            {
                ["decision"] = "approved-for-minimal-append-only-test"
            });

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(System.IO.Path.Combine(
                directory.FullName,
                "src",
                "OneBrain.Core",
                "Approval",
                "RedactionBeforePersistenceService.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
