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
using Microsoft.Extensions.Logging;
namespace CritterServer.Domains
{
    public class UserDomain
    {
        IUserRepository UserRepo;
        IJwtProvider JWTProvider;
        ITransactionScopeFactory TransactionScopeFactory;
        public UserDomain(IUserRepository userRepo, IJwtProvider jwtProvider, ITransactionScopeFactory transactionScopeFactory)
        {
            UserRepo = userRepo;
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
    }

}
