using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Contract
{
    /// <summary>
    /// Functional behavior through exceptions
    /// Always logs as info, but will log the InternalMessage, and output the ClientMessage to the front end
    /// </summary>
    public class CritterException : Exception
    {
        public string InternalMessage { get; set; }
        public string ClientMessage { get; set; }
        public System.Net.HttpStatusCode HttpStatus  { get; set; }
    }
}
