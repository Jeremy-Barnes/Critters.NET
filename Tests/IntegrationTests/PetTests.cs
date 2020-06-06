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

namespace Tests.IntegrationTests
{
    /// <summary>
    /// Creaated once, reused for all tests in UserTests
    /// Used to hold expensive resources that can be reused (like a DB connection!)
    /// </summary>
    public class PetTestsContext : TestUtilities
    {
        private static string jwtSecretKey = "T25lIEV4Y2VwdGlvbmFsbHkgTG9uZyBTZWNyZXQgS2V5IFBsZWFzZSEgRm9yIFJlYWwhIEV2ZW4gTG9uZ2VyIFRoYW4gWW91J2QgUmVhc29uYWJseSBBbnRpY2lwYXRl";

        public UserDomain userAccountDomain => new UserDomain(userRepo, jwtProvider);
        public PetDomain petDomain => new PetDomain(petRepo, cfgRepo);

        public IUserRepository userRepo => new UserRepository(GetNewDbConnection());
        public IPetRepository petRepo => new PetRepository(GetNewDbConnection());
        public IConfigRepository cfgRepo => new ConfigRepository(GetNewDbConnection());

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
            PetColor1 = cfgRepo.CreatePetColor(new PetColorConfig() { ColorName = Guid.NewGuid().ToString().Substring(0, 5), ImagePatternPath = "8clFw0e.jpg" }).Result;
            PetColor2 = cfgRepo.CreatePetColor(new PetColorConfig() { ColorName = Guid.NewGuid().ToString().Substring(0, 5), ImagePatternPath = "8clFw0e.jpg" }).Result;
            PetSpecies1 = cfgRepo.CreatePetSpecies(new PetSpeciesConfig() { SpeciesName = Guid.NewGuid().ToString().Substring(0, 5), Description = "", MaxHitPoints = 1000, ImageBasePath = "https://i.imgur.com/" }).Result;
            PetSpecies2 = cfgRepo.CreatePetSpecies(new PetSpeciesConfig() { SpeciesName = Guid.NewGuid().ToString().Substring(0, 5), Description = "", MaxHitPoints = 1000, ImageBasePath = "https://i.imgur.com" }).Result;
            var uid1 = userRepo.CreateUser(RandomUserNotPersisted());
            var uid2 = userRepo.CreateUser(RandomUserNotPersisted());
            var users = userRepo.RetrieveUsersByIds(uid1, uid2).Result;
            OwnerUser1 = users.AsList()[0];
            OwnerUser2 = users.AsList()[1];
        }

    }

    public class PetTests : IClassFixture<PetTestsContext>
    {
        PetTestsContext context;

        public PetTests(PetTestsContext context)
        {
            this.context = context;
        }

        [Fact]
        public async void CreateAndRetrievePetById()
        {
            var nonPersistedPet = context.RandomPetNotPersisted(context.PetColor2, context.PetSpecies1, context.OwnerUser1.UserId);

            var createPet = await context.petDomain.CreatePet(nonPersistedPet, context.OwnerUser1);

            var retrievedPet = (await context.petDomain.RetrievePets(createPet.PetId)).FirstOrDefault();
            
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
            var createPet = await context.petDomain.CreatePet(nonPersistedPet, context.OwnerUser1);
            var retrievedPets = (await context.petDomain.RetrievePetsByOwner(context.OwnerUser1.UserId)).AsList();

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
            var createPet = await context.petDomain.CreatePet(nonPersistedPet, context.OwnerUser1);
            var retrievedPets = (await context.petDomain.RetrieveFullPetInformationByOwner(context.OwnerUser1.UserId)).AsList();

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
            var createPet = await context.petDomain.CreatePet(nonPersistedPet, context.OwnerUser1);
            var retrievedPet = (await context.petDomain.RetrieveFullPetInformation(createPet.PetId)).First();

            Assert.Equal(nonPersistedPet.PetName, createPet.PetName);
            Assert.Equal(nonPersistedPet.PetName, createPet.PetName);
            Assert.Equal(nonPersistedPet.PetName, retrievedPet.Pet.PetName);
            Assert.Equal(retrievedPet.Pet.PetId, createPet.PetId);
            Assert.Equal(retrievedPet.Pet.SpeciesId, nonPersistedPet.SpeciesId);
            Assert.Equal(retrievedPet.Pet.ColorId, nonPersistedPet.ColorId);
            Assert.Equal(retrievedPet.Color.PetColorConfigID, nonPersistedPet.ColorId);
            Assert.Equal(retrievedPet.Species.PetSpeciesConfigID, nonPersistedPet.SpeciesId);
        }

    }
}
