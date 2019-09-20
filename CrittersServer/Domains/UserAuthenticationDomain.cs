using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using CritterServer.DataAccess;
using CritterServer.Models;
using Dapper;

namespace CritterServer.Domains
{
    public class UserAuthenticationDomain
    {
        IUserRepository userRepo;

        public UserAuthenticationDomain(IUserRepository userRepo)
        {
            this.userRepo = userRepo;
        }

        public int CreateUserAccount(User user)
        {
            int createdId = -1;
            using (var trans = new TransactionScope(TransactionScopeOption.Required))
            {
                //TODO transform the inbound data
                createdId = userRepo.CreateUser(user);

                trans.Complete();
            }
            return createdId;
        }

        public User RetrieveUser(int userId)
        {
            return userRepo.RetrieveUserById(userId);
        }

        public User RetrieveUser(string login)
        {
            return null;
        }
    }

}