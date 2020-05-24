using CritterServer.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Domains
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationHub : Hub<IUserClient>, ControllerBase
    {
        public override Task OnConnectedAsync()
        {
            notifyNewConnectionState(this.Context.User.Identity.Name, this.Context.ConnectionId, true);


            return base.OnConnectedAsync();
        }


        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (exception != null)
                Log.Error("A SignalR error happened", exception);
            notifyNewConnectionState(this.Context.User.Identity.Name, this.Context.ConnectionId, false);
            return base.OnDisconnectedAsync(exception);
        }

        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        public void Connect()
        {

        }

        private void notifyNewConnectionState(string username, string currentConnectionId, bool isConnecting)
        {
            this.Clients.AllExcept(currentConnectionId).ReceiveNotification(new Notification($"{username} has {(isConnecting ? "come online!" : "left.")}"));
        }
        protected override void Dispose(bool disposing)
        {
            //this.Dispose();
        }
    }

    public interface IUserClient
    {
        Task ReceiveNotification(Notification serverNotification);
    }

}

