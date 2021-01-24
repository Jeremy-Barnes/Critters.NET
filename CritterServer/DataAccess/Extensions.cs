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
                if (dbConnection is DbConnection)
                {
                    var dbc = dbConnection as DbConnection;
                    dbc.EnlistTransaction(Transaction.Current);
                }
                return;
            }
            dbConnection.Open();
        }
    }
    public class TransactionScopeFactory : ITransactionScopeFactory
    {
        private TransactionScope Transaction;
        private IDbConnection DbConnection;
        public TransactionScopeFactory(IDbConnection dbConnection)
        {
            DbConnection = dbConnection;
        }

        public TransactionScope Create()
        {
            string guid = Guid.NewGuid().ToString();
            guid += "\r\n" + Environment.StackTrace;
            try
            {
                TransactionScope transaction;
                if (System.Transactions.Transaction.Current == null)
                {
                    Console.WriteLine($"Txn is null {guid}");
                    var txnOpts = new TransactionOptions
                    {
                        IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted
                    };
                    transaction = new TransactionScope(TransactionScopeOption.Required, txnOpts, TransactionScopeAsyncFlowOption.Enabled);
                } 
                else
                {
                    Console.WriteLine($"Txn is NOT null {guid}");
                    transaction = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled);
                }
                if (this.DbConnection is DbConnection)
                {
                    var dbc = DbConnection as DbConnection;

                    if (dbc.State != ConnectionState.Open)
                    {
                        Console.WriteLine($"Txn is opening {guid}");
                        dbc.Open();
                    }
                    else
                    {
                        Console.WriteLine($"Txn is enlisting {guid}");
                        dbc.EnlistTransaction(System.Transactions.Transaction.Current);
                    }
                }
                return transaction;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ex {guid}");
                throw ex;
            }
        }
    }

    public interface ITransactionScopeFactory 
    {
        public abstract TransactionScope Create();
    }
}
