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

    public class NewMessageAlert : Notification
    {
        public MessageDetails Message { get; set; }

        public NewMessageAlert(MessageDetails message)
        {
            this.Message = message;
            this.AlertText = $"New message from: {message.SenderUsername}: {message.Message.MessagePreview(50)}";
        }
    }


    public class GameAlert : Notification
    {
        public int GameType{ get; set; }

        public GameAlert(string message, GameType game)
        {
            GameType = (int)game;
            this.AlertText = message;
        }
    }
}
