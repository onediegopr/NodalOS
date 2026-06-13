using System.Security.Cryptography;
using System.Text;
using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Identity;
using OneBrain.Core.Models;
using OneBrain.Core.Safety;
using OneBrain.Core.Selectors;
using OneBrain.Core.Selectors.Web;

namespace OneBrain.Core.Execution;

public static class SafeClickPlanner
{
    public static SafeClickExecutionPlan Plan(SafeClickPlanInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var manifest = input.Manifest;
        if (manifest == null)
        {
            return Blocked(
                FailureKind.PolicyDenied,
                "MissingManifest",
                IdentityStrength.None,
                parityAgrees: null,
                wouldUseUnsafeFallback: false,
                reasons: ["approval manifest is required for safe.click planning"]);
        }

        var approvalBinding = ApprovalManifestBuilder.TryBuildApprovalBinding(manifest);
        var expectedIdentity = BuildExpectedIdentity(manifest, approvalBinding);
        var selector = manifest.ApprovedSelector ?? approvalBinding?.Selector;
        var identityStrength = ResolveIdentityStrength(manifest, expectedIdentity);
        var parityAgrees = manifest.ShadowAgreesWithLegacy;

        var contract = new RecipeSafetyContract(
            SchemaVersion: 1,
            ContractId: BuildContractId(input, manifest, expectedIdentity),
            ActionKind: string.IsNullOrWhiteSpace(input.ActionKind) ? "click" : input.ActionKind.Trim(),
            ExpectedIdentity: expectedIdentity,
            Selector: selector,
            WindowConstraints: new ExecutionWindowConstraints(
                LocalPilotOnly: true,
                ExternalNavigationBlocked: true),
            Reversible: input.Reversible,
            MaxActions: 1,
            ActionCeiling: ActionCeiling.FullActionWithPreflight,
            Provenance: Provenance.Uia,
            TrustLevel: identityStrength == IdentityStrength.Strong
                ? TrustLevel.ProfileVerified
                : TrustLevel.InferredLowConfidence,
            ApprovalRef: approvalBinding);

        var contractValidation = new ContractValidator().Validate(contract);
        if (!contractValidation.IsValid)
        {
            return Blocked(
                contractValidation.FailureKind ?? FailureKind.PolicyDenied,
                "ContractInvalid",
                identityStrength,
                parityAgrees,
                wouldUseUnsafeFallback: ComputeWouldUseUnsafeFallback(input.Candidates, null, selector),
                reasons: contractValidation.Reasons,
                contractValid: false);
        }

        var candidateIdentities = input.Candidates
            .Select(WebCandidateMapper.ToElementIdentity)
            .ToList();

        var bindingResult = new ApprovalBindingValidator().Validate(
            contract.ApprovalRef!,
            contract.ExpectedIdentity!,
            candidateIdentities,
            reversible: input.Reversible);

        if (!bindingResult.Success)
        {
            return Blocked(
                bindingResult.FailureKind ?? FailureKind.Blocked,
                string.IsNullOrWhiteSpace(bindingResult.BlockReason) ? "BindingInvalid" : bindingResult.BlockReason,
                identityStrength,
                parityAgrees,
                wouldUseUnsafeFallback: ComputeWouldUseUnsafeFallback(input.Candidates, bindingResult.ObservedIdentityDigest, selector),
                reasons: bindingResult.Reasons,
                contractValid: true,
                bindingVerdict: bindingResult.MatchVerdict);
        }

        return new SafeClickExecutionPlan(
            ProjectedState: StepState.Bound,
            FailureKind: null,
            BlockReason: null,
            IdentityStrength: identityStrength,
            ContractValid: true,
            BindingVerdict: bindingResult.MatchVerdict,
            ParityAgrees: parityAgrees,
            WouldDispatch: false,
            WouldUseUnsafeFallback: ComputeWouldUseUnsafeFallback(input.Candidates, bindingResult.ObservedIdentityDigest, selector),
            Reasons: bindingResult.Reasons.Count == 0
                ? ["contract valid", "approval binding matched", "dispatch intentionally disabled in shadow planner"]
                : bindingResult.Reasons);
    }

    private static SafeClickExecutionPlan Blocked(
        FailureKind failureKind,
        string blockReason,
        IdentityStrength identityStrength,
        bool? parityAgrees,
        bool wouldUseUnsafeFallback,
        IReadOnlyList<string> reasons,
        bool contractValid = false,
        string? bindingVerdict = null)
    {
        return new SafeClickExecutionPlan(
            ProjectedState: StepState.Blocked,
            FailureKind: failureKind,
            BlockReason: blockReason,
            IdentityStrength: identityStrength,
            ContractValid: contractValid,
            BindingVerdict: bindingVerdict,
            ParityAgrees: parityAgrees,
            WouldDispatch: false,
            WouldUseUnsafeFallback: wouldUseUnsafeFallback,
            Reasons: reasons.Count == 0 ? ["planner blocked fail-closed"] : reasons);
    }

