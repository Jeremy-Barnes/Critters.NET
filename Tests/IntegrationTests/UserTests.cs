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

namespace Tests.IntegrationTests
{
    public class UserTestsContext
    {
        private static string jwtSecretKey = "T25lIEV4Y2VwdGlvbmFsbHkgTG9uZyBTZWNyZXQgS2V5IFBsZWFzZSEgRm9yIFJlYWwhIEV2ZW4gTG9uZ2VyIFRoYW4gWW91J2QgUmVhc29uYWJseSBBbnRpY2lwYXRl";

        public IDbConnection dbConnection;
        public UserAuthenticationDomain userAccountDomain;
        public IUserRepository userRepo;
        public JwtProvider jwtProvider = new JwtProvider(
            jwtSecretKey,
            new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSecretKey)),
                ValidIssuer = "critters!",
                ValidateAudience = false,
                ValidateActor = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = true
            });

        public List<User> extantUsers = new List<User>();

        public UserTestsContext()
        {
            DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
            dbConnection = DbProviderFactories.GetFactory("Npgsql").CreateConnection();
            dbConnection.ConnectionString = "Server=localhost; Port=5432; User Id=LocalApp;Password=localapplicationpassword;Database=CrittersDB";
            userRepo = new UserRepository(dbConnection);
            userAccountDomain = new UserAuthenticationDomain(userRepo, jwtProvider);
        }

        public User RandomUser()
        {
            User randomUser = new User()
            {
                Birthdate = DateTime.UtcNow.ToShortDateString(),
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
            this.extantUsers.Add(randomUser);
            return randomUser;
        }
    }

    public class UserTests : IClassFixture<UserTestsContext>
    {
        UserTestsContext context;

        public UserTests(UserTestsContext context)
        {
            this.context = context;
        }

        [Fact]
        public void UserAccountCreateAndRetrieveWorks()
        {
            User randomUser = context.RandomUser();
            string jwt = context.userAccountDomain.CreateAccount(randomUser).Result;

            var retrievedDbUser = context.userAccountDomain.RetrieveUserByEmail(randomUser.EmailAddress);
            Assert.Equal(randomUser.UserName, retrievedDbUser.UserName);
            Assert.NotEmpty(jwt);

        }

        [Fact]
        public void UserLoginWorksAndCreatesValidJwt()
        {
            User randomUser = context.RandomUser();
            string password = randomUser.Password;

            context.userAccountDomain.CreateAccount(randomUser).Wait();
            randomUser.Password = password; //gets overwritten as the hashed value during acct create

            string jwt = context.userAccountDomain.Login(randomUser);

            Assert.NotEmpty(jwt);
            Assert.True(context.jwtProvider.ValidateToken(jwt));
        }

        [Fact]
        public void DuplicateCreateFails()
        {
            User randomUser = context.RandomUser();
            string password = randomUser.Password;

            context.userAccountDomain.CreateAccount(randomUser).Wait();
            randomUser.Password = password; //gets overwritten as the hashed value during acct create

            string jwt = context.userAccountDomain.Login(randomUser);

            Assert.NotEmpty(jwt);
            Assert.True(context.jwtProvider.ValidateToken(jwt));

            Assert.ThrowsAny<Exception>(() => context.userAccountDomain.CreateAccount(randomUser).Result);
        }
    }
}
