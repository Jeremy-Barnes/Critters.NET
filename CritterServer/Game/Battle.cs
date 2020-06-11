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
    public class Battle : Game
    {
        /*** Game Info***/
        public override GameType GameType => GameType.Battle;
        private DateTime StartTime;
        private (Pet, User) Team1;
        private (Pet, User) Team2;

        /*** Turn State ***/
        private bool FightWon = false;
        private (Pet, User) CurrentTurnTeam;
        private FightMove Team1Move = null;
        private FightMove Team2Move = null;

        /*** Final Game State ***/
        private (Pet, User) WinningTeam;

        public Battle(User host, IServiceProvider services, Action<string> gameOverCallback, string gameName = null) : base(host, services, gameOverCallback, gameName)
        {
            this.TicksPerSecond = 0.5f; //extremely low TPS because this is currently a turn based play by post fight
            StartTime = DateTime.UtcNow;
        }

        public override void Tick(TimeSpan deltaT)
        {
            if(FightWon)
            {
                //todo send messages, db sync
                return;
            }

            if(Team1Move != null && Team2Move != null) //resolve turn
            {

                //at the very end, null out moves and begin next turn
                Team1Move = null;
                Team2Move = null;
                CurrentTurnTeam = Team1;
            }
        }

        public override async Task AcceptUserInput(string userCommand, User user)
        {
            if (FightWon || GameOver)
            {
                throw new CritterException("Sorry, this fight is over!", null, System.Net.HttpStatusCode.Gone);
            }

            if (user.UserId != CurrentTurnTeam.Item2.UserId)
                throw new CritterException("It's not your turn!",
                    $"User {user.UserId} tried to make a move in a fight that when it wasn't their turn.", System.Net.HttpStatusCode.Forbidden);

            FightMove command = JsonConvert.DeserializeObject<FightMove>(userCommand);

            if(command.Surrender)
            {
                if(Team1.Item2.UserId == user.UserId)
                {
                    WinningTeam = Team2;
                } 
                else if(Team1.Item2.UserId == user.UserId)
                {
                    WinningTeam = Team1;
                }
                FightWon = true;
                return;
            }

            if(CurrentTurnTeam == Team1)
            {
                if (Team1Move == null)
                {
                    Team1Move = command;
                    CurrentTurnTeam = Team2;
                }
            } 
            else if(CurrentTurnTeam == Team2)
            {
                if (Team2Move == null)
                {
                    Team2Move = command;
                    CurrentTurnTeam = Team1;
                }
            }
            else //better never happen
            {
                GameOver = true;
                throw new CritterException("Invalid fight state reached! Game over, man!",
                    $"An invalid user {user.UserId} was permitted to make a fight move, the team state is corrupted, this fight is abandoned. " +
                    $"Users {Team1.Item2.UserId} and {Team1.Item2.UserId} were robbed!", 
                    System.Net.HttpStatusCode.InternalServerError);
            }
        }
    }

    public class FightMove
    {
        public enum AttackAction
        {
            QuickAttack,
            HeavyAttack,
            Dodge
        }

        public FightMove() { }
        public AttackAction Action { get; set; }
        public bool Surrender { get; set; }
    }
}
