using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace CritterServer.DataAccess
{
    public static class Extensions
    {
        public static void TryOpen(this IDbConnection dbConnection)
        {
            if (dbConnection.State == ConnectionState.Open)
            {
                if(dbConnection is DbConnection)
                {
                    var dbc = dbConnection as DbConnection;
                    dbc.EnlistTransaction(Transaction.Current);
                }
                return;
            }
            dbConnection.Open();
        }
    }
}
