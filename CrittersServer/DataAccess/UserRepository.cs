using CritterServer.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.DataAccess
{
    public class UserRepository : IUserRepository
    {
        IDbConnection dbConnection;

        public UserRepository(IDbConnection dbConnection)
        {
            this.dbConnection = dbConnection;
        }

        public int CreateUser(User user)
        {
            dbConnection.TryOpen();
            int output = dbConnection.Query<int>("INSERT INTO users(userName, firstName, lastName, emailAddress, password, gender, birthdate, cash, city, state, country, postcode, " +
                "isActive, salt)" +
                "VALUES(@userName, @firstName, @lastName, @emailAddress, @password, @gender, @birthdate, @cash, @city, @state, @country, @postcode, " +
                "@isActive, @salt) RETURNING userID",
                new
                { userName = user.UserName, firstName = user.FirstName, lastName = user.LastName, emailAddress = user.EmailAddress, password = user.Password, gender = user.Gender.ToLower(), birthdate = Convert.ToDateTime(user.Birthdate), cash = user.Cash,
                    city = user.City, state = user.State, country = user.Country, postcode = user.Postcode, isActive = user.IsActive, salt = user.Salt}).First();
            return output;
        }

        public User RetrieveUserByEmail(string email)
        {
            dbConnection.TryOpen();
            return dbConnection.Query<User>("select * from users where emailAddress = @emailAddress", new { emailAddress = email }).FirstOrDefault();
        }

        public User RetrieveUserById(int userId)
        {
            dbConnection.TryOpen();
            return dbConnection.Query<User>("select * from users where userID = @userID", new { userID = userId }).FirstOrDefault();
        }

        public User RetrieveUserByUserName(string userName)
        {
            dbConnection.TryOpen();
            return dbConnection.Query<User>("select * from users where userName = @userName", new { userName = userName }).FirstOrDefault();
        }
    }

    public interface IUserRepository : IRepository
    {
        int CreateUser(User user);
        User RetrieveUserById(int userId);
        User RetrieveUserByEmail(string email);
        User RetrieveUserByUserName(string userName);
    }
}
