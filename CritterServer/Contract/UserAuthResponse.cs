using CritterServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Contract
{
    public class UserAuthResponse
    {
        public User user { get; set; }
        public string authToken { get; set; }

    }
}
