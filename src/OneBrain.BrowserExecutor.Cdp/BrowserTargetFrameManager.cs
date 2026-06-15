using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public enum BrowserTargetState { Alive, Active, Visible, UserFacing, Background, Detached, Destroyed, Stale, Navigating, Redirecting, Popup, Unknown }
public enum BrowserTargetEventType { TargetCreated, TargetAttached, TargetDetached, TargetDestroyed, TargetActivated, NavigationStarted, NavigationCommitted, NavigationFinished, FrameAttached, FrameDetached, FrameNavigated, PopupOpened, WindowOpened, DownloadStarted }

public sealed record BrowserTargetRecord(
    string TargetId,
    string? TabId,
    Uri Url,
    string Title,
    long Generation,
    BrowserTargetState State,
    DateTimeOffset UpdatedAtUtc,
    bool? IsActive = null,
    bool? IsVisible = null,
    bool? IsUserFacing = null)
{
    public bool IsUsable => State is BrowserTargetState.Alive or BrowserTargetState.Active or BrowserTargetState.Visible or BrowserTargetState.UserFacing or BrowserTargetState.Background;
}

public sealed record BrowserFrameContext(
    string TargetId,
    string FrameId,
    string? ParentFrameId,
    Uri Url,
    string Name,
    BrowserTargetState State,
    long Generation,
    bool CrossOriginLimited)
{
    public bool CanUseForVerification => State is not BrowserTargetState.Detached and not BrowserTargetState.Destroyed and not BrowserTargetState.Stale;
}

public sealed record BrowserFrameTree(string TargetId, IReadOnlyDictionary<string, BrowserFrameContext> Frames)
{
    public BrowserFrameContext MainFrame =>
        Frames.Values.FirstOrDefault(frame => frame.ParentFrameId is null) ??
        throw new InvalidOperationException("Frame tree does not contain a main frame.");
}

public sealed record BrowserNavigationEvent(
    BrowserTargetEventType EventType,
    string TargetId,
    string? FrameId,
    Uri? Url,
    long Generation,
    DateTimeOffset OccurredAtUtc,
    string Reason);

public sealed record BrowserPopupPolicy(bool AllowPopups, bool AllowNewWindows)
{
    public static BrowserPopupPolicy BlockByDefault { get; } = new(false, false);
}

public sealed record BrowserTargetSelectionPolicy(bool RequireExplicitTarget, bool PreferUserFacing, string? ExpectedHost = null)
{
    public static BrowserTargetSelectionPolicy Explicit(string? expectedHost = null) => new(true, true, expectedHost);
}

public sealed record BrowserCdpRuntimeEvent(
    BrowserTargetEventType EventType,
    string TargetId,
    string? FrameId = null,
    string? ParentFrameId = null,
    Uri? Url = null,
    string Title = "",
    string Reason = "",
    bool CrossOriginLimited = false);

public sealed class BrowserTargetRegistry
{
    private readonly Dictionary<string, BrowserTargetRecord> _targets = new(StringComparer.Ordinal);
    private readonly List<BrowserNavigationEvent> _events = [];

    public IReadOnlyCollection<BrowserTargetRecord> Targets => _targets.Values.ToList();
    public IReadOnlyList<BrowserNavigationEvent> Events => _events.ToList();

    public BrowserTargetRecord UpsertTarget(string targetId, Uri url, string title, BrowserTargetState state, string? tabId = null, bool? isActive = null, bool? isVisible = null, bool? isUserFacing = null)
    {
        var currentGeneration = _targets.TryGetValue(targetId, out var current) ? current.Generation : 0;
        var next = new BrowserTargetRecord(targetId, tabId, url, title, currentGeneration, state, DateTimeOffset.UtcNow, isActive, isVisible, isUserFacing);
        _targets[targetId] = next;
        _events.Add(new BrowserNavigationEvent(BrowserTargetEventType.TargetCreated, targetId, null, url, next.Generation, DateTimeOffset.UtcNow, state.ToString()));
        return next;
    }

