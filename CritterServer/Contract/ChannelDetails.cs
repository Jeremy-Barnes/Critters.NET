using CritterServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Contract
{
    public class ChannelDetails
    {
        public List<Message> Messages { get; set; }
        public List<User> Users { get; set; }

        public List<string> UserNames { get; set; }
        public Channel Channel { get; set; }

        public ChannelDetails()
        {
            Messages = new List<Message>();
            UserNames = new List<string>();
        }

    }
}
