using CritterServer.Utilities.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Models
{
    public class Channel
    {
        public int ChannelId { get; set; }
        [MaxLength(50)]
        public string ChannelName { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
