using CritterServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Contract
{
    public class MessageDetails
    {
        public string SenderUsername { get; set; }
        public Message Message { get; set; }
    }
}
