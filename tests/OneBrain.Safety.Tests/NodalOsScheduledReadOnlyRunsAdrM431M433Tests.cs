using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ScheduledReadOnlyRunsAdr")]
[TestCategory("OrchestrationInProcessFacadeV1")]
[TestCategory("BrowserAdapterProjectSkeleton")]
public sealed class NodalOsScheduledReadOnlyRunsAdrM431M433Tests
{
    [TestMethod]
    public void ScheduledReadOnlyBoundaryDiscoveryExists()
    {
        Assert.IsTrue(File.Exists(DiscoveryPath()));
    }

    [TestMethod]
    public void ScheduledReadOnlyAdrExists()
    {
        Assert.IsTrue(File.Exists(AdrPath()));
    }

    [TestMethod]
    public void ArtifactExists()
    {
        Assert.IsTrue(File.Exists(ArtifactPath()));
    }

    [TestMethod]
    public void ArtifactMarksImplementationDeferred()
    {
        StringAssert.Contains(ReadArtifact(), "\"implementationDeferred\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoScheduler()
    {
        StringAssert.Contains(ReadArtifact(), "\"noSchedulerImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoTimer()
    {
        StringAssert.Contains(ReadArtifact(), "\"noTimerImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoBackgroundWorker()
    {
        StringAssert.Contains(ReadArtifact(), "\"noBackgroundWorkerImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoUi()
    {
        StringAssert.Contains(ReadArtifact(), "\"noUiImplemented\": true");
    }

    [TestMethod]
    public void ArtifactMarksNoExecution()
    {
        StringAssert.Contains(ReadArtifact(), "\"noExecutionImplemented\": true");
    }

    [TestMethod]
    public void ArtifactDefinesReadOnly()
    {
        StringAssert.Contains(ReadArtifact(), "\"readOnlyDefinitionCreated\": true");
        StringAssert.Contains(ReadAdr(), "Definition Of Read-Only");
    }

    [TestMethod]
    public void ArtifactForbidsClickTypeSubmitUploadDownload()
    {
        StringAssert.Contains(ReadArtifact(), "\"clickTypeSubmitUploadDownloadForbidden\": true");
        var adr = ReadAdr();

        StringAssert.Contains(adr, "click");
        StringAssert.Contains(adr, "type");
        StringAssert.Contains(adr, "submit");
        StringAssert.Contains(adr, "upload");
        StringAssert.Contains(adr, "download");
    }

    [TestMethod]
    public void ArtifactForbidsLoginCaptchaTwoFactor()
    {
        StringAssert.Contains(ReadArtifact(), "\"loginCaptchaTwoFactorForbidden\": true");
        var adr = ReadAdr();

        StringAssert.Contains(adr, "login");
        StringAssert.Contains(adr, "captcha");
        StringAssert.Contains(adr, "2FA");
    }

    [TestMethod]
    public void ArtifactForbidsPaymentSendDeleteSignPublish()
    {
        StringAssert.Contains(ReadArtifact(), "\"paymentSendDeleteSignPublishForbidden\": true");
        var adr = ReadAdr();

        StringAssert.Contains(adr, "pay");
        StringAssert.Contains(adr, "send");
        StringAssert.Contains(adr, "delete");
        StringAssert.Contains(adr, "sign");
        StringAssert.Contains(adr, "publish");
    }

    [TestMethod]
    public void ArtifactRequiresPolicyGate()
    {
        StringAssert.Contains(ReadArtifact(), "\"policyGateRequired\": true");
        StringAssert.Contains(ReadAdr(), "Policy Gates");
    }

    [TestMethod]
    public void ArtifactRequiresEvidenceRedaction()
    {
        StringAssert.Contains(ReadArtifact(), "\"evidenceRedactionRequired\": true");
        StringAssert.Contains(ReadAdr(), "Evidence And Redaction Gates");
    }

    [TestMethod]
    public void AdrMentionsRunReportProgressReportOnly()
    {
        var adr = ReadAdr();

        StringAssert.Contains(adr, "RunReport");
        StringAssert.Contains(adr, "ProgressReport");
        StringAssert.Contains(adr, "Outputs are limited to RunReport, ProgressReport");
    }

    [TestMethod]
    public void AdrUsesNodalOsName_NotNexaProjectName()
    {
        var combined = ReadAdr() + ReadDiscovery();

        StringAssert.Contains(combined, "NODAL OS");
        Assert.IsFalse(combined.Contains("Project: NEXA", StringComparison.OrdinalIgnoreCase));
    }

    private static string ReadArtifact() => File.ReadAllText(ArtifactPath());

    private static string ReadAdr() => File.ReadAllText(AdrPath());

    private static string ReadDiscovery() => File.ReadAllText(DiscoveryPath());

    private static string ArtifactPath() =>
        Path.Combine(FindRepoRoot(), "artifacts", "agent-operations", "m433", "scheduled-read-only-runs-adr-summary.json");

    private static string AdrPath() =>
        Path.Combine(FindRepoRoot(), "docs", "architecture", "scheduled-read-only-runs-decision-record.md");

    private static string DiscoveryPath() =>
        Path.Combine(FindRepoRoot(), "docs", "reports", "scheduled-read-only-runs-boundary-discovery-m431.md");

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OneBrain.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
