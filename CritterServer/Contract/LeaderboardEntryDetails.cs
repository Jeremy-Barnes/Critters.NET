using CritterServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Contract
{
    public class LeaderboardEntryDetails
    {
        public string Username { get; set; }
        public LeaderboardEntry Entry { get; set; }
    }
}
