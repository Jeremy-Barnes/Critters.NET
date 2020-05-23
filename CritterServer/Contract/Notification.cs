using CritterServer.Models;
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
        public Notification() { }

        public Notification(string message)
        {
            this.AlertText = message;
        }

        /// <summary>
        /// Generic message to display to user
        /// </summary>
        public string AlertText { get; set; }

    }

    public class MessageAlert : Notification
    {
        public MessageDetails message { get; set; }

        public MessageAlert(MessageDetails message)
        {
            this.message = message;
            this.AlertText = $"New message from: {message.SenderUsername}: {message.Message.MessagePreview(50)}";
        }
    }
}
