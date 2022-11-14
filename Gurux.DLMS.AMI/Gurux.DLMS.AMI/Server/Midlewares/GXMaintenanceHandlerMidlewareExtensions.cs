using Microsoft.AspNetCore.Builder;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    /// <summary>
    /// This class is used to handle maintenance mode.
    /// </summary>
    internal static class GXMaintenanceHandlerMidlewareExtensions
    {
        public static IApplicationBuilder UseMaintenanceModeHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GXMaintenanceMidleware>();
        }
    }
}
