using CritterServer.Contract;
using CritterServer.Domains;
using CritterServer.Game;
using CritterServer.Models;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Hubs
{
    [HubPath("battlehub")]
    public class BattleHub : BaseGameHub<IBattleClient>
    {
        PetDomain PetDomain;
        public BattleHub(MultiplayerGameService gameManager, UserDomain userDomain, PetDomain petDomain) : base(gameManager, userDomain)
        {
            this.PetDomain = petDomain;
        }

        //Correct flow
        //GameController PUT CreateGame
        //SignalR battlehub/gamehub Connect
        //SignalR ConfigureMatch
        public async Task ConfigureMatch(string gameId, int hostPetId, string? allowedUserName, int? petId)
        {
            Battle game = this.GameManager.GetGame(gameId) as Battle;
            if (game == null) return;

            var username = this.Context.GetHttpContext().User.Identity.Name;

            var hostAndPet = await GetUserAndPetForUsername(username, hostPetId);

            if (game.Host.UserId != hostAndPet.Owner.UserId)
            {
                throw new CritterException("Sorry, you're not the host!", $"Some chump {hostAndPet.Owner.UserId} tried to configure a match {gameId} they didn't own", System.Net.HttpStatusCode.Forbidden);
            }

            if (!string.IsNullOrEmpty(allowedUserName) && petId.HasValue)
            {
                var team2 = await GetUserAndPetForUsername(allowedUserName, petId.Value);
                game.ChallengeTeamToBattle(team2.Owner, team2.Pet);
            }

            await game.JoinGame(hostAndPet.Owner, hostAndPet.Pet);

        }

        //Correct flow 
        //GameController PUT JoinGame
        //SignalR battlehub/gamehub Connect
        //SignalR AcceptChallenge
        //SignalR SendMove
        public async Task AcceptChallenge(string gameId, int petId)
        {
            var game = this.GameManager.GetGame(gameId) as Battle;
            if (game != null)
            {
                var username = this.Context.GetHttpContext().User.Identity.Name;
                var ownerAndPet = await GetUserAndPetForUsername(username, petId);
                await game.JoinGame(ownerAndPet.Owner, ownerAndPet.Pet);
            }
        }

        public async Task SendMove(string gameId, BattleMove move)
        {
            var username = this.Context.GetHttpContext().User.Identity.Name;
            var game = this.GameManager.GetGame(gameId) as Battle;
            await game.AcceptUserInput(move, username);
        }

        private async Task<(User Owner, Pet Pet)> GetUserAndPetForUsername(string ownerUsername, int petId)
        {
            User activeUser = await this.UserDomain.RetrieveUserByUserName(ownerUsername);
            Pet pet = (await this.PetDomain.RetrievePets(petId)).FirstOrDefault();

            if (pet == null || pet.OwnerId != activeUser.UserId)
            {
                throw new CritterException("That's not your pet!", $"Some creep {activeUser.UserId} tried to enter a battle with a pet {petId} that wasn't theirs", System.Net.HttpStatusCode.Forbidden);
            }
            return (activeUser, pet);
        }
    }

    public interface IBattleClient : IGameClient
    {
        Task ReceiveGamestate(BattleState gameState);
    }
}
