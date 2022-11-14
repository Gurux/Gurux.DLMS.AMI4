using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.SignalR;

namespace Gurux.DLMS.AMI.Hubs
{
    /// <summary>
    /// GuruxAMI Hub sends notification events.
    /// </summary>
    public class GuruxAMiHub : Hub<IGXHubEvents>
    {
        private readonly IGXHost _host;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GuruxAMiHub(IGXHost host)
        {
            _host = host;
        }

        /// <summary>
        /// Save occurred exception to the database.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                GXSystemLog log = new GXSystemLog();
                log.Message = exception.Message;
                await _host.Connection.InsertAsync(GXInsertArgs.Insert(log));
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
