using Gurux.DLMS.AMI.Server.Internal;
using Gurux.UI.Components.Enums;
using Gurux.UI.Components.Help;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Loads trusted HTML through AMI for the isolated help viewer.</summary>
[ApiController]
[Route("api/Help/Article")]
[Authorize]
public sealed class HelpArticleController(GXHelpArticleReader reader) : ControllerBase
{
    /// <summary>Returns an official HTML document, leaving sanitization to the viewer.</summary>
    [HttpGet]
    public async Task<ActionResult<GXHelpContent>> Get(string address, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || !GXAmiManualHelpCatalog.IsAllowedAddress(uri))
        {
            return BadRequest();
        }
        try
        {
            var fallback = uri.AbsolutePath.StartsWith("/fi/", StringComparison.Ordinal)
                ? new Uri("https://www.gurux.fi/" + uri.AbsolutePath[4..]) : null;
            var html = await reader.LoadHtmlAsync(uri, fallback, cancellationToken);
            return html == null ? NotFound() : new GXHelpContent(HelpKind.Html, html)
            {
                BaseAddress = uri.AbsoluteUri,
                ArticleSelector = "article.node--type-book .field--name-body, article .field--name-body"
            };
        }
        catch (Exception error) when (error is HttpRequestException or InvalidDataException)
        {
            return Problem(statusCode: 502, detail: "The help article could not be loaded.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Problem(statusCode: 504, detail: "The help article request timed out.");
        }
    }
}
