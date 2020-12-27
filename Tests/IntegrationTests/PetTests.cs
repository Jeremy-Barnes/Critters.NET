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

namespace Tests.IntegrationTests
{
    /// <summary>
    /// Creaated once, reused for all tests in PetTests
    /// Used to hold expensive resources that can be reused (like a DB connection!)
    /// </summary>
    public class PetTestsContext : TestUtilities
    {
        UserDomain userAccountDomain => new UserDomain(userRepo, jwtProvider);
        PetDomain petDomain => new PetDomain(petRepo, cfgRepo);
        //public IDbTransaction scopedDbTransaction;
        public IDbConnection scopedDbConn;

        IUserRepository userRepo => new UserRepository(scopedDbConn);//, scopedDbTransaction);
        IPetRepository petRepo => new PetRepository(scopedDbConn);//, scopedDbTransaction);
        IConfigRepository cfgRepo => new ConfigRepository(scopedDbConn);//, scopedDbTransaction);

        public JwtProvider jwtProvider = new JwtProvider(
            jwtSecretKey,
            new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSecretKey)),
                ValidIssuer = "critters!",
                ValidateAudience = false,
                ValidateActor = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = true
            });

        public int PetColor1;
        public int PetColor2;
        public int PetSpecies1;
        public int PetSpecies2;
        public User OwnerUser1;
        public User OwnerUser2;

        public PetTestsContext()
        {
            scopedDbConn = GetNewDbConnection();
            scopedDbConn.Open();
            //scopedDbTransaction = scopedDbConn.BeginTransaction(IsolationLevel.ReadCommitted);
            PetColor1 = cfgRepo.CreatePetColor(new PetColorConfig() { ColorName = Guid.NewGuid().ToString().Substring(0, 5), ImagePatternPath = "8clFw0e.jpg" }).Result;
            PetColor2 = cfgRepo.CreatePetColor(new PetColorConfig() { ColorName = Guid.NewGuid().ToString().Substring(0, 5), ImagePatternPath = "8clFw0e.jpg" }).Result;
            PetSpecies1 = cfgRepo.CreatePetSpecies(new PetSpeciesConfig() { SpeciesName = Guid.NewGuid().ToString().Substring(0, 5), Description = "", MaxHitPoints = 1000, ImageBasePath = "https://i.imgur.com/" }).Result;
            PetSpecies2 = cfgRepo.CreatePetSpecies(new PetSpeciesConfig() { SpeciesName = Guid.NewGuid().ToString().Substring(0, 5), Description = "", MaxHitPoints = 1000, ImageBasePath = "https://i.imgur.com" }).Result;
            var uid1 = userRepo.CreateUser(RandomUserNotPersisted()).Result.Value;
            var uid2 = userRepo.CreateUser(RandomUserNotPersisted()).Result.Value;
            var users = userRepo.RetrieveUsersByIds(uid1, uid2).Result;
            OwnerUser1 = users.AsList()[0];
            OwnerUser2 = users.AsList()[1];
        }

    }

    public class PetTests : IClassFixture<PetTestsContext>
    {
        PetTestsContext context;
        public UserDomain userAccountDomain => new UserDomain(userRepo, context.jwtProvider);
        public PetDomain petDomain => new PetDomain(petRepo, cfgRepo);
        IDbTransaction scopedDbTransaction;
        IDbConnection scopedDbConn;
        public IUserRepository userRepo => new UserRepository(scopedDbConn);//, scopedDbTransaction);
        public IPetRepository petRepo => new PetRepository(scopedDbConn);//, scopedDbTransaction);
        public IConfigRepository cfgRepo => new ConfigRepository(scopedDbConn);//, scopedDbTransaction);

        public PetTests(PetTestsContext context)
        {
            this.context = context;
            this.scopedDbConn = context.scopedDbConn;
            //this.scopedDbTransaction = context.scopedDbTransaction;
        }

        [Fact]
        public async void CreateAndRetrievePetById()
        {
            var nonPersistedPet = context.RandomPetNotPersisted(context.PetColor2, context.PetSpecies1, context.OwnerUser1.UserId);

            var createPet = await petDomain.CreatePet(nonPersistedPet, context.OwnerUser1);

            var retrievedPet = (await petDomain.RetrievePets(createPet.PetId)).FirstOrDefault();
            
            Assert.Equal(nonPersistedPet.PetName, createPet.PetName);
            Assert.Equal(nonPersistedPet.PetName, createPet.PetName);
            Assert.Equal(nonPersistedPet.PetName, retrievedPet?.PetName);
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
            var nonPersistedPet = context.RandomPetNotPersisted(context.PetColor2, context.PetSpecies1, context.OwnerUser1.UserId);
            var createPet = await petDomain.CreatePet(nonPersistedPet, context.OwnerUser1);
            var retrievedPets = (await petDomain.RetrievePetsByOwner(context.OwnerUser1.UserId)).AsList();

            Assert.Equal(nonPersistedPet.PetName, createPet.PetName);
            Assert.Equal(nonPersistedPet.PetName, createPet.PetName);
            Assert.Contains(retrievedPets, rp => nonPersistedPet.PetName == rp.PetName && 
            rp.PetId == createPet.PetId && 
            rp.SpeciesId == nonPersistedPet.SpeciesId && 
            rp.ColorId == nonPersistedPet.ColorId);
        }

        [Fact]
        public async void CreateAndRetrieveFullPetByOwner()
        {
            var nonPersistedPet = context.RandomPetNotPersisted(context.PetColor2, context.PetSpecies1, context.OwnerUser1.UserId);
            var createPet = await petDomain.CreatePet(nonPersistedPet, context.OwnerUser1);
            var retrievedPets = (await petDomain.RetrieveFullPetInformationByOwner(context.OwnerUser1.UserId)).AsList();

            Assert.Equal(nonPersistedPet.PetName, createPet.PetName);
            Assert.Equal(nonPersistedPet.PetName, createPet.PetName);
            Assert.Contains(retrievedPets, rp => nonPersistedPet.PetName == rp.Pet.PetName && 
            rp.Pet.PetId == createPet.PetId && 
            rp.Pet.SpeciesId == nonPersistedPet.SpeciesId && 
            rp.Pet.ColorId == nonPersistedPet.ColorId &&
            rp.Color.PetColorConfigID == nonPersistedPet.ColorId &&
            rp.Species.PetSpeciesConfigID == nonPersistedPet.SpeciesId);
        }

        [Fact]
        public async void CreateAndRetrieveFullPetById()
        {
            var nonPersistedPet = context.RandomPetNotPersisted(context.PetColor2, context.PetSpecies1, context.OwnerUser1.UserId);
            var createPet = await petDomain.CreatePet(nonPersistedPet, context.OwnerUser1);
            var retrievedPet = (await petDomain.RetrieveFullPetInformation(createPet.PetId)).First();

            Assert.Equal(nonPersistedPet.PetName, createPet.PetName);
            Assert.Equal(nonPersistedPet.PetName, createPet.PetName);
            Assert.Equal(nonPersistedPet.PetName, retrievedPet.Pet.PetName);
            Assert.Equal(retrievedPet.Pet.PetId, createPet.PetId);
            Assert.Equal(retrievedPet.Pet.SpeciesId, nonPersistedPet.SpeciesId);
            Assert.Equal(retrievedPet.Pet.ColorId, nonPersistedPet.ColorId);
            Assert.Equal(retrievedPet.Color.PetColorConfigID, nonPersistedPet.ColorId);
            Assert.Equal(retrievedPet.Species.PetSpeciesConfigID, nonPersistedPet.SpeciesId);
        }

        [Fact]
        public void CreateWithInvalidSpeciesFails()
        {
            var nonPersistedPet = context.RandomPetNotPersisted(context.PetColor2, Int32.MinValue, context.OwnerUser1.UserId);
            Assert.ThrowsAsync<CritterException>(() => petDomain.CreatePet(nonPersistedPet, context.OwnerUser1));
        }

        [Fact]
        public void CreateWithInvalidColorFails()
        {
            var nonPersistedPet = context.RandomPetNotPersisted(Int32.MinValue, context.PetSpecies1, context.OwnerUser1.UserId);
            Assert.ThrowsAsync<CritterException>(() => petDomain.CreatePet(nonPersistedPet, context.OwnerUser1));
        }

        [Fact]
        public void RetrieveSpecies()
        {
            Assert.NotEmpty(petDomain.RetrieveAvailableSpecies(context.OwnerUser1).Result);
        }

        [Fact]
        public void RetrieveColors()
        {
            Assert.NotEmpty(petDomain.RetrieveAvailableColors(context.OwnerUser1).Result);
        }

    }
}
