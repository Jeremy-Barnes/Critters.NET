using CritterServer.Utilities.Serialization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CritterServer.Contract
{
    /// <summary>
    /// Functional behavior through exceptions
    /// Defaults to logging as info, but will log the InternalMessage, and output the ClientMessage to the front end
    /// </summary>
    public class CritterException : Exception
    {
        [InternalOnly]
        public string InternalMessage { get; set; }
        public string ClientMessage { get; set; }
        public HttpStatusCode HttpStatus { get; set; }

        /// <summary>
        ///Defaults to informational, increase if this is functional behavior that also needs to populate the logs
        /// </summary>
        [InternalOnly]
        public LogLevel LogLevelOverride { get; set; } = LogLevel.Information;

        public CritterException(string ClientMessage, string InternalMessage, HttpStatusCode HttpCode, Exception InnerException = null, LogLevel LogLevelOverride = LogLevel.Information) 
            : base(InternalMessage, InnerException)
        {
            this.InternalMessage = InternalMessage ?? ClientMessage;
            this.ClientMessage = ClientMessage;
            this.HttpStatus = HttpCode;
            this.LogLevelOverride = LogLevelOverride;
        }

        public CritterException(string ClientMessage, string InternalMessage, HttpStatusCode HttpCode, LogLevel LogLevelOverride = LogLevel.Information)
           : base(InternalMessage, null)
        {
            this.InternalMessage = InternalMessage ?? ClientMessage;
            this.ClientMessage = ClientMessage;
            this.HttpStatus = HttpCode;
            this.LogLevelOverride = LogLevelOverride;
        }

        public CritterException(string ClientMessage, string InternalMessage, HttpStatusCode HttpCode)
          : base(InternalMessage, null)
        {
            this.InternalMessage = InternalMessage ?? ClientMessage;
            this.ClientMessage = ClientMessage;
            this.HttpStatus = HttpCode;
        }
    }
}
