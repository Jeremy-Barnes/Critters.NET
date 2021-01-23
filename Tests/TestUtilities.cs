using CritterServer.Domains.Components;
using CritterServer.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Tests
{
    public class TestUtilities
    {
        static TestUtilities()
        {
            DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
        }

        protected static string jwtSecretKey = "T25lIEV4Y2VwdGlvbmFsbHkgTG9uZyBTZWNyZXQgS2V5IFBsZWFzZSEgRm9yIFJlYWwhIEV2ZW4gTG9uZ2VyIFRoYW4gWW91J2QgUmVhc29uYWJseSBBbnRpY2lwYXRl";

        public JwtProvider JWTProvider = new JwtProvider(
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


        public User RandomUserNotPersisted()
        {
            User randomUser = new User()
            {
                Birthdate = DateTime.UtcNow,
                City = "Chicago",
                Country = "USA",
                EmailAddress = $"{GetRandomString(6)}@{GetRandomString(6)}.com",
                FirstName = GetRandomString(6),
                LastName = GetRandomString(6),
                Gender = "male",
                IsActive = true,
                Password = GetRandomString(6),
                Postcode = "60654",
                Salt = "GARBAGEVALUE",
                State = "Illinois",
                UserName = GetRandomString(6)
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
                Name = Guid.NewGuid().ToString().Substring(0, 6)
            };
        }


        public IDbConnection GetNewDbConnection()
        {

            var dbConnection = DbProviderFactories.GetFactory("Npgsql").CreateConnection();
            dbConnection.ConnectionString = $"Server={Environment.GetEnvironmentVariable("PGHOST") ?? "localhost"}; Port=5432; User Id=LocalApp;Password=localapplicationpassword;Database=CrittersDB";
            return dbConnection;
        }

        public string GetRandomString(int length)
        {
            if(length < 25)
                return Guid.NewGuid().ToString().Substring(0, length);
            else
            {
                string builtUp = "";
                while(builtUp.Length < length)
                {
                    builtUp += Guid.NewGuid().ToString();
                }
                return builtUp.Substring(0, length);
            }
        }

    }
}
