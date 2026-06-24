using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("QualityHardening")]
[TestCategory("M885")]
[TestCategory("M886")]
[TestCategory("M887")]
[TestCategory("M888")]
[TestCategory("M889")]
[TestCategory("M890")]
[TestCategory("M891")]
[TestCategory("M892")]
[TestCategory("M893")]
[TestCategory("M894")]
[TestCategory("M895")]
[TestCategory("M896")]
[TestCategory("M885M896")]
public sealed class NodalOsQualityHardeningM885M896Tests
{
    private const string FreezeLockGoNoGo = "artifacts/agent-operations/m873-m884/simulated-runtime-foundation-freeze-lock-go-no-go.json";
    private const string FreezeLockContract = "artifacts/agent-operations/m874/simulated-foundation-freeze-lock-contract.json";
    private const string ReAuditResult = "artifacts/agent-operations/m871/focused-re-audit-result.json";
    private const string AuditQuality = "artifacts/agent-operations/m863-m868/audit-quality-remediation-go-no-go.json";
    private const string GovernanceGoNoGo = "artifacts/agent-operations/m827-m844/simulated-runtime-governance-pre-audit-go-no-go.json";

    private static readonly string[] AllowedPrefixes =
    [
        "tests/OneBrain.Safety.Tests/",
        "docs/reports/",
        "artifacts/agent-operations/"
    ];

