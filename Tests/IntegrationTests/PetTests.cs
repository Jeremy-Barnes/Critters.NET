using CritterServer.Models;
using CritterServer.DataAccess;
using CritterServer.Domains;
using System;
using System.Data;
using Xunit;
using Dapper;
using System.Linq;
using CritterServer.Contract;

namespace Tests.IntegrationTests
{
    /// <summary>
    /// Created once, reused for all tests in PetTests
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
                DBConn.Dispose();
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
        public void CreateAndRetrievePetById()
        {
            try
            {
                var nonPersistedPet = Context.RandomPetNotPersisted(Context.PetColor2, Context.PetSpecies1, Context.OwnerUser1.UserId);

                var createPet = PetDomain.CreatePet(nonPersistedPet, Context.OwnerUser1).Result;

                var retrievedPet = (PetDomain.RetrievePets(createPet.PetId)).Result.FirstOrDefault();

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
            catch (AggregateException a)
            {
                Console.WriteLine(a.Message);
                foreach (Exception innerException in a.Flatten().InnerExceptions)
                {
                    Console.WriteLine(innerException.Message);
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        [Fact]
        public void CreateAndRetrievePetByOwner()
        {
            try
            {
                var nonPersistedPet = Context.RandomPetNotPersisted(Context.PetColor2, Context.PetSpecies1, Context.OwnerUser1.UserId);
                var createPet = PetDomain.CreatePet(nonPersistedPet, Context.OwnerUser1).Result;
                var retrievedPets = (PetDomain.RetrievePetsByOwner(Context.OwnerUser1.UserId)).Result.AsList();

                Assert.Equal(nonPersistedPet.Name, createPet.Name);
                Assert.Equal(nonPersistedPet.Name, createPet.Name);
                Assert.Contains(retrievedPets, rp => nonPersistedPet.Name == rp.Name &&
                rp.PetId == createPet.PetId &&
                rp.SpeciesId == nonPersistedPet.SpeciesId &&
                rp.ColorId == nonPersistedPet.ColorId);
            }
            catch (AggregateException a)
            {
                Console.WriteLine(a.Message);
                foreach (Exception innerException in a.Flatten().InnerExceptions)
                {
                    Console.WriteLine(innerException.Message);
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        [Fact]
        public void CreateAndRetrieveFullPetByOwner()
        {
            try
            {
                var nonPersistedPet = Context.RandomPetNotPersisted(Context.PetColor2, Context.PetSpecies1, Context.OwnerUser1.UserId);
                var createPet = PetDomain.CreatePet(nonPersistedPet, Context.OwnerUser1).Result;
                var retrievedPets = (PetDomain.RetrieveFullPetInformationByOwner(Context.OwnerUser1.UserId)).Result.AsList();

                Assert.Equal(nonPersistedPet.Name, createPet.Name);
                Assert.Equal(nonPersistedPet.Name, createPet.Name);
                Assert.Contains(retrievedPets, rp => nonPersistedPet.Name == rp.Pet.Name &&
                rp.Pet.PetId == createPet.PetId &&
                rp.Pet.SpeciesId == nonPersistedPet.SpeciesId &&
                rp.Pet.ColorId == nonPersistedPet.ColorId &&
                rp.Color.PetColorConfigId == nonPersistedPet.ColorId &&
                rp.Species.PetSpeciesConfigId == nonPersistedPet.SpeciesId);
            }
            catch (AggregateException a)
            {
                Console.WriteLine(a.Message);
                foreach (Exception innerException in a.Flatten().InnerExceptions)
                {
                    Console.WriteLine(innerException.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }

        [Fact]
        public void CreateAndRetrieveFullPetById()
        {
            try
            {
                var nonPersistedPet = Context.RandomPetNotPersisted(Context.PetColor2, Context.PetSpecies1, Context.OwnerUser1.UserId);
                var createPet = PetDomain.CreatePet(nonPersistedPet, Context.OwnerUser1).Result;
                var retrievedPet = (PetDomain.RetrieveFullPetInformation(createPet.PetId)).Result.First();

                Assert.Equal(nonPersistedPet.Name, createPet.Name);
                Assert.Equal(nonPersistedPet.Name, createPet.Name);
                Assert.Equal(nonPersistedPet.Name, retrievedPet.Pet.Name);
                Assert.Equal(retrievedPet.Pet.PetId, createPet.PetId);
                Assert.Equal(retrievedPet.Pet.SpeciesId, nonPersistedPet.SpeciesId);
                Assert.Equal(retrievedPet.Pet.ColorId, nonPersistedPet.ColorId);
                Assert.Equal(retrievedPet.Color.PetColorConfigId, nonPersistedPet.ColorId);
                Assert.Equal(retrievedPet.Species.PetSpeciesConfigId, nonPersistedPet.SpeciesId);
            }
            catch (AggregateException a)
            {
                Console.WriteLine(a.Message);
                foreach (Exception innerException in a.Flatten().InnerExceptions)
                {
                    Console.WriteLine(innerException.Message);
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
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
            try
            {
                var nonPersistedPet = Context.RandomPetNotPersisted(Int32.MinValue, Context.PetSpecies1, Context.OwnerUser1.UserId);
                Assert.ThrowsAsync<CritterException>(() => PetDomain.CreatePet(nonPersistedPet, Context.OwnerUser1));
            }
            catch (AggregateException a)
            {
                Console.WriteLine(a.Message);
                foreach (Exception innerException in a.Flatten().InnerExceptions)
                {
                    Console.WriteLine(innerException.Message);
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;

            }
        }

        [Fact]
        public void RetrieveSpecies()
        {
            try
            {
                Assert.NotEmpty(PetDomain.RetrieveAvailableSpecies(Context.OwnerUser1).Result);
            }
            catch (AggregateException a)
            {
                Console.WriteLine(a.Message);
                foreach (Exception innerException in a.Flatten().InnerExceptions)
                {
                    Console.WriteLine(innerException.Message);
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;

            }

        }

        [Fact]
        public void RetrieveColors()
        {
            try
            {
                Assert.NotEmpty(PetDomain.RetrieveAvailableColors(Context.OwnerUser1).Result);
            }
            catch (AggregateException a)
            {
                Console.WriteLine(a.Message);
                foreach (Exception innerException in a.Flatten().InnerExceptions)
                {
                    Console.WriteLine(innerException.Message);
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

    }
}
