using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Cli.Recipes;
using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Models;
using OneBrain.Core.Recipes;
using OneBrain.Core.Safety;

namespace OneBrain.Recipes.Tests;

[TestClass]
public sealed class ApprovalManifestV3Tests
{
    [TestMethod]
    public void BuildWithoutIdentityIsByteIdenticalToV2Golden()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");

        var manifest = ApprovalManifestBuilder.Build(preflight, "controlled");
        var expectedJson = BuildExpectedV2ManifestJson(preflight, "controlled");

        Assert.AreEqual(expectedJson, manifest.ManifestJson);
        Assert.AreEqual(ApprovalManifestBuilder.PolicyVersion, manifest.PolicyVersion);
        Assert.AreEqual(ApprovalManifestBuilder.ComputeEvidenceHash(
            preflight.TargetText,
            "controlled",
            preflight.Decision,
            preflight.RiskCategory,
            preflight.RiskLevel,
            ApprovalManifestBuilder.PolicyVersion), manifest.EvidenceHash);
        Assert.IsNull(manifest.IdentitySchemaVersion);
        Assert.IsFalse(manifest.ManifestJson!.Contains("identitySchemaVersion", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BuildWithIdentityKeepsPolicyVersionApprovalV2()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");

        var manifest = ApprovalManifestBuilder.Build(preflight, "controlled", CreateWeakIdentityInput());

        Assert.AreEqual("approval-v2", manifest.PolicyVersion);
    }

    [TestMethod]
    public void BuildWithIdentityKeepsEvidenceHashV2Unchanged()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");

        var v2 = ApprovalManifestBuilder.Build(preflight, "controlled");
        var v3 = ApprovalManifestBuilder.Build(preflight, "controlled", CreateWeakIdentityInput());

        Assert.AreEqual(v2.EvidenceHash, v3.EvidenceHash);
    }

    [TestMethod]
    public void BuildWithIdentityAddsIdentitySchemaVersionApprovalV3()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");

        var manifest = ApprovalManifestBuilder.Build(preflight, "controlled", CreateWeakIdentityInput());

