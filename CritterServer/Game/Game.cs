using CritterServer.Domains;
using CritterServer.Models;
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

namespace CritterServer
{
    public abstract class Game
    {
        public User Host { get; internal set; }
        public int ticks;
        public string Id { get; internal set; }
        public bool GameOver { get; internal set; }
        protected float TicksPerSecond { get; set; }
        protected IServiceProvider Services;
        private Action<string> GameEndCallBack;
        public abstract GameType GameType { get;}
        public Game(User host, IServiceProvider services, Action<string> gameEndCallBack, string gameName = null)
        {
            this.Host = host;
            this.ticks = 0;
            this.Id = gameName ?? Guid.NewGuid().ToString().Substring(0, 6);
            this.TicksPerSecond = 60f;
            this.Services = services;
            this.GameEndCallBack = gameEndCallBack;
            this.GameOver = false;
        }

        public void Run()
        {
            try { 
                Stopwatch timer = new Stopwatch();

                TimeSpan totalLastTickTimeMs = TimeSpan.Zero;
                while (!GameOver && ticks < Int32.MaxValue)
                {
                    timer.Restart();
                    ticks++;
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

    }

    public enum GameType
    {
        NumberGuesser,
        Snake,
        TicTacToe
            //3 games! Pretty ambitious.
    }
}
