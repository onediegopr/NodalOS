using System.Text.Json;

namespace OneBrain.Core.Profiles;

public sealed class ProfileLoader
{
    public ProfileLoaderResult Load(string path)
    {
        if (!File.Exists(path))
            return new ProfileLoaderResult { Success = false, Error = $"Profile file not found: {path}" };

        try
        {
            var json = File.ReadAllText(path);
            var profile = JsonSerializer.Deserialize<ProfileDefinition>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (profile == null)
                return new ProfileLoaderResult { Success = false, Error = $"Failed to parse profile: {path}" };

            var errors = Validate(profile);
            if (errors.Count > 0)
                return new ProfileLoaderResult { Success = false, Error = string.Join("; ", errors), Profile = profile };

            return new ProfileLoaderResult { Success = true, Profile = profile };
        }
        catch (Exception ex)
        {
            return new ProfileLoaderResult { Success = false, Error = $"Error loading profile: {ex.Message}" };
        }
    }

    private static List<string> Validate(ProfileDefinition p)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(p.Id))
            errors.Add("Missing required field: id");

        if (string.IsNullOrWhiteSpace(p.Type) || p.Type is not ("web" or "app"))
            errors.Add("Missing or invalid field: type (must be 'web' or 'app')");

        if (p.Type == "web" && string.IsNullOrWhiteSpace(p.Url))
            errors.Add("Web profiles require 'url' field");

        if (p.Type == "app" && string.IsNullOrWhiteSpace(p.Process))
            errors.Add("App profiles require 'process' field");

        return errors;
    }

    public IReadOnlyDictionary<string, string> ToVariables(ProfileDefinition profile, string? prefix = null)
    {
        var vars = new Dictionary<string, string>();
        var p = prefix ?? ("profile." + profile.Id);

        vars[p + ".id"] = profile.Id;
        vars[p + ".type"] = profile.Type;
        if (profile.DisplayName != null) vars[p + ".displayName"] = profile.DisplayName;
        if (profile.Url != null) vars[p + ".url"] = profile.Url;
        if (profile.Process != null) vars[p + ".process"] = profile.Process;

        if (profile.Expected != null)
        {
            if (profile.Expected.TitleContains != null) vars[p + ".expected.titleContains"] = profile.Expected.TitleContains;
            if (profile.Expected.TextContains != null) vars[p + ".expected.textContains"] = profile.Expected.TextContains;
        }

        if (profile.Read != null)
        {
            if (profile.Read.PreferredProperty != null) vars[p + ".read.preferredProperty"] = profile.Read.PreferredProperty;
            if (profile.Read.FallbackProperty != null) vars[p + ".read.fallbackProperty"] = profile.Read.FallbackProperty;
        }

        if (profile.Safety != null)
        {
            vars[p + ".safety.allowForms"] = profile.Safety.AllowForms ? "true" : "false";
            vars[p + ".safety.allowLogin"] = profile.Safety.AllowLogin ? "true" : "false";
            vars[p + ".safety.allowPurchase"] = profile.Safety.AllowPurchase ? "true" : "false";
            vars[p + ".safety.allowSensitiveActions"] = profile.Safety.AllowSensitiveActions ? "true" : "false";
            if (profile.Safety is { AllowClose: false }) vars[p + ".safety.allowClose"] = "false";
            if (profile.Safety.AllowDelete) vars[p + ".safety.allowDelete"] = "true";
        }

        return vars;
    }
}

public sealed class ProfileLoaderResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public ProfileDefinition? Profile { get; init; }
}
