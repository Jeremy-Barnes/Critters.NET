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
using CritterServer.Contract;

namespace Tests.IntegrationTests
{
    public class UserTestScope: TestUtilities, IDisposable
    {

        private IDbConnection ScopedDbConnection;

        public IUserRepository UserRepo;
        public UserDomain UserAccountDomain;


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

        public UserTestScope()
        {
            ScopedDbConnection = GetNewDbConnection();
            UserRepo = new UserRepository(ScopedDbConnection);
            var transactionScopeFactory = new TransactionScopeFactory(ScopedDbConnection);
            UserAccountDomain = new UserDomain(UserRepo, JWTProvider, transactionScopeFactory);
        }

        public void Dispose()
        {
        }

    }

    public class UserTests
    {

        public UserTests()
        {
        }

        [Fact]
        public void UserAccountCreateAndRetrieveWorks()
        {
            using (var scope = new UserTestScope())
            {

                User randomUser = scope.RandomUserNotPersisted();
                string jwt = scope.UserAccountDomain.CreateAccount(randomUser).Result;

                var retrievedDbUser = scope.UserAccountDomain.RetrieveUserByEmail(randomUser.EmailAddress).Result;
                Assert.Equal(randomUser.UserName, retrievedDbUser.UserName);
                Assert.NotEmpty(jwt);
            }


        }

        [Fact]
        public void UserLoginWorksAndCreatesValidJwt()
        {
            User randomUser;
            using (var scope = new UserTestScope())
            {
                randomUser = scope.RandomUserNotPersisted();
                string password = randomUser.Password;

                scope.UserAccountDomain.CreateAccount(randomUser).Wait();
                randomUser.Password = password; //gets overwritten as the hashed value during acct create

            }

            using (var scope = new UserTestScope())
            {
                string jwt = new UserTestScope().UserAccountDomain.Login(randomUser).Result;

                Assert.NotEmpty(jwt);
                Assert.True(scope.JWTProvider.ValidateToken(jwt));

            }
        }

        [Fact]
        public void DuplicateCreateFails()
        {
            User randomUser;
            using (var scope = new UserTestScope())
            {
                randomUser = scope.RandomUserNotPersisted();
                string password = randomUser.Password;

                scope.UserAccountDomain.CreateAccount(randomUser).Wait();
                randomUser.Password = password; //gets overwritten as the hashed value during acct create
                string jwt = scope.UserAccountDomain.Login(randomUser).Result;
                Assert.NotEmpty(jwt);
                Assert.True(scope.JWTProvider.ValidateToken(jwt));
            }
            using (var scope = new UserTestScope())
            {
                Assert.ThrowsAsync<CritterException>(() => scope.UserAccountDomain.CreateAccount(randomUser));
            }
        }
    }
}
