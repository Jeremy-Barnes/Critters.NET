using CritterServer.Models;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CritterServer
{
    public class GameManagerService : IHostedService
    {
        List<Game> games = new List<Game>();
        List<Task> runningGames = new List<Task>();
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void StartGame(User host)
        {
            var game = new Game(host);
            games.Add(game);
            runningGames.Add(Task.Run(game.Run));
        }

        public string PrintLoops()
        {
            string loops = "";

            foreach(Game g in games)
            {
                loops += $"Game loop at {g.loops} \r\n";
            }

            return loops;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
