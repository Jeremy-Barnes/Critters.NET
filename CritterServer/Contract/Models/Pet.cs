using CritterServer.Pipeline;
using CritterServer.Utilities.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Models
{
    public class Pet
    {
        [BindNever]
        public int PetId { get; set; }
        [MaxLength(24)]
        public string PetName { get; set; }
        public int Level { get; set; }
        public int CurrentHitPoints { get; set; }
        [AcceptedValues(false, true, "male", "female", "other")]
        public string Gender { get; set; }
        public int SpeciesId { get; set; }
        public int ColorId { get; set; }
        [InternalOnly]
        [BindNever]
        public int OwnerId { get; set; }
        public bool IsAbandoned { get; set; }
    }

    public class PetSpeciesConfig
    {
        public int PetSpeciesConfigID { get; set; }
        [MaxLength(24)]
        public string SpeciesName { get; set; }
        public int MaxHitPoints { get; set; }
        [MaxLength(2000)]
        public string Description { get; set; }
        [MaxLength(200)]
        public string ImageBasePath { get; set; }
    }

    public class PetColorConfig
    {
        public int PetColorConfigID { get; set; }
        [MaxLength(24)]
        public string ColorName { get; set; }
        [MaxLength(200)]
        public string ImagePatternPath { get; set; }
    }
}
