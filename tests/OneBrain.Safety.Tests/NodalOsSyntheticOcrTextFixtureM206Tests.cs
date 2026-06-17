using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("SyntheticOcrTextFixture")]
[TestCategory("SyntheticOcrFixtureGenerator")]
[TestCategory("PixelRedaction")]
[TestCategory("ImagePixelRedaction")]
[TestCategory("OcrRedactionPrecondition")]
[TestCategory("OcrVisionActivationGate")]
[TestCategory("OcrVisionNoAuthority")]
[TestCategory("OcrVisionPrivacy")]
[TestCategory("BrowserGroundingSnapshot")]
[TestCategory("PrivatePreviewReadiness")]
[TestCategory("LocalPreviewReleaseCandidate")]
[TestCategory("NodalOsNamingAudit")]
[TestCategory("BrowserRuntimePhaseGate")]
public sealed class NodalOsSyntheticOcrTextFixtureM206Tests
{
    [TestMethod]
    public void FixtureGenerator_CreatesNonSensitiveCrop()
    {
        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var fixture = generator.Generate("TEST");

        Assert.AreEqual(NodalOsSyntheticOcrTextFixtureStatus.Ready, fixture.Status);
        Assert.IsTrue(fixture.SafeForOcr);
        Assert.IsFalse(fixture.Sensitive);
#pragma warning disable MSTEST0017
        Assert.AreEqual(expected: "TEST", actual: fixture.ExpectedText);
        Assert.AreEqual(expected: 640, actual: fixture.Width);
        Assert.AreEqual(expected: 640, actual: fixture.Height);
#pragma warning restore MSTEST0017
    }

    [TestMethod]
    public void FixtureGenerator_NeverCreatesFullScreenCrop()
    {
        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var fixture = generator.Generate("HELLO");

        Assert.IsFalse(fixture.FullScreen);
    }

    [TestMethod]
    public void FixtureGenerator_DoesNotPersistRawImage()
    {
        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var fixture = generator.Generate("SAFE");

        Assert.IsFalse(fixture.OriginalRawPersisted);
    }

    [TestMethod]
    public void FixtureGenerator_PassesPixelRedactionV2()
    {
        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var fixture = generator.Generate("NODAL");

        Assert.IsNotNull(fixture.RedactionResult);
        Assert.IsTrue(fixture.RedactionResult.SafeForOcr);
        Assert.IsFalse(fixture.RedactionResult.OriginalRawPersisted);
    }

    [TestMethod]
    public void FixtureGenerator_MetadataIncludesExpectedText()
    {
        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var fixture = generator.Generate("ABC123");

#pragma warning disable MSTEST0017
        Assert.AreEqual(expected: "ABC123", actual: fixture.ExpectedText);
#pragma warning restore MSTEST0017
        Assert.IsTrue(fixture.ImageBytes.Length > 0);
    }

    [TestMethod]
    public void FixtureGenerator_CatalogIncludesMultipleVariants()
    {
        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var catalog = generator.GenerateCatalog();

        Assert.IsTrue(catalog.Fixtures.Count >= 6);
        Assert.IsTrue(catalog.ReadyCount > 0);
        Assert.IsTrue(catalog.AllReady);
        Assert.IsTrue(catalog.NoRawPersistence);
        Assert.IsTrue(catalog.NoFullScreen);
        Assert.IsTrue(catalog.NoSensitive);
    }

    [TestMethod]
    public void FixtureGenerator_RejectsSensitiveText()
    {
        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var fixture = generator.Generate("PASSWORD");

        Assert.AreEqual(NodalOsSyntheticOcrTextFixtureStatus.BlockedBySensitiveText, fixture.Status);
        Assert.IsFalse(fixture.SafeForOcr);
    }

    [TestMethod]
    public void FixtureGenerator_BlocksUnsupportedDimensions()
    {
        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var options = generator.DefaultOptions() with { Width = 30 };
        var fixture = generator.Generate("TEST", options);

        Assert.AreEqual(NodalOsSyntheticOcrTextFixtureStatus.BlockedByUnsupportedDimensions, fixture.Status);
    }

    [TestMethod]
    public void FixtureGenerator_BlocksFullScreen()
    {
        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var options = generator.DefaultOptions() with { AllowFullScreen = true };
        var fixture = generator.Generate("TEST", options);

        Assert.AreEqual(NodalOsSyntheticOcrTextFixtureStatus.BlockedByFullScreen, fixture.Status);
    }

    [TestMethod]
    public void FixtureGenerator_SupportsMultipleColorSchemes()
    {
        var generator = new NodalOsSyntheticOcrTextFixtureGenerator();
        var schemes = new[]
        {
            NodalOsSyntheticOcrTextColorScheme.BlackOnWhite,
            NodalOsSyntheticOcrTextColorScheme.WhiteOnBlack,
            NodalOsSyntheticOcrTextColorScheme.GrayOnWhite
        };

        foreach (var scheme in schemes)
        {
            var options = generator.DefaultOptions() with { ColorScheme = scheme };
            var fixture = generator.Generate("TEXT", options);
            Assert.AreEqual(NodalOsSyntheticOcrTextFixtureStatus.Ready, fixture.Status, $"scheme {scheme} should be ready");
        }
    }
}
