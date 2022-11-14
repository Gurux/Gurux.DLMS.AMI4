using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

/// <summary>
/// Get access token from the request query.
/// </summary>
public class ConfigureJwtBearerOptions : IPostConfigureOptions<JwtBearerOptions>
{
    /// <summary>
    /// Get access token from the request query.
    /// </summary>
    public void PostConfigure(string name, JwtBearerOptions options)
    {
        var originalOnMessageReceived = options.Events.OnMessageReceived;
        options.Events.OnMessageReceived = async context =>
        {
            await originalOnMessageReceived(context);

            if (string.IsNullOrEmpty(context.Token))
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/guruxami"))
                {
                    
                    context.Token = accessToken;
                }
            }
        };
    }
}