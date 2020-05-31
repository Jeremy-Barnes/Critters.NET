using CritterServer.Contract;
using CritterServer.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CritterServer
{
    public class GameManagerService : IHostedService
    {
        Dictionary<string, Game> games = new Dictionary<string, Game>();
        List<Task> runningGames = new List<Task>();
        IServiceProvider Services;
        public GameManagerService(IServiceProvider services)
        {
            Services = services;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public string StartGame(User host)
        {
            try 
            { 
                var game = new GuessTheNumber(host, Services, EndGame);
                games.Add(game.Id, game);
                runningGames.Add(Task.Run(game.Run));
                return game.Id;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Start game error");
                throw;
            }
        }

        public void Dispatch(string command, string gameId, User user)
        {
            try
            {
                if (games.ContainsKey(gameId))
                {
                    games[gameId].AcceptUserInput(command, user);
                }
                else
                {
                    throw new CritterException("That game doesn't exist!", $"User {user.UserId} failed to issue command to gameId {gameId}",
                        System.Net.HttpStatusCode.NotFound, Microsoft.Extensions.Logging.LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Game dispatch error");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private void EndGame(string gameId)
        {
            this.games.Remove(gameId);
        }
    }
}
