using CritterServer.Contract;
using CritterServer.Domains;
using CritterServer.Hubs;
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
            }
        }

        public override async Task AcceptUserInput(string userCommand, User user)
        {
            FightMove command = JsonConvert.DeserializeObject<FightMove>(userCommand);
            await AcceptUserInput(command, user.UserName);
        }

        public async Task AcceptUserInput(FightMove command, string userName)
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
                Team1.Pet = pet;
            }
            else if(Team2.Owner.UserId == user.UserId)
            {
                Team2.Pet = pet;
            } 
            else
            {
                return false;
            }
            return true;
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

    [HubPath("battlehub")]
    public class BattleHub : BaseGameHub<IBattleClient>
    {
        PetDomain PetDomain;
        public BattleHub(GameManagerService gameManager, UserDomain userDomain, PetDomain petDomain): base(gameManager, userDomain)
        {
            this.PetDomain = petDomain;
        }

        //Correct flow
        //GameController PUT CreateGame
        //SignalR battlehub/gamehub Connect
        //SignalR ConfigureMatch
        // IF ConfigureMatch didn't set up the Hosts pet, JoinGame
        public async Task ConfigureMatch(string gameId, int hostPetId, string? allowedUserName, int? petId)
        {
            Battle game = this.GameManager.GetGame(gameId) as Battle;
            var username = this.Context.GetHttpContext().User.Identity.Name;
            User activeUser;
            Pet pet = null;
            if (petId.HasValue) {
                var ownerAndPet = await GetUserAndPetForUsername(username, petId.Value);
                activeUser = ownerAndPet.Owner;
                pet = ownerAndPet.Pet;
            } else
            {
                activeUser = await this.UserDomain.RetrieveUserByUserName(username);
            }
            if (game.Host.UserId != activeUser.UserId)
            {
                throw new CritterException("Sorry, you're not the host!", $"Some chump {activeUser.UserId} tried to configure a match {gameId} they didn't own", System.Net.HttpStatusCode.Forbidden);
            }
            if (pet != null) {
                await game.JoinGame(activeUser, pet);
            }
        }

        //Correct flow 
        //GameController PUT JoinGame
        //SignalR battlehub/gamehub Connect
        //SignalR AcceptChallenge
        //SignalR SendMove
        public async Task AcceptChallenge(string gameId, int petId)
        {
            var username = this.Context.GetHttpContext().User.Identity.Name;
            var game = this.GameManager.GetGame(gameId) as Battle;
            var ownerAndPet = await GetUserAndPetForUsername(username, petId);
            await game.JoinGame(ownerAndPet.Owner, ownerAndPet.Pet);
        }

        public async Task SendMove(string gameId, FightMove move)
        {
            var username = this.Context.GetHttpContext().User.Identity.Name;
            var game = this.GameManager.GetGame(gameId) as Battle;
            await game.AcceptUserInput(move, username);
        }

        private async Task<(User Owner,Pet Pet)> GetUserAndPetForUsername(string ownerUsername, int petId)
        {
            User activeUser = await this.UserDomain.RetrieveUserByUserName(ownerUsername);
            Pet pet = (await this.PetDomain.RetrievePets(petId)).FirstOrDefault();

            if (pet.OwnerId != activeUser.UserId)
            {
                throw new CritterException("That's not your pet!", $"Some creep {activeUser.UserId} tried to enter a battle with a pet {petId} that wasn't theirs", System.Net.HttpStatusCode.Forbidden);
            }

            return (activeUser, pet);
        }
    }

    public interface IBattleClient : IGameClient
    {
        Task ReceiveGamestate();
    }
}
