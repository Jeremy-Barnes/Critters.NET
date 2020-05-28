using CritterServer.Contract;
using CritterServer.Models;
using CritterServer.Pipeline.Middleware;
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
    [ApiController]
    public class GameHub : Hub<IGameClient>
    {
        private readonly MessageDomain messageDomain;
        private readonly UserDomain userDomain;
        public GameHub(MessageDomain messageDomain, UserDomain userDomain)
        {
            this.messageDomain = messageDomain;
            this.userDomain = userDomain;
        }

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
            var username = this.Context.GetHttpContext().User.Identity.Name;
            User activeUser = userDomain.RetrieveUserByUserName(username).Result;
            var channels = messageDomain.GetChannels(null, activeUser).Result;
            foreach (var channel in channels)
            {
                this.Groups.AddToGroupAsync(this.Context.ConnectionId, GetChannelGroupIdentifier(channel.Channel.ChannelId)).Wait();
            }
        }

        private void notifyNewConnectionState(string username, string currentConnectionId, bool isConnecting)
        {
            this.Clients.AllExcept(currentConnectionId).ReceiveNotification(new Notification($"{username} has {(isConnecting ? "come online!" : "left.")}"));
        }

        public static string GetChannelGroupIdentifier(int channelId)
        {
            return $"ChatChannel{channelId}";
        }

        protected override void Dispose(bool disposing)
        {
            //this.Dispose();
        }
    }

    public interface IGameClient
    {
        Task ReceiveNotification(Notification serverNotification);
    }

}

