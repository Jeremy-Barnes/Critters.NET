using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Transactions;
using CritterServer.Contract;
using CritterServer.DataAccess;
using CritterServer.Domains.Components;
using CritterServer.Models;
using Dapper;
using jabarnes.Metaphone;
using Newtonsoft.Json;

namespace CritterServer.Domains
{
    public class UserDomain
    {
        IUserRepository UserRepo;
        IFriendshipRepository FriendRepo;
        IJwtProvider JWTProvider;
        ITransactionScopeFactory TransactionScopeFactory;
        public UserDomain(IUserRepository userRepo, IFriendshipRepository friendRepo, IJwtProvider jwtProvider, ITransactionScopeFactory transactionScopeFactory)
        {
            UserRepo = userRepo;
            FriendRepo = friendRepo;
            JWTProvider = jwtProvider;
            TransactionScopeFactory = transactionScopeFactory;
        }

        public async Task<string> CreateAccount(User user)
        {
            bool conflictFound = await UserRepo.UserExistsByUserNameOrEmail(user.UserName, user.EmailAddress);
            if (conflictFound)
                throw new CritterException($"Sorry, someone already exists with that name or email!", $"Duplicate account creation attempt on {user.UserName} or {user.EmailAddress}", System.Net.HttpStatusCode.Conflict);

            using (var trans = TransactionScopeFactory.Create())
            {
                
                user.Cash = 500; //TODO economics
                user.IsActive = true;
                user.Salt = BCrypt.Net.BCrypt.GenerateSalt();
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password, user.Salt);

                user.UserId = await UserRepo.CreateUser(user) ?? throw new CritterException("Could not create account, try again!", null, System.Net.HttpStatusCode.Conflict);

                List<int> metaphones = new List<int>();
                var doubles = new List<ShortDoubleMetaphone>();
                doubles.Add(new ShortDoubleMetaphone(user.UserName));
                doubles.Add(new ShortDoubleMetaphone(user.FirstName));
                doubles.Add(new ShortDoubleMetaphone(user.LastName));
                doubles.ForEach(d => { metaphones.Add(d.PrimaryShortKey); metaphones.Add(d.AlternateShortKey); });
                metaphones = metaphones.Distinct().AsList();
                await UserRepo.InsertMetaphone(user.UserId, metaphones.ToArray());
                
                trans.Complete();
            }
            user = await RetrieveUser(user.UserId);
            return JWTProvider.GenerateToken(user);
        }

        public async Task<string> Login(User user)
        {
            User dbUser = null;
            if (!string.IsNullOrEmpty(user.UserName))
            {
                dbUser = await RetrieveUserByUserName(user.UserName);
            }
            else if (!string.IsNullOrEmpty(user.EmailAddress))
            {
                dbUser = await RetrieveUserByEmail(user.EmailAddress);
            }

            if (dbUser != null && !string.IsNullOrEmpty(user.Password))
            {

                string hashPassword = BCrypt.Net.BCrypt.HashPassword(user.Password, dbUser.Salt);
                if (dbUser.Password == hashPassword) //success
                {
                    user = dbUser;
                    return JWTProvider.GenerateToken(user);
                }
            }
            throw new CritterException($"The provided credentials were invalid for {user.UserName ?? user.EmailAddress}", null, System.Net.HttpStatusCode.Unauthorized);
        }

        public async Task<User> RetrieveUser(int userId)
        {
            return (await UserRepo.RetrieveUsersByIds(userId)).FirstOrDefault();
        }

        public async Task<List<User>> RetrieveUsers(IEnumerable<int> userIds)
        {
            return (await UserRepo.RetrieveUsersByIds(userIds.ToArray())).AsList();
        }

        public async Task<User> RetrieveUserByUserName(string userName)
        {
            return (await UserRepo.RetrieveUsersByUserName(userName)).FirstOrDefault();
        }

        public async Task<IEnumerable<User>> RetrieveUsersByUserName(IEnumerable<string> userNames)
        {
            return await UserRepo.RetrieveUsersByUserName(userNames.ToArray());
        }

        public async Task<User> RetrieveUserByEmail(string email)
        {
            return await UserRepo.RetrieveUserByEmail(email);
        }

        public async Task<IEnumerable<User>> RetrieveUsersBySoundsLike(int metaphone)
        {
            if (metaphone < 0) return null;
            return await UserRepo.RetrieveUsersIfMetaphoneMatch(metaphone);
        }