        Assert.AreEqual("approval-v3", manifest.IdentitySchemaVersion);
        StringAssert.Contains(manifest.ManifestJson!, "\"identitySchemaVersion\":\"approval-v3\"");
    }

    [TestMethod]
    public void WeakWebIdentityProducesWeakStrength()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");

        var manifest = ApprovalManifestBuilder.Build(preflight, "controlled", CreateWeakIdentityInput());

        Assert.AreEqual(IdentityStrength.Weak, manifest.IdentityStrength);
        Assert.IsFalse(string.IsNullOrWhiteSpace(manifest.ApprovedIdentityDigest));
        Assert.IsNotNull(manifest.ApprovedSelector);
    }

    [TestMethod]
    public void StrongIdentityProducesStrongStrength()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");

        var manifest = ApprovalManifestBuilder.Build(preflight, "controlled", CreateStrongIdentityInput());

        Assert.AreEqual(IdentityStrength.Strong, manifest.IdentityStrength);
    }

    [TestMethod]
    public void MalformedOrEmptyIdentityProducesNoneAndNoDigest()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");
        var input = new ApprovedIdentityInput(new ElementIdentity(), "web-shadow", null);

        var manifest = ApprovalManifestBuilder.Build(preflight, "controlled", input);

        Assert.AreEqual("approval-v3", manifest.IdentitySchemaVersion);
        Assert.AreEqual(IdentityStrength.None, manifest.IdentityStrength);
        Assert.IsTrue(string.IsNullOrWhiteSpace(manifest.ApprovedIdentityDigest));
        Assert.IsNull(manifest.ApprovedSelector);
        Assert.IsTrue(string.IsNullOrWhiteSpace(manifest.IdentityBindingHash));
    }

    [TestMethod]
    public void ValidateApprovalBindingStillPassesWithV3Manifest()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");
        var manifest = ApprovalManifestBuilder.Build(preflight, "controlled", CreateWeakIdentityInput());

        var result = new RecipeRunner().Run(new RecipeDefinition("safe-click-v3-manifest")
        {
            Variables = BuildApprovalVariables(manifest),
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "safe-click",
                    Kind = "safe.click",
                    SaveAs = "safeClick",
                    Args = new Dictionary<string, string>
                    {
                        ["targettext"] = manifest.TargetText,
                        ["mode"] = manifest.Mode,
                        ["approvalprefix"] = "approval",
                        ["proc"] = "process-that-does-not-exist-onebrain"
                    }
                }
            ]
        });

        Assert.IsFalse(result.Steps[0].Message.Contains("approval binding invalid", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.Steps[0].Message.Contains("evidenceHash mismatch", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TryBuildApprovalBindingBuildsWhenMetadataPresent()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");
        var manifest = ApprovalManifestBuilder.Build(preflight, "controlled", CreateWeakIdentityInput());

        var binding = ApprovalManifestBuilder.TryBuildApprovalBinding(manifest);

        Assert.IsNotNull(binding);
        Assert.AreEqual("click", binding.ActionKind);
        Assert.AreEqual(manifest.ApprovedIdentityDigest, binding.ApprovedIdentityDigest);
        Assert.AreEqual(manifest.EvidenceHash, binding.EvidenceHash);
    }

    [TestMethod]
    public void IdentityMismatchIsNonBlockingSignal()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");

        var recipe = new RecipeDefinition("approval-manifest-v3-mismatch")
        {
            Variables = new Dictionary<string, string>
            {
                ["clickPreflight.evidenceJson"] = preflight.EvidenceJson ?? "",
                ["clickPreflight.identity.name"] = "Categorias",
                ["clickPreflight.identity.controlType"] = "Button",
                ["clickPreflight.identity.source"] = "web-shadow",
                ["clickPreflight.identity.shadowAgreesWithLegacy"] = "false"
            },
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "approval",
                    Kind = "approval.manifest",
                    SaveAs = "approval",
                    Args = new Dictionary<string, string>
                    {
                        ["from"] = "clickPreflight",
                        ["mode"] = "controlled"
                    }
                }
            ]
        };

        var result = new RecipeRunner().Run(recipe);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("true", result.Variables!["approval.identity.mismatch"]);
        Assert.AreEqual("false", result.Variables!["approval.identity.shadowAgreesWithLegacy"]);
    }

    [TestMethod]
    public void SafeClickResultUnchangedExceptAdditiveApprovalIdentityVariables()
    {
        var preflight = ClickPreflightEvaluator.Evaluate("Categorias");

        var v2 = RunApprovalThenSafeClick(preflight, includeIdentity: false);
        var v3 = RunApprovalThenSafeClick(preflight, includeIdentity: true);

        Assert.AreEqual(v2.Success, v3.Success);
        Assert.AreEqual(v2.Steps.Last().Success, v3.Steps.Last().Success);
        Assert.AreEqual(v2.Steps.Last().Message, v3.Steps.Last().Message);
        Assert.AreEqual(v2.Variables!["safeClick.result"], v3.Variables!["safeClick.result"]);
        Assert.AreEqual(v2.Variables!["safeClick.reason"], v3.Variables!["safeClick.reason"]);
        Assert.IsFalse(v2.Variables.ContainsKey("approval.identity.schemaVersion"));
        Assert.AreEqual("approval-v3", v3.Variables!["approval.identity.schemaVersion"]);
        Assert.IsTrue(v3.Variables.ContainsKey("approval.identity.selector"));
    }

    private static RecipeRunResult RunApprovalThenSafeClick(ClickPreflightResult preflight, bool includeIdentity)
    {
        var variables = new Dictionary<string, string>
        {
            ["clickPreflight.evidenceJson"] = preflight.EvidenceJson ?? ""
        };

        if (includeIdentity)
        {
            variables["clickPreflight.identity.name"] = "Categorias";
            variables["clickPreflight.identity.controlType"] = "Button";
            variables["clickPreflight.identity.automationId"] = "categories-button";
            variables["clickPreflight.identity.source"] = "web-shadow";
            variables["clickPreflight.identity.shadowAgreesWithLegacy"] = "true";
        }

        var recipe = new RecipeDefinition("approval-then-safe-click")
        {
            Variables = variables,
            Steps =
            [
                new RecipeStepDefinition
                {
                    Id = "approval",
                    Kind = "approval.manifest",
                    SaveAs = "approval",
                    Args = new Dictionary<string, string>
                    {
                        ["from"] = "clickPreflight",
                        ["mode"] = "controlled"
                    }
                },
                new RecipeStepDefinition
                {
                    Id = "safe-click",
                    Kind = "safe.click",
                    SaveAs = "safeClick",
                    Args = new Dictionary<string, string>
                    {
                        ["targettext"] = "Categorias",
                        ["mode"] = "controlled",
                        ["approvalprefix"] = "approval",
                        ["proc"] = "process-that-does-not-exist-onebrain"
                    }
                }
            ]
        };

        return new RecipeRunner().Run(recipe);
    }

    private static Dictionary<string, string> BuildApprovalVariables(ApprovalManifest manifest)
    {
        var variables = new Dictionary<string, string>
        {
            ["approval.targetText"] = manifest.TargetText,
            ["approval.mode"] = manifest.Mode,
            ["approval.policyVersion"] = manifest.PolicyVersion,
            ["approval.decision"] = manifest.Decision,
            ["approval.riskCategory"] = manifest.RiskCategory,
            ["approval.riskLevel"] = manifest.RiskLevel,
            ["approval.evidenceHash"] = manifest.EvidenceHash,
            ["approval.executionAllowedInThisHito"] = manifest.ExecutionAllowedInThisHito ? "true" : "false"
        };

        if (!string.IsNullOrWhiteSpace(manifest.IdentitySchemaVersion))
            variables["approval.identity.schemaVersion"] = manifest.IdentitySchemaVersion;
        if (!string.IsNullOrWhiteSpace(manifest.ApprovedIdentityDigest))
            variables["approval.identity.digest"] = manifest.ApprovedIdentityDigest;
        if (!string.IsNullOrWhiteSpace(manifest.IdentitySource))
            variables["approval.identity.source"] = manifest.IdentitySource;
        if (manifest.ApprovedSelector != null)
            variables["approval.identity.selector"] = JsonSerializer.Serialize(manifest.ApprovedSelector);
        if (!string.IsNullOrWhiteSpace(manifest.IdentityBindingHash))
            variables["approval.identity.bindingHash"] = manifest.IdentityBindingHash;
        if (manifest.ShadowAgreesWithLegacy.HasValue)
            variables["approval.identity.shadowAgreesWithLegacy"] = manifest.ShadowAgreesWithLegacy.Value ? "true" : "false";

        return variables;
    }

    private static string BuildExpectedV2ManifestJson(ClickPreflightResult preflight, string mode)
    {
        var normalizedMode = string.IsNullOrWhiteSpace(mode) ? "commercialWeb" : mode.Trim();
        var evidenceHash = ApprovalManifestBuilder.ComputeEvidenceHash(
            preflight.TargetText,
            normalizedMode,
            preflight.Decision,
            preflight.RiskCategory,
            preflight.RiskLevel,
            ApprovalManifestBuilder.PolicyVersion);

        var manifest = new Dictionary<string, object>
        {
            ["policyVersion"] = ApprovalManifestBuilder.PolicyVersion,
            ["actionType"] = "click",
            ["targetText"] = preflight.TargetText,
            ["mode"] = normalizedMode,
            ["siteMode"] = normalizedMode,
            ["decision"] = preflight.Decision,
            ["riskCategory"] = preflight.RiskCategory,
            ["riskLevel"] = preflight.RiskLevel,
            ["allowed"] = preflight.Allowed,
            ["blocked"] = preflight.Blocked,
            ["requiresApproval"] = preflight.RequiresApproval,
            ["requiresReview"] = preflight.RequiresReview,
            ["reason"] = preflight.Reason,
            ["executionAllowedInThisHito"] = true,
            ["evidenceHash"] = evidenceHash
        };

        if (!string.IsNullOrWhiteSpace(preflight.NearbyDangerousSignalsJson))
            manifest["nearbyDangerousSignals"] = JsonSerializer.Deserialize<JsonElement>(preflight.NearbyDangerousSignalsJson);

        return JsonSerializer.Serialize(manifest);
    }

    private static ApprovedIdentityInput CreateWeakIdentityInput()
    {
        return new ApprovedIdentityInput(
            new ElementIdentity
            {
                Name = "Categorias",
                ControlType = "Button",
                AutomationId = "categories-button",
                BoundsHint = "10,10,120,24",
                Provenance = Provenance.Inferred
            },
            "web-shadow",
            new OneBrain.Core.Selectors.Web.WebSelectorParity
            {
                EngineFound = true,
                EngineVerdict = "LikelySame",
                EngineSelectedName = "Categorias",
                AgreesWithLegacy = true
            });
    }

    private static ApprovedIdentityInput CreateStrongIdentityInput()
    {
        return new ApprovedIdentityInput(
            new ElementIdentity
            {
                RuntimeId = "42.1.9",
                Name = "Categorias",
                ControlType = "Button",
                AutomationId = "categories-button",
                Provenance = Provenance.Inferred
            },
            "uia",
            null);
    }
}
