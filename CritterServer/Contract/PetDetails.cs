using CritterServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Contract
{
    public class PetDetails
    {
        public PetDetails(Pet pet, PetSpeciesConfig species, PetColorConfig color)
        {
            this.Pet = pet;
            this.Species = species;
            this.Color = color;
        }

        public Pet Pet { get; set; }
        public PetSpeciesConfig Species { get; set; }
        public PetColorConfig Color { get; set; }
    }
}
