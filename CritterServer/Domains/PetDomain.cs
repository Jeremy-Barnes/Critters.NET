using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Transactions;
using CritterServer.Contract;
using CritterServer.DataAccess;
using CritterServer.Domains.Components;
using CritterServer.Models;
using Dapper;
using Microsoft.Extensions.Logging;
namespace CritterServer.Domains
{
    public class PetDomain
    {
        IPetRepository PetRepo;
        IConfigRepository CfgRepo;
        public PetDomain(IPetRepository petRepo, IConfigRepository cfgRepo)
        {
            this.PetRepo = petRepo;
            this.CfgRepo = cfgRepo;
        }

        public async Task<Pet> CreatePet(Pet pet, User user)
        {

            using (var trans = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                ValidatePet(pet, user);
                pet.Level = 0;
                pet.OwnerID = user.UserId;
                pet.CurrentHitPoints = 50; //todo health...onomics and pet stats STR DEX WIS CHAR INT CON luck????
                try
                {
                    pet.PetId = await PetRepo.CreatePet(pet, pet.OwnerID);
                } 
                catch(Exception ex)
                {
                    throw new CritterException("Could not create pet, please try again!", "Something went wrong unexpectedly at the DB level", System.Net.HttpStatusCode.InternalServerError, ex, LogLevel.Critical);
                }
                //todo verify configs
                trans.Complete();
            }
            pet = (await RetrievePets(pet.PetId)).FirstOrDefault();
            return pet;
        }

        public async Task<IEnumerable<Pet>> RetrievePetsByOwner(int userId)
        {
            return await PetRepo.RetrievePetsByOwnerId(userId);
        }

        public async Task<IEnumerable<Pet>> RetrievePets(params int[] petIds)
        {
            return await PetRepo.RetrievePetsByIds(petIds);
        }

        public async Task<IEnumerable<PetDetails>> RetrieveFullPetInformation(IEnumerable<int> petIds)
        {
            return await RetrieveFullPetInformation(petIds.ToArray());
        }

        public async Task<IEnumerable<PetDetails>> RetrieveFullPetInformation(params int[] petIds)
        {
            return await PetRepo.RetrieveFullPetsByIds(petIds.ToArray());
        }

        public async Task<IEnumerable<PetDetails>> RetrieveFullPetInformationByOwner(int ownerId)
        {
            return await PetRepo.RetrieveFullPetsByOwnerId(ownerId);
        }

        public async Task<IEnumerable<PetColorConfig>> RetrieveAvailableColors(User user)
        {
            return await CfgRepo.RetrieveColorsByIds(); //todo jab unlockables!
        }

        public async Task<IEnumerable<PetSpeciesConfig>> RetrieveAvailableSpecies(User user)
        {
            return await CfgRepo.RetrieveSpeciesByIds();//todo jab unlockables
        }

        #region Validation
        private async void ValidatePet(Pet pet, User owner)
        {
            var dbPet = (await PetRepo.RetrievePetsByNames(pet.PetName)).FirstOrDefault();
            if (dbPet?.PetId != pet.PetId)
            {
                throw new CritterException("Sorry that name is already taken!", null, System.Net.HttpStatusCode.Conflict);
            }
            var color = (await CfgRepo.RetrieveColorsByIds(pet.ColorId)).FirstOrDefault();
            ValidateUserAccessToColor(owner, color);
            var species = (await CfgRepo.RetrieveSpeciesByIds(pet.SpeciesId)).FirstOrDefault();
            ValidateUserAccessToSpecies(owner, species);
        }

        private async void ValidateUserAccessToColor(User petOwner, PetColorConfig color)
        {
            if (color == null)
                throw new CritterException("Invalid color selection!", null, System.Net.HttpStatusCode.NotFound);
            //todo JAB unlockables
        }

        private async void ValidateUserAccessToSpecies(User petOwner, PetSpeciesConfig species)
        {
            if (species == null)
                throw new CritterException("Invalid species selection!", null, System.Net.HttpStatusCode.NotFound);
            //todo JAB unlockables
        }
        #endregion
    }
}
