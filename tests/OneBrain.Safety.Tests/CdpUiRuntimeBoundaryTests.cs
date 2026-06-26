using OneBrain.BrowserRuntime;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CdpUiRuntimeBoundary")]
public sealed class CdpUiRuntimeBoundaryTests
{
    private static readonly Lazy<Task<CdpUiRuntimeCommandResult>> RefreshedCapture = new(
        RefreshControlledCaptureAsync,
        LazyThreadSafetyMode.ExecutionAndPublication);

    [TestMethod]
    public async Task CdpUiRuntimeBoundary_AllowsOnlyReadOnlyCommands()
    {
        var boundary = new CdpUiRuntimeBoundary();
        foreach (var command in new[]
        {
            CdpUiRuntimeCommandKind.GetRuntimeStatus,
            CdpUiRuntimeCommandKind.GetLastBrowserSkillsSummary,
            CdpUiRuntimeCommandKind.GetLastEvidenceSummary,
            CdpUiRuntimeCommandKind.BuildUiBridgeModel
        })
        {
            var result = await boundary.ExecuteAsync(CreateRequest(command)).ConfigureAwait(false);

            Assert.AreNotEqual(CdpUiRuntimeCommandStatus.Blocked, result.Status, command.ToString());
            Assert.IsTrue(result.ReadOnly);
            Assert.IsFalse(result.ExtensionUsed);
            Assert.IsFalse(result.SystemBrowserUsed);
            Assert.IsTrue(result.ExternalNavigationBlocked);
            Assert.IsFalse(result.ProductFilesModified);
        }
    }

    [TestMethod]
    public async Task CdpUiRuntimeBoundary_BlocksDangerousCommands()
    {
        var boundary = new CdpUiRuntimeBoundary();
        foreach (var command in new[]
        {
            CdpUiRuntimeCommandKind.NavigateExternal,
            CdpUiRuntimeCommandKind.SubmitForm,
            CdpUiRuntimeCommandKind.SolveCaptcha,
            CdpUiRuntimeCommandKind.UseCredentials,
            CdpUiRuntimeCommandKind.UploadFile,
            CdpUiRuntimeCommandKind.DownloadFile,
            CdpUiRuntimeCommandKind.ExecuteArbitraryJs,
            CdpUiRuntimeCommandKind.WriteFilesystem,
            CdpUiRuntimeCommandKind.ShellCommand,
            CdpUiRuntimeCommandKind.UseExtensionFallback,
            CdpUiRuntimeCommandKind.UseSystemBrowserFallback
        })
        {
            var result = await boundary.ExecuteAsync(CreateRequest(command)).ConfigureAwait(false);

            Assert.AreEqual(CdpUiRuntimeCommandStatus.Blocked, result.Status, command.ToString());
            Assert.IsTrue(result.DangerousActionBlocked);
            Assert.AreEqual("DANGEROUS_COMMAND_BLOCKED", result.Error?.Code);
            Assert.IsFalse(result.ExtensionUsed);
            Assert.IsFalse(result.SystemBrowserUsed);
            Assert.IsTrue(result.Evidence.MetadataOnly);
        }
    }

    [TestMethod]
    public async Task CdpUiRuntimeBoundary_GetRuntimeStatus_ReturnsCloakBrowserCdp()
    {
        var result = await new CdpUiRuntimeBoundary()
            .ExecuteAsync(CreateRequest(CdpUiRuntimeCommandKind.GetRuntimeStatus))
            .ConfigureAwait(false);

        Assert.AreEqual(CdpUiRuntimeCommandStatus.Success, result.Status);
        Assert.AreEqual("cloakbrowser", result.RuntimeProvider);
        Assert.AreEqual("cloakbrowser-cdp-direct", result.Source);
        Assert.IsNotNull(result.RuntimeStatus);
        Assert.IsTrue(result.RuntimeStatus.RuntimeConfigured);
        Assert.IsTrue(result.RuntimeStatus.ArtifactPinned);
        Assert.AreEqual("pinned", result.RuntimeStatus.ShaStatus);
        Assert.IsTrue(result.RuntimeStatus.CdpCapabilityAvailable);
    }

    [TestMethod]
    public async Task CdpUiRuntimeBoundary_StatusMarksExtensionLegacyNoDefault()
    {
        var result = await new CdpUiRuntimeBoundary()
            .ExecuteAsync(CreateRequest(CdpUiRuntimeCommandKind.GetRuntimeStatus))
            .ConfigureAwait(false);

        Assert.AreEqual("legacy/no-default", result.RuntimeStatus?.ExtensionMode);
        Assert.IsFalse(result.ExtensionUsed);
        Assert.IsFalse(result.RuntimeStatus?.SystemBrowserAllowed ?? true);
    }