        public async Task<User> ChangeUserCash(int byAmount, User user)
        {
            using (var trans = TransactionScopeFactory.Create())
            {
                await UserRepo.UpdateUserCash(user.UserId, byAmount);
                user.Cash += byAmount;
                trans.Complete();
            }
            return user;
        }

        public async Task ChangeUsersCash(List<(int UserId, int CashDelta)> userIdAndCashDeltas)
        {
            using (var trans = TransactionScopeFactory.Create())
            {
                await UserRepo.UpdateUsersCash(userIdAndCashDeltas.ToArray());
                trans.Complete();
            }
        }


        public async Task<IEnumerable<FriendshipDetails>> RetrieveFriends(User activeUser)
        {
            IEnumerable<Friendship> dbShips = await FriendRepo.RetrieveFriendships(activeUser.UserId);

            IEnumerable<int> friendsIds = dbShips.Select(dbs => 
                dbs.RequestedUserId == activeUser.UserId ? dbs.RequesterUserId : dbs.RequestedUserId);

            var dbFriends = (await UserRepo.RetrieveUsersByIds(friendsIds.ToArray())).ToDictionary(user => user.UserId);
            dbFriends.Add(activeUser.UserId, activeUser);

            var friendshipDetails = dbShips
                .Where(dbs => dbFriends.ContainsKey(dbs.RequesterUserId) && dbFriends.ContainsKey(dbs.RequestedUserId))
                .Select(dbs => new FriendshipDetails()
                {
                    Friendship = dbs,
                    RequestedUserName = dbFriends[dbs.RequestedUserId].UserName,
                    RequesterUserName = dbFriends[dbs.RequesterUserId].UserName
                });
            return friendshipDetails;
        }


        public async Task<FriendshipDetails> UpdateFriendship(string friendUserName, User activeUser, bool unfriend)
        {
            User friend = (await UserRepo.RetrieveUsersByUserName(friendUserName)).FirstOrDefault();
            if(friend == null || friend.UserId == activeUser.UserId)
            {
                string badRequestMsg = null;
                if(friend?.UserId == activeUser.UserId)
                {
                    badRequestMsg = "You're already friends with yourself, silly.";
                }
                throw new CritterException(badRequestMsg??$"No one exists with that name: {friendUserName}!",
                    $"Invalid friendrequest sent by User {activeUser.UserId} to {friendUserName}", System.Net.HttpStatusCode.NotFound);
            }
            Friendship dbShip = (await FriendRepo.RetrieveFriendships(activeUser.UserId, friend.UserId)).FirstOrDefault();
            bool success = false;
            if(dbShip == null)
            {
                if(unfriend)
                {
                    throw new CritterException($"You can't unfriend {friend.UserName}, you aren't friends to start with!",
                        $"Attempted to delete nonextant friendship for " +
                        $"{activeUser.UserName } : {activeUser.UserId} and {friendUserName} : {friend.UserId}",
                        System.Net.HttpStatusCode.NotFound);
                }
                success = await FriendRepo.CreateFriendship(activeUser.UserId, friend.UserId);
                dbShip = new Friendship() { Accepted = false, DateSent = DateTime.UtcNow };
            } 
            else
            {
                if (dbShip.RequesterUserId == activeUser.UserId && !unfriend)
                {
                    throw new CritterException($"Relax! {friendUserName} will get your request.",
                    $"Weird double tap on Friendship for " +
                    $"{activeUser.UserName} : {activeUser.UserId} and {friendUserName} : {friend.UserId} \r\n " +
                    $"{JsonConvert.SerializeObject(dbShip, Formatting.Indented)}", System.Net.HttpStatusCode.BadRequest);
                }
                else if (dbShip.RequestedUserId == activeUser.UserId && !unfriend)
                {
                    success = await FriendRepo.AcceptFriendship(activeUser.UserId, friend.UserId);
                    dbShip.Accepted = true;
                } 
                else if (unfriend)
                {
                    success = await FriendRepo.DeleteFriendship(activeUser.UserId, friend.UserId);
                }
            }
            if (!success)
            {
                throw new CritterException($"Couldn't update your friendship with {friendUserName}! Try again!",
                    $"Failed DB update on Friendship for " +
                    $"{activeUser.UserName} : {activeUser.UserId} and {friendUserName} : {friend.UserId} \r\n " +
                    $"{JsonConvert.SerializeObject(dbShip, Formatting.Indented)}", System.Net.HttpStatusCode.InternalServerError);
            }
            return new FriendshipDetails()
            {
                RequesterUserName = activeUser.UserName,
                RequestedUserName = friend.UserName,
                Friendship = dbShip
            };
        }
    }
}
