using CritterServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Game
{
    public class Player
    {
        public User User { get; set; }
        public string SignalRConnectionId { get; set; }
        public Player(User user, string signalRConnectionId)
        {
            this.User = user;
            this.SignalRConnectionId = signalRConnectionId;
        }

        public Player(User user)
        {
            this.User = user;
        }

    }
}
