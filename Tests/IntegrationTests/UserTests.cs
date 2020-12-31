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
using System.Linq;

namespace Tests.IntegrationTests
{
    public class UserTestScope: TestUtilities, IDisposable
    {

        private IDbConnection ScopedDbConnection;

        public IUserRepository UserRepo;
        public UserDomain UserAccountDomain;
        public IFriendshipRepository FriendRepo;
        public UserTestScope()
        {
            ScopedDbConnection = GetNewDbConnection();
            UserRepo = new UserRepository(ScopedDbConnection);
            FriendRepo = new FriendshipRepository(ScopedDbConnection);
            var transactionScopeFactory = new TransactionScopeFactory(ScopedDbConnection);
            UserAccountDomain = new UserDomain(UserRepo, FriendRepo, JWTProvider, transactionScopeFactory);
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


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SelfFriendshipFails(bool unfriend)
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
                Assert.ThrowsAsync<CritterException>(() => scope.UserAccountDomain.UpdateFriendship(randomUser.UserName, randomUser, unfriend));
            }
        }


        [Fact]
        public void DoubleTapFriendshipFails()
        {
            User randomUser;
            User randomFriend;
            using (var scope = new UserTestScope())
            {
                randomUser = scope.RandomUserNotPersisted();
                randomFriend = scope.RandomUserNotPersisted();

                string password = randomUser.Password;
                string friendPassword = randomFriend.Password;
                scope.UserAccountDomain.CreateAccount(randomUser).Wait();
                scope.UserAccountDomain.CreateAccount(randomFriend).Wait();

                randomUser.Password = password; //gets overwritten as the hashed value during acct create
                randomFriend.Password = friendPassword;
                scope.UserAccountDomain.UpdateFriendship(randomFriend.UserName, randomUser, false).Wait();
            }
            using (var scope = new UserTestScope())
            {
                Assert.ThrowsAsync<CritterException>(() => scope.UserAccountDomain.UpdateFriendship(randomFriend.UserName, randomUser, false));
            }
        }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AddFriendshipNoNewUnfriend(bool unfriend)
        {
            User randomUser;
            User randomFriend;
            using (var scope = new UserTestScope())
            {
                randomUser = scope.RandomUserNotPersisted();
                randomFriend = scope.RandomUserNotPersisted();

                string password = randomUser.Password;
                string friendPassword = randomFriend.Password;
                scope.UserAccountDomain.CreateAccount(randomUser).Wait();
                scope.UserAccountDomain.CreateAccount(randomFriend).Wait();

                randomUser.Password = password; //gets overwritten as the hashed value during acct create
                randomFriend.Password = friendPassword;
            }
            using (var scope = new UserTestScope())
            {
                if(unfriend)
                    Assert.ThrowsAsync<CritterException>(() => scope.UserAccountDomain.UpdateFriendship(randomFriend.UserName, randomUser, true));
                else
                {
                    var details = scope.UserAccountDomain.UpdateFriendship(randomFriend.UserName, randomUser, unfriend).Result;
                    Assert.NotNull(details);
                    Assert.Equal(randomFriend.UserName, details.RequestedUserName);
                    Assert.Equal(randomUser.UserName, details.RequesterUserName);
                    Assert.False(details.Friendship.Accepted);
                }
            }
        }


        [Fact]
        public void DeleteFriendshipRemovesRequest()
        {
            User randomUser;
            User randomFriend;
            using (var scope = new UserTestScope())
            {
                randomUser = scope.RandomUserNotPersisted();
                randomFriend = scope.RandomUserNotPersisted();

                scope.UserAccountDomain.CreateAccount(randomUser).Wait();
                scope.UserAccountDomain.CreateAccount(randomFriend).Wait();
                var details = scope.UserAccountDomain.UpdateFriendship(randomFriend.UserName, randomUser, false).Result;

            }
            using (var scope = new UserTestScope())
            {
                Assert.NotEmpty(scope.UserAccountDomain.RetrieveFriends(randomUser).Result);

                var details = scope.UserAccountDomain.UpdateFriendship(randomFriend.UserName, randomUser, true).Result;
                Assert.NotNull(details);
                Assert.Equal(randomFriend.UserName, details.RequestedUserName);
                Assert.Equal(randomUser.UserName, details.RequesterUserName);
                Assert.Empty(scope.UserAccountDomain.RetrieveFriends(randomUser).Result);
            }
        }


        [Fact]
        public void DeleteFriendshipRemovesFriend()
        {
            User randomUser;
            User randomFriend;
            using (var scope = new UserTestScope())
            {
                randomUser = scope.RandomUserNotPersisted();
                randomFriend = scope.RandomUserNotPersisted();

                scope.UserAccountDomain.CreateAccount(randomUser).Wait();
            }
            using (var scope = new UserTestScope())
            {
                scope.UserAccountDomain.CreateAccount(randomFriend).Wait();
            }
            using (var scope = new UserTestScope())
            {
                var details = scope.UserAccountDomain.UpdateFriendship(randomFriend.UserName, randomUser, false).Result;
            }

            using (var scope = new UserTestScope())
            {
                Assert.NotEmpty(scope.UserAccountDomain.RetrieveFriends(randomFriend).Result);
                Assert.True(scope.UserAccountDomain.UpdateFriendship(randomUser.UserName, randomFriend, false).Result.Friendship.Accepted);
            }
            using (var scope = new UserTestScope())
            { 
                Assert.True(scope.UserAccountDomain.RetrieveFriends(randomFriend).Result.First().Friendship.Accepted);//db check
            }

            using (var scope = new UserTestScope())
            {
                Assert.NotEmpty(scope.UserAccountDomain.RetrieveFriends(randomUser).Result);

                var details = scope.UserAccountDomain.UpdateFriendship(randomUser.UserName, randomFriend, true).Result;
                Assert.NotNull(details);
            }
            using (var scope = new UserTestScope())
            {
                Assert.Empty(scope.UserAccountDomain.RetrieveFriends(randomFriend).Result);
            }
        }
    }
}