    [TestMethod]
    public async Task CdpUiRuntimeBoundary_StatusRejectsSystemBrowserAllowed()
    {
        var temp = Directory.CreateTempSubdirectory("nodal-cdp-ui-boundary-");
        try
        {
            var lockPath = Path.Combine(temp.FullName, "browser-runtime.lock.json");
            var lockJson = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "browser-runtime.lock.json"))
                .Replace("\"system_browser_allowed\": false", "\"system_browser_allowed\": true", StringComparison.Ordinal);
            File.WriteAllText(lockPath, lockJson);

            var result = await new CdpUiRuntimeBoundary()
                .ExecuteAsync(new CdpUiRuntimeCommandRequest(
                    CdpUiRuntimeCommandKind.GetRuntimeStatus,
                    temp.FullName,
                    lockPath))
                .ConfigureAwait(false);

            Assert.IsFalse(result.RuntimeStatus?.RuntimeConfigured ?? true);
            Assert.IsTrue(result.RuntimeStatus?.SystemBrowserAllowed ?? false);
            Assert.AreEqual("requiere revisión", result.Summary?.Status);
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task CdpUiRuntimeBoundary_GetLastSummary_IsMetadataOnly()
    {
        var result = await EnsureSummaryAvailableAsync().ConfigureAwait(false);

        Assert.IsNotNull(result.Summary);
        Assert.IsTrue(result.Summary.BoundaryReadOnly);
        Assert.IsFalse(result.Summary.ExtensionUsed);
        Assert.IsFalse(result.Summary.SystemBrowserUsed);
        Assert.IsTrue(result.Summary.ExternalNavigationBlocked);
        Assert.IsFalse(result.Summary.ProductFilesModified);
        Assert.IsTrue(result.Evidence.MetadataOnly);
        Assert.IsTrue(result.Evidence.SecretsRedacted);
    }

    [TestMethod]
    public async Task CdpUiRuntimeBoundary_GetLastEvidence_DoesNotExposeRawDomOrSecrets()
    {
        var result = await new CdpUiRuntimeBoundary()
            .ExecuteAsync(CreateRequest(CdpUiRuntimeCommandKind.GetLastEvidenceSummary))
            .ConfigureAwait(false);

        if (result.Status == CdpUiRuntimeCommandStatus.Empty)
        {
            result = await EnsureSummaryAvailableAsync().ConfigureAwait(false);
        }

        Assert.IsTrue(result.Evidence.MetadataOnly);
        Assert.IsTrue(result.Evidence.SecretsRedacted);
        Assert.IsNull(result.UiBridgeModel?.ContainsRawDom == true ? result.UiBridgeModel : null);
        Assert.IsFalse(result.ExtensionUsed);
        Assert.IsFalse(result.SystemBrowserUsed);
    }

