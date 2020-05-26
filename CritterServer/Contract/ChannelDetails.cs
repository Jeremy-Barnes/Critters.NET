using CritterServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Contract
{
    public class ChannelDetails
    {
        public IEnumerable<MessageDetails> Messages { get; set; }
        public IEnumerable<User> Users { get; set; }

        public IEnumerable<string> UserNames { get; set; }
        public Channel Channel { get; set; }

        public ChannelDetails()
        {
            Messages = new List<MessageDetails>();
            UserNames = new List<string>();
        }

    }
}
