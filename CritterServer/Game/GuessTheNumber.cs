﻿using CritterServer.Contract;
using CritterServer.Domains;
using CritterServer.Models;
using Dapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer
{
    public class GuessTheNumber : Game
    {
        private DateTime StartTime;
        private ConcurrentDictionary<int, List<Bet>> UserIdToBets = new ConcurrentDictionary<int, List<Bet>>();
        private ConcurrentDictionary<int, User> LocalUserCacheByUserId = new ConcurrentDictionary<int, User>();
        private bool BettingIsClosed = false;
        public override GameType GameType => GameType.NumberGuesser;

        public GuessTheNumber(User host, IServiceProvider services, Action<string> gameOverCallback, string gameName = null) : base(host, services, gameOverCallback, gameName)
        {
            this.TicksPerSecond = 0.5f;
            StartTime = DateTime.UtcNow;
        }

        private bool LastCallSent = false;


        public override void Tick(TimeSpan deltaT)
        {
            if(DateTime.UtcNow - StartTime >= TimeSpan.FromMinutes(5))
            {
                BettingIsClosed = true;
                Random random = new Random();
                int theWinningNumber = random.Next(1, 10);
                SelectWinners(theWinningNumber);
                GameOver = true;
            }
            else if(!LastCallSent && DateTime.UtcNow - StartTime > TimeSpan.FromMinutes(3.5))
            {
                LastCallSent = true;
                SendSystemMessage("Only a minute left! Get your bets in!");
            }
        }

        private void SendSystemMessage(string message)
        {
            Task.Run(async () =>
            {
                using (var scope = Services.CreateScope())
                {
                    var hubContext =
                        scope.ServiceProvider
                            .GetRequiredService<IHubContext<GameHub,IGameClient>>();

                    await hubContext.Clients.Group(GameHub.GetChannelGroupIdentifier(this.Id)).ReceiveSystemMessage(message);
                }
            });
        }

        private void SendAlert(string message, List<string> userNames = null, List<Tuple<string, string>> userNameAndMessages = null)
        {
            Task.Run(async () =>
            {
                using (var scope = Services.CreateScope())
                {
                    var hubContext =
                        scope.ServiceProvider
                            .GetRequiredService<IHubContext<GameHub, IGameClient>>();
                    GameAlert alert = new GameAlert(message, GameType);

                    if(userNames != null && userNames.Any() && !string.IsNullOrEmpty(message))
                        await hubContext.Clients.Users(userNames).ReceiveNotification(alert);
                    if (userNameAndMessages != null && userNameAndMessages.Any())
                    {
                        foreach (var userNameToMessage in userNameAndMessages)
                        {
                            await hubContext.Clients.User(userNameToMessage.Item1).ReceiveNotification(new GameAlert(userNameToMessage.Item2, GameType));
                        }
                    }
                    if((userNames == null || !userNames.Any()) && !string.IsNullOrEmpty(message))
                    {
                        await hubContext.Clients.Group(GameHub.GetChannelGroupIdentifier(this.Id)).ReceiveNotification(alert);
                    }
                }
            });
        }

        private void SelectWinners(int theWinningNumber)
        {
            Task.Run(async () =>
            {
                SendSystemMessage($"The winning number is {theWinningNumber}!");
                var winners = UserIdToBets.Select(kvp => Tuple.Create<int, int?>(kvp.Key, kvp.Value.
                    Where(bettingPair => bettingPair.NumberGuess == theWinningNumber).Select(bet => bet.Wager).FirstOrDefault())).Where(t => t.Item2.HasValue && t.Item2 > 0).AsList();

                await PayWinners(winners, calculateTotalPot(), theWinningNumber);
            });
        }

        private async Task PayWinners(List<Tuple<int, int?>> winnersAndTheirBets, int totalPotSize, int winningNumber)
        {
            int winningAmount = winnersAndTheirBets.Sum(t => t.Item2).Value;
            List<Tuple<int, int>> winnerAndTheirWinnings = new List<Tuple<int, int>>();
            List<Tuple<string, string>> userNameToWinMessage = new List<Tuple<string, string>>();
            foreach (var winnerAndBet in winnersAndTheirBets)
            {
                int winnings = (int)(((1.0 * winnerAndBet.Item2.Value) / (1.0 * winningAmount)) * totalPotSize);
                winnerAndTheirWinnings.Add(Tuple.Create(winnerAndBet.Item1, winnings));
                userNameToWinMessage.Add(Tuple.Create(LocalUserCacheByUserId[winnerAndBet.Item1].UserName, $"Your bet for {winnerAndBet.Item2.Value} on {winningNumber} won! You receive {winnings}"));
            }
            SendAlert(null, null, userNameToWinMessage);
            using (var scope = Services.CreateScope())
            {
                var userDomain =
                    scope.ServiceProvider
                        .GetRequiredService<UserDomain>();

                if (winnerAndTheirWinnings.Count > 0)
                {
                    await userDomain.ChangeUsersCash(winnerAndTheirWinnings);
                }
            }
        }

        private int calculateTotalPot()
        {
            int totalPotSize = UserIdToBets.SelectMany(user => user.Value).Sum(bet => bet.Wager);
            return totalPotSize;
        }

        public override async Task AcceptUserInput(string userCommand, User user)
        {

            if (BettingIsClosed)
            {
                throw new CritterException("Sorry, no longer accepting guesses!", null, System.Net.HttpStatusCode.Gone);
            }
            NumberGuess command = JsonConvert.DeserializeObject<NumberGuess>(userCommand);
            using (var scope = Services.CreateScope())
            {
                var userDomain =
                    scope.ServiceProvider
                        .GetRequiredService<UserDomain>();

                if (user.UserId == 0)
                {
                    user = await userDomain.RetrieveUserByUserName(user.UserName);
                }
                if (command.CashWagered > user.Cash)
                {
                    throw new CritterException("You don't have enough to cover that bet!", null, System.Net.HttpStatusCode.BadRequest);
                }


                if (UserIdToBets.TryGetValue(user.UserId, out var value))
                {
                    if (value.Where(bet => bet.NumberGuess == command.NumberGuessed).Any())
                    {
                        throw new CritterException("You can't guess the same number twice!", null, System.Net.HttpStatusCode.BadRequest);
                    }
                    else
                    {
                        value.Add(new Bet(command.NumberGuessed, command.CashWagered));
                    }
                }
                else
                {
                    UserIdToBets.TryAdd(user.UserId, new List<Bet> { new Bet(command.NumberGuessed, command.CashWagered) });
                }
                LocalUserCacheByUserId.TryAdd(user.UserId, user);

                SendSystemMessage($"{user.UserName} bets {command.CashWagered} on {command.NumberGuessed}, bringing to total pot up to {calculateTotalPot()}");
                await userDomain.ChangeUserCash(-1 * command.CashWagered, user);
            }
        }
    }

    public class Bet
    {
        public int NumberGuess;
        public int Wager;

        public Bet(int numberGuess, int wager)
        {
            this.NumberGuess = numberGuess;
            this.Wager = wager; 
        }
    }

    public class NumberGuess
    {
        [Range(1, Int32.MaxValue)]
        public int CashWagered { get; set; }

        public int NumberGuessed { get; set; }
    }
}
