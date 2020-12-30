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
            try
            {
                TransactionScope transaction;
                if (System.Transactions.Transaction.Current == null)
                {
                    var txnOpts = new TransactionOptions
                    {
                        IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted
                    };
                    //var tran = new TransactionScope(
                    transaction = new TransactionScope(TransactionScopeOption.Required, txnOpts, TransactionScopeAsyncFlowOption.Enabled);
                } else
                {
                    transaction = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled);
                }
                if (this.DbConnection is DbConnection)
                {
                    var dbc = DbConnection as DbConnection;

                    if (dbc.State != ConnectionState.Open)
                    {
                        dbc.Open();
                    }
                    else
                    {
                        dbc.EnlistTransaction(System.Transactions.Transaction.Current);
                    }
                }
                return transaction;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //public TransactionScope Create()
        //{
        //    try
        //    {
        //        var transaction = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled);
        //        if (this.DbConnection is DbConnection)
        //        {
        //            var dbc = DbConnection as DbConnection;

        //            if (dbc.State != ConnectionState.Open)
        //            {
        //                dbc.Open();
        //            }
        //            //else
        //            {
        //                dbc.EnlistTransaction(System.Transactions.Transaction.Current);
        //            }
        //        }
        //        return transaction;
        //    } catch(Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
    }

    public interface ITransactionScopeFactory 
    {
        public abstract TransactionScope Create();
    }

    public class Txn : IDisposable
    {
        bool committed = false;
        IDbTransaction tx;
        public Txn(IDbTransaction tx)
        {
            this.tx = tx;
        }
        public void Commit()
        {
            tx.Commit();
        }
        public void Dispose()
        {
            if (!committed)
            {
                tx.Rollback();
            }
        }
    }

}
