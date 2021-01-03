using CritterServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Contract
{
    public class FriendshipDetails
    {
        public string RequesterUserName { get; set; }
        public string RequestedUserName { get; set; }

        public Friendship Friendship { get; set; }
    }
}
