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
        public Message message { get; set; }

        public MessageAlert(Message message)
        {
            this.message = message;
            this.AlertText = $"New message from: {message.SenderUserName}: {message.MessagePreview(50)}";
        }
    }
}
