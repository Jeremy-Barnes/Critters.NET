using CritterServer.Contract;
using CritterServer.Domains;
using CritterServer.Hubs;
using CritterServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CritterServer.Game
{
    public class Battle : Game<IBattleClient, BattleHub>
    {
        /*** Game Info***/
        public override GameType GameType => GameType.Battle;
        private DateTime StartTime;
        private (Pet Pet, User Owner) Team1;
        private (Pet Pet, User Owner) Team2;

        /*** Turn State ***/
        private bool FightWon = false;
        private BattleMove Team1Move = null;
        private BattleMove Team2Move = null;
        private Stack<BattleMove> Team1Combo = new Stack<BattleMove>();
        private Stack<BattleMove> Team2Combo = new Stack<BattleMove>();
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
            } 
            else if(Team1Move != null && Team2Move != null) //resolve turn
            {
                var team1Bonuses = CalculateAtkDefBonuses(Team1Move, Team1Combo);
                var team2Bonuses = CalculateAtkDefBonuses(Team2Move, Team2Combo);

                int team2DamageIssued = (int)(team2Bonuses.Attack / ((double)Math.Max(1, team1Bonuses.Defense)));
                int team1DamageIssued = (int)(team1Bonuses.Attack / ((double)Math.Max(1, team2Bonuses.Defense)));

                if(Team1.Pet.CurrentHitPoints == 0 && Team2.Pet.CurrentHitPoints > 0) //no damage from an unconscious pet
                {
                    Team2.Pet.CurrentHitPoints += team1DamageIssued;
                    team1DamageIssued = 0;
                    FightWon = true;
                    WinningTeam = Team2;
                }
                else if (Team2.Pet.CurrentHitPoints == 0 && Team1.Pet.CurrentHitPoints > 0)
                {
                    Team1.Pet.CurrentHitPoints += team2DamageIssued;
                    team2DamageIssued = 0;
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
            }
            SendGameState();
        }

        public override async Task AcceptUserInput(string userCommand, User user)
        {
            BattleMove command = JsonConvert.DeserializeObject<BattleMove>(userCommand);
            await AcceptUserInput(command, user.UserName);
        }

        public async Task AcceptUserInput(BattleMove command, string userName)
        {
            if (FightWon || GameOver)
            {
                throw new CritterException("Sorry, this fight is over!", null, System.Net.HttpStatusCode.Gone);
            }
            User user = this.Players.GetPlayer(userName).User;

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
                else
                {
                    return;
                }

                SendSystemMessage($"{user.UserName} runs away!");
                FightWon = true;
                return;
            }


            if (user.UserId == Team1.Owner.UserId && Team1Move == null)
            {
                Team1Move = command;
            }
            else if (user.UserId == Team2.Owner.UserId && Team2Move == null)
            {
                Team2Move = command;
            }
        }

        public async Task<bool> JoinGame(User user, Pet pet)
        {
            if(user.UserId == Host.UserId)
            {
                Team1.Owner = user;
                Team1.Pet = pet;
            }
            else if(Team2.Owner == null || Team2.Owner.UserId == user.UserId)
            {
                if(pet.PetId == Team2.Pet.PetId)
                    Team2.Pet = pet;
            } 
            else
            {
                return false;
            }
            return true;
        }

        public void ChallengeTeamToBattle(User user, Pet pet)
        {
            Team2 = (pet, user);
        }

        private void SendGameState()
        {
            Task.Run(async () =>
            {
                var gameState = new BattleState() {
                    Team1MoveSubmitted = Team1Move != null,
                    Team2MoveSubmitted = Team2Move != null,
                    Team1Pet = Team1.Pet,
                    Team2Pet = Team2.Pet,
                    Team1Player = Team1.Owner,
                    Team2Player = Team2.Owner,
                    Timestamp = DateTime.UtcNow
                };
                using (var scope = Services.CreateScope())
                {
                    var hubContext =
                        scope.ServiceProvider
                            .GetRequiredService<IHubContext<BattleHub, IBattleClient>>();

                    await hubContext.Clients.Group(GameHub.GetChannelGroupIdentifier(this.Id)).ReceiveGamestate(gameState);
                }
            });
        }

        private string ConstructVerb(BattleMove fightMove, int damageIssued, Pet enemy)
        {
            switch (fightMove.Action)
            {
                case BattleMove.AttackAction.Dodge: return $"dodges {enemy.PetName}";
                case BattleMove.AttackAction.QuickAttack: return $"jabs {enemy.PetName} for {damageIssued}";
                case BattleMove.AttackAction.HeavyAttack: return $"slams {enemy.PetName} for {damageIssued}";//and welcome to the jam
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

        private (int Attack, int Defense) CalculateAtkDefBonuses(BattleMove teamMove, Stack<BattleMove> combo)
        {
            int attackBonus = 0;
            int defenseBonus = 0;
            Random random = new Random();

            int combonus = DetectComboDepth(teamMove, combo);
            Team1Combo.Push(teamMove);
            switch (teamMove.Action)
            {
                case BattleMove.AttackAction.Dodge:
                    defenseBonus += 30 + 10*combonus + random.Next(0, 10);
                    break;
                case BattleMove.AttackAction.QuickAttack:
                    attackBonus += 10 + 10*combonus + random.Next(0, 15);
                    break;
                case BattleMove.AttackAction.HeavyAttack:
                    attackBonus += (20 - 5*combonus) + random.Next(0, 25);
                    break;
            }
            return (attackBonus, defenseBonus);
        }

        private int DetectComboDepth(BattleMove thisTurn, Stack<BattleMove> currentCombo)
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

    public class BattleMove
    {
        public enum AttackAction
        {
            QuickAttack,
            HeavyAttack,
            Dodge
        }

        public BattleMove() { }
        public AttackAction Action { get; set; }
        public bool Surrender { get; set; }
    }

    public struct BattleState
    {
        public Pet Team1Pet { get; set; }
        public User Team1Player { get; set; }
        public Pet Team2Pet { get; set; }
        public User Team2Player { get; set; }
        public bool Team1MoveSubmitted { get; set; }
        public bool Team2MoveSubmitted { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
