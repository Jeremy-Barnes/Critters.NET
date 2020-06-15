using CritterServer.Contract;
using CritterServer.Hubs;
using CritterServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CritterServer.Game
{
    public abstract class Game
    {
        public User Host { get; protected set; }
        public string Id { get; protected set; }
        public abstract GameType GameType { get; }
        public bool GameOver { get; protected set; }
        public PlayerCache Players { get; protected set; }
        protected float TicksPerSecond { get; set; }
        protected int Ticks { get; set; }

        protected readonly IServiceProvider Services;

        private Action<string> GameEndCallBack;

        public Game(User host, IServiceProvider services, Action<string> gameEndCallBack, string gameName = null)
        {
            this.Host = host;
            this.Ticks = 0;
            this.Id = gameName ?? Guid.NewGuid().ToString().Substring(0, 6);
            this.TicksPerSecond = 60f;
            this.Services = services;
            this.GameEndCallBack = gameEndCallBack;
            this.GameOver = false;
            this.Players = new PlayerCache();
            if(host != null)
                Players.AddPlayer(new Player(host));
        }

        public void Run()
        {
            try { 
                Stopwatch timer = new Stopwatch();

                TimeSpan totalLastTickTimeMs = TimeSpan.Zero;
                while (!GameOver && Ticks < Int32.MaxValue)
                {
                    timer.Restart();
                    Ticks++;
                    this.Tick(totalLastTickTimeMs);            
                    Thread.Sleep(Math.Max(0, (int)((TimeSpan.FromSeconds(1.0) - (timer.Elapsed * TicksPerSecond)) / TicksPerSecond).TotalMilliseconds));
                    totalLastTickTimeMs = timer.Elapsed;
                }
                GameEndCallBack.Invoke(this.Id);
            } 
            catch(Exception ex)
            {
                Log.Error(ex, "Game error");
            }
        }
        public abstract void Tick(TimeSpan deltaT);
        public abstract Task AcceptUserInput(string userCommand, User user);

        /// <summary>
        /// Adds user to the list of players, without signalR connection ID (added in JoinGameChat method)
        /// Async and overrideable so that games can allow hosts to permit/reject each player
        /// </summary>
        /// <param name="user"></param>
        /// <param name="joinGameData"></param>
        /// <returns></returns>
        public virtual async Task<bool> JoinGame(User user, string joinGameData)
        {
            var player = new Player(user, null);
            Players.AddPlayer(player);
            return true;
        }
        
        /// <summary>
        /// Async and overrideable so that games can allow hosts to permit/reject each player
        /// </summary>
        /// <param name="user"></param>
        /// <param name="signalRConnectonId"></param>
        /// <returns></returns>
        public virtual async Task<bool> JoinGameChat(User user, string signalRConnectonId)
        {
            Players.AddPlayer(new Player(user, signalRConnectonId));
            return true;
        }

        protected void SendSystemMessage(string message)
        {
            Task.Run(async () =>
            {
                using (var scope = Services.CreateScope())
                {
                    var hubContext =
                        scope.ServiceProvider
                            .GetRequiredService<IHubContext<GameHub, IGameClient>>();

                    await hubContext.Clients.Group(GameHub.GetChannelGroupIdentifier(this.Id)).ReceiveSystemMessage(message);
                }
            });
        }

        protected void SendAlert(string message, List<string> userNames = null, List<Tuple<string, string>> userNameAndMessages = null)
        {
            Task.Run(async () =>
            {
                using (var scope = Services.CreateScope())
                {
                    var hubContext =
                        scope.ServiceProvider
                            .GetRequiredService<IHubContext<GameHub, IGameClient>>();
                    GameAlert alert = new GameAlert(message, this.GameType);

                    if (userNames != null && userNames.Any() && !string.IsNullOrEmpty(message))
                        await hubContext.Clients.Users(userNames).ReceiveNotification(alert);
                    if (userNameAndMessages != null && userNameAndMessages.Any())
                    {
                        foreach (var userNameToMessage in userNameAndMessages)
                        {
                            await hubContext.Clients.User(userNameToMessage.Item1).ReceiveNotification(new GameAlert(userNameToMessage.Item2, this.GameType));
                        }
                    }
                    if ((userNames == null || !userNames.Any()) && !string.IsNullOrEmpty(message))
                    {
                        await hubContext.Clients.Group(GameHub.GetChannelGroupIdentifier(this.Id)).ReceiveNotification(alert);
                    }
                }
            });
        }

        public async virtual void TerminateGame()
        {
            using (var scope = Services.CreateScope())
            {
                var hubContext =
                    scope.ServiceProvider
                        .GetRequiredService<IHubContext<GameHub, IGameClient>>();
                await hubContext.Clients.Group(GameHub.GetChannelGroupIdentifier(this.Id)).ReceiveSystemMessage($"Game {this.Id} has ended.");
                foreach(Player p in Players.Values)
                    await hubContext.Groups.RemoveFromGroupAsync(p.SignalRConnectionId, GameHub.GetChannelGroupIdentifier(this.Id));
            }
        }

        ~Game()
        {
            GameOver = true;
            TerminateGame();
        }
    }

    public enum GameType
    {
        NumberGuesser,
        Snake,
        TicTacToe,
        Battle,
        Wheel,
            //5 games! Pretty ambitious.
    }
}
