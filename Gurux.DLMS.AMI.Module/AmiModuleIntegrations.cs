using Gurux.DLMS.AMI.Shared;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
namespace Gurux.DLMS.AMI.Module;

/// <summary>Projection returned by a module after checking access to its target.</summary>
public sealed record GXAmiTarget(string Name, string Path);
/// <summary>Resolves module targets after checking user access.</summary>
public interface IAmiTargetProvider
{
    /// <summary>Resolves an accessible target for the specified user, or returns null.</summary>
    Task<GXAmiTarget?> ResolveAsync(ClaimsPrincipal user, string target, string targetId, CancellationToken cancellationToken);
}
/// <summary>Exports and imports module target data for a user.</summary>
public interface IGXAmiDataExchangeProvider
{
    /// <summary>Exports the requested target records for the specified user.</summary>
    Task<IReadOnlyList<JsonElement>> ExportAsync(ClaimsPrincipal user, string target, IReadOnlyList<Guid>? ids, CancellationToken cancellationToken);
    /// <summary>Imports target data using the specified rule and returns the number of imported records.</summary>
    Task<int> ImportAsync(ClaimsPrincipal user, string target, JsonElement data, string rule, CancellationToken cancellationToken);
}

/// <summary>Host-owned notification transport. Providers select authorized recipients.</summary>
public interface IGXAmiModuleEventPublisher
{
    /// <summary>Publishes a module target event to the authorized recipients.</summary>
    Task PublishAsync(IReadOnlyList<string> recipients, string moduleId, string target, string targetId, string action, CancellationToken cancellationToken);
}

/// <summary>Optional module-owned membership cleanup in the host's deletion transaction.</summary>
public interface IGXAmiMembershipLifecycle
{
    /// <summary>The host owns the database and transaction; the provider must not dispose or complete them.</summary>
    Task UserGroupsDeletingAsync(IGXDatabase database, IDbTransaction transaction, IReadOnlyList<Guid> groupIds, CancellationToken cancellationToken);
}
