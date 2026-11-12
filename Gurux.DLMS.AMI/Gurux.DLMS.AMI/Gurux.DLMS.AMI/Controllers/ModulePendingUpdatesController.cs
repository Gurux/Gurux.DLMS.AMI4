using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Module;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Installer-only status and cancellation of prepared module batches.</summary>
/// <param name="services">Host services owning the durable pending batch.</param>
/// <param name="repository">Enforces module resource access.</param>
[ApiController, Route("api/Module/Pending")]
[Authorize(Policy = GXModulePolicies.Add), Authorize(Policy = GXModulePolicies.Edit)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ModulePendingUpdatesController(IServiceProvider services, IModuleRepository repository) : ControllerBase
{
    /// <summary>Returns prepared versions without changing the installed module metadata.</summary>
    [HttpGet]
    public async Task<GXModulePendingUpdateStatus> Get(CancellationToken token)
    {
        var batch = await services.GetRequiredService<GXModulePendingUpdates>().ReadAsync(token);
        if (batch == null)
        {
            return new();
        }
        foreach (var package in batch.Packages)
        {
            try
            {
                await repository.ReadAsync(package.Id, null, token);
            }
            catch (Exception error) when (error is UnauthorizedAccessException or GXAmiForbiddenException or GXAmiNotFoundException)
            {
                return new();
            }
        }
        return batch.ToResponse();
    }

    /// <summary>Cancels preparation without touching running module packages.</summary>
    [HttpPost("Cancel")]
    public async Task<IActionResult> Cancel(CancellationToken token)
    {
        var pending = services.GetRequiredService<GXModulePendingUpdates>();
        try
        {
            await pending.ExclusiveAsync(async ct =>
            {
                var batch = await pending.ReadAsync(ct);
                foreach (var package in batch?.Packages ?? [])
                {
                    await repository.ReadAsync(package.Id, null, ct);
                }
                if (batch?.State == "Applying")
                {
                    throw new InvalidOperationException("A module update is being applied.");
                }
                if (batch?.State == "PendingRestart")
                {
                    batch.State = "Cancelled";
                    await pending.SaveAsync(batch, ct);
                }
                return true;
            }, token);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (GXAmiForbiddenException)
        {
            return Forbid();
        }
        catch (InvalidOperationException error)
        {
            return Conflict(new { error.Message });
        }
    }
}
