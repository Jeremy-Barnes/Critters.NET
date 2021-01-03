using CritterServer.Utilities.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Models
{
    public class LeaderboardEntry
    {
        public int GameId { get; set; }
        [InternalOnly]
        public int PlayerId { get; set; }
        public DateTime DateSubmitted { get; set; }
        public int Score{ get; set; }
    }
}
