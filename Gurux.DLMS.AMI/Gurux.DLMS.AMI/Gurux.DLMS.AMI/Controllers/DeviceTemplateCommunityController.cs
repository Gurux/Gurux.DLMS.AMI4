using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Publishes saved device templates using owner-authorized reads.</summary>
/// <param name="community">Durable community publication service.</param>
[ApiController, Route("api/DeviceTemplateCommunity"), Authorize]
public sealed class DeviceTemplateCommunityController(GXScriptCommunityService community) : ControllerBase
{
    /// <summary>Exports one saved device template after explicit CC0 publication consent.</summary>
    [HttpPost("Publish"), Authorize(Policy = GXDeviceTemplatePolicies.Add), Authorize(Policy = GXDeviceTemplatePolicies.View)]
    public async Task<ActionResult<GXScriptCommunitySubmission>> Publish(GXPublishCommunityDeviceTemplate request, CancellationToken token)
    {
        try
        {
            var owner = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
            return await community.PublishDeviceTemplateAsync(owner, request, token);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException error)
        {
            return Problem(statusCode: 400, title: "Invalid community device template", detail: error.Message);
        }
    }
}
