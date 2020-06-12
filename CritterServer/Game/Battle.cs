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
        private Stack<FightMove> Team1Combo = new Stack<FightMove>();
        private Stack<FightMove> Team2Combo = new Stack<FightMove>();
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

                var team1Bonuses = CalculateAtkDefBonuses(Team1Move, Team1Combo);
                var team2Bonuses = CalculateAtkDefBonuses(Team2Move, Team2Combo);

                int team1DamageTaken = (int)(team2Bonuses.Item1 / ((double)team1Bonuses.Item2));
                int team2DamageTaken = (int)(team1Bonuses.Item1 / ((double)team2Bonuses.Item2));

                Team1.Item1.CurrentHitPoints -= team1DamageTaken;
                Team2.Item1.CurrentHitPoints -= team2DamageTaken;

                if(Team1.Item1.CurrentHitPoints <= 0 && Team2.Item1.CurrentHitPoints > 0) //no damage from an unconscious pet
                {
                    Team2.Item1.CurrentHitPoints += team2DamageTaken;
                }
                else if (Team2.Item1.CurrentHitPoints <= 0 && Team1.Item1.CurrentHitPoints > 0)
                {
                    Team1.Item1.CurrentHitPoints += team1DamageTaken;
                }

                //syncDB and send messages

                //at the very end, null out moves and begin next turn
                Team1Move = null;
                Team2Move = null;
                CurrentTurnTeam = Team1;
            }
        }

        private (int, int) CalculateAtkDefBonuses(FightMove teamMove, Stack<FightMove> combo)
        {
            int attackBonus = 0;
            int defenceBonus = 0;
            Random random = new Random();

            int combonus = DetectComboDepth(teamMove, combo);
            Team1Combo.Push(teamMove);
            switch (teamMove.Action)
            {
                case FightMove.AttackAction.Dodge:
                    defenceBonus += 30 + 10*combonus + random.Next(0, 10);
                    break;
                case FightMove.AttackAction.QuickAttack:
                    attackBonus += 10 + 10*combonus + random.Next(0, 15);
                    break;
                case FightMove.AttackAction.HeavyAttack:
                    attackBonus += (20 - 5*combonus) + random.Next(0, 25);
                    break;
            }
            return (attackBonus, defenceBonus);
        }

        private int DetectComboDepth(FightMove thisTurn, Stack<FightMove> currentCombo)
        {
            int comboDepth = 0;

            if (currentCombo.Count > 2 && currentCombo.Peek().Action == thisTurn.Action)
            {
                while (currentCombo.Count > 0 && currentCombo.Peek().Action == thisTurn.Action) //todo other combo definitions (dodge, dodge, quick)
                {
                    comboDepth++;
                }
            }
            if(currentCombo.Count > 0)
                currentCombo.Clear();
            return comboDepth;
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
