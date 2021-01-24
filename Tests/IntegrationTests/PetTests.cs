using CritterServer.Models;
using CritterServer.DataAccess;
using CritterServer.Domains;
using CritterServer.Domains.Components;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using Xunit;
using Dapper;
using System.Linq;
using CritterServer.Contract;
using System.Transactions;

namespace Tests.IntegrationTests
{
    /// <summary>
    /// Creaated once, reused for all tests in PetTests
    /// Used to set up durable resources that can be reused (like users!)
    /// </summary>
    public class PetTestsContext : TestUtilities
    {
        private UserDomain userAccountDomain => new UserDomain(UserRepo, null, JWTProvider, new TransactionScopeFactory(DBConn));
        private PetDomain petDomain => new PetDomain(PetRepo, CfgRepo, new TransactionScopeFactory(DBConn));
        private IDbConnection DBConn;

        private IUserRepository UserRepo => new UserRepository(DBConn);
        private IPetRepository PetRepo => new PetRepository(DBConn);
        private IConfigRepository CfgRepo => new ConfigRepository(DBConn);

        public int PetColor1;
        public int PetColor2;
        public int PetSpecies1;
        public int PetSpecies2;
        public User OwnerUser1;
        public User OwnerUser2;

        public PetTestsContext()
        {
            using (var t = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                DBConn = GetNewDbConnection();
                PetColor1 = CfgRepo.CreatePetColor(new PetColorConfig() { Name = Guid.NewGuid().ToString().Substring(0, 5), ImagePatternPath = "8clFw0e.jpg" }).Result;
                PetColor2 = CfgRepo.CreatePetColor(new PetColorConfig() { Name = Guid.NewGuid().ToString().Substring(0, 5), ImagePatternPath = "8clFw0e.jpg" }).Result;
                PetSpecies1 = CfgRepo.CreatePetSpecies(new PetSpeciesConfig() { Name = Guid.NewGuid().ToString().Substring(0, 5), Description = "", MaxHitPoints = 1000, ImageBasePath = "https://i.imgur.com/" }).Result;
                PetSpecies2 = CfgRepo.CreatePetSpecies(new PetSpeciesConfig() { Name = Guid.NewGuid().ToString().Substring(0, 5), Description = "", MaxHitPoints = 1000, ImageBasePath = "https://i.imgur.com" }).Result;
                var uid1 = UserRepo.CreateUser(RandomUserNotPersisted()).Result.Value;
                var uid2 = UserRepo.CreateUser(RandomUserNotPersisted()).Result.Value;
                var users = UserRepo.RetrieveUsersByIds(uid1, uid2).Result;
                OwnerUser1 = users.AsList()[0];
                OwnerUser2 = users.AsList()[1];
                t.Complete();
            }
        }
    }

    public class PetTests : IClassFixture<PetTestsContext>
    {
        PetTestsContext Context;
        public UserDomain UserAccountDomain => new UserDomain(UserRepo, FriendRepo, Context.JWTProvider, new TransactionScopeFactory(TestScopedDBConn));
        public PetDomain PetDomain => new PetDomain(PetRepo, CfgRepo, new TransactionScopeFactory(TestScopedDBConn));
        IDbConnection TestScopedDBConn;
        public IUserRepository UserRepo => new UserRepository(TestScopedDBConn);
        public IFriendshipRepository FriendRepo => new FriendshipRepository(TestScopedDBConn);

        public IPetRepository PetRepo => new PetRepository(TestScopedDBConn);
        public IConfigRepository CfgRepo => new ConfigRepository(TestScopedDBConn);

        public PetTests(PetTestsContext context)
        {
            this.Context = context;
            this.TestScopedDBConn = context.GetNewDbConnection();
        }

        [Fact]
        public async void CreateAndRetrievePetById()
        {
            var nonPersistedPet = Context.RandomPetNotPersisted(Context.PetColor2, Context.PetSpecies1, Context.OwnerUser1.UserId);

            var createPet = await PetDomain.CreatePet(nonPersistedPet, Context.OwnerUser1);

            var retrievedPet = (await PetDomain.RetrievePets(createPet.PetId)).FirstOrDefault();
            
            Assert.Equal(nonPersistedPet.Name, createPet.Name);
            Assert.Equal(nonPersistedPet.Name, createPet.Name);
            Assert.Equal(nonPersistedPet.Name, retrievedPet?.Name);
            Assert.Equal(createPet.PetId, retrievedPet.PetId);

            Assert.Equal(nonPersistedPet.SpeciesId, createPet.SpeciesId);
            Assert.Equal(nonPersistedPet.ColorId, createPet.ColorId);
            Assert.Equal(nonPersistedPet.OwnerId, retrievedPet?.OwnerId);
            Assert.False(createPet.IsAbandoned);
            Assert.False(retrievedPet.IsAbandoned);
        }

