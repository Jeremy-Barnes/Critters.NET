using CritterServer.Contract;
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

namespace CritterServer.Game
{
    public class GuessTheNumber : Game
    {
        private DateTime StartTime;
        private ConcurrentDictionary<int, List<Bet>> UserIdToBets = new ConcurrentDictionary<int, List<Bet>>();
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

        private void SelectWinners(int theWinningNumber)
        {
            Task.Run(async () =>
            {
                SendSystemMessage($"The winning number is {theWinningNumber}!");
                var winners = UserIdToBets.Select(kvp => Tuple.Create<int, int?>(kvp.Key, kvp.Value.
                    Where(bettingPair => bettingPair.NumberGuessed == theWinningNumber).Select(bet => bet.CashWagered).FirstOrDefault())).Where(t => t.Item2.HasValue && t.Item2 > 0).AsList();

                await PayWinners(winners, CalculateTotalPot(), theWinningNumber);
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
                userNameToWinMessage.Add(Tuple.Create(Players[winnerAndBet.Item1].User.UserName, $"Your bet for {winnerAndBet.Item2.Value} on {winningNumber} won! You receive {winnings}"));
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

        public override async Task AcceptUserInput(string userCommand, User user)
        {

            if (BettingIsClosed)
            {
                throw new CritterException("Sorry, no longer accepting guesses!", null, System.Net.HttpStatusCode.Gone);
            }
            Bet command = JsonConvert.DeserializeObject<Bet>(userCommand);
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
                    if (value.Where(bet => bet.NumberGuessed == command.NumberGuessed).Any())
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

                SendSystemMessage($"{user.UserName} bets {command.CashWagered} on {command.NumberGuessed}, bringing to total pot up to {CalculateTotalPot()}");
                await userDomain.ChangeUserCash(-1 * command.CashWagered, user);
            }
        }

        private int CalculateTotalPot()
        {
            int totalPotSize = UserIdToBets.SelectMany(user => user.Value).Sum(bet => bet.CashWagered);
            return totalPotSize;
        }
    }

    public class Bet
    {
        public Bet() { }
        public Bet(int guess, int cash)
        {
            CashWagered = cash;
            NumberGuessed = guess;
        }
        [Range(1, Int32.MaxValue)]
        public int CashWagered { get; set; }

        public int NumberGuessed { get; set; }
    }
}