    public BrowserTargetRecord ApplyNavigation(string targetId, Uri url, string title, BrowserTargetEventType eventType = BrowserTargetEventType.NavigationCommitted)
    {
        if (!_targets.TryGetValue(targetId, out var current))
            throw new KeyNotFoundException($"Unknown target: {targetId}");

        var nextGeneration = current.Generation + 1;
        var state = eventType switch
        {
            BrowserTargetEventType.NavigationStarted => BrowserTargetState.Navigating,
            BrowserTargetEventType.NavigationCommitted => BrowserTargetState.Alive,
            BrowserTargetEventType.NavigationFinished => BrowserTargetState.Alive,
            _ => current.State
        };
        var next = current with { Url = url, Title = title, Generation = nextGeneration, State = state, UpdatedAtUtc = DateTimeOffset.UtcNow };
        _targets[targetId] = next;
        _events.Add(new BrowserNavigationEvent(eventType, targetId, "main", url, nextGeneration, DateTimeOffset.UtcNow, eventType.ToString()));
        return next;
    }

    public BrowserTargetRecord MarkTarget(string targetId, BrowserTargetState state, BrowserTargetEventType eventType)
    {
        if (!_targets.TryGetValue(targetId, out var current))
            throw new KeyNotFoundException($"Unknown target: {targetId}");

        var next = current with { State = state, UpdatedAtUtc = DateTimeOffset.UtcNow };
        _targets[targetId] = next;
        _events.Add(new BrowserNavigationEvent(eventType, targetId, null, current.Url, current.Generation, DateTimeOffset.UtcNow, state.ToString()));
        return next;
    }

    public BrowserTargetRecord? TryGet(string targetId) =>
        _targets.TryGetValue(targetId, out var target) ? target : null;
}

public sealed class BrowserFrameManager
{
    private readonly Dictionary<string, Dictionary<string, BrowserFrameContext>> _framesByTarget = new(StringComparer.Ordinal);

    public BrowserFrameContext AttachFrame(string targetId, string frameId, string? parentFrameId, Uri url, string name = "", bool crossOriginLimited = false)
    {
        if (!_framesByTarget.TryGetValue(targetId, out var frames))
        {
            frames = new Dictionary<string, BrowserFrameContext>(StringComparer.Ordinal);
            _framesByTarget[targetId] = frames;
        }

        var generation = frames.TryGetValue(frameId, out var current) ? current.Generation + 1 : 0;
        var frame = new BrowserFrameContext(targetId, frameId, parentFrameId, url, name, BrowserTargetState.Alive, generation, crossOriginLimited);
        frames[frameId] = frame;
        return frame;
    }

    public BrowserFrameContext NavigateFrame(string targetId, string frameId, Uri url)
    {
        var frame = RequireFrame(targetId, frameId);
        var next = frame with { Url = url, Generation = frame.Generation + 1, State = BrowserTargetState.Alive };
        _framesByTarget[targetId][frameId] = next;
        return next;
    }

    public BrowserFrameContext DetachFrame(string targetId, string frameId)
    {
        var frame = RequireFrame(targetId, frameId);
        var next = frame with { State = BrowserTargetState.Detached, Generation = frame.Generation + 1 };
        _framesByTarget[targetId][frameId] = next;
        return next;
    }

    public BrowserFrameTree GetTree(string targetId)
    {
        if (!_framesByTarget.TryGetValue(targetId, out var frames))
            throw new KeyNotFoundException($"Unknown frame target: {targetId}");
        return new BrowserFrameTree(targetId, new Dictionary<string, BrowserFrameContext>(frames, StringComparer.Ordinal));
    }

