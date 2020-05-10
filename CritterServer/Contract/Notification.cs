using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Contract
{
    /// <summary>
    /// Base class for banner-type notifications from the server
    /// </summary>
    public class Notification
    {
        public Notification(string message)
        {
            this.Message = message;
        }

        /// <summary>
        /// Generic message to display to user
        /// </summary>
        public string Message { get; set; }

    }
}
