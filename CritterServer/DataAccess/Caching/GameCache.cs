using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.DataAccess.Caching
{
    /// <summary>
    /// Cache handler for IMemoryCache or Redis
    /// Manages keys so the repository doesn't have to worry about key construction
    /// </summary>
    public class GameCache : IGameCache
    {
        private const string GAMESIGNINGKEY_KEY = "GameKey";
        private const string SCORESUBMISSIONSCOUNT_KEY = "ScoreSubmissionsCount";

        private IMemoryCache Cache;
        public GameCache(IMemoryCache memoryCache) //todo swap with redis
        {
            Cache = memoryCache;
        }


        public Task<string> GetGameSigningKey(int gameId, int userId)
        {
            if (Cache.TryGetValue($"{GAMESIGNINGKEY_KEY}:{gameId}:{userId}", out string value))
                return Task.FromResult(value);
            else return Task.FromResult(default(string));
        }

        public void SetGameSigningKey(int gameId, int userId, string signingKey, TimeSpan? ttl = null)
        {
            if (ttl == null)
            {
                ttl = TimeSpan.FromHours(24);
            }
            Cache.Set($"{GAMESIGNINGKEY_KEY}:{gameId}:{userId}", signingKey, ttl.Value);
        }

        public void InvalidateGameSigningKey(int gameId, int userId)
        {
            Cache.Remove($"{GAMESIGNINGKEY_KEY}:{gameId}:{userId}");
        }

        public Task<int> GetScoreSubmissionsCount(int gameId, int userId)
        {
            if (Cache.TryGetValue($"{SCORESUBMISSIONSCOUNT_KEY}:{gameId}:{userId}", out int value))
                return Task.FromResult(value);
            else return Task.FromResult(0);
        }

        public void SetScoreSubmissionsCount(int gameId, int userId, int submissionsCount, TimeSpan? ttl = null)
        {
            if (ttl == null)
            {
                ttl = TimeSpan.FromHours(24);
            }
            Cache.Set($"{SCORESUBMISSIONSCOUNT_KEY}:{gameId}:{userId}", submissionsCount, ttl.Value);
        }

        public void InvalidateScoreSubmissionsCount(int gameId, int userId)
        {
            Cache.Remove($"{SCORESUBMISSIONSCOUNT_KEY}:{gameId}:{userId}");
        }
    }

    public interface IGameCache
    {
        Task<string> GetGameSigningKey(int gameId, int userId);
        Task<int> GetScoreSubmissionsCount(int gameId, int userId);
        void InvalidateGameSigningKey(int gameId, int userId);
        void InvalidateScoreSubmissionsCount(int gameId, int userId);
        void SetGameSigningKey(int gameId, int userId, string signingKey, TimeSpan? ttl = null);
        void SetScoreSubmissionsCount(int gameId, int userId, int submissionsCount, TimeSpan? ttl = null);
    }

}
