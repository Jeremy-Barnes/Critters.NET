using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.DataAccess
{
    public class NpcRepository : INpcRepository
    {
        IDbConnection dbConnection;

        public NpcRepository(IDbConnection dbConnection)
        {
            this.dbConnection = dbConnection;
        }



    }

    public interface INpcRepository : IRepository
    {
    }
}
