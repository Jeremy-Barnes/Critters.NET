using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using CritterServer.Contract;
using CritterServer.DataAccess;
using CritterServer.Domains.Components;
using CritterServer.Models;
using Dapper;
using Microsoft.Extensions.Logging;
namespace CritterServer.Domains
{
    public class GameDomain
    {
        IConfigRepository ConfigRepo;
        IGameRepository GameRepo;
        UserDomain UserDomain;
        IJwtProvider JwtProvider;
        ITransactionScopeFactory TransactionScopeFactory;

        public GameDomain(IConfigRepository cfgRepo, IGameRepository gameRepository, UserDomain userDomain, IJwtProvider jwtProvider, ITransactionScopeFactory transactionScopeFactory)
        {
            ConfigRepo = cfgRepo;
            UserDomain = userDomain;
            GameRepo = gameRepository;
            JwtProvider = jwtProvider;
            TransactionScopeFactory = transactionScopeFactory;
        }

        public async Task<string> CreateKey(User activeUser, int gameId)
        {
            byte[] random = new Byte[256];

            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(random);
            var key = Convert.ToBase64String(random);
            GameRepo.SaveGameSigningKey(gameId, activeUser.UserId, key);
            return await Task.FromResult(key);
        }

        public async Task<GameScoreResult> SubmitScore(User activeUser, int gameId, int score, string scoreToken)
        {
            var key = await GameRepo.GetGameSigningKey(gameId, activeUser.UserId);
            GameRepo.DeleteGameSigningKey(gameId, activeUser.UserId);
            bool succeeded = false;
            ClaimsPrincipal token = null;
            bool validClaims = false;
            if (key != null)
            {
                token = JwtProvider.CrackJwt(scoreToken, key);

                validClaims = token?.Claims.Where(c => c.Type == "g").FirstOrDefault().Value == gameId.ToString();//don't forget about GUS
                validClaims &= token?.Claims.Where(c => c.Type == "u").FirstOrDefault().Value == activeUser.UserName;
                validClaims &= token?.Claims.Where(c => c.Type == "s").FirstOrDefault().Value == score.ToString();
            }
            if (!validClaims)
            {
                throw new CritterException("Sorry, we couldn't record this score! Something went wrong.",
                    $"Potential cheating: user {activeUser.UserId} at game {gameId} with score {score} and token: {scoreToken}",
                    System.Net.HttpStatusCode.BadRequest, LogLevel.Warning);
            }

            GameScoreResult result = new GameScoreResult();
            using (var trans = TransactionScopeFactory.Create())
            {
                GameConfig gameCfg = (await ConfigRepo.RetrieveGamesConfigByIds(true, gameId)).FirstOrDefault();
                if(gameCfg == null)
                {
                    throw new CritterException("Sorry, we couldn't record this score because this game doesn't exist!",
                            $"Invalid game ID entered by user {activeUser.UserId} for game {gameId} with score {score} and token: {scoreToken}",
                            System.Net.HttpStatusCode.NotFound, LogLevel.Error);
                }
                var previousSubmissions = await GameRepo.GetScoreSubmissionCount(gameId, activeUser.UserId);
                if(gameCfg.DailyCashCountCap <= previousSubmissions)
                {
                    throw new CritterException("Sorry, we couldn't record this score, you have submitted your score too many times for this game today!",
                        gameCfg.DailyCashCountCap < previousSubmissions ? $"User {activeUser.UserId} over-submitted at game {gameId} with token: {scoreToken} somehow" : null,
                        System.Net.HttpStatusCode.TooManyRequests, gameCfg.DailyCashCountCap < previousSubmissions ? LogLevel.Warning : LogLevel.Debug);
                }
                bool success = await GameRepo.UpsertGameScore(activeUser.UserId, gameId, score);
                if (gameCfg.ScoreToCashFactor.HasValue)
                {
                    int cashVal = (int)(gameCfg.ScoreToCashFactor.Value * score);
                    cashVal = Math.Min(cashVal, gameCfg.CashCap ?? Int32.MaxValue);
                    result.CashWon = cashVal;
                    activeUser = await UserDomain.ChangeUserCash(cashVal, activeUser);
                }
                result.RemainingSubmissions = (gameCfg.DailyCashCountCap ?? 100) - (previousSubmissions + 1);
                await GameRepo.SetScoreSubmissionCount(gameId, activeUser.UserId, previousSubmissions+1, DateTime.UtcNow.Date.AddDays(1));

                trans.Complete();
                succeeded = true;
            }
            if (!succeeded)
            {
                GameRepo.SaveGameSigningKey(gameId, activeUser.UserId, key); //let em try again if we Db fail
                throw new CritterException("We failed to record your score. Please try and submit again! Contact an admin if this continues.",
                        $"Failed to record good score for user {activeUser.UserId} at game {gameId} with score {score} and token: {scoreToken}",
                        System.Net.HttpStatusCode.InternalServerError, LogLevel.Error);

            }
            return result;
        }

        public async Task<IEnumerable<LeaderboardEntryDetails>> RetrieveLeaderboard(int gameId)
        {
            var now = DateTime.UtcNow;
            var month = new DateTime(now.Year, now.Month, 1);
            GameConfig gameCfg = (await ConfigRepo.RetrieveGamesConfigByIds(true, gameId)).FirstOrDefault();
            int topCount = gameCfg.LeaderboardMaxSpot ?? 10;

            IEnumerable<LeaderboardEntry> leaderboard = await GameRepo.GetLeaderboard(gameId, topCount, month);

            List<int> users = leaderboard.Select(le => le.PlayerId).AsList();
            Dictionary<int, string> userNames = (await UserDomain.RetrieveUsers(users)).ToDictionary(user => user.UserId, user => user.UserName);

            IEnumerable<LeaderboardEntryDetails> retVal = leaderboard.Select(le => new LeaderboardEntryDetails { 
                Entry = le, 
                Username = userNames.GetValueOrDefault(le.PlayerId, "Unknown") 
            });
            return retVal;
        }

        public async Task<IEnumerable<GameConfig>> RetrieveGameConfigs(int[] cfgIds)
        {
            if (cfgIds != null && cfgIds.Any())
            {
                return (await ConfigRepo.RetrieveGamesConfigByIds(true, cfgIds.ToArray()));
            } else
            {
                return (await ConfigRepo.RetrieveGameConfigs(true));
            }
        }

    }
}
