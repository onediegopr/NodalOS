using OneBrain.Core.Approval;

namespace OneBrain.Core.ExecutorHarness;

public static class ExecutorHarnessSafetyMatrix
{
    public static ExecutorHarnessSafetyMatrixEvaluation Evaluate(ExecutorHarnessTarget target, ApprovalDecision? decision)
    {
        var passed = new List<string>();
        var blocked = new List<string>();
        var requiresApproval = new List<string>();
        var notes = new List<string>
        {
            "executor harness safety matrix is fail-closed",
            "only benign local Pilot harness target can execute"
        };

        var resolution = ExecutorHarnessTargetResolver.ResolveTarget(target);
        if (resolution.Success)
            passed.Add("target resolved to allowlisted local harness");
        else
            blocked.Add(resolution.Message);

        Check(target.ControlledSurface, "controlled surface confirmed", "target is not controlled", passed, blocked);
        Check(target.IsBenign, "target marked benign", "target is not benign", passed, blocked);
        Check(target.HasSafeExecutor, "safe executor present", "safe executor is missing", passed, blocked);
        Check(
            string.Equals(target.ActionKind, ApprovalActionKinds.BenignHarnessClick, StringComparison.OrdinalIgnoreCase),
            "action kind scoped to benign harness click",
            "action kind is outside benign harness scope",
            passed,
            blocked);

        if (decision == null)
        {
            requiresApproval.Add("approval decision is required");
            blocked.Add("approval decision is required");
        }
        else
        {
            Check(
                string.Equals(decision.Decision, ApprovalDecisionKinds.Approved, StringComparison.OrdinalIgnoreCase),
                "approval decision approved",
                "approval decision is not approved",
                passed,
                blocked);
            Check(
                decision.ExecutionAllowed,
                "execution allowed by scoped approval decision",
                "approval decision does not allow executor harness action",
                passed,
                blocked);
        }

        var allowed = blocked.Count == 0;
        return new ExecutorHarnessSafetyMatrixEvaluation(
            Allowed: allowed,
            Status: allowed ? "allowed" : ExecutorHarnessStatuses.Blocked,
            Passed: passed.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Blocked: blocked.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            RequiresApproval: requiresApproval,
            Notes: notes);
    }

    private static void Check(
        bool condition,
        string pass,
        string block,
        ICollection<string> passed,
        ICollection<string> blocked)
    {
        if (condition)
            passed.Add(pass);
        else
            blocked.Add(block);
    }
}
