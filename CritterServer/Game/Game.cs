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
    public class Game
    {
        public User Host { get; internal set; }
        public int loops;
        public string Id { get; internal set; }
        protected float TicksPerSecond { get; set; }
        protected IServiceProvider Services;
        public Game(User host, IServiceProvider services)
        {
            this.Host = host;
            this.loops = 0;
            this.Id = Guid.NewGuid().ToString().Substring(0, 6);
            this.TicksPerSecond = 60f;
            this.Services = services;
        }

        public void Run()
        {
            try { 
                Stopwatch timer = new Stopwatch();

                TimeSpan timeSinceLastDbSync = TimeSpan.Zero;
                TimeSpan totalLastTickTimeMs = TimeSpan.Zero;
                while (loops < Int32.MaxValue)
                {
                    timer.Restart();
                    loops++;
                    timeSinceLastDbSync += totalLastTickTimeMs;
                    if (TimeSpan.FromSeconds(10) - timeSinceLastDbSync <= TimeSpan.Zero)
                    {
                        SyncToDb();
                        timeSinceLastDbSync = TimeSpan.Zero;
                    }
                    Thread.Sleep(Math.Max(0, (int)((TimeSpan.FromSeconds(1.0) - (timer.Elapsed * TicksPerSecond)) / TicksPerSecond).TotalMilliseconds));
                    totalLastTickTimeMs = timer.Elapsed;
                }
            } 
            catch(Exception ex)
            {
                Log.Error(ex, "Game error");
            }
        }

        public void AcceptUserInput(string command, User user)
        {
            UsersToDbSync.Push(user);
        }

        private ConcurrentStack<User> UsersToDbSync = new ConcurrentStack<User>();

        private void SyncToDb()
        {
            Task.Run(async () =>
            {
                using (var scope = Services.CreateScope())
                {
                    var userDomain =
                        scope.ServiceProvider
                            .GetRequiredService<UserDomain>();
                   
                    if (UsersToDbSync.TryPop(out User user))
                        await userDomain.ChangeUserCash(-5, user);
                }
            });
        }

    }
}