    private static readonly string[] ProhibitedPrefixes =
    [
        "browser-extension/",
        "src/",
        "release/",
        "store/",
        "packaging/",
        "provider/",
        "cloud/",
        "filesystem-write/",
        "browser-automation/",
        "capability-unlock/"
    ];

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? Environment.CurrentDirectory;
    }

    private static string ReadAll(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(ReadAll(relativePath));

    [TestMethod]
    public void browser_runtime_smoke_cleanup_diagnosis_created()
    {
        using var doc = ReadJson("artifacts/agent-operations/m885/browser-runtime-smoke-cleanup-diagnosis.json");
        var root = doc.RootElement;

        Assert.AreEqual("BROWSER_RUNTIME_SMOKE_CLEANUP_DIAGNOSIS", root.GetProperty("diagnosisType").GetString());
        Assert.AreEqual("TEST_ONLY_READY", root.GetProperty("status").GetString());
        Assert.AreEqual("BrowserRuntimeSmokeCleanupLeavesNoManagedProcessPortOrProfile", root.GetProperty("failingTest").GetString());
        Assert.IsTrue(root.GetProperty("identifiedCauses").EnumerateArray().Any(x => x.GetString() == "temp directory residual onebrain-cdp-*"));
        Assert.IsFalse(root.GetProperty("productRuntimeUnlock").GetBoolean());
        Assert.IsFalse(root.GetProperty("bridgeCspModification").GetBoolean());
    }

    [TestMethod]
    public void cleanup_guard_or_quarantine_decision_created_and_preserves_visibility()
    {
        using var doc = ReadJson("artifacts/agent-operations/m886/browser-runtime-smoke-cleanup-guard-decision.json");
        var root = doc.RootElement;

        Assert.AreEqual("OPTION_B_EXTERNAL_QUARANTINE_VISIBLE", root.GetProperty("strategy").GetString());
        Assert.AreEqual("BROWSER_RUNTIME_SMOKE_CLEANUP_EXTERNAL_QUARANTINED", root.GetProperty("decision").GetString());
        Assert.IsTrue(root.GetProperty("testVisibilityPreserved").GetBoolean());
        Assert.IsFalse(root.GetProperty("failuresHidden").GetBoolean());
        Assert.IsFalse(root.GetProperty("securityTestsDisabled").GetBoolean());
        Assert.IsTrue(root.GetProperty("markedExternalIfQuarantined").GetBoolean());
    }

    [TestMethod]
    public void prohibited_path_drift_scanner_created_and_accepts_current_allowed_paths()
    {
        using var doc = ReadJson("artifacts/agent-operations/m888/prohibited-path-drift-scanner.json");
        Assert.AreEqual("REAL_PATH_DRIFT_SCANNER_TEST_ONLY", doc.RootElement.GetProperty("scannerType").GetString());

        var changedFiles = CurrentChangedFiles();
        var scan = PathDriftScanner.Scan(changedFiles, AllowedPrefixes, ProhibitedPrefixes);

        Assert.IsFalse(scan.HasProhibitedPaths, string.Join(Environment.NewLine, scan.ProhibitedChangedFiles));
        Assert.IsTrue(scan.AllowedChangedFiles.All(IsAllowedPath), string.Join(Environment.NewLine, scan.AllowedChangedFiles));
    }

    [TestMethod]
    public void path_drift_scanner_rejects_prohibited_paths()
    {
        foreach (var prohibited in new[]
        {
            "browser-extension/onebrain-chrome-lab/manifest.json",
            "src/OneBrain.ChromeLab.Bridge/CspPolicy.cs",
            "src/OneBrain.AgentOperations.Adapters.Browser/ProviderCloudClient.cs",
            "release/chrome-web-store/package.json",
            "provider/cloud/live-client.cs",
            "filesystem-write/real-writer.cs",
            "browser-automation/real-automation.cs",
            "capability-unlock/unlock.cs"
        })
        {
            var scan = PathDriftScanner.Scan([prohibited], AllowedPrefixes, ProhibitedPrefixes);
            Assert.IsTrue(scan.HasProhibitedPaths, prohibited);
        }
    }

    [TestMethod]
    public void freeze_baseline_path_manifest_records_baseline_and_fails_when_prohibited_file_injected()
    {
        using var doc = ReadJson("artifacts/agent-operations/m889/freeze-baseline-path-manifest.json");
        var root = doc.RootElement;

        Assert.AreEqual("1a70db853eb6084115cb1239814e2727bc4bb6da", root.GetProperty("baselineCommit").GetString());
        Assert.IsTrue(root.GetProperty("allowedPathPrefixes").EnumerateArray().Any(x => x.GetString() == "tests/OneBrain.Safety.Tests/"));
        Assert.IsTrue(root.GetProperty("prohibitedPathPrefixes").EnumerateArray().Any(x => x.GetString() == "browser-extension/"));
        Assert.IsFalse(root.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(root.GetProperty("bridgeCspModified").GetBoolean());

        var scan = PathDriftScanner.Scan(["browser-extension/onebrain-chrome-lab/service_worker.js"], AllowedPrefixes, ProhibitedPrefixes);
        Assert.IsTrue(scan.HasProhibitedPaths);
    }

    [TestMethod]
    public void typed_assertion_parses_critical_artifacts()
    {
        AssertGoNoGoArtifact(FreezeLockGoNoGo, "SIMULATED_RUNTIME_FOUNDATION_FREEZE_LOCK_READY_WITH_FLAKY_OR_IO_CAVEAT");
        AssertBooleanFalse(FreezeLockGoNoGo, "productFilesModified");
        AssertBooleanFalse(FreezeLockGoNoGo, "bridgeCspModified");

        using var lockDoc = ReadJson(FreezeLockContract);
        Assert.AreEqual("SIMULATED_FOUNDATION_FREEZE_LOCK_READY", lockDoc.RootElement.GetProperty("lockStatus").GetString());
        Assert.IsFalse(lockDoc.RootElement.GetProperty("productiveRuntime").GetBoolean());
        Assert.IsFalse(lockDoc.RootElement.GetProperty("providerCloud").GetBoolean());
        Assert.IsFalse(lockDoc.RootElement.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(lockDoc.RootElement.GetProperty("bridgeCspModified").GetBoolean());

        using var reaudit = ReadJson(ReAuditResult);
        Assert.AreEqual("REAUDIT_GO_F1_F2_REMEDIATED", reaudit.RootElement.GetProperty("decision").GetString());
        Assert.IsTrue(reaudit.RootElement.GetProperty("freezeLockEligible").GetBoolean());
        Assert.IsFalse(reaudit.RootElement.GetProperty("freezeLockActivated").GetBoolean());

        using var remediation = ReadJson(AuditQuality);
        Assert.AreEqual("AUDIT_QUALITY_REMEDIATION_READY_FOR_REAUDIT", remediation.RootElement.GetProperty("decision").GetString());
        Assert.AreEqual("PASS", remediation.RootElement.GetProperty("validationStatus").GetString());
    }

    [TestMethod]
    public void typed_assertion_rejects_wrong_decision_or_unsafe_booleans()
    {
        var safe = new TypedArtifact("EXPECTED", ProductiveRuntime: false, ProductFilesModified: false, BridgeCspModified: false, ReleaseStoreReady: false);
        Assert.IsTrue(safe.IsValid("EXPECTED"));

        Assert.IsFalse((safe with { Decision = "WRONG" }).IsValid("EXPECTED"));
        Assert.IsFalse((safe with { ProductiveRuntime = true }).IsValid("EXPECTED"));
        Assert.IsFalse((safe with { ProductFilesModified = true }).IsValid("EXPECTED"));
        Assert.IsFalse((safe with { BridgeCspModified = true }).IsValid("EXPECTED"));
        Assert.IsFalse((safe with { ReleaseStoreReady = true }).IsValid("EXPECTED"));
    }

    [TestMethod]
    public void self_referential_json_guard_requires_typed_assertions_for_critical_artifacts()
    {
        using var doc = ReadJson("artifacts/agent-operations/m892/self-referential-json-check-guard.json");
        var root = doc.RootElement;

        Assert.IsTrue(root.GetProperty("criticalArtifactsRequireTypedAssertions").GetBoolean());
        Assert.IsTrue(root.GetProperty("docSmokeStringChecksAllowedAsNonBlocking").GetBoolean());
        Assert.AreEqual("NON_BLOCKING_DOC_SMOKE_ONLY", root.GetProperty("remainingSelfChecksStatus").GetString());
    }

    [TestMethod]
    public void wording_normalization_rejects_product_release_store_and_productive_ready_claims()
    {
        using var doc = ReadJson("artifacts/agent-operations/m893/wording-normalization.json");
        var root = doc.RootElement;

        Assert.AreEqual("TEST_ONLY_READY", root.GetProperty("status").GetString());
        var forbidden = root.GetProperty("forbiddenClaims").EnumerateArray().Select(static x => x.GetString()).ToArray();
        CollectionAssert.Contains(forbidden, "PRODUCT_READY");
        CollectionAssert.Contains(forbidden, "PUBLIC_RELEASE_READY");
        CollectionAssert.Contains(forbidden, "CHROME_WEB_STORE_READY");
        CollectionAssert.Contains(forbidden, "PRODUCTIVE_RUNTIME_READY");
    }

    [TestMethod]
    public void freeze_baseline_consistency_revalidated()
    {
        using var doc = ReadJson("artifacts/agent-operations/m895/freeze-baseline-consistency-revalidation.json");
        var root = doc.RootElement;

        Assert.IsTrue(root.GetProperty("freezeLockStillSimulatedTestOnly").GetBoolean());
        Assert.IsFalse(root.GetProperty("runtimeProductive").GetBoolean());
        Assert.IsFalse(root.GetProperty("providerCloud").GetBoolean());
        Assert.IsFalse(root.GetProperty("filesystemBrowserCapabilityUnlock").GetBoolean());
        Assert.AreEqual("NO-GO", root.GetProperty("releaseStore").GetString());
        Assert.IsFalse(root.GetProperty("productFilesModified").GetBoolean());
        Assert.IsFalse(root.GetProperty("bridgeCspModified").GetBoolean());
        Assert.AreEqual("TEST_ONLY_READY", root.GetProperty("pathDriftScan").GetString());
        Assert.AreEqual("TEST_ONLY_READY", root.GetProperty("typedAssertions").GetString());
        Assert.AreEqual("TEST_ONLY_READY", root.GetProperty("wordingPrecision").GetString());
    }

    private static void AssertGoNoGoArtifact(string relativePath, string expectedDecision)
    {
        using var doc = ReadJson(relativePath);
        Assert.AreEqual(expectedDecision, doc.RootElement.GetProperty("decision").GetString());
    }

    private static void AssertBooleanFalse(string relativePath, string propertyName)
    {
        using var doc = ReadJson(relativePath);
        Assert.IsFalse(doc.RootElement.GetProperty(propertyName).GetBoolean(), propertyName);
    }

    private static string[] CurrentChangedFiles()
    {
        var fromDiff = GitLines("diff --name-only HEAD");
        var fromUntracked = GitLines("ls-files --others --exclude-standard");

        return fromDiff.Concat(fromUntracked)
            .Select(static x => x.Replace('\\', '/'))
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] GitLines(string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = RepoRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        Assert.IsNotNull(process);
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, error);
        return output.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool IsAllowedPath(string path) =>
        AllowedPrefixes.Any(prefix => path.Replace('\\', '/').StartsWith(prefix, StringComparison.Ordinal));

    private sealed record TypedArtifact(
        string Decision,
        bool ProductiveRuntime,
        bool ProductFilesModified,
        bool BridgeCspModified,
        bool ReleaseStoreReady)
    {
        public bool IsValid(string expectedDecision) =>
            Decision == expectedDecision &&
            !ProductiveRuntime &&
            !ProductFilesModified &&
            !BridgeCspModified &&
            !ReleaseStoreReady;
    }

    private static class PathDriftScanner
    {
        public static PathDriftScan Scan(IEnumerable<string> changedFiles, IReadOnlyCollection<string> allowedPrefixes, IReadOnlyCollection<string> prohibitedPrefixes)
        {
            var normalized = changedFiles.Select(static x => x.Replace('\\', '/')).ToArray();
            var prohibited = normalized
                .Where(path =>
                    prohibitedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.Ordinal)) ||
                    path.Contains("Bridge", StringComparison.Ordinal) && path.StartsWith("src/", StringComparison.Ordinal) ||
                    path.Contains("CSP", StringComparison.Ordinal) && path.StartsWith("src/", StringComparison.Ordinal))
                .ToArray();

            var allowed = normalized
                .Where(path => allowedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.Ordinal)))
                .ToArray();

            return new PathDriftScan(allowed, prohibited);
        }
    }

    private sealed record PathDriftScan(string[] AllowedChangedFiles, string[] ProhibitedChangedFiles)
    {
        public bool HasProhibitedPaths => ProhibitedChangedFiles.Length > 0;
    }
}
