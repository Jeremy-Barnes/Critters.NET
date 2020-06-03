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

        public PetDomain(IPetRepository petRepo)
        {
            this.PetRepo = petRepo;
        }

        public async Task<Pet> CreatePet(Pet pet, User user)
        {
            using (var trans = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                pet.CurrentHitPoints = 0;
                pet.Level = 0;
                pet.OwnerID = user.UserId;

                pet.PetID = await PetRepo.CreatePet(pet, pet.OwnerID);
                //todo verify configs
                trans.Complete();
            }
            pet = (await RetrievePets(pet.PetID)).FirstOrDefault();
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
    }
}
