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
    /// <summary>
    /// Creaated once, reused for all tests in UserTests
    /// Used to hold expensive resources that can be reused (like a DB connection!)
    /// </summary>
    public class UserTestsContext : TestUtilities
    {
        private static string jwtSecretKey = "T25lIEV4Y2VwdGlvbmFsbHkgTG9uZyBTZWNyZXQgS2V5IFBsZWFzZSEgRm9yIFJlYWwhIEV2ZW4gTG9uZ2VyIFRoYW4gWW91J2QgUmVhc29uYWJseSBBbnRpY2lwYXRl";
        public UserDomain userAccountDomain => new UserDomain(userRepo, jwtProvider);
        public IUserRepository userRepo => new UserRepository(GetNewDbConnection());

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

        public UserTestsContext()
        {
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
            User randomUser = context.RandomUserNotPersisted();
            string jwt = context.userAccountDomain.CreateAccount(randomUser).Result;

            var retrievedDbUser = context.userAccountDomain.RetrieveUserByEmail(randomUser.EmailAddress).Result;
            Assert.Equal(randomUser.UserName, retrievedDbUser.UserName);
            Assert.NotEmpty(jwt);

        }

        [Fact]
        public void UserLoginWorksAndCreatesValidJwt()
        {
            User randomUser = context.RandomUserNotPersisted();
            string password = randomUser.Password;

            context.userAccountDomain.CreateAccount(randomUser).Wait();
            randomUser.Password = password; //gets overwritten as the hashed value during acct create

            string jwt = context.userAccountDomain.Login(randomUser).Result;

            Assert.NotEmpty(jwt);
            Assert.True(context.jwtProvider.ValidateToken(jwt));
        }

        [Fact]
        public void DuplicateCreateFails()
        {
            User randomUser = context.RandomUserNotPersisted();
            string password = randomUser.Password;

            context.userAccountDomain.CreateAccount(randomUser).Wait();
            randomUser.Password = password; //gets overwritten as the hashed value during acct create

            string jwt = context.userAccountDomain.Login(randomUser).Result;

            Assert.NotEmpty(jwt);
            Assert.True(context.jwtProvider.ValidateToken(jwt));

            Assert.ThrowsAny<Exception>(() => context.userAccountDomain.CreateAccount(randomUser).Result);
        }
    }
}
