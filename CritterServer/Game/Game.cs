using CritterServer.Domains;
using CritterServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
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
        public Dictionary<int, Player> Players { get; protected set; }
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
            this.Players = new Dictionary<int, Player>();
            if(host != null)
                Players.Add(host.UserId, new Player(host));
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

        public void JoinGame(User user, string signalRConnectonId)
        {
            if (Players.ContainsKey(user.UserId))
            {
                var player = Players[user.UserId];
                player.SignalRConnectionId = signalRConnectonId;
                player.User = user;
            }
            else 
            {
                var player = new Player(user, signalRConnectonId);
                Players.Add(user.UserId, player);
            }
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
