using CritterServer.Utilities.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Models
{
    public class Friendship
    {
        [InternalOnly]
        public int RequesterUserId { get; set; }
        [InternalOnly]
        public int RequestedUserId { get; set; }
        public DateTime DateSent { get; set; }
        public bool Accepted { get; set; }
    }
}
