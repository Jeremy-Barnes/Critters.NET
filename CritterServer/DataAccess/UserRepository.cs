﻿using CritterServer.Models;
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
                {
                    userName = user.UserName,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    emailAddress = user.EmailAddress,
                    password = user.Password,
                    gender = user.Gender.ToLower(),
                    birthdate = Convert.ToDateTime(user.Birthdate),
                    cash = user.Cash,
                    city = user.City,
                    state = user.State,
                    country = user.Country,
                    postcode = user.Postcode,
                    isActive = user.IsActive,
                    salt = user.Salt
                }).First();
            return output;
        }

        public async Task<User> RetrieveUserByEmail(string email)
        {
            dbConnection.TryOpen();
            return (await dbConnection.QueryAsync<User>("SELECT * from users WHERE emailAddress = @emailAddress AND isActive = true", new { emailAddress = email })).FirstOrDefault();
        }

        public async Task<IEnumerable<User>> RetrieveUsersByIds(params int[] userIds)
        {
            dbConnection.TryOpen();
            return await dbConnection.QueryAsync<User>("SELECT * FROM users WHERE userID = ANY(@userIdList) AND isActive = true",
                new { userIdList = userIds.Distinct().AsList() });
        }

        public async Task<IEnumerable<User>> RetrieveUsersByUserName(params string[] userNames)
        {
            dbConnection.TryOpen();
            var users = await dbConnection.QueryAsync<User>("SELECT * FROM users WHERE userName = ANY(@userNameList) AND isActive = true", new { userNameList = userNames.Distinct().AsList() });

            return users;
        }

        public async Task<bool> UserExistsByUserNameOrEmail(string userName, string email)
        {
            string whereClause =
                (!string.IsNullOrEmpty(userName) ? ("userName = @userName" + (!string.IsNullOrEmpty(email) ? " OR " : "")) : "") +
                (!string.IsNullOrEmpty(email) ? "emailAddress = @email" : "");
            dbConnection.TryOpen();
            var searchResult = await dbConnection.QueryAsync<bool>($"SELECT EXISTS (SELECT 1 FROM users WHERE {whereClause} )", new { userName = userName, email = email });
            return searchResult.FirstOrDefault();
        }

        /// <summary>
        /// You really shouldn't be calling this.
        /// </summary>
        /// <param name="developer"></param>
        /// <returns></returns>
        public int CreateDeveloper(User developer)
        {
            dbConnection.TryOpen();
            int output = dbConnection.Query<int>("INSERT INTO users(userName, firstName, lastName, emailAddress, password, gender, birthdate, cash, city, state, country, postcode, " +
                "isActive, salt, isDev)" +
                "VALUES(@userName, @firstName, @lastName, @emailAddress, @password, @gender, @birthdate, @cash, @city, @state, @country, @postcode, " +
                "@isActive, @salt, @isDev) RETURNING userID",
                new
                {
                    userName = developer.UserName,
                    firstName = developer.FirstName,
                    lastName = developer.LastName,
                    emailAddress = developer.EmailAddress,
                    password = developer.Password,
                    gender = developer.Gender.ToLower(),
                    birthdate = Convert.ToDateTime(developer.Birthdate),
                    cash = developer.Cash,
                    city = developer.City,
                    state = developer.State,
                    country = developer.Country,
                    postcode = developer.Postcode,
                    isActive = developer.IsActive,
                    salt = developer.Salt,
                    isDev = true//AAAAAAAAAAAAAAAAAAH
                }).First();
            return output;
        }

        public async Task<User> RetrieveDevByEmail(string email)
        {
            dbConnection.TryOpen();
            return (await dbConnection.QueryAsync<User>("SELECT * from users WHERE emailAddress = @emailAddress AND isActive = true and isDev = true", new { emailAddress = email })).FirstOrDefault();
        }

        public async Task<User> RetrieveDevByUserName(string userName)
        {
            dbConnection.TryOpen();
            var users = await dbConnection.QueryAsync<User>("SELECT * FROM users WHERE userName = @userName AND isActive = true AND isDev = true", new { userName });

            return users.FirstOrDefault();
        }

    }

    public interface IUserRepository : IRepository
    {
        int CreateUser(User user);
        Task<IEnumerable<User>> RetrieveUsersByIds(params int[] userIds);
        Task<User> RetrieveUserByEmail(string email);
        Task<IEnumerable<User>> RetrieveUsersByUserName(params string[] userNames);
        Task<bool> UserExistsByUserNameOrEmail(string userName, string email);
        int CreateDeveloper(User developer);
        Task<User> RetrieveDevByEmail(string email);
        Task<User> RetrieveDevByUserName(string userNames);
    }
}
