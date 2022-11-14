using Microsoft.AspNetCore.Builder;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    /// <summary>
    /// This class is used to save all user actions for the User log table.
    /// </summary>
    internal static class GXErrorHandlerMidlewareExtensions
    {
        public static IApplicationBuilder UseErrorHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GXErrorHandlerMidleware>();
        }
    }
}
