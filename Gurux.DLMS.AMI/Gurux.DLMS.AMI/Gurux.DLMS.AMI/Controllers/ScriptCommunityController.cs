using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Owner-scoped community publication and receipt endpoints.</summary>
[ApiController, Route("api/ScriptCommunity"), Authorize]
public sealed class ScriptCommunityController(GXScriptCommunityService community) : ControllerBase
{
    private string Owner => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
    /// <summary>Returns the current owner’s unacknowledged submissions.</summary>
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<List<GXScriptCommunitySubmission>> List(CancellationToken token) => community.ListAsync(Owner, token);

    /// <summary>Acknowledges one of the current owner’s terminal submissions.</summary>
    [HttpPost("Acknowledge")]
    public async Task<IActionResult> Acknowledge(GXPublishCommunityScript request, CancellationToken token)
    {
        await community.AcknowledgeAsync(Owner, request.Id, token);
        return NoContent();
    }

    /// <summary>Publishes authorized persisted script source anonymously.</summary>
    [HttpPost("Publish"), Authorize(Policy = GXScriptPolicies.Add), Authorize(Policy = GXScriptPolicies.View)]
    public async Task<ActionResult<GXScriptCommunitySubmission>> Publish(GXPublishCommunityScript request, CancellationToken token)
    {
        try
        {
            return await community.PublishAsync(Owner, request.Id, token);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException error)
        {
            return Problem(statusCode: 400, title: "Invalid community script", detail: error.Message);
        }
    }
}
