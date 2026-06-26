using System.Security.Cryptography;

namespace OneBrain.BrowserRuntime;

public sealed record CdpForkUpdateReleasePipelineRequest(
    string RepositoryRoot,
    string RuntimeRepositoryPath,
    string LockfilePath,
    string LocalConfigPath,
    string RuntimeOriginUrl,
    string RuntimeUpstreamUrl,
    string RuntimeBranch,
    string RuntimeHead);

public sealed record CdpForkUpdateReleasePipelineResult(
    bool IsReady,
    string RuntimeRepository,
    string RuntimeBranch,
    string RuntimeHead,
    string RuntimeVersion,
    string RuntimeCommit,
    string UpstreamCommit,
    string BinarySha256,
    bool LockfileValid,
    bool LocalConfigPresent,
    bool RuntimeRepositoryPresent,
    bool RuntimeArtifactPresent,
    bool RuntimeArtifactInsideFork,
    bool RuntimeArtifactUnderManagedCache,
    bool ArtifactHashVerified,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool ExtensionFallbackAllowed,
    bool SystemBrowserFallbackAllowed,
    IReadOnlyList<string> Errors);

public sealed class CdpForkUpdateReleasePipeline
{
    public const string ExpectedRuntimeRepository = "nodal-cloakbrowser-runtime";
    public const string ExpectedRuntimeBranch = "nodal/runtime";
    public const string ExpectedOriginUrl = "https://github.com/onediegopr/nodal-cloakbrowser-runtime";
    public const string ExpectedUpstreamUrl = "https://github.com/CloakHQ/cloakbrowser";
    public const string ExpectedRuntimePathPolicy = "env-or-local-config";
    public const string ExpectedRuntimeChannel = "nodal-runtime";

    public CdpForkUpdateReleasePipelineResult Evaluate(CdpForkUpdateReleasePipelineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RepositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RuntimeRepositoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LockfilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LocalConfigPath);

        var errors = new List<string>();
        var runtimeRepositoryPresent = Directory.Exists(request.RuntimeRepositoryPath);
        var runtimeLock = BrowserRuntimeLock.Load(request.LockfilePath);
        var lockValidation = runtimeLock.Validate();
        if (!lockValidation.IsValid)
        {
            errors.AddRange(lockValidation.Errors);
        }

        Require(runtimeLock.RuntimeRepo.Equals(ExpectedRuntimeRepository, StringComparison.OrdinalIgnoreCase), "RuntimeRepoMustBeNodalFork", errors);
        Require(runtimeLock.RuntimeSource.Equals("fork", StringComparison.OrdinalIgnoreCase), "RuntimeSourceMustBeFork", errors);
        Require(runtimeLock.RuntimeChannel.Equals(ExpectedRuntimeChannel, StringComparison.OrdinalIgnoreCase), "RuntimeChannelMustBeNodalRuntime", errors);
        Require(runtimeLock.RuntimePathPolicy.Equals(ExpectedRuntimePathPolicy, StringComparison.OrdinalIgnoreCase), "RuntimePathPolicyMustUseLocalConfigOrEnvironment", errors);
        Require(runtimeLock.HasPinnedRuntimeArtifact, "RuntimeArtifactMustBePinnedInLockfile", errors);
        Require(!runtimeLock.ExtensionEnabled, "ExtensionMustRemainDisabledInRuntimeLock", errors);
        Require(!runtimeLock.SystemBrowserAllowed, "SystemBrowserMustRemainDisabledInRuntimeLock", errors);