    private static ElementIdentity? BuildExpectedIdentity(ApprovalManifest manifest, ApprovalBinding? approvalBinding)
    {
        if (manifest.ApprovedSelector?.ExpectedIdentity != null)
            return manifest.ApprovedSelector.ExpectedIdentity;

        if (approvalBinding?.Selector.ExpectedIdentity != null)
            return approvalBinding.Selector.ExpectedIdentity;

        var selector = manifest.ApprovedSelector ?? approvalBinding?.Selector;
        if (selector == null)
            return null;

        if (string.IsNullOrWhiteSpace(selector.Role) &&
            string.IsNullOrWhiteSpace(selector.Name) &&
            string.IsNullOrWhiteSpace(selector.AutomationId) &&
            string.IsNullOrWhiteSpace(selector.HelpText) &&
            string.IsNullOrWhiteSpace(selector.LegacyName) &&
            string.IsNullOrWhiteSpace(selector.ClassName) &&
            string.IsNullOrWhiteSpace(selector.AncestorPath))
        {
            return null;
        }

        return new ElementIdentity
        {
            RuntimeId = "",
            AutomationId = selector.AutomationId ?? "",
            Name = selector.Name ?? "",
            HelpText = selector.HelpText ?? "",
            LegacyName = selector.LegacyName ?? "",
            Role = selector.Role ?? "",
            ControlType = selector.Role ?? "",
            ClassName = selector.ClassName ?? "",
            AncestorPath = selector.AncestorPath ?? "",
            Provenance = selector.Provenance
        };
    }

    private static IdentityStrength ResolveIdentityStrength(ApprovalManifest manifest, ElementIdentity? expectedIdentity)
    {
        if (manifest.IdentityStrength != IdentityStrength.None)
            return manifest.IdentityStrength;

        if (expectedIdentity == null)
            return IdentityStrength.None;

        if (expectedIdentity.IsStrong)
            return IdentityStrength.Strong;

        return HasWeakIdentityCriteria(expectedIdentity)
            ? IdentityStrength.Weak
            : IdentityStrength.None;
    }

    private static bool HasWeakIdentityCriteria(ElementIdentity identity)
    {
        return !string.IsNullOrWhiteSpace(identity.AutomationId) ||
               !string.IsNullOrWhiteSpace(identity.Name) ||
               !string.IsNullOrWhiteSpace(identity.HelpText) ||
               !string.IsNullOrWhiteSpace(identity.LegacyName) ||
               !string.IsNullOrWhiteSpace(identity.EffectiveControlType) ||
               !string.IsNullOrWhiteSpace(identity.ClassName) ||
               !string.IsNullOrWhiteSpace(identity.AncestorPath) ||
               !string.IsNullOrWhiteSpace(identity.BoundsHint) ||
               !string.IsNullOrWhiteSpace(identity.ParentFingerprint) ||
               identity.SiblingIndex.HasValue;
    }

    private static bool ComputeWouldUseUnsafeFallback(
        IReadOnlyList<WebCandidate> candidates,
        string? observedIdentityDigest,
        SelectorDefinition? selector)
    {
        if (candidates.Count == 0)
            return false;

        if (!string.IsNullOrWhiteSpace(observedIdentityDigest))
        {
            var matchedCandidate = candidates.FirstOrDefault(candidate =>
                string.Equals(
                    ElementFingerprintBuilder.Build(WebCandidateMapper.ToElementIdentity(candidate)),
                    observedIdentityDigest,
                    StringComparison.Ordinal));

            if (matchedCandidate != null)
                return !matchedCandidate.HasInvoke;
        }

        if (selector != null)
        {
            var resolution = SelectorEngine.Resolve(selector, candidates.Select(WebCandidateMapper.ToElementIdentity).ToList());
            if (resolution.Success && resolution.BestMatch != null)
            {
                var matchedCandidate = candidates.FirstOrDefault(candidate =>
                    string.Equals(
                        ElementFingerprintBuilder.Build(WebCandidateMapper.ToElementIdentity(candidate)),
                        ElementFingerprintBuilder.Build(resolution.BestMatch),
                        StringComparison.Ordinal));

                if (matchedCandidate != null)
                    return !matchedCandidate.HasInvoke;
            }
        }

        if (candidates.Count == 1)
            return !candidates[0].HasInvoke;

        return candidates.All(candidate => !candidate.HasInvoke);
    }

    private static string BuildContractId(
        SafeClickPlanInput input,
        ApprovalManifest manifest,
        ElementIdentity? expectedIdentity)
    {
        var digest = expectedIdentity == null
            ? manifest.ApprovedIdentityDigest ?? ""
            : ElementFingerprintBuilder.Build(expectedIdentity);

        var canonical = string.Join("|",
            "safe-click-plan",
            input.Mode?.Trim() ?? "",
            input.TargetText?.Trim() ?? "",
            input.ActionKind?.Trim() ?? "click",
            digest,
            manifest.PolicyVersion?.Trim() ?? "");

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
