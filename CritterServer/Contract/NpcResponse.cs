using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Contract
{
    public class NpcResponse
    {
        public Dictionary<string, int> interactionAndCodes;
        public List<string> messages;

    }
}
