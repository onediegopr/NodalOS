using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class ChromeExtensionCompanionM5Tests
{
    private static string ReadServiceWorker()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory, "Repository root was not found.");
        var path = Path.Combine(directory!.FullName, "browser-extension", "onebrain-chrome-lab", "service_worker.js");
        Assert.IsTrue(File.Exists(path), $"Missing service worker at {path}");
        return File.ReadAllText(path);
    }

    [TestMethod]
    public void ServiceWorkerDeclaresCoreGovernedCompanionMode()
    {
        var source = ReadServiceWorker();

        StringAssert.Contains(source, "EXTENSION_RUNTIME_MODE = 'core-governed-companion'");
        StringAssert.Contains(source, "CORE_GOVERNED_MODE = true");
        StringAssert.Contains(source, "LEGACY_RUNNER_ENABLED = false");
        StringAssert.Contains(source, "companionMode: true");
        StringAssert.Contains(source, "relayMode: true");
        StringAssert.Contains(source, "serviceWorkerRunOwner: false");
        StringAssert.Contains(source, "canVerifyFinalSuccess: false");
        StringAssert.Contains(source, "contentScriptAuthoritative: false");
    }

    [TestMethod]
    public void ExtensionHelloAdvertisesCompanionCapabilities()
    {
        var source = ReadServiceWorker();

        StringAssert.Contains(source, "type: 'extension.hello'");
        StringAssert.Contains(source, "runtimeKind: EXTENSION_RUNTIME_MODE");
        StringAssert.Contains(source, "capabilities: EXTENSION_CAPABILITIES");
    }

    [TestMethod]
    public void LegacyRecipeRunnerIsDisabledByDefault()
    {
        var source = ReadServiceWorker();

        StringAssert.Contains(source, "if (!LEGACY_RUNNER_ENABLED)");
        StringAssert.Contains(source, "errorCode: 'legacy_runner_disabled'");
        StringAssert.Contains(source, "The extension companion cannot own or complete runs.");
        StringAssert.Contains(source, "recipeRunner = LEGACY_RUNNER_ENABLED ? (saved.recipeRunner || recipeRunner) : null");
        StringAssert.Contains(source, "recipeRunner: LEGACY_RUNNER_ENABLED ? recipeRunner : null");
    }

    [TestMethod]
    public void ToolResultsAreRelayOnlyAndNotVerified()
    {
        var source = ReadServiceWorker();

        StringAssert.Contains(source, "type: 'tool.result'");
        StringAssert.Contains(source, "actionId: request.actionId || request.requestId || ''");
        StringAssert.Contains(source, "correlationId: request.correlationId || request.requestId || request.runId || ''");
        StringAssert.Contains(source, "runtimeKind: EXTENSION_RUNTIME_MODE");
        StringAssert.Contains(source, "source: 'extension-relay'");
        StringAssert.Contains(source, "authoritative: false");
        StringAssert.Contains(source, "verificationStatus: 'NotVerified'");
        StringAssert.Contains(source, "evidenceRefs: []");
    }

    [TestMethod]
    public void CompletedRunStatusRequiresVerifiedEvidence()
    {
        var source = ReadServiceWorker();

        StringAssert.Contains(source, "normalizeCoreRunStatus(message)");
        StringAssert.Contains(source, "isVerificationStronglyVerified(message)");
        StringAssert.Contains(source, "status: 'uncertain'");
        StringAssert.Contains(source, "verificationStatus: message.verificationStatus || 'Uncertain'");
        StringAssert.Contains(source, "errorCode: message.errorCode || 'verification_required'");
    }

    [TestMethod]
    public void RuntimeSnapshotExposesCoreGovernedStatus()
    {
        var source = ReadServiceWorker();

        StringAssert.Contains(source, "runtimeKind: EXTENSION_RUNTIME_MODE");
        StringAssert.Contains(source, "coreGovernedMode: CORE_GOVERNED_MODE");
        StringAssert.Contains(source, "capabilities: EXTENSION_CAPABILITIES");
    }
}
