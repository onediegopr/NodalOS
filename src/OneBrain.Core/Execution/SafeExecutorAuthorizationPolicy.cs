using OneBrain.Core.Contracts;
using OneBrain.Core.Safety;

namespace OneBrain.Core.Execution;

public sealed record SafeExecutorAuthorizationDecision(
    bool Allowed,
    FailureKind FailureKind,
    string BlockReason,
    string Reason);

public static class SafeExecutorAuthorizationPolicy
{
    public static SafeExecutorAuthorizationDecision Evaluate(ApprovalManifest? manifest, string actionKind)
    {
        if (manifest == null)
        {
            return new SafeExecutorAuthorizationDecision(
                false,
                FailureKind.PolicyDenied,
                "ApprovalManifestRequired",
                $"{actionKind} requires approval manifest");
        }

        if (!manifest.ExecutionAllowedInThisHito)
        {
            return new SafeExecutorAuthorizationDecision(
                false,
                FailureKind.PolicyDenied,
                "ExecutionNotAllowedInThisHito",
                $"{actionKind} requires executionAllowedInThisHito=true");
        }

        return new SafeExecutorAuthorizationDecision(
            true,
            FailureKind.PolicyDenied,
            "",
            "");
    }
}
