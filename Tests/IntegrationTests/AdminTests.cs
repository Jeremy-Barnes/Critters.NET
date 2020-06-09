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
    /// <summary>
    /// Creaated once, reused for all tests in AdminTests
    /// Used to hold expensive resources that can be reused (like a DB connection!)
    /// </summary>
    public class AdminTestsContext : TestUtilities
    {
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
        public AdminTestsContext()
        {

        }
    }

    public class AdminTests : IClassFixture<AdminTestsContext>
    {
        AdminTestsContext context;
        IDbConnection ScopedDbConnection;
        public UserDomain UserDomain => new UserDomain(UserRepo, context.jwtProvider);
        public PetDomain petDomain => new PetDomain(PetRepo, CfgRepo);
        public IUserRepository UserRepo => new UserRepository(ScopedDbConnection);
        public IPetRepository PetRepo => new PetRepository(ScopedDbConnection);
        public IConfigRepository CfgRepo => new ConfigRepository(ScopedDbConnection);
        AdminDomain AdminDomain => new AdminDomain(CfgRepo, UserRepo, context.jwtProvider);

        public AdminTests(AdminTestsContext context)
        {
            this.context = context;
            ScopedDbConnection = context.GetNewDbConnection();
        }

        [Fact]
        public void NonDevCantLogin()
        {
            var nonDev = context.RandomUserNotPersisted();
            UserDomain.CreateAccount(nonDev).Wait();
            
            Assert.ThrowsAsync<CritterException>(() => AdminDomain.LoginDev(nonDev));
        }

        [Fact]
        public void DevCanLogin()
        {
            var dev = context.RandomUserNotPersisted();
            var devPass = dev.Password;
            Assert.NotEmpty(AdminDomain.CreateDev(dev, null).Result);
            dev.Password = devPass;
            Assert.NotEmpty(AdminDomain.LoginDev(dev).Result);
        }
    }
}
