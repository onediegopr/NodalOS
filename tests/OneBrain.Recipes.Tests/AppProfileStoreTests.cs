using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.AppProfiles;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class AppProfileStoreTests
{
    [TestMethod]
    public void Write_And_Read_App_Profile_Under_Artifacts()
    {
        var temp = CreateTempDir();
        var profile = AppProfileDemoFixture.CreateDemoProductEvidenceProfile();

        var result = AppProfileStore.Write(temp, profile);
        var profiles = AppProfileStore.ReadAll(temp);

        Assert.IsTrue(result.Success, result.Error);
        StringAssert.Contains(result.RelativePath.Replace('\\', '/'), "artifacts/app-profiles/");
        StringAssert.Contains(File.ReadAllText(result.Path), AppProfileStore.SchemaVersion);
        Assert.AreEqual(profile.Id, profiles.Single().Id);
    }

    [TestMethod]
    public void External_Fragile_Profile_Requires_DiagnosticAllowed()
    {
        var profile = AppProfileDemoFixture.CreateMercadoLibreProfile() with
        {
            SupportedCapabilities = [AppProfileCapabilities.ReadOnly, AppProfileCapabilities.ExternalFragile],
            RiskPolicy = AppProfileDemoFixture.CreateMercadoLibreProfile().RiskPolicy with { DiagnosticAllowed = false }
        };

        var result = AppProfilePolicy.Validate(profile);

        Assert.IsFalse(result.CanActivate);
        Assert.IsTrue(result.Issues.Any(issue => issue.Code == "external_fragile_requires_diagnostic_allowed"));
    }

    [TestMethod]
    public void Login_Payment_And_Purchase_Are_Blocked_By_Profile()
    {
        var profile = AppProfileDemoFixture.CreatePilotProfile() with
        {
            SupportedCapabilities = [AppProfileCapabilities.ReadOnly, AppProfileCapabilities.Login, AppProfileCapabilities.Payment, AppProfileCapabilities.Purchase]
        };

        var result = AppProfilePolicy.Validate(profile);

        Assert.IsFalse(result.CanActivate);
        Assert.IsTrue(result.Issues.Any(issue => issue.Code == "login_blocked_by_default"));
        Assert.IsTrue(result.Issues.Any(issue => issue.Code == "payment_blocked_by_default"));
        Assert.IsTrue(result.Issues.Any(issue => issue.Code == "purchase_blocked_by_default"));
    }

    [TestMethod]
    public void Profile_Changes_Do_Not_Enable_Unsafe_Actions_By_Default()
    {
        var profile = AppProfileDemoFixture.CreateSodimacProfile();
        var result = AppProfilePolicy.Validate(profile);

        Assert.IsTrue(result.CanActivate, string.Join("; ", result.Issues.Select(issue => issue.Message)));
        Assert.IsTrue(profile.RiskPolicy.ReadOnlyByDefault);
        Assert.IsTrue(profile.RiskPolicy.BlocksLogin);
        Assert.IsTrue(profile.RiskPolicy.BlocksCookies);
        Assert.IsTrue(profile.RiskPolicy.BlocksPayment);
        Assert.IsTrue(profile.RiskPolicy.BlocksPurchase);
        Assert.IsFalse(profile.RiskPolicy.AllowsSafeClick);
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "onebrain-app-profile-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
