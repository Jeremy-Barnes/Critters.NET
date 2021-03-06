﻿using CritterServer.Contract;
using CritterServer.Models;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CritterServer.Game
{
    public class MultiplayerGameService : IHostedService
    {
        ConcurrentDictionary<string, MultiplayerGame> RunningGames = new ConcurrentDictionary<string, MultiplayerGame>();
        ConcurrentBag<Task> RunningTasks = new ConcurrentBag<Task>();
        IServiceProvider Services;
        public MultiplayerGameService(IServiceProvider services)
        {
            Services = services;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public List<string> GetGames(GameType? gameType)
        {
            if(gameType.HasValue)
                return RunningGames.Where(g => g.Value.GameType == gameType).Select(kvp => kvp.Key).AsList();
            else
                return RunningGames.Keys.AsList();
        }

        public MultiplayerGame GetGame(string gameId)
        {
            try
            {
                return this.RunningGames[gameId];
            } 
            catch(KeyNotFoundException)
            {
                return null;
            }
        }

        public string StartGame(string gameId, GameType gameType, User host)
        {
            try
            {
                if (GetGame(gameId) != null) throw new CritterException("A game already exists with that name!", null, HttpStatusCode.Conflict);
                MultiplayerGame game = null;
                switch (gameType)
                {
                    case GameType.NumberGuesser: game = new GuessTheNumber(host, Services, EndGame, gameId); break;
                    case GameType.Battle: game = new Battle(host, Services, EndGame, gameId); break;
                }
                if (game == null) return null;

                RunningGames.TryAdd(game.Id, game);
                RunningTasks.Add(Task.Run(game.Run));
                return game.Id;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Start game error");
                if (ex is CritterException) throw;
                throw new CritterException("Couldn't start game!", "", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<bool> RequestJoinGame(string gameId, User user, string joinGameData)
        {
            var game = GetGame(gameId);
            return await game?.JoinGameWithoutChat(user, joinGameData);
        }

        public void Dispatch(string command, string gameId, User user)
        {
            try
            {
                if (RunningGames.ContainsKey(gameId))
                {
                    RunningGames[gameId].AcceptUserInput(command, user);
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
                if (ex is CritterException) throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private void EndGame(string gameId)
        {
            try
            {
                if(this.RunningGames.TryRemove(gameId, out var value))
                {
                    value.TerminateGame();
                }
            } 
            catch(Exception ex)
            {
                Log.Error($"Error ending game {gameId}", ex);
            }
        }
    }
}
