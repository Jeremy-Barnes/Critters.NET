using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using CritterServer.Contract;
using CritterServer.DataAccess;
using CritterServer.Models;
using Microsoft.Extensions.Logging;
namespace CritterServer.Domains
{
    public class PetDomain
    {
        IPetRepository PetRepo;
        IConfigRepository CfgRepo;
        ITransactionScopeFactory TransactionScopeFactory;

        public PetDomain(IPetRepository petRepo, IConfigRepository cfgRepo, ITransactionScopeFactory transactionScopeFactory)
        {
            this.PetRepo = petRepo;
            this.CfgRepo = cfgRepo;
            TransactionScopeFactory = transactionScopeFactory;
        }

        public async Task<Pet> CreatePet(Pet pet, User user)
        {

            using (var trans = TransactionScopeFactory.Create())
            {
                await ValidatePet(pet, user);
                pet.Level = 0;
                pet.OwnerId = user.UserId;
                pet.CurrentHitPoints = 50; //todo health...onomics and pet stats STR DEX WIS CHAR INT CON luck????
                try
                {
                    pet.PetId = await PetRepo.CreatePet(pet, pet.OwnerId);
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
        public async Task ChangePetHealth(List<(int PetId, int HealthDelta)> petToHpDelta)
        {
            using (var trans = TransactionScopeFactory.Create())
            {
                await PetRepo.UpdatePetHealth(petToHpDelta.ToArray());
                trans.Complete();
            }
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
            return await CfgRepo.RetrieveColors(); //todo jab unlockables!
        }

        public async Task<IEnumerable<PetSpeciesConfig>> RetrieveAvailableSpecies(User user)
        {
            return await CfgRepo.RetrieveSpecies();//todo jab unlockables
        }

        #region Validation
        private async Task ValidatePet(Pet pet, User owner)
        {
            var dbPet = (await PetRepo.RetrievePetsByNames(pet.PetName)).FirstOrDefault();
            if (dbPet != null && dbPet.PetId != pet.PetId)
            {
                throw new CritterException("Sorry that name is already taken!", null, System.Net.HttpStatusCode.Conflict);
            }
            var color = (await CfgRepo.RetrieveColorsByIds(pet.ColorId)).FirstOrDefault();
            await ValidateUserAccessToColor(owner, color);
            var species = (await CfgRepo.RetrieveSpeciesByIds(pet.SpeciesId)).FirstOrDefault();
            await ValidateUserAccessToSpecies(owner, species);
        }

        private async Task ValidateUserAccessToColor(User petOwner, PetColorConfig color)
        {
            if (color == null)
                throw new CritterException("Invalid color selection!", null, System.Net.HttpStatusCode.NotFound);
            //todo JAB unlockables
        }

        private async Task ValidateUserAccessToSpecies(User petOwner, PetSpeciesConfig species)
        {
            if (species == null)
                throw new CritterException("Invalid species selection!", null, System.Net.HttpStatusCode.NotFound);
            //todo JAB unlockables
        }
        #endregion
    }
}
