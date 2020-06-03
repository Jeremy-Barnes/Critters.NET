using CritterServer.Utilities.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Models
{
    public class Pet
    {
        [BindNever]
        public int PetID { get; set; }
        public string PetName { get; set; }
        public int Level { get; set; }
        public int CurrentHitPoints { get; set; }
        public string Gender { get; set; }
        public int SpeciesId { get; set; }
        public int ColorId { get; set; }
        [InternalOnly]
        [BindNever]
        public int OwnerID { get; set; }
        public bool IsAbandoned { get; set; }
    }

    public class PetSpeciesConfig
    {
        public int PetSpeciesConfigID { get; set; }
        public string SpeciesName { get; set; }
        public int MaxHitPoints { get; set; }
        public string Description { get; set; }
        public string ImageBasePath { get; set; }
    }

    public class PetColorConfig
    {
        public int PetColorConfigID { get; set; }
        public string ColorName { get; set; }
        public string ImagePatternPath { get; set; }
    }
}
