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
        private (Pet Pet, User Owner) Team1;
        private (Pet Pet, User Owner) Team2;

        /*** Turn State ***/
        private bool FightWon = false;
        private (Pet Pet, User Owner) CurrentTurnTeam;
        private FightMove Team1Move = null;
        private FightMove Team2Move = null;
        private Stack<FightMove> Team1Combo = new Stack<FightMove>();
        private Stack<FightMove> Team2Combo = new Stack<FightMove>();
        /*** Final Game State ***/
        private (Pet Pet, User Owner)? WinningTeam;

        public Battle(User host, IServiceProvider services, Action<string> gameOverCallback, string gameName = null) : base(host, services, gameOverCallback, gameName)
        {
            this.TicksPerSecond = 0.5f; //extremely low TPS because this is currently a turn based play-by-post fight
            StartTime = DateTime.UtcNow;
            Team1 = (null, host);
        }

        public override void Tick(TimeSpan deltaT)
        {
            if(FightWon)
            {
                if(WinningTeam != null)
                {
                    SendSystemMessage($"{WinningTeam.Value.Pet.PetName} wins!");
                } else
                {
                    SendSystemMessage($"This fight ends in a draw.");
                }
                return;
            }

            if(Team1Move != null && Team2Move != null) //resolve turn
            {
                var team1Bonuses = CalculateAtkDefBonuses(Team1Move, Team1Combo);
                var team2Bonuses = CalculateAtkDefBonuses(Team2Move, Team2Combo);

                int team2DamageIssued = (int)(team2Bonuses.Attack / ((double)team1Bonuses.Defense));
                int team1DamageIssued = (int)(team1Bonuses.Attack / ((double)team2Bonuses.Defense));

                if(Team1.Pet.CurrentHitPoints == 0 && Team2.Pet.CurrentHitPoints > 0) //no damage from an unconscious pet
                {
                    Team2.Pet.CurrentHitPoints += team1DamageIssued;
                    team1DamageIssued = 0;
                    FightWon = true;
                    WinningTeam = Team2;
                }
                else if (Team2.Pet.CurrentHitPoints == 0 && Team1.Pet.CurrentHitPoints > 0)
                {
                    Team1.Pet.CurrentHitPoints += team1DamageIssued;
                    team1DamageIssued = 0;
                    FightWon = true;
                    WinningTeam = Team1;
                } 
                else if (Team1.Pet.CurrentHitPoints == 0 && Team2.Pet.CurrentHitPoints == 0)
                {
                    FightWon = true;
                    WinningTeam = null;
                }

                int team1DamageTaken = Math.Min(team2DamageIssued, Team1.Pet.CurrentHitPoints);
                int team2DamageTaken = Math.Min(team1DamageIssued, Team2.Pet.CurrentHitPoints);

                Team1.Pet.CurrentHitPoints -= team1DamageTaken;
                Team2.Pet.CurrentHitPoints -= team2DamageTaken;

                //sync to DB and send messages
                UpdatePetHealth(new List<(int PetId, int HpDelta)> { (Team1.Pet.PetId, team1DamageTaken), (Team2.Pet.PetId, team2DamageTaken)});

                SendSystemMessage($"{Team1.Pet.PetName} {ConstructVerb(Team1Move, team1DamageIssued, Team2.Pet)}!");
                SendSystemMessage($"{Team2.Pet.PetName} {ConstructVerb(Team2Move, team2DamageIssued, Team1.Pet)}!");
                if(Team1.Pet.CurrentHitPoints == 0)
                {
                    SendSystemMessage($"{Team1.Pet.PetName} is knocked out!");
                }
                if (Team2.Pet.CurrentHitPoints == 0)
                {
                    SendSystemMessage($"{Team2.Pet.PetName} is knocked out!");
                }
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

            if (user.UserId != CurrentTurnTeam.Owner.UserId)
                throw new CritterException("It's not your turn!",
                    $"User {user.UserId} tried to make a move in a fight that when it wasn't their turn.", System.Net.HttpStatusCode.Forbidden);

            FightMove command = JsonConvert.DeserializeObject<FightMove>(userCommand);

            if (command.Surrender)
            {
                if (Team1.Owner.UserId == user.UserId)
                {
                    WinningTeam = Team2;
                }
                else if (Team1.Owner.UserId == user.UserId)
                {
                    WinningTeam = Team1;
                }

                SendSystemMessage($"{user.UserName} runs away!");
                FightWon = true;
                return;
            }

            if (CurrentTurnTeam == Team1)
            {
                if (Team1Move == null)
                {
                    Team1Move = command;
                    CurrentTurnTeam = Team2;
                }
            }
            else if (CurrentTurnTeam == Team2)
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

        public override async Task<bool> JoinGame(User user, string joinGameData)
        {
            Pet pet = JsonConvert.DeserializeObject<Pet>(joinGameData);
            if(user.UserId == Host.UserId)
            {
                Team1.Pet = pet;
            }
            else if(Team2.Owner.UserId == user.UserId)
            {
                Team2.Pet = pet;
            }
            return false;
        }

        private string ConstructVerb(FightMove fightMove, int damageIssued, Pet enemy)
        {
            switch (fightMove.Action)
            {
                case FightMove.AttackAction.Dodge: return $"dodges {enemy.PetName}";
                case FightMove.AttackAction.QuickAttack: return $"jabs {enemy.PetName} for {damageIssued}";
                case FightMove.AttackAction.HeavyAttack: return $"slams {enemy.PetName} for {damageIssued}";//and welcome to the jam
                default: return "does nothing at all";
            }
        }
        
        private void UpdatePetHealth(List<(int PetId, int HpDelta)> hpUpdates)
        {
            Task.Run(async () =>
            {
                using (var scope = Services.CreateScope())
                {
                    var petDomain =
                        scope.ServiceProvider
                            .GetRequiredService<PetDomain>();

                    if (hpUpdates.Count > 0)
                    {
                        await petDomain.ChangePetHealth(hpUpdates);
                    }
                }
            });
        }

        private (int Attack, int Defense) CalculateAtkDefBonuses(FightMove teamMove, Stack<FightMove> combo)
        {
            int attackBonus = 0;
            int defenseBonus = 0;
            Random random = new Random();

            int combonus = DetectComboDepth(teamMove, combo);
            Team1Combo.Push(teamMove);
            switch (teamMove.Action)
            {
                case FightMove.AttackAction.Dodge:
                    defenseBonus += 30 + 10*combonus + random.Next(0, 10);
                    break;
                case FightMove.AttackAction.QuickAttack:
                    attackBonus += 10 + 10*combonus + random.Next(0, 15);
                    break;
                case FightMove.AttackAction.HeavyAttack:
                    attackBonus += (20 - 5*combonus) + random.Next(0, 25);
                    break;
            }
            return (attackBonus, defenseBonus);
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
