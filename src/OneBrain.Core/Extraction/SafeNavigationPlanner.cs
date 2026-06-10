using System.Text.Json;

namespace OneBrain.Core.Extraction;

/// <summary>Pure planner: input discovered items -> output navigation plan. No side effects.</summary>
public static class SafeNavigationPlanner
{
    private static readonly string[] AlwaysBlocked = ["payment", "dangerous", "auth"];
    private static readonly string[] RequiresApproval = ["nav"];
    private static readonly string[] AlwaysAllowed = ["safe"];

    public static NavigationPlan Plan(List<ActionItem>? items)
    {
        if (items == null || items.Count == 0)
            return new NavigationPlan { HasExecutableActions = false, Summary = "no actions to plan" };

        var allowed = new List<ActionItem>();
        var blocked = new List<ActionItem>();
        var requiresApproval = new List<ActionItem>();

        foreach (var item in items)
        {
            if (AlwaysBlocked.Contains(item.Severity))
                blocked.Add(item);
            else if (RequiresApproval.Contains(item.Severity))
                requiresApproval.Add(item);
            else
                allowed.Add(item);
        }

        var blockedReasons = blocked.Select(b => $"{b.Match}: {b.Severity}").ToList();
        var candidates = requiresApproval.Select(r => r.Match).ToList();

        var summary = blocked.Count > 0
            ? $"BLOCKED {blocked.Count} dangerous/auth/payment items. {requiresApproval.Count} nav candidates require approval."
            : $"{allowed.Count} safe, {requiresApproval.Count} nav candidates";

        return new NavigationPlan
        {
            Allowed = allowed,
            Blocked = blocked,
            RequiresApproval = requiresApproval,
            AllowedCount = allowed.Count,
            BlockedCount = blocked.Count,
            RequiresApprovalCount = requiresApproval.Count,
            HasExecutableActions = false, // NEVER true in this hito
            Summary = summary,
            BlockedReasonsJson = JsonSerializer.Serialize(blockedReasons),
            CandidatesJson = JsonSerializer.Serialize(candidates)
        };
    }
}

public sealed class NavigationPlan
{
    public List<ActionItem> Allowed { get; init; } = new();
    public List<ActionItem> Blocked { get; init; } = new();
    public List<ActionItem> RequiresApproval { get; init; } = new();
    public int AllowedCount { get; init; }
    public int BlockedCount { get; init; }
    public int RequiresApprovalCount { get; init; }
    public bool HasExecutableActions { get; init; }
    public string Summary { get; init; } = "";
    public string? BlockedReasonsJson { get; init; }
    public string? CandidatesJson { get; init; }
}
