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
        IUserRepository userRepo;
        IJwtProvider jwtProvider;

        public UserDomain(IUserRepository userRepo, IJwtProvider jwtProvider)
        {
            this.userRepo = userRepo;
            this.jwtProvider = jwtProvider;
        }

        public async Task<string> CreateAccount(User user)
        {
            using (var trans = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                user.Cash = 500; //TODO economics
                user.IsActive = true;
                user.Salt = BCrypt.Net.BCrypt.GenerateSalt();
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password, user.Salt);

                user.UserId = userRepo.CreateUser(user);

                trans.Complete();
            }
            user = await RetrieveUser(user.UserId);
            return jwtProvider.GenerateToken(user);
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
                    return jwtProvider.GenerateToken(user);
                }
            }
            throw new CritterException($"The provided credentials were invalid for {user.UserName ?? user.EmailAddress}", null, System.Net.HttpStatusCode.Unauthorized);
        }

        public async Task<User> RetrieveUser(int userId)
        {
            return (await userRepo.RetrieveUsersByIds(userId)).FirstOrDefault();
        }

        public async Task<List<User>> RetrieveUsers(IEnumerable<int> userIds)
        {
            return (await userRepo.RetrieveUsersByIds(userIds.ToArray())).AsList();
        }

        public async Task<User> RetrieveUserByUserName(string userName)
        {
            return (await userRepo.RetrieveUsersByUserName(userName)).FirstOrDefault();
        }

        public async Task<IEnumerable<User>> RetrieveUsersByUserName(IEnumerable<string> userNames)
        {
            return await userRepo.RetrieveUsersByUserName(userNames.ToArray());

        }

        public async Task<User> RetrieveUserByEmail(string email)
        {
            return await userRepo.RetrieveUserByEmail(email);
        }
    }

}
