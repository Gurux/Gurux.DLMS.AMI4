namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Canonical configuration landing-page routes and their legacy aliases.</summary>
public static class GXConfigurationRoutes
{
    private static readonly (string Legacy, string Canonical)[] Targets =
    [
        ("system", "system"),
        ("status", "status"),
        ("security", "security"),
        ("language", "languages"),
        ("Module", "modules"),
        ("agent", "agents"),
        ("Component", "components"),
        ("Block", "blocks"),
        ("Menu", "menus"),
        ("Tab", "tabs"),
        ("log", "logs"),
        ("View", "views"),
        ("Trigger", "triggers"),
        ("Workflow", "workflows"),
        ("prune", "prune"),
        ("appearance", "appearance"),
        ("Account", "account"),
        ("Manufacturer", "manufacturers"),
        ("cron", "cron"),
        ("KeyManagement", "key-management"),
        ("authentications", "authentication"),
        ("statistic", "statistics"),
        ("maintenance", "maintenance"),
        ("Script", "scripts"),
        ("changehistory", "history"),
        ("http-proxy", "http-proxy"),
        ("access-control", "access-control"),
    ];

    /// <summary>Normalizes known landing pages, retaining query strings and fragments.</summary>
    public static string Canonicalize(string path)
    {
        int suffixIndex = path.IndexOfAny(['?', '#']);
        var route = suffixIndex < 0 ? path : path[..suffixIndex];
        var suffix = suffixIndex < 0 ? string.Empty : path[suffixIndex..];
        var parts = route.Trim('/').Split('/');
        var prefix = route.StartsWith('/') ? "/" : string.Empty;
        if (parts.Length == 1 && parts[0].Equals("config", StringComparison.OrdinalIgnoreCase))
        {
            return prefix + "config" + suffix;
        }
        if (parts.Length != 2 || !parts[0].Equals("config", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }
        foreach (var target in Targets)
        {
            if (parts[1].Equals(target.Legacy, StringComparison.OrdinalIgnoreCase)
                || parts[1].Equals(target.Canonical, StringComparison.OrdinalIgnoreCase)
                || (target.Canonical == "agents" && parts[1].Equals("AgentInstallers", StringComparison.OrdinalIgnoreCase))
                || (target.Canonical == "views" && parts[1].Equals("view", StringComparison.OrdinalIgnoreCase)))
            {
                return prefix + "config/" + target.Canonical + suffix;
            }
        }
        return path;
    }

    /// <summary>Resolves a canonical target to the existing component selector.</summary>
    public static string? LegacyTarget(string? target)
    {
        foreach (var entry in Targets)
        {
            if (string.Equals(target, entry.Canonical, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Legacy;
            }
        }
        return target;
    }

    /// <summary>Restores an internal landing-page path used by existing management components.</summary>
    public static string LegacyPath(string path)
    {
        var parts = path.Trim('/').Split('/');
        return parts.Length == 2 && parts[0].Equals("config", StringComparison.OrdinalIgnoreCase)
            ? (path.StartsWith('/') ? "/" : string.Empty) + "config/" + LegacyTarget(parts[1]) : path;
    }
}
