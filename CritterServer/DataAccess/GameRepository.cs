using CritterServer.DataAccess.Caching;
using CritterServer.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.DataAccess
{
    public class GameRepository : IGameRepository
    {
        IDbConnection dbConnection;
        IGameCache GameCache;

        public GameRepository(IDbConnection dbConnection, IGameCache gameCache)
        {
            this.dbConnection = dbConnection;
            GameCache = gameCache;
        }

        public async Task<bool> UpsertGameScore(int userId, int gameId, int score)
        {
            int output = await dbConnection.ExecuteAsync(
                @"INSERT INTO leaderboardEntries as le (gameID, score, playerID)
                VALUES(@gameId, @score, @userId) 
                ON CONFLICT (gameID, playerID) DO UPDATE
                SET score = @score, dateSubmitted = NOW()
                WHERE le.score < @score;",
                new
                {
                    userId,
                    score,
                    gameId
                });
            return output == 1;
        }

        public async Task<IEnumerable<LeaderboardEntry>> GetLeaderboard(int gameId, int topCount, DateTime startDate)
        {
            var output = await dbConnection.QueryAsync<LeaderboardEntry>("SELECT * FROM leaderboardEntries WHERE gameID = @gameId AND dateSubmitted >= @startDate " +
                "ORDER BY score desc FETCH FIRST @topCount ROWS ONLY",
                new
                {
                    topCount,
                    gameId,
                    startDate
                });
            return output;
        }

        public void SaveGameSigningKey(int gameId, int userId, string gameKey)
        {
            GameCache.SetGameSigningKey(gameId, userId, gameKey, TimeSpan.FromMinutes(15));
        }

        public Task<string> GetGameSigningKey(int gameId, int userId)
        {
            return GameCache.GetGameSigningKey(gameId, userId);
        }

        public void DeleteGameSigningKey(int gameId, int userId)
        {
            GameCache.InvalidateGameSigningKey(gameId, userId);
        }

        public async Task<int> GetScoreSubmissionCount(int gameId, int userId)
        {
            return await GameCache.GetScoreSubmissionsCount(gameId, userId); ;
        }

        public Task SetScoreSubmissionCount(int gameId, int userId, int timesSubmitted, DateTime absoluteExpiry)
        {
            GameCache.SetScoreSubmissionsCount(gameId,userId, timesSubmitted, absoluteExpiry - DateTime.UtcNow);
            return Task.CompletedTask;
        }
    }

    public interface IGameRepository : IRepository
    {
        Task<bool> UpsertGameScore(int userId, int gameId, int score);
        Task<IEnumerable<LeaderboardEntry>> GetLeaderboard(int gameId, int topCount, DateTime startDate);
        void SaveGameSigningKey(int gameId, int userId, string gameKey);
        Task<string> GetGameSigningKey(int gameId, int userId);
        void DeleteGameSigningKey(int gameId, int userId);
        Task<int> GetScoreSubmissionCount(int gameId, int userId);
        Task SetScoreSubmissionCount(int gameId, int userId, int timesSubmitted, DateTime absoluteExpiry);
    }
}
