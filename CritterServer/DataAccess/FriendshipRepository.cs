using CritterServer.Contract;
using CritterServer.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CritterServer.DataAccess
{
    public class FriendshipRepository : IFriendshipRepository
    {
        IDbConnection dbConnection;

        public FriendshipRepository(IDbConnection dbConnection)
        {
            this.dbConnection = dbConnection;
        }

        public async Task<bool> CreateFriendship(int userId, int friendId)
        {
            int success = await dbConnection.ExecuteAsync("INSERT INTO friendships(requesterUserID, requestedUserID)" +
               "VALUES(@requester, @requested)",
               new
               {
                   requester = userId,
                   requested = friendId,
                });
            return success == 1;
        }

        public async Task<IEnumerable<Friendship>> RetrieveFriendships(int userId, int? friendId = null)
        {
            var output = await dbConnection.QueryAsync<Friendship>( //union all might be more appropro 
                $@"SELECT * FROM friendships
                   WHERE (requesterUserID = @userId{(friendId.HasValue ? " AND requestedUserID = @friendId)" : ")")}
                   OR (requestedUserID = @userId{(friendId.HasValue ? " AND requesterUserID = @friendId)" : ")")} 
                ",
               new
               {
                   userId,
                   friendId
               });
            return output;
        }

        public async Task<bool> AcceptFriendship(int userId, int friendId)
        {

            int output = await dbConnection.ExecuteAsync(
                $@"UPDATE friendships 
                SET accepted = true
                WHERE requestedUserID = @userId AND requesterUserID = @friendId",
                new
                {
                    userId,
                    friendId
                });
            return output == 1;
        }

        public async Task<bool> DeleteFriendship(int userId, int friendId)
        {
                int output = await dbConnection.ExecuteAsync(
                    $@"DELETE FROM friendships 
                    WHERE (requesterUserID = @userId AND requestedUserID = @friendId)
                    OR (requestedUserID = @userId AND requesterUserID = @friendId)",                
                    new
                    {
                        userId,
                        friendId
                    });
                return output == 1;
        }
    }

    public interface IFriendshipRepository : IRepository
    {
        Task<bool> CreateFriendship(int userId, int friendId);
        Task<IEnumerable<Friendship>> RetrieveFriendships(int userId, int? friendId = null);
        Task<bool> AcceptFriendship(int userId, int friendId);
        Task<bool> DeleteFriendship(int userId, int friendId);
    }
}
