using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Transactions;
using CritterServer.DataAccess;
using CritterServer.Domains.Components;
using CritterServer.Models;
using Dapper;
using Microsoft.Extensions.Logging;
namespace CritterServer.Domains
{
    public class UserAuthenticationDomain
    {
        IUserRepository userRepo;
        IJwtProvider jwtProvider;

        public UserAuthenticationDomain(IUserRepository userRepo, IJwtProvider jwtProvider)
        {
            this.userRepo = userRepo;
            this.jwtProvider = jwtProvider;
        }

        public string CreateAccount(User user)
        {
            using (var trans = new TransactionScope(TransactionScopeOption.Required))
            {
                //TODO validate incoming properties (Birthday, gender, email, username)
                user.Cash = 500; //TODO economics
                user.IsActive = true;
                user.Salt = BCrypt.Net.BCrypt.GenerateSalt();
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password, user.Salt);

                user.UserId = userRepo.CreateUser(user);

                trans.Complete();
            }
            user = RetrieveUser(user.UserId);
            return jwtProvider.GenerateToken(user);
        }

        public string Login(User user)
        {
            User dbUser = null;
            if (!string.IsNullOrEmpty(user.UserName))
            {
                dbUser = RetrieveUserByUserName(user.UserName);
            }
            else if (!string.IsNullOrEmpty(user.EmailAddress))
            {
                dbUser = RetrieveUserByEmail(user.EmailAddress);
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
            throw new InvalidCredentialException($"The provided credentials were invalid for {user.UserName ?? user.EmailAddress}");//todo make unauthorized
        }

        public User RetrieveUser(int userId)
        {
            return userRepo.RetrieveUserById(userId);
        }

        public User RetrieveUserByUserName(string userName)
        {
            return userRepo.RetrieveUserByUserName(userName);

        }

        public User RetrieveUserByEmail(string email)
        {
            return userRepo.RetrieveUserByUserName(email);
        }
    }

}
