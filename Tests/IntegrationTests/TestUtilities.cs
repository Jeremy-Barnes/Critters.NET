using CritterServer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Tests.IntegrationTests
{
    public class TestUtilities
    {
        static TestUtilities()
        {
            DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
        }

        public User RandomUserNotPersisted()
        {
            User randomUser = new User()
            {
                Birthdate = DateTime.UtcNow,
                City = "Chicago",
                Country = "USA",
                EmailAddress = $"{Guid.NewGuid().ToString().Substring(0, 6)}@{Guid.NewGuid().ToString().Substring(0, 6)}.com",
                FirstName = Guid.NewGuid().ToString().Substring(0, 6),
                LastName = Guid.NewGuid().ToString().Substring(0, 6),
                Gender = "male",
                IsActive = true,
                Password = Guid.NewGuid().ToString().Substring(0, 6),
                Postcode = "60654",
                Salt = "GARBAGEVALUE",
                State = "Illinois",
                UserName = Guid.NewGuid().ToString().Substring(0, 6)
            };
            return randomUser;
        }

        public Pet RandomPetNotPersisted(int color, int species, int ownerId)
        {
            var mathRand = new Random();
            return new Pet()
            {
                ColorId = color,
                SpeciesId = species,
                Gender = "other",
                CurrentHitPoints = mathRand.Next(),
                IsAbandoned = false,
                Level = mathRand.Next(),
                OwnerId = ownerId,
                PetName = Guid.NewGuid().ToString().Substring(0, 6)
            };
        }

        public IDbConnection GetNewDbConnection()
        {
            var dbConnection = DbProviderFactories.GetFactory("Npgsql").CreateConnection();
            dbConnection.ConnectionString = "Server=localhost; Port=5432; User Id=LocalApp;Password=localapplicationpassword;Database=CrittersDB";
            return dbConnection;
        }

    }
}
