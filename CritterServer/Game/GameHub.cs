using CritterServer.Contract;
using CritterServer.Domains;
using CritterServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CritterServer.Game
{
    [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
    public class GameHub : Hub<IGameClient>
    {
        private readonly UserDomain UserDomain;
        private readonly GameManagerService GameManager;


        public GameHub(GameManagerService gameManager, UserDomain userDomain)
        {
            this.GameManager = gameManager;
            this.UserDomain = userDomain;
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
            User activeUser = UserDomain.RetrieveUserByUserName(username).Result;
            this.Groups.AddToGroupAsync(this.Context.ConnectionId, GetChannelGroupIdentifier(gameId)).Wait();
            GameManager.JoinGame(gameId, activeUser, this.Context.ConnectionId);
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
    }

    public interface IGameClient
    {
        Task ReceiveNotification(GameAlert serverNotification);
        Task ReceiveChat(string sender, string message);
        Task ReceiveSystemMessage(string message);

    }

}

