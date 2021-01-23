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
using Newtonsoft.Json;
using jabarnes;
using jabarnes.Metaphone;
using CritterServer.Utilities;

namespace CritterServer.Domains
{
    public class SearchDomain
    {
        UserDomain UserDomain;
        IUserRepository UserRepo;
        IFriendshipRepository FriendRepo;
        IJwtProvider JWTProvider;
        ITransactionScopeFactory TransactionScopeFactory;

        public SearchDomain(UserDomain userDomain, IUserRepository userRepo, IFriendshipRepository friendRepo, IJwtProvider jwtProvider, ITransactionScopeFactory transactionScopeFactory)
        {
            UserDomain = userDomain;
            UserRepo = userRepo;
            FriendRepo = friendRepo;
            JWTProvider = jwtProvider;
            TransactionScopeFactory = transactionScopeFactory;
        }

        public async Task<SearchResult> Search(string searchString)
        {
            var result = new SearchResult();
            result.Users = await SearchUsers(searchString);
            //todo search pets, pages, items
            return result;
        }

        public async Task<IEnumerable<User>> SearchUsers(string searchString)
        {
            List<User> results = new List<User>();
            if (string.IsNullOrEmpty(searchString))
            {
                return results;
            }

            if (searchString.IsValidEmail())
            {
                var exactMatch = await UserDomain.RetrieveUserByEmail(searchString);
                if (exactMatch != null)
                    results.Add(exactMatch);
            }
            else
            {
                var topResult = await UserDomain.RetrieveUserByUserName(searchString);
                if (topResult != null)
                    results.Add(topResult);

                var metaphone = new ShortDoubleMetaphone(searchString);

                results.AddRange(await UserDomain.RetrieveUsersBySoundsLike(metaphone.PrimaryShortKey));
                if (metaphone.AlternateShortKey != ShortDoubleMetaphone.METAPHONE_INVALID_KEY && metaphone.AlternateShortKey != metaphone.PrimaryShortKey)
                {
                    results.AddRange((await UserDomain.RetrieveUsersBySoundsLike(metaphone.AlternateShortKey)));
                }
            }
            return results;

        }


        public async Task<IEnumerable<User>> RetrieveUsersByUserName(IEnumerable<string> userNames)
        {
            return await UserRepo.RetrieveUsersByUserName(userNames.ToArray());
        }

        public async Task<User> RetrieveUserByEmail(string email)
        {
            return await UserRepo.RetrieveUserByEmail(email);
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
                if(friend.UserId == activeUser.UserId)
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
