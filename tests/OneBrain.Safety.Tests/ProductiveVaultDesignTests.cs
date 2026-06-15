using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class ProductiveVaultDesignTests
{
    [TestMethod]
    public void ProductiveVaultDesignDoesNotEnableRealVault()
    {
        var design = ProductiveVaultDesign.Current;

        Assert.IsFalse(design.RealVaultEnabled);
        Assert.IsTrue(design.Validate().IsValid);
    }

    [TestMethod]
    public void ProductiveVaultDesignDoesNotReturnSecretValues()
    {
        var design = ProductiveVaultDesign.Current;
        var guard = new ProductiveVaultDesignGuard();
        var dpapi = guard.EvaluateProvider(BrowserProductiveVaultProviderKind.WindowsDpapi);
        var credentialManager = guard.EvaluateProvider(BrowserProductiveVaultProviderKind.WindowsCredentialManager);

        Assert.IsFalse(design.ReturnsSecretValues);
        Assert.IsFalse(dpapi.SecretValueReturned);
        Assert.IsFalse(credentialManager.SecretValueReturned);
        Assert.IsTrue(dpapi.IsFailClosed);
        Assert.IsTrue(credentialManager.IsFailClosed);
    }

    [TestMethod]
    public void ProductiveVaultDesignKeepsCompanionOutOfSecretValues()
    {
        var design = ProductiveVaultDesign.Current;

        Assert.IsFalse(design.CompanionCanReceiveSecretValues);
        CollectionAssert.Contains(design.Guardrails.ToList(), "companion never sees secret values");
    }

    [TestMethod]
    public void ProductiveVaultDesignRequiresScopedAccessPolicy()
    {
        var design = ProductiveVaultDesign.Current;

        Assert.IsTrue(design.RequiresScopedAccessPolicy);
        CollectionAssert.Contains(design.Guardrails.ToList(), "retrieval is scoped and audited");
    }

    [TestMethod]
    public void ProductiveVaultDesignDocumentsRotationAndRevocation()
    {
        var design = ProductiveVaultDesign.Current;

        Assert.IsTrue(design.DocumentsRotation);
        Assert.IsTrue(design.DocumentsRevocation);
        Assert.IsTrue(design.StorageOptions.All(o => !string.IsNullOrWhiteSpace(o.Rotation)));
    }

    [TestMethod]
    public void ProductiveVaultDesignFailsClosedForUnknownProvider()
    {
        var decision = new ProductiveVaultDesignGuard().EvaluateProvider((BrowserProductiveVaultProviderKind)999);

        Assert.AreEqual(BrowserProductiveVaultDecisionKind.FailClosed, decision.Decision);
        Assert.IsTrue(decision.IsFailClosed);
        Assert.IsFalse(decision.SecretValueReturned);
    }

    [TestMethod]
    public void ProductiveVaultDesignComparesRequiredStorageCandidates()
    {
        var providers = ProductiveVaultDesign.Current.StorageOptions.Select(o => o.ProviderKind).ToHashSet();

        Assert.IsTrue(providers.Contains(ProductiveVaultDesignProviderKind.DpapiCurrentUser));
        Assert.IsTrue(providers.Contains(ProductiveVaultDesignProviderKind.DpapiLocalMachine));
        Assert.IsTrue(providers.Contains(ProductiveVaultDesignProviderKind.WindowsCredentialManager));
        Assert.IsTrue(providers.Contains(ProductiveVaultDesignProviderKind.OsBackedEncryptedFile));
        Assert.IsTrue(providers.Contains(ProductiveVaultDesignProviderKind.ExternalVaultFuture));
    }
}
