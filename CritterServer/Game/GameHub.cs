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
    [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
    public class GameHub : Hub<IGameClient>
    {
        private readonly MessageDomain messageDomain;
        private readonly UserDomain userDomain;
        private readonly GameManagerService GameManager;


        public GameHub(GameManagerService gameManager, UserDomain userDomain)
        {
            this.GameManager = gameManager;
            this.userDomain = userDomain;
        }


        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (exception != null)
                Log.Error("A SignalR error happened", exception);
            return base.OnDisconnectedAsync(exception);
        }

        public void Connect(string gameId)
        {
            var username = this.Context.GetHttpContext().User.Identity.Name;
            User activeUser = userDomain.RetrieveUserByUserName(username).Result;
            this.Groups.AddToGroupAsync(this.Context.ConnectionId, GetChannelGroupIdentifier(gameId)).Wait();
           
        }

        public async void SendChatMessage(string message, string gameId)
        {
            //todo some kind of content filtering for the love of god
            await this.Clients.OthersInGroup(GetChannelGroupIdentifier(gameId)).ReceiveChat(this.Context.User.Identity.Name, message);
        }

        public static string GetChannelGroupIdentifier(string gameId)
        {
            return $"GameChannel{gameId}";
        }

        //protected override void Dispose(bool disposing)
        //{
        //    Dispose();
        //}
    }

    public interface IGameClient
    {
        Task ReceiveNotification(GameAlert serverNotification);
        Task ReceiveChat(string sender, string message);
        Task ReceiveSystemMessage(string message);

    }

}

