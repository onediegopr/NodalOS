using OneBrain.Core.Actions;
using OneBrain.Observation;
using OneBrain.Actions.Uia;
using OneBrain.Verification.Reports;

namespace OneBrain.Verification.Engine;

public sealed class BasicActionVerifier
{
    private readonly CognitiveSnapshotReader _reader = new();
    private readonly UiaActionExecutor _executor = new();

    public VerifiedActionResult ExecuteAndVerify(ActionRequest request)
    {
        var before = _reader.Read(request.ProcessName, request.WindowTitle);
        var actionResult = _executor.Execute(request);

        System.Threading.Thread.Sleep(500);

        var after = _reader.Read(request.ProcessName, request.WindowTitle);

        var targetExistsAfter = TargetExists(request.TargetRef, after);

        var report = new ActionVerificationReport(
            ElementsBefore: before?.Elements.Count ?? 0,
            ElementsAfter: after?.Elements.Count ?? 0,
            TitleBefore: before?.Window.Title ?? "N/A",
            TitleAfter: after?.Window.Title ?? "N/A",
            TargetExistsAfter: targetExistsAfter,
            SelectorUsed: request.TargetRef,
            SnapshotBefore: before != null,
            SnapshotAfter: after != null,
            SameProcess: before?.Window.ProcessId == after?.Window.ProcessId,
            ElementCountChanged: (before?.Elements.Count ?? 0) != (after?.Elements.Count ?? 0),
            Notes: "Structural verification complete.");

        return new VerifiedActionResult(
            Success: actionResult.Success && after != null,
            Message: actionResult.Message,
            Action: actionResult,
            Verification: report);
    }

    private static bool TargetExists(string selector, dynamic? snapshot)
    {
        if (snapshot == null)
        {
            return false;
        }

        string value = selector;
        string kind = "ref";

        if (selector.Contains(':'))
        {
            var parts = selector.Split(':', 2);
            kind = parts[0];
            value = parts[1];
        }

        foreach (var e in snapshot.Elements)
        {
            if (kind == "role" && string.Equals((string)e.Role, value, StringComparison.OrdinalIgnoreCase)) return true;
            if (kind == "name" && ((string)e.Name).Contains(value, StringComparison.OrdinalIgnoreCase)) return true;
            if (kind == "automation-id" && string.Equals((string)e.AutomationId, value, StringComparison.OrdinalIgnoreCase)) return true;
            if (kind == "class" && string.Equals((string)e.ClassName, value, StringComparison.OrdinalIgnoreCase)) return true;
            if (kind == "ref" && string.Equals((string)e.Ref, value, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }
}
