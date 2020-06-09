using CritterServer.Models;
using CritterServer.DataAccess;
using CritterServer.Domains;
using CritterServer.Domains.Components;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using Xunit;

namespace Tests.IntegrationTests
{
    /// <summary>
    /// Creaated once, reused for all tests in AdminTests
    /// Used to hold expensive resources that can be reused (like a DB connection!)
    /// </summary>
    public class AdminTestsContext
    {
       
    }

    public class AdminTests : IClassFixture<AdminTestsContext>
    {
        AdminTestsContext context;

        public AdminTests(AdminTestsContext context)
        {
            this.context = context;
        }

    }
}
