using CritterServer.Domains;
using CritterServer.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CritterServer
{
    public class GuessTheNumber : Game<NumberGuess>
    {
        private DateTime StartTime;
        private ConcurrentDictionary<int, Tuple<User, List<Tuple<int, int>>>> userIDToBets = new ConcurrentDictionary<int, Tuple<User, List<Tuple<int, int>>>>();
        public GuessTheNumber(User host, IServiceProvider services, string gameName = null) : base(host, services,gameName)
        {
            this.TicksPerSecond = 0.5f;
            StartTime = DateTime.UtcNow;
        }

        public override void Tick(TimeSpan deltaT)
        {
            if(StartTime - DateTime.UtcNow > TimeSpan.FromMinutes(5))
            {
                //choose a winner
            } 
            else
            {
                //no op??
            }
        }

        private void SelectWinners()
        {

        }

        public override void AcceptUserInput(NumberGuess command, User user)
        {
            if(userIDToBets.TryGetValue(user.UserId, out var value))
            {
                if(value.Item2.Where(pair => pair.Item1 == command.NumberGuessed).Any())
                {
                    //complain
                } 
                else
                {
                    //validate that user has CashWagered + sum(all existing bets)
                    //maybe take the money _now_?
                    value.Item2.Add(Tuple.Create(command.NumberGuessed, command.CashWagered));
                }
            } 
            else
            {
                userIDToBets.TryAdd(user.UserId, Tuple.Create(user, new List<Tuple<int, int>{ Tuple.Create(command.NumberGuessed, command.CashWagered) }));
            }
        }
    }

    public class NumberGuess : GameCommand
    {
        [Range(1, Int32.MaxValue)]
        public int CashWagered { get; set; }

        public int NumberGuessed { get; set; }
    }
}
