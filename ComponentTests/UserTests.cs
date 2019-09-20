using CritterServer.DataAccess;
using CritterServer.Domains;
using CritterServer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Xunit;

namespace ComponentTests
{
    public class UserTestsContext
    {
        public IDbConnection dbConnection;
        public UserAuthenticationDomain userAccountDomain;
        public IUserRepository userRepo;

        public List<User> extantUsers = new List<User>();

        public UserTestsContext()
        {
            DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
            dbConnection = DbProviderFactories.GetFactory("Npgsql").CreateConnection();
            dbConnection.ConnectionString = "Server=localhost; Port=5432; User Id=LocalApp;Password=localapplicationpassword;Database=CrittersDB";
            userRepo = new UserRepository(dbConnection);
            userAccountDomain = new UserAuthenticationDomain(userRepo);
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
            int userId = context.userAccountDomain.CreateUserAccount(randomUser);

            var retrievedDbUser = context.userAccountDomain.RetrieveUser(userId);
            Assert.Equal(randomUser.UserName, retrievedDbUser.UserName);
        }
    }
}
