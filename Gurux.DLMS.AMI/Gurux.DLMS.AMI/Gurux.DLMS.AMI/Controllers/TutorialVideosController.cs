using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Provides the public tutorial feed through the application's authenticated origin.</summary>
[ApiController]
[Route("api/Block/Tutorials")]
[Authorize(Roles = GXRoles.Admin)]
public sealed class TutorialVideosController(IHttpClientFactory clients, IMemoryCache cache,
    ILogger<TutorialVideosController> logger) : ControllerBase
{
    /// <summary>Returns tutorials, caching a successful public feed for five minutes.</summary>
    [HttpGet]
    public async Task<ActionResult<GXVideoTutorialFeed>> Get(CancellationToken cancellationToken = default)
    {
        try
        {
            var feed = await cache.GetOrCreateAsync("Gurux.Ami.TutorialVideos", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                using var client = clients.CreateClient("TutorialVideos");
                return await client.GetFromJsonAsync<GXVideoTutorialFeed>("https://gurux.fi/api/ami/tutorials", cancellationToken)
                    ?? throw new JsonException("The tutorial feed was empty.");
            });
            return feed!;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Problem("Tutorial videos are currently unavailable.", statusCode: StatusCodes.Status502BadGateway);
        }
        catch (Exception error) when (error is HttpRequestException or JsonException)
        {
            logger.LogWarning(error, "Cannot read tutorial videos.");
            return Problem("Tutorial videos are currently unavailable.", statusCode: StatusCodes.Status502BadGateway);
        }
    }
}