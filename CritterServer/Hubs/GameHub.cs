using CritterServer.Contract;
using CritterServer.Domains;
using CritterServer.Game;
using CritterServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CritterServer.Hubs
{
    [HubPath("gamehub")]
    public class GameHub : BaseGameHub<IGameClient> 
    {
        public GameHub(GameManagerService gameManager, UserDomain userDomain) : base(gameManager, userDomain)
        {
        }
    }

    [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
    public class BaseGameHub<T> : Hub<T> where T : class, IGameClient
    {
        private readonly UserDomain UserDomain;
        private readonly GameManagerService GameManager;


        public BaseGameHub(GameManagerService gameManager, UserDomain userDomain)
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

        public async virtual Task Connect(string gameId)
        {
            var username = this.Context.GetHttpContext().User.Identity.Name;
            User activeUser = UserDomain.RetrieveUserByUserName(username).Result;
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, GetChannelGroupIdentifier(gameId));
            await GameManager.JoinGameChat(gameId, activeUser, this.Context.ConnectionId);
        }

        public async virtual Task SendChatMessage(string message, string gameId)
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

