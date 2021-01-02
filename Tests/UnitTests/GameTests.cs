using CritterServer.DataAccess;
using CritterServer.Domains;
using CritterServer.Domains.Components;
using CritterServer.Game;
using CritterServer.Hubs;
using CritterServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Tests.UnitTests
{
    public class GameTests
    {

        [Fact]
        public void BattleOnlyAllowsPermittedPlayers()
        {
            PetRepo = new Mock<IPetRepository>();
            CfgRepo = new Mock<IConfigRepository>();
            UserRepo = new Mock<IUserRepository>();
            TransactionScopeFactory = new Mock<ITransactionScopeFactory>();
            TransactionScopeFactory.Setup(itsf => itsf.Create()).Returns(new System.Transactions.TransactionScope());
            CoachZ = new User
            {
                Birthdate = new DateTime(1963, 9, 5),
                Cash = 100,
                City = "Freecountry",
                Country = "USA",
                EmailAddress = "coachzed@sbemail.com",
                FirstName = "Coach",
                LastName = "Z",
                Postcode = "12345",
                State = "Sweet, Sweet Rainbow Bridge",
                UserId = 1,
                UserName = "DaCoach",
                Gender = "Male",
                IsActive = true
            };

            StrongBad = new User
            {
                Birthdate = new DateTime(1985, 9, 5),
                Cash = 500,
                City = "Behind The Tire",
                Country = "Strong Badia",
                EmailAddress = "strongbad@homestarrunner.com",
                FirstName = "Strong",
                LastName = "Bad",
                Postcode = "12344",
                State = "Near The Fence",
                UserId = 2,
                UserName = "SedBed",
                Gender = "Male",
                IsActive = true
            };

            Homsar = new User
            {
                Birthdate = new DateTime(1985, 9, 5),
                Cash = 500,
                City = "Unknown",
                Country = "Homestarmy",
                EmailAddress = "î̸̢͓͕̰͎͓̖̻͆ͅ'̶̢̹̮͙͚̪͓̝̥͂͆̀̀̈͛̐͛̈́͠m̶̹̫͎͉̮͈̠̫̮̞̑͗̽͂͋̐̓͋̕͝ạ̶̛̦͔̹͑͐̍͌̃͊̒͘͘s̷͇͌̔̚o̶̢̹̞͖̖̪̮͉̗̍̿̒̽̔́̓͠ͅn̵̼̦̭̍̅̏͂̌̿g̴̬̤͕̎̽͐̎̓̊͗̒͝f̷̩̓͘͝r̷̡̧̤̺̹̄̄̂͆̈́̍̔ǒ̶͚̬̠̑̏̇͑̍͋̅̕m̵̠̝͔͙͐̅̔̀̂̌̓̀͌͝ͅt̴̡͇̻̼̲̮̯͖̓h̵̻̥͎̋̅̊̅̋̏͐̊̿͐ë̶̢̻̳̟̹̝̼͔̬̜́̍̄̑̿̎ś̸̭͎̝̔̚i̷̢͚̦̫͗x̵̢̢̞̟̞͖̤̟̬̕ͅt̴͔͓͋͐̃̉̔̄͠͝î̷̘͖̥̲̓͐͜ȩ̵̦͚͉̱̰͈̫̋̆̓͆̉̿͝s̴̨̨͕̻̱̖͖̎͒̈́͊",
                FirstName = "Homsar",
                LastName = "CupOfCoffee",
                Postcode = "123",
                State = "???",
                UserId = 3,
                UserName = "StaveItOff123",
                Gender = "Male",
                IsActive = true
            };

            TheCheat = new Pet
            {
                ColorId = 1,
                CurrentHitPoints = 50,
                Gender = "male",
                IsAbandoned = false,
                Level = 0,
                OwnerId = 2,
                PetId = 1,
                Name = "The Cheat",
                SpeciesId = 1
            };

            TheSneak = new Pet
            {
                ColorId = 5,
                CurrentHitPoints = 40,
                Gender = "male",
                IsAbandoned = false,
                Level = 0,
                OwnerId = 1,
                PetId = 2,
                Name = "The Sneak",
                SpeciesId = 2
            };

            StripedGreenRabbit = new Pet
            {
                ColorId = 3,
                CurrentHitPoints = 40,
                Gender = "male",
                IsAbandoned = false,
                Level = 0,
                OwnerId = 3,
                PetId = 3,
                Name = "Stripe`d Green Rabbit",
                SpeciesId = 3
            };

            AddPet(TheSneak);
            AddPet(TheCheat);
            AddUser(CoachZ);
            AddUser(StrongBad);
            AddUser(Homsar);
            AddPet(StripedGreenRabbit);
            PetDomain = new PetDomain(PetRepo.Object, CfgRepo.Object, TransactionScopeFactory.Object);
            UserDomain = new UserDomain(UserRepo.Object, null, TransactionScopeFactory.Object);

            var battleHubContext = new Mock<IHubContext<BattleHub, IBattleClient>>();
            //var battleHub = new Mock<BattleHub>().Setup(bh => bh.AcceptChallenge(It.IsAny<string>(), It.IsAny<int>())).
            var isp = new Mock<IServiceProvider>();
            isp.Setup(p => p.GetService(typeof(PetDomain))).Returns(PetDomain);
            isp.Setup(p => p.GetService(typeof(IHubContext<BattleHub, IBattleClient>))).Returns(battleHubContext.Object);
            Battle game = new Battle(CoachZ, isp.Object, (s) => { }, "The Very Cool Game");

            Assert.True(game.JoinGame(CoachZ, TheSneak).Result);
            game.ChallengeTeamToBattle(StrongBad, TheCheat);
            Assert.False(game.JoinGame(Homsar, StripedGreenRabbit).Result);
            Assert.False(game.JoinGame(Homsar, TheSneak).Result);
            Assert.True(game.JoinGame(StrongBad, TheCheat).Result);
        }

        private void AddPet(Pet pet)
        {
            PetRepo.Setup(pd => pd.RetrievePetsByIds(pet.PetId)).ReturnsAsync(new List<Pet> { pet });
        }

        private void AddUser(User user)
        {
            UserRepo.Setup(ud => ud.RetrieveUsersByIds(user.UserId)).ReturnsAsync(new List<User> { user });
            UserRepo.Setup(ud => ud.RetrieveUsersByUserName(user.UserName)).ReturnsAsync(new List<User> { user });
        }

        PetDomain PetDomain;
        UserDomain UserDomain;
        Mock<IConfigRepository> CfgRepo;
        Mock<IPetRepository> PetRepo;
        Mock<IUserRepository> UserRepo;
        Mock<ITransactionScopeFactory> TransactionScopeFactory;
        User CoachZ;
        User StrongBad;
        User Homsar;
        Pet TheCheat;
        Pet TheSneak;
        Pet StripedGreenRabbit;
    }
}
