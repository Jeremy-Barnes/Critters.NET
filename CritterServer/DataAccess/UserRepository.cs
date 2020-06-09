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

        public async Task UpdateUserCash(int userId, int deltaCash)
        {
            dbConnection.TryOpen();
            await dbConnection.ExecuteAsync($"UPDATE users SET cash = cash + @cash WHERE userID = @userId", new { userId, cash = deltaCash });
        }

        public async Task UpdateUsersCash(IEnumerable<Tuple<int, int>> userIdAndDeltaCashAmounts)
        {
            dbConnection.TryOpen();
            await dbConnection.ExecuteAsync($"UPDATE users SET cash = cash + @cash WHERE userID = @userId", userIdAndDeltaCashAmounts.Select(u => new { userId = u.Item1, cash = u.Item2 }));
        }
    }

    public interface IUserRepository : IRepository
    {
        int CreateUser(User user);
        Task<IEnumerable<User>> RetrieveUsersByIds(params int[] userIds);
        Task<User> RetrieveUserByEmail(string email);
        Task<IEnumerable<User>> RetrieveUsersByUserName(params string[] userNames);
        Task<bool> UserExistsByUserNameOrEmail(string userName, string email);
        Task UpdateUserCash(int userId, int deltaCash);
        Task UpdateUsersCash(IEnumerable<Tuple<int, int>> userIdAndDeltaCashAmounts);
    }
}
