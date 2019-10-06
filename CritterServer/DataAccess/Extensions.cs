using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.DataAccess
{
    public static class Extensions
    {
        public static void TryOpen(this IDbConnection dbConnection)
        {
            if (dbConnection.State == ConnectionState.Open)
                return;
            dbConnection.Open();
        }
    }
}
