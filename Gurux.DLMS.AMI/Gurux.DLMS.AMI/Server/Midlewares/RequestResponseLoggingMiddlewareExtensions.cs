namespace Gurux.DLMS.AMI.Server.Midlewares
{
    /// <summary>
    /// This class is used to save all user actions for the User log table.
    /// </summary>
    internal static class RequestResponseLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
        }
    }
}