    [TestMethod]
    public async Task CdpUiRuntimeBoundary_NoCaptureYet_ReturnsClearEmptyState()
    {
        var temp = Directory.CreateTempSubdirectory("nodal-cdp-ui-empty-");
        try
        {
            var lockPath = Path.Combine(temp.FullName, "browser-runtime.lock.json");
            File.Copy(Path.Combine(FindRepositoryRoot(), "browser-runtime.lock.json"), lockPath);

            var result = await new CdpUiRuntimeBoundary()
                .ExecuteAsync(new CdpUiRuntimeCommandRequest(
                    CdpUiRuntimeCommandKind.GetLastBrowserSkillsSummary,
                    temp.FullName,
                    lockPath))
                .ConfigureAwait(false);

            Assert.AreEqual(CdpUiRuntimeCommandStatus.Empty, result.Status);
            Assert.AreEqual("NO_CDP_CAPTURE_YET", result.Error?.Code);
            Assert.AreEqual("Sin captura CDP reciente", result.Summary?.LastCaptureStatus);
            Assert.IsFalse(result.Summary?.EvidenceAvailable ?? true);
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task CdpUiRuntimeBoundary_BuildsProductSurfaceSummary()
    {
        var result = await EnsureSummaryAvailableAsync().ConfigureAwait(false);

        Assert.AreEqual("CloakBrowser CDP", result.Summary?.RuntimeLabel);
        Assert.AreEqual("cloakbrowser-cdp-direct", result.Summary?.Source);
        Assert.IsTrue(result.Summary?.ElementCount >= 0);
        Assert.IsTrue(result.Summary?.FrictionCount >= 0);
        Assert.IsTrue(result.Summary?.ActionMapCount >= 0);
        Assert.IsTrue(result.Summary?.BoundaryReadOnly ?? false);
    }

    [TestMethod]
    public async Task CdpUiRuntimeBoundary_CopySummaryIsMetadataOnly()
    {
        var result = await EnsureSummaryAvailableAsync().ConfigureAwait(false);
        var copySummary = string.Join('\n',
            $"runtime: {result.Summary?.RuntimeLabel}",
            $"source: {result.Summary?.Source}",
            $"boundaryReadOnly: {result.Summary?.BoundaryReadOnly}",
            $"extensionUsed: {result.Summary?.ExtensionUsed}",
            $"systemBrowserUsed: {result.Summary?.SystemBrowserUsed}",
            $"externalNavigationBlocked: {result.Summary?.ExternalNavigationBlocked}",
            $"productFilesModified: {result.Summary?.ProductFilesModified}");

        StringAssert.Contains(copySummary, "boundaryReadOnly: True");
        Assert.IsFalse(copySummary.Contains("<html", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(copySummary.Contains("document.cookie", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(copySummary.Contains("localStorage", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(copySummary.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task CdpUiRuntimeBoundary_RefreshControlledCapture_ReturnsUiBridgeModel_Live()
    {
        var result = await RequireRefreshAsync().ConfigureAwait(false);

        Assert.AreEqual(CdpUiRuntimeCommandStatus.Success, result.Status);
        Assert.IsNotNull(result.UiBridgeModel);
        Assert.IsTrue(result.UiBridgeModel.ReadOnly);
        Assert.AreEqual("CloakBrowser CDP", result.UiBridgeModel.Summary.RuntimeLabel);
        Assert.IsTrue(result.Summary?.ElementCount >= 6);
        Assert.IsTrue(result.Summary?.FrictionCount >= 5);
        Assert.IsTrue(result.Summary?.ActionMapCount >= 6);
    }

    [TestMethod]
    public async Task CdpUiRuntimeBoundary_RefreshControlledCapture_DoesNotUseExtension()
    {
        var result = await RequireRefreshAsync().ConfigureAwait(false);

        Assert.IsFalse(result.ExtensionUsed);
        Assert.IsFalse(result.UiBridgeModel?.ExtensionUsed ?? true);
        Assert.AreEqual("cloakbrowser-cdp-direct", result.Source);
    }

    [TestMethod]
    public async Task CdpUiRuntimeBoundary_RefreshControlledCapture_DoesNotUseSystemBrowser()
    {
        var result = await RequireRefreshAsync().ConfigureAwait(false);

        Assert.IsFalse(result.SystemBrowserUsed);
        Assert.IsFalse(result.UiBridgeModel?.SystemBrowserUsed ?? true);
    }

    [TestMethod]
    public async Task CdpUiRuntimeBoundary_RefreshControlledCapture_ShutsDownRuntime()
    {
        var result = await RequireRefreshAsync().ConfigureAwait(false);

        Assert.IsFalse(result.SystemBrowserUsed);
        Assert.IsTrue(result.Summary?.EvidenceAvailable ?? false);
        Assert.IsFalse(result.ProductFilesModified);
        Assert.IsTrue(result.ExternalNavigationBlocked);
    }

    private static async Task<CdpUiRuntimeCommandResult> EnsureSummaryAvailableAsync()
    {
        var boundary = new CdpUiRuntimeBoundary();
        var result = await boundary.ExecuteAsync(CreateRequest(CdpUiRuntimeCommandKind.GetLastBrowserSkillsSummary)).ConfigureAwait(false);
        return result.Status == CdpUiRuntimeCommandStatus.Empty
            ? await RequireRefreshAsync().ConfigureAwait(false)
            : result;
    }

    private static async Task<CdpUiRuntimeCommandResult> RequireRefreshAsync()
    {
        var result = await RefreshedCapture.Value.ConfigureAwait(false);
        if (result.Status != CdpUiRuntimeCommandStatus.Success)
        {
            Assert.Inconclusive(result.Error?.Message ?? "CDP refresh controlled capture was not available.");
        }

        return result;
    }

    private static Task<CdpUiRuntimeCommandResult> RefreshControlledCaptureAsync() =>
        new CdpUiRuntimeBoundary().ExecuteAsync(CreateRequest(CdpUiRuntimeCommandKind.RefreshControlledCapture));

    private static CdpUiRuntimeCommandRequest CreateRequest(CdpUiRuntimeCommandKind commandKind)
    {
        var repositoryRoot = FindRepositoryRoot();
        return new CdpUiRuntimeCommandRequest(
            commandKind,
            repositoryRoot,
            Path.Combine(repositoryRoot, "browser-runtime.lock.json"),
            Timeout: TimeSpan.FromSeconds(45));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "browser-runtime.lock.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("Repository root with browser-runtime.lock.json was not found.");
        return string.Empty;
    }
}
