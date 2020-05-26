using CritterServer.Utilities.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Models
{
    public class Message
    {
        public int MessageId { get; set; }
        [InternalOnly]
        public int SenderUserId { get; set; }
        public DateTime DateSent { get; set; }
        public string MessageText { get; set; }
        public string MessageSubject { get; set; }
        public int? ParentMessageId{ get; set; }
        public int ChannelId { get; set; }

        public string MessagePreview(int? truncateAfter)
        {
            string preview = (MessageSubject ?? MessageText) ?? "";
            if (truncateAfter.HasValue)
            {
                preview = preview.Substring(0, Math.Min(truncateAfter.Value, preview.Length));
            }
            return preview;
        }
    }
}
