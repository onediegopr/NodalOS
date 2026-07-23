using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace OneBrain.Pilot;

public static class NodalOsDesktopLaunchRuntime
{
    public const string DefaultLoopbackUrl = "http://127.0.0.1:5084";

    private const int ErrorSuccess = 0;
    private const int ErrorInsufficientBuffer = 122;

    private static readonly HashSet<string> PackagedProductPaths = new(StringComparer.Ordinal)
    {
        "/",
        "/api/mission-control",
        "/workspace/select",
        "/api/workspace/selection",
        "/workspace/clear",
        "/mission/new",
        "/api/mission/draft",
        "/mission/clear",
        "/mission/execution",
        "/api/mission/execution",
        "/mission/rollback",
        "/mission/execution/clear",
        "/mission/handoff.md",
        "/models/config",
        "/api/models/config",
        "/models/test",
        "/models/clear",
        "/teach",
        "/api/teach",
        "/teach/bind",
        "/teach/capture",
        "/teach/finish",
        "/teach/review",
        "/teach/save",
        "/teach/discard",
        "/settings/diagnostics"
    };

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(
        ref uint packageFullNameLength,
        StringBuilder? packageFullName);

    public static bool IsPackagedProductPath(string? path) =>
        !string.IsNullOrWhiteSpace(path) && PackagedProductPaths.Contains(path);

    public static bool IsPackaged()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        try
        {
            uint length = 0;
            var result = GetCurrentPackageFullName(ref length, null);
            return result is ErrorSuccess or ErrorInsufficientBuffer;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    public static string ResolveProductDataRoot(
        string? requestedRoot = null,
        string? localApplicationData = null)
    {
        var root = requestedRoot;
        if (string.IsNullOrWhiteSpace(root))
        {
            localApplicationData ??= Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localApplicationData))
                localApplicationData = Path.GetTempPath();
            root = Path.Combine(localApplicationData, "NodalOS", "ProductData");
        }

        root = Path.GetFullPath(root);
        Directory.CreateDirectory(root);
        return root;
    }

    public static string ResolveLoopbackUrls(string? requestedUrls)
    {
        var value = string.IsNullOrWhiteSpace(requestedUrls)
            ? DefaultLoopbackUrl
            : requestedUrls.Trim();
        var urls = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (urls.Length is < 1 or > 4)
            throw new ArgumentException("One to four loopback URLs are required.", nameof(requestedUrls));

        foreach (var url in urls)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
                !string.Equals(parsed.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrEmpty(parsed.UserInfo) ||
                !string.IsNullOrEmpty(parsed.Query) ||
                !string.IsNullOrEmpty(parsed.Fragment) ||
                (parsed.AbsolutePath.Length > 1 && parsed.AbsolutePath != "/"))
            {
                throw new ArgumentException("Desktop runtime URLs must be absolute loopback HTTP origins.", nameof(requestedUrls));
            }

            var loopback = string.Equals(parsed.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                IPAddress.TryParse(parsed.Host, out var address) && IPAddress.IsLoopback(address);
            if (!loopback)
                throw new ArgumentException("Desktop runtime cannot bind outside loopback.", nameof(requestedUrls));
        }

        return string.Join(';', urls);
    }

    public static bool ShouldOpenBrowser(IReadOnlyList<string> args, bool packaged)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Any(value => string.Equals(value, "--no-open-browser", StringComparison.OrdinalIgnoreCase)))
            return false;
        if (args.Any(value => string.Equals(value, "--open-browser", StringComparison.OrdinalIgnoreCase)))
            return true;
        return packaged;
    }

    public static void RegisterBrowserLaunch(
        IHostApplicationLifetime lifetime,
        string urls,
        Action<string>? opener = null)
    {
        ArgumentNullException.ThrowIfNull(lifetime);
        var target = FirstOrigin(urls);
        opener ??= OpenWithShell;

        lifetime.ApplicationStarted.Register(() =>
        {
            try
            {
                opener(target);
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }
        });
    }

    private static string FirstOrigin(string urls) => ResolveLoopbackUrls(urls)
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .First();

    private static void OpenWithShell(string target)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = target,
            UseShellExecute = true
        });
    }
}