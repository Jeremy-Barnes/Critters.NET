using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Models
{
    public class Npc
    {
        public int npcID { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string imagePath { get; set; }

        [JsonIgnore]
        public string methodScripts { get; set; }
    }
}