        [Fact]
        public async void CreateAndRetrievePetByOwner()
        {
            var nonPersistedPet = Context.RandomPetNotPersisted(Context.PetColor2, Context.PetSpecies1, Context.OwnerUser1.UserId);
            var createPet = await PetDomain.CreatePet(nonPersistedPet, Context.OwnerUser1);
            var retrievedPets = (await PetDomain.RetrievePetsByOwner(Context.OwnerUser1.UserId)).AsList();

            Assert.Equal(nonPersistedPet.Name, createPet.Name);
            Assert.Equal(nonPersistedPet.Name, createPet.Name);
            Assert.Contains(retrievedPets, rp => nonPersistedPet.Name == rp.Name && 
            rp.PetId == createPet.PetId && 
            rp.SpeciesId == nonPersistedPet.SpeciesId && 
            rp.ColorId == nonPersistedPet.ColorId);
        }

        [Fact]
        public async void CreateAndRetrieveFullPetByOwner()
        {
            var nonPersistedPet = Context.RandomPetNotPersisted(Context.PetColor2, Context.PetSpecies1, Context.OwnerUser1.UserId);
            var createPet = await PetDomain.CreatePet(nonPersistedPet, Context.OwnerUser1);
            var retrievedPets = (await PetDomain.RetrieveFullPetInformationByOwner(Context.OwnerUser1.UserId)).AsList();

            Assert.Equal(nonPersistedPet.Name, createPet.Name);
            Assert.Equal(nonPersistedPet.Name, createPet.Name);
            Assert.Contains(retrievedPets, rp => nonPersistedPet.Name == rp.Pet.Name && 
            rp.Pet.PetId == createPet.PetId && 
            rp.Pet.SpeciesId == nonPersistedPet.SpeciesId && 
            rp.Pet.ColorId == nonPersistedPet.ColorId &&
            rp.Color.PetColorConfigId == nonPersistedPet.ColorId &&
            rp.Species.PetSpeciesConfigId == nonPersistedPet.SpeciesId);
        }

        [Fact]
        public async void CreateAndRetrieveFullPetById()
        {
            var nonPersistedPet = Context.RandomPetNotPersisted(Context.PetColor2, Context.PetSpecies1, Context.OwnerUser1.UserId);
            var createPet = await PetDomain.CreatePet(nonPersistedPet, Context.OwnerUser1);
            var retrievedPet = (await PetDomain.RetrieveFullPetInformation(createPet.PetId)).First();

            Assert.Equal(nonPersistedPet.Name, createPet.Name);
            Assert.Equal(nonPersistedPet.Name, createPet.Name);
            Assert.Equal(nonPersistedPet.Name, retrievedPet.Pet.Name);
            Assert.Equal(retrievedPet.Pet.PetId, createPet.PetId);
            Assert.Equal(retrievedPet.Pet.SpeciesId, nonPersistedPet.SpeciesId);
            Assert.Equal(retrievedPet.Pet.ColorId, nonPersistedPet.ColorId);
            Assert.Equal(retrievedPet.Color.PetColorConfigId, nonPersistedPet.ColorId);
            Assert.Equal(retrievedPet.Species.PetSpeciesConfigId, nonPersistedPet.SpeciesId);
        }

        [Fact]
        public void CreateWithInvalidSpeciesFails()
        {
            var nonPersistedPet = Context.RandomPetNotPersisted(Context.PetColor2, Int32.MinValue, Context.OwnerUser1.UserId);
            Assert.ThrowsAsync<CritterException>(() => PetDomain.CreatePet(nonPersistedPet, Context.OwnerUser1));
        }

        [Fact]
        public void CreateWithInvalidColorFails()
        {
            var nonPersistedPet = Context.RandomPetNotPersisted(Int32.MinValue, Context.PetSpecies1, Context.OwnerUser1.UserId);
            Assert.ThrowsAsync<CritterException>(() => PetDomain.CreatePet(nonPersistedPet, Context.OwnerUser1));
        }

        [Fact]
        public void RetrieveSpecies()
        {
            Assert.NotEmpty(PetDomain.RetrieveAvailableSpecies(Context.OwnerUser1).Result);
        }

        [Fact]
        public void RetrieveColors()
        {
            Assert.NotEmpty(PetDomain.RetrieveAvailableColors(Context.OwnerUser1).Result);
        }

    }
}
