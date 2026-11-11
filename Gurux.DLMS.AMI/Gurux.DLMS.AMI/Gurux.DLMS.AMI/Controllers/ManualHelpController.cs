using Gurux.DLMS.AMI.Server.Internal;
using Gurux.UI.Components.Enums;
using Gurux.UI.Components.Help;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Provides the official manual as embedded help without website chrome.</summary>
[ApiController]
[Route("api/Help/Manual")]
[Authorize]
public sealed class ManualHelpController(GXHelpArticleReader reader) : ControllerBase
{
    /// <summary>Returns HTML for article extraction in the isolated help view.</summary>
    [HttpGet]
    public async Task<ActionResult<GXHelpContent>> Get(string? topic = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var address = GXAmiManualHelpCatalog.GetAddress(topic);
            var html = await reader.LoadHtmlAsync(address, cancellationToken);
            return html == null ? NotFound() : new GXHelpContent(HelpKind.Html, html)
            {
                BaseAddress = address.AbsoluteUri,
                ArticleSelector = "article.node--type-book .field--name-body, article .field--name-body"
            };
        }
        catch (Exception error) when (error is HttpRequestException or InvalidDataException)
        {
            return Problem(statusCode: 502, detail: "The official AMI manual could not be loaded.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Problem(statusCode: 504, detail: "The official AMI manual request timed out.");
        }
    }
}