    private BrowserFrameContext RequireFrame(string targetId, string frameId)
    {
        if (!_framesByTarget.TryGetValue(targetId, out var frames) || !frames.TryGetValue(frameId, out var frame))
            throw new KeyNotFoundException($"Unknown frame: {targetId}/{frameId}");
        return frame;
    }
}

public sealed class BrowserTargetManager
{
    public BrowserTargetRegistry Registry { get; } = new();
    public BrowserFrameManager Frames { get; } = new();

    public BrowserNavigationEvent ApplyRuntimeEvent(BrowserCdpRuntimeEvent runtimeEvent)
    {
        var url = runtimeEvent.Url ?? new Uri("about:blank");
        BrowserNavigationEvent emitted;
        switch (runtimeEvent.EventType)
        {
            case BrowserTargetEventType.TargetCreated:
            case BrowserTargetEventType.TargetAttached:
                var created = Registry.UpsertTarget(runtimeEvent.TargetId, url, runtimeEvent.Title, BrowserTargetState.Alive);
                emitted = new BrowserNavigationEvent(runtimeEvent.EventType, runtimeEvent.TargetId, runtimeEvent.FrameId, url, created.Generation, DateTimeOffset.UtcNow, runtimeEvent.Reason);
                break;
            case BrowserTargetEventType.TargetDestroyed:
                var destroyed = Registry.MarkTarget(runtimeEvent.TargetId, BrowserTargetState.Destroyed, BrowserTargetEventType.TargetDestroyed);
                emitted = new BrowserNavigationEvent(runtimeEvent.EventType, runtimeEvent.TargetId, runtimeEvent.FrameId, destroyed.Url, destroyed.Generation, DateTimeOffset.UtcNow, runtimeEvent.Reason);
                break;
            case BrowserTargetEventType.TargetDetached:
                var detached = Registry.MarkTarget(runtimeEvent.TargetId, BrowserTargetState.Detached, BrowserTargetEventType.TargetDetached);
                emitted = new BrowserNavigationEvent(runtimeEvent.EventType, runtimeEvent.TargetId, runtimeEvent.FrameId, detached.Url, detached.Generation, DateTimeOffset.UtcNow, runtimeEvent.Reason);
                break;
            case BrowserTargetEventType.NavigationStarted:
            case BrowserTargetEventType.NavigationCommitted:
            case BrowserTargetEventType.NavigationFinished:
                var navigated = Registry.ApplyNavigation(runtimeEvent.TargetId, url, runtimeEvent.Title, runtimeEvent.EventType);
                if (!string.IsNullOrWhiteSpace(runtimeEvent.FrameId))
                    Frames.NavigateFrame(runtimeEvent.TargetId, runtimeEvent.FrameId, url);
                emitted = new BrowserNavigationEvent(runtimeEvent.EventType, runtimeEvent.TargetId, runtimeEvent.FrameId, url, navigated.Generation, DateTimeOffset.UtcNow, runtimeEvent.Reason);
                break;
            case BrowserTargetEventType.FrameAttached:
                var frame = Frames.AttachFrame(runtimeEvent.TargetId, runtimeEvent.FrameId ?? "main", runtimeEvent.ParentFrameId, url, runtimeEvent.Title, runtimeEvent.CrossOriginLimited);
                emitted = new BrowserNavigationEvent(runtimeEvent.EventType, runtimeEvent.TargetId, frame.FrameId, url, frame.Generation, DateTimeOffset.UtcNow, runtimeEvent.Reason);
                break;
            case BrowserTargetEventType.FrameDetached:
                var detachedFrame = Frames.DetachFrame(runtimeEvent.TargetId, runtimeEvent.FrameId ?? "main");
                emitted = new BrowserNavigationEvent(runtimeEvent.EventType, runtimeEvent.TargetId, detachedFrame.FrameId, detachedFrame.Url, detachedFrame.Generation, DateTimeOffset.UtcNow, runtimeEvent.Reason);
                break;
            case BrowserTargetEventType.FrameNavigated:
                var frameNavigated = Frames.NavigateFrame(runtimeEvent.TargetId, runtimeEvent.FrameId ?? "main", url);
                emitted = new BrowserNavigationEvent(runtimeEvent.EventType, runtimeEvent.TargetId, frameNavigated.FrameId, url, frameNavigated.Generation, DateTimeOffset.UtcNow, runtimeEvent.Reason);
                break;
            case BrowserTargetEventType.PopupOpened:
            case BrowserTargetEventType.WindowOpened:
                var popup = Registry.UpsertTarget(runtimeEvent.TargetId, url, runtimeEvent.Title, BrowserTargetState.Popup);
                emitted = new BrowserNavigationEvent(runtimeEvent.EventType, runtimeEvent.TargetId, runtimeEvent.FrameId, url, popup.Generation, DateTimeOffset.UtcNow, runtimeEvent.Reason);
                break;
            default:
                emitted = new BrowserNavigationEvent(runtimeEvent.EventType, runtimeEvent.TargetId, runtimeEvent.FrameId, url, 0, DateTimeOffset.UtcNow, runtimeEvent.Reason);
                break;
        }

        return emitted;
    }

