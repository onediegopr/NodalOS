using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.AI;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class AIModelConfigurationTests
{
    [TestMethod]
    public void Configuration_Loads_Four_Official_Profiles()
    {
        var profiles = AIModelConfiguration.LoadOfficialProfiles(new Dictionary<string, string?>());

        Assert.AreEqual(4, profiles.Count);
        CollectionAssert.Contains(profiles.Select(profile => profile.ProfileId).ToArray(), AIProfileIds.CheapIntent);
        CollectionAssert.Contains(profiles.Select(profile => profile.ProfileId).ToArray(), AIProfileIds.StandardTask);
        CollectionAssert.Contains(profiles.Select(profile => profile.ProfileId).ToArray(), AIProfileIds.CriticalReasoner);
        CollectionAssert.Contains(profiles.Select(profile => profile.ProfileId).ToArray(), AIProfileIds.VisionVerifier);
    }

    [TestMethod]
    public void Configuration_Does_Not_Hardcode_Model()
    {
        var profiles = AIModelConfiguration.LoadOfficialProfiles(new Dictionary<string, string?>());

        Assert.IsTrue(profiles.All(profile => string.IsNullOrWhiteSpace(profile.Model)));
    }

    [TestMethod]
    public void Configuration_Reads_Model_From_Config()
    {
        var profiles = AIModelConfiguration.LoadOfficialProfiles(new Dictionary<string, string?>
        {
            ["OB_AI_STANDARD_TASK_MODEL"] = "configured-model"
        });

        Assert.AreEqual("configured-model", profiles.Single(profile => profile.ProfileId == AIProfileIds.StandardTask).Model);
    }

    [TestMethod]
    public void Api_Key_Masking_Never_Reveals_Full_Secret()
    {
        const string secret = "sk-test-secret-A91F";

        var masked = AIModelConfiguration.MaskSecret(secret);

        Assert.AreEqual("sk-...A91F", masked);
        Assert.AreNotEqual(secret, masked);
        Assert.IsFalse(masked.Contains("test-secret", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Configuration_Stores_Only_Masked_Key_State()
    {
        var profiles = AIModelConfiguration.LoadOfficialProfiles(new Dictionary<string, string?>
        {
            ["OB_AI_CHEAP_INTENT_API_KEY"] = "sk-test-secret-A91F"
        });
        var profile = profiles.Single(item => item.ProfileId == AIProfileIds.CheapIntent);

        Assert.IsTrue(profile.ApiKeyConfigured);
        Assert.AreEqual("sk-...A91F", profile.ApiKeyMasked);
        Assert.AreEqual("OB_AI_CHEAP_INTENT_API_KEY", profile.ApiKeySecretName);
    }
}
