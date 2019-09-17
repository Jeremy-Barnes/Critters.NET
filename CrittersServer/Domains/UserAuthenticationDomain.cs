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
        IDbConnection dbConnection;

        public UserAuthenticationDomain(IDbConnection dbConnection)
        {
            this.dbConnection = dbConnection;
        }

        public void CreateUserAccount()
        {
            using (var trans = new TransactionScope(TransactionScopeOption.Required))
            {
                dbConnection.TryOpen();
                var x = dbConnection.Query<User>("select * from users");
                dbConnection.Execute("INSERT INTO users(username, firstname, lastname, emailaddress, password, sex, birthdate, salt, city, state, country, postcode, tokenselector, tokenvalidator, cash, isactive)VALUES('test', 'fname', 'lname', 'test1', 'bbb', 'male', '12-05-1991', 'asdasdasd', 'Chicago', 'IL', 'USA', '60613', 'A', 'b', 100, true); ");
                var y = dbConnection.Query<User>("select * from users");

                trans.Complete();
            }
        }
    }

}