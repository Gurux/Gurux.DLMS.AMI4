namespace Gurux.DLMS.AMI.Shared.Rest;

/// <summary>Policies to evaluate for the current HTTP user.</summary>
public sealed class CheckPolicies
{
    /// <summary>Gets or sets the names of the policies to evaluate for the current user.</summary>
    public string[] PolicyNames { get; set; } = [];
}

/// <summary>Server authorization decisions, without policy definitions.</summary>
public sealed class CheckPoliciesResponse
{
    /// <summary>Gets or sets the authorization decision for each evaluated policy name.</summary>
    public Dictionary<string, bool> Results { get; set; } = new(StringComparer.Ordinal);
}