        Require(runtimeRepositoryPresent, "RuntimeRepositoryMissing", errors);
        Require(NormalizeRemote(request.RuntimeOriginUrl).Equals(ExpectedOriginUrl, StringComparison.OrdinalIgnoreCase), "RuntimeOriginUrlMismatch", errors);
        Require(NormalizeRemote(request.RuntimeUpstreamUrl).Equals(ExpectedUpstreamUrl, StringComparison.OrdinalIgnoreCase), "RuntimeUpstreamUrlMismatch", errors);
        Require(request.RuntimeBranch.Equals(ExpectedRuntimeBranch, StringComparison.OrdinalIgnoreCase), "RuntimeBranchMismatch", errors);
        Require(string.IsNullOrWhiteSpace(request.RuntimeHead) || request.RuntimeHead.Equals(runtimeLock.RuntimeCommit, StringComparison.OrdinalIgnoreCase), "RuntimeHeadMustMatchLockfileCommit", errors);

        var localConfigPresent = File.Exists(request.LocalConfigPath);
        Require(localConfigPresent, "LocalRuntimeConfigMissing", errors);
        var localConfig = localConfigPresent
            ? BrowserRuntimeLocalConfig.Load(request.LocalConfigPath)
            : BrowserRuntimeLocalConfig.Empty;
        Require(localConfig.HasExecutablePath, "LocalRuntimeExecutablePathMissing", errors);

        var artifactPath = localConfig.CloakBrowserExecutablePath;
        var artifactPresent = localConfig.HasExecutablePath && File.Exists(artifactPath);
        var artifactInsideFork = artifactPresent && IsPathInside(artifactPath, request.RuntimeRepositoryPath);
        var artifactUnderManagedCache = artifactPresent && ContainsManagedRuntimeCacheSegment(artifactPath);
        var artifactHashVerified = artifactPresent && HashFile(artifactPath).Equals(runtimeLock.BinarySha256, StringComparison.OrdinalIgnoreCase);

        Require(artifactPresent, "PinnedRuntimeArtifactMissing", errors);
        Require(artifactInsideFork, "PinnedRuntimeArtifactMustStayInsideRuntimeFork", errors);
        Require(artifactUnderManagedCache, "PinnedRuntimeArtifactMustUseManagedRuntimeCache", errors);
        Require(artifactHashVerified, "PinnedRuntimeArtifactHashMismatch", errors);

        return new CdpForkUpdateReleasePipelineResult(
            IsReady: errors.Count == 0,
            RuntimeRepository: runtimeLock.RuntimeRepo,
            RuntimeBranch: request.RuntimeBranch,
            RuntimeHead: request.RuntimeHead,
            RuntimeVersion: runtimeLock.RuntimeVersion,
            RuntimeCommit: runtimeLock.RuntimeCommit,
            UpstreamCommit: runtimeLock.UpstreamCommit,
            BinarySha256: runtimeLock.BinarySha256,
            LockfileValid: lockValidation.IsValid,
            LocalConfigPresent: localConfigPresent,
            RuntimeRepositoryPresent: runtimeRepositoryPresent,
            RuntimeArtifactPresent: artifactPresent,
            RuntimeArtifactInsideFork: artifactInsideFork,
            RuntimeArtifactUnderManagedCache: artifactUnderManagedCache,
            ArtifactHashVerified: artifactHashVerified,
            ExtensionUsed: false,
            SystemBrowserUsed: false,
            ExtensionFallbackAllowed: false,
            SystemBrowserFallbackAllowed: false,
            Errors: errors);
    }

    private static string NormalizeRemote(string remote)
    {
        if (string.IsNullOrWhiteSpace(remote))
        {
            return string.Empty;
        }

        return RemoveSuffix(remote.Trim().TrimEnd('/'), ".git");
    }

    private static bool IsPathInside(string path, string root)
    {
        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsManagedRuntimeCacheSegment(string path)
    {
        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        return path
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Any(part => part.Equals(".cloakbrowser", StringComparison.OrdinalIgnoreCase));
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static void Require(bool condition, string errorCode, List<string> errors)
    {
        if (!condition)
        {
            errors.Add(errorCode);
        }
    }

    private static string RemoveSuffix(string value, string suffix) =>
        value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? value[..^suffix.Length]
            : value;
}
