using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace OneBrain.Cli.Accessibility;

// ── COM IAccessible (read-only subset) ─────────────────────────────────────

[ComImport, Guid("618736e0-3c3d-11cf-810c-00aa00389b71")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
internal interface IAccessibleCom
{
    // IDispatch slots
    // 0x60020000: GetTypeInfoCount, GetTypeInfo, GetIDsOfNames, Invoke (4 slots)

    // IAccessible properties/methods:
    [DispId(-5000)]   object  get_accParent();
    [DispId(-5001)]   int     get_accChildCount();

    [DispId(-5003)]
    object accChild(object varChild);

    [DispId(-5002)]
    void accName(object varChild, [MarshalAs(UnmanagedType.BStr)] out string pszName);

    [DispId(-5004)]
    void accValue(object varChild, [MarshalAs(UnmanagedType.BStr)] out string pszValue);

    [DispId(-5005)]
    void accDescription(object varChild, [MarshalAs(UnmanagedType.BStr)] out string pszDescription);

    [DispId(-5006)]
    void accRole(object varChild, out object pvarRole);

    [DispId(-5007)]
    void accState(object varChild, out object pvarState);

    [DispId(-5008)]
    void accHelp(object varChild, [MarshalAs(UnmanagedType.BStr)] out string pszHelp);

    [DispId(-5010)]
    void accKeyboardShortcut(object varChild, [MarshalAs(UnmanagedType.BStr)] out string pszKeyboardShortcut);

    [DispId(-5011)]
    object accFocus { get; }

    [DispId(-5012)]
    object accSelection { get; }

    [DispId(-5013)]
    void accDefaultAction(object varChild, [MarshalAs(UnmanagedType.BStr)] out string pszDefaultAction);

    [DispId(-5014)]
    void accSelect(int flagsSelect, object varChild);

    [DispId(-5015)]
    void accLocation(out int pxLeft, out int pyTop, out int pcxWidth, out int pcyHeight, object varChild);

    [DispId(-5016)]
    object accNavigate(int navDir, object varStart);

    [DispId(-5017)]
    object accHitTest(int xLeft, int yTop);

    [DispId(-5018)]
    void accDoDefaultAction(object varChild);
}

// ── MSAA Reader ──────────────────────────────────────────────────────────

public sealed class MsaaAccessibleReader
{
    private const uint OBJID_CLIENT = 0xFFFFFFFC;
    private const uint OBJID_WINDOW = 0x00000000;
    private const int  MaxNodes    = 2000;
    private const int  MaxDepth    = 30;

    private static readonly Guid IAccessibleGuid = new("{618736e0-3c3d-11cf-810c-00aa00389b71}");

    #region P/Invoke

    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr hWndParent, Delegates.EnumWindowsProc lpEnumFunc, IntPtr lParam);
    private static class Delegates { public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam); }
    [DllImport("user32.dll")] private static extern bool EnumWindows(Delegates.EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool IsWindowEnabled(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("oleacc.dll")] private static extern uint AccessibleObjectFromWindow(IntPtr hwnd, uint dwObjectID, ref Guid riid,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppvObject);

    [DllImport("oleacc.dll", CharSet = CharSet.Auto)] private static extern uint GetRoleText(uint dwRole, [Out] StringBuilder lpszRole, uint cchRoleMax);
    [DllImport("oleacc.dll", CharSet = CharSet.Auto)] private static extern uint GetStateText(uint dwState, [Out] StringBuilder lpszState, uint cchStateMax);

    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }

    #endregion

    public MsaaDiscoveryResult Discover(IntPtr sessionHwnd, string targetText, string processName = "msedge")
    {
        if (sessionHwnd == IntPtr.Zero || string.IsNullOrWhiteSpace(targetText))
            return new MsaaDiscoveryResult { Found = false, Reason = "invalid input" };

        var normalizedTarget = targetText.Trim();
        var allNodes = new List<MsaaNodeInfo>();
        var allCandidates = new List<MsaaCandidate>();
        var hwndSummaries = new List<HwndSummary>();

        // 1. Collect all HWNDs: top-level browser windows + child HWNDs
        var hwnds = new List<(IntPtr hwnd, bool isTop)>();
        var topHwnds = new List<IntPtr>();

        EnumWindows((h, _) =>
        {
            if (!IsWindowVisible(h)) return true;
            var cn = new StringBuilder(256);
            GetClassName(h, cn, cn.Capacity);
            uint pid;
            try
            {
                GetWindowThreadProcessId(h, out pid);
                using var proc = System.Diagnostics.Process.GetProcessById((int)pid);
                if (!proc.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            catch { return true; }
            topHwnds.Add(h);
            return true;
        }, IntPtr.Zero);

        foreach (var top in topHwnds)
        {
            hwnds.Add((top, true));
            // Recursive child enumeration
            CollectChildHwnds(top, hwnds, 3);
        }

        // 2. Probe each HWND via MSAA
        foreach (var (hwnd, isTop) in hwnds)
        {
            var summary = BuildHwndSummary(hwnd, isTop);
            try
            {
                var nodes = new List<MsaaNodeInfo>();
                var candidates = new List<MsaaCandidate>();

                foreach (var objId in new[] { OBJID_CLIENT, OBJID_WINDOW })
                {
                    try
                    {
                        object accObj;
                        var guid = IAccessibleGuid;
                        uint hr = AccessibleObjectFromWindow(hwnd, objId, ref guid, out accObj);
                        if (hr != 0 || accObj is not IAccessibleCom acc) continue;

                        var visited = new HashSet<object>();
                        Walk(acc, null, nodes, candidates, normalizedTarget, 0, summary.HwndHex, objId, visited);
                        if (nodes.Count > 0) break; // got data, stop probing other objId
                    }
                    catch { }
                }

                summary.NodeCount = nodes.Count;
                summary.LinkCount = nodes.Count(n => n.Role == "link");
                summary.ButtonCount = nodes.Count(n => n.Role == "push button");
                allNodes.AddRange(nodes);
                allCandidates.AddRange(candidates);
            }
            catch { }
            hwndSummaries.Add(summary);
        }

        // 3. Produce result
        var linkCount = allNodes.Count(n => n.Role == "link");
        var btnCount = allNodes.Count(n => n.Role == "push button");

        if (allCandidates.Count == 0)
            return new MsaaDiscoveryResult
            {
                Found = false, NodeCount = allNodes.Count, CandidateCount = 0,
                LinkCount = linkCount, ButtonCount = btnCount,
                Reason = $"not found in {hwnds.Count} hwnds ({allNodes.Count} nodes total)",
                HwndSummaries = hwndSummaries
            };

        // Pick best actionable candidate
        var actionable = allCandidates.Where(c => c.IsEnabled &&
            (c.Role == "link" || c.Role == "push button" ||
             !string.IsNullOrEmpty(c.DefaultAction))).ToList();

        if (actionable.Count == 0)
            return new MsaaDiscoveryResult
            {
                Found = false, NodeCount = allNodes.Count, CandidateCount = allCandidates.Count,
                LinkCount = linkCount, ButtonCount = btnCount,
                Reason = $"found {allCandidates.Count} candidates but none actionable",
                CandidatesJson = JsonSerializer.Serialize(allCandidates.Take(10)),
                HwndSummaries = hwndSummaries
            };

        var best = actionable[0];
        if (actionable.Count > 1)
        {
            var link = actionable.FirstOrDefault(c => c.Role == "link");
            if (link != null) best = link;
        }

        return new MsaaDiscoveryResult
        {
            Found = true, NodeCount = allNodes.Count, CandidateCount = allCandidates.Count,
            LinkCount = linkCount, ButtonCount = btnCount,
            SelectedName = best.Name, SelectedRole = best.Role,
            SelectedDefaultAction = best.DefaultAction, SelectedLocation = best.Location,
            SourceHwnd = best.SourceHwnd,
            Reason = actionable.Count == 1 ? "exact MSAA match" : $"ambiguous: {actionable.Count} matches",
            CandidatesJson = JsonSerializer.Serialize(actionable.Take(10)),
            HwndSummaries = hwndSummaries
        };
    }

    #region Tree walk

    private void Walk(IAccessibleCom acc, object? childId, List<MsaaNodeInfo> nodes,
        List<MsaaCandidate> candidates, string targetText, int depth,
        string hwndHex, uint objId, HashSet<object> visited)
    {
        if (depth > MaxDepth || nodes.Count >= MaxNodes) return;

        object varChild = childId ?? 0;

        string name = "", value = "", defaultAction = "";
        object roleObj = 0, stateObj = 0;
        int left = 0, top = 0, width = 0, height = 0;

        try
        {
            acc.accName(varChild, out name);
            acc.accRole(varChild, out roleObj);
            acc.accState(varChild, out stateObj);
            int roleInt = ToInt(roleObj);
            uint stateUint = unchecked((uint)ToInt(stateObj));
            bool isEnabled = (stateUint & 0x00000004) == 0;
            string roleText = ResolveRoleText(roleInt);
            string stateText = ResolveStateText(stateUint);

            try { acc.accValue(varChild, out value); } catch { }
            try { acc.accDefaultAction(varChild, out defaultAction); } catch { }
            try { acc.accLocation(out left, out top, out width, out height, varChild); } catch { }

            string location = width > 0 && height > 0 ? $"{left},{top},{width}x{height}" : "";

            var node = new MsaaNodeInfo
            {
                Name = name ?? "", Role = roleText, State = stateText,
                Value = value ?? "", DefaultAction = defaultAction ?? "",
                Location = location, Depth = depth, HwndHex = hwndHex,
                IsEnabled = isEnabled,
                ObjId = objId == OBJID_CLIENT ? "client" : "window"
            };
            nodes.Add(node);

            // Match target
            if (!string.IsNullOrWhiteSpace(name) &&
                (name.Trim().Equals(targetText, StringComparison.OrdinalIgnoreCase) ||
                 name.Contains(targetText, StringComparison.OrdinalIgnoreCase)))
            {
                candidates.Add(new MsaaCandidate
                {
                    Name = name, Role = roleText, State = stateText,
                    DefaultAction = defaultAction ?? "", Location = location,
                    IsEnabled = isEnabled, SourceHwnd = hwndHex
                });
            }

            // Enumerate children
            int childCount = 0;
            try { childCount = acc.get_accChildCount(); } catch { }
            if (childCount <= 0 || nodes.Count >= MaxNodes) return;

            for (int i = 1; i <= childCount && nodes.Count < MaxNodes; i++)
            {
                try
                {
                    object child = acc.accChild(i);
                    if (child == null) continue;
                    if (visited.Contains(child)) continue;
                    visited.Add(child);

                    if (child is IAccessibleCom childAcc)
                    {
                        Walk(childAcc, 0, nodes, candidates, targetText, depth + 1, hwndHex, objId, visited);
                    }
                    else
                    {
                        int cid = ToInt(child);
                        string cname = "";
                        try { acc.accName(cid, out cname); } catch { }
                        if (!string.IsNullOrWhiteSpace(cname))
                        {
                            var cnode = new MsaaNodeInfo
                            {
                                Name = cname, Role = "simple child", State = "",
                                Depth = depth + 1, HwndHex = hwndHex, IsEnabled = isEnabled
                            };
                            nodes.Add(cnode);
                            if (cname.Trim().Equals(targetText, StringComparison.OrdinalIgnoreCase) ||
                                cname.Contains(targetText, StringComparison.OrdinalIgnoreCase))
                                candidates.Add(new MsaaCandidate { Name = cname, Role = "simple child", IsEnabled = isEnabled, SourceHwnd = hwndHex });
                        }
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    #endregion

    #region Helpers

    private static int ToInt(object obj)
    {
        try
        {
            if (obj is int i) return i;
            if (obj is uint u) return (int)u;
            if (obj is long l) return (int)l;
            if (obj is double d) return (int)d;
            return Convert.ToInt32(obj);
        }
        catch { return 0; }
    }

    private static string ResolveRoleText(int role)
    {
        var sb = new StringBuilder(256);
        GetRoleText((uint)role, sb, 256);
        return sb.ToString().Trim();
    }

    private static string ResolveStateText(uint state)
    {
        var sb = new StringBuilder(512);
        GetStateText(state, sb, 512);
        return sb.ToString().Trim();
    }

    private static void CollectChildHwnds(IntPtr parent, List<(IntPtr hwnd, bool isTop)> results, int maxDepth)
    {
        if (maxDepth <= 0) return;
        var children = new List<IntPtr>();
        EnumChildWindows(parent, (child, _) => { children.Add(child); return true; }, IntPtr.Zero);
        foreach (var child in children)
        {
            results.Add((child, false));
            CollectChildHwnds(child, results, maxDepth - 1);
        }
    }

    private static HwndSummary BuildHwndSummary(IntPtr hwnd, bool isTopLevel)
    {
        var cn = new StringBuilder(256);
        GetClassName(hwnd, cn, cn.Capacity);
        var sb = new StringBuilder(256);
        GetWindowText(hwnd, sb, sb.Capacity);
        GetWindowRect(hwnd, out var r);

        return new HwndSummary
        {
            HwndHex = $"0x{hwnd.ToInt64():X}",
            ClassName = cn.ToString(),
            Title = sb.ToString(),
            IsVisible = IsWindowVisible(hwnd),
            IsEnabled = IsWindowEnabled(hwnd),
            IsTopLevel = isTopLevel,
            Rect = $"{r.Left},{r.Top},{r.Right - r.Left}x{r.Bottom - r.Top}"
        };
    }

    #endregion
}

// ── DTOs ──────────────────────────────────────────────────────────────────

public sealed class MsaaDiscoveryResult
{
    public bool Found { get; init; }
    public int NodeCount { get; init; }
    public int CandidateCount { get; init; }
    public int LinkCount { get; init; }
    public int ButtonCount { get; init; }
    public string? SelectedName { get; init; }
    public string? SelectedRole { get; init; }
    public string? SelectedDefaultAction { get; init; }
    public string? SelectedLocation { get; init; }
    public string? SourceHwnd { get; init; }
    public string Reason { get; init; } = "";
    public string? CandidatesJson { get; init; }
    public IReadOnlyList<HwndSummary> HwndSummaries { get; init; } = Array.Empty<HwndSummary>();
}

public sealed class MsaaNodeInfo
{
    public string Name { get; init; } = "";
    public string Role { get; init; } = "";
    public string State { get; init; } = "";
    public string Value { get; init; } = "";
    public string DefaultAction { get; init; } = "";
    public string Location { get; init; } = "";
    public int Depth { get; init; }
    public string HwndHex { get; init; } = "";
    public bool IsEnabled { get; init; }
    public string ObjId { get; init; } = "";
}

public sealed class MsaaCandidate
{
    public string Name { get; init; } = "";
    public string Role { get; init; } = "";
    public string State { get; init; } = "";
    public string DefaultAction { get; init; } = "";
    public string Location { get; init; } = "";
    public bool IsEnabled { get; init; }
    public string SourceHwnd { get; init; } = "";
}

public sealed class HwndSummary
{
    public string HwndHex { get; init; } = "";
    public string ClassName { get; init; } = "";
    public string Title { get; init; } = "";
    public string Rect { get; init; } = "";
    public bool IsVisible { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsTopLevel { get; init; }
    public int NodeCount { get; set; }
    public int LinkCount { get; set; }
    public int ButtonCount { get; set; }
}