    public BrowserTargetRecord SelectTarget(BrowserTargetSelectionPolicy policy, string? explicitTargetId = null)
    {
        if (policy.RequireExplicitTarget && string.IsNullOrWhiteSpace(explicitTargetId))
            throw new InvalidOperationException("Explicit target is required; active tab is not assumed.");

        var candidates = Registry.Targets.Where(target => target.IsUsable).ToList();
        if (!string.IsNullOrWhiteSpace(explicitTargetId))
            candidates = candidates.Where(target => target.TargetId == explicitTargetId).ToList();

        if (!string.IsNullOrWhiteSpace(policy.ExpectedHost))
            candidates = candidates.Where(target => target.Url.Host.EndsWith(policy.ExpectedHost, StringComparison.OrdinalIgnoreCase)).ToList();

        if (policy.PreferUserFacing)
            candidates = candidates.OrderByDescending(target => target.IsUserFacing == true).ThenByDescending(target => target.IsActive == true).ToList();

        return candidates.FirstOrDefault() ?? throw new InvalidOperationException("No target matched the selection policy.");
    }

    public BrowserTargetContext ToTargetContext(string runId, string browserSessionId, BrowserTargetRecord target, BrowserFrameContext frame)
    {
        if (!target.IsUsable)
            throw new InvalidOperationException($"Target is not usable: {target.State}");
        if (!frame.CanUseForVerification)
            throw new InvalidOperationException($"Frame is not usable: {frame.State}");

        return new BrowserTargetContext(
            RunId: runId,
            BrowserId: "chrome-cdp",
            BrowserSessionId: browserSessionId,
            BrowserContextId: null,
            WindowId: null,
            TargetId: target.TargetId,
            PageId: target.TargetId,
            TabId: target.TabId,
            FrameId: frame.FrameId,
            ParentFrameId: frame.ParentFrameId,
            Url: frame.Url,
            Title: target.Title,
            Generation: Math.Max(target.Generation, frame.Generation),
            LivenessToken: BrowserTargetContext.CreateLivenessToken(target.TargetId, frame.FrameId, Math.Max(target.Generation, frame.Generation)),
            ObservedAtUtc: DateTimeOffset.UtcNow,
            IsActive: target.IsActive,
            IsVisible: target.IsVisible,
            IsUserFacing: target.IsUserFacing,
            ReadyState: null,
            Source: BrowserTargetSource.Cdp);
    }

    public bool CanExecute(BrowserAction action, BrowserTargetRecord target, BrowserFrameContext frame)
    {
        if (action.CanModifyState && !target.IsUsable)
            return false;
        if (action.CanModifyState && !frame.CanUseForVerification)
            return false;
        return true;
    }
}
