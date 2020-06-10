using CritterServer.Contract;
using CritterServer.Models;
using Dapper;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.DataAccess
{
    public class PetRepository : IPetRepository
    {
        IDbConnection dbConnection;

        public PetRepository(IDbConnection dbConnection)
        {
            this.dbConnection = dbConnection;
        }

        public async Task<int> CreatePet(Pet pet, int ownerUserId)
        {
            dbConnection.TryOpen();
            int output = (await dbConnection.QueryAsync<int>("INSERT INTO pets(petName, gender, colorID, ownerID, speciesID, currentHitPoints, isAbandoned)" +
                "VALUES(@petName, @gender, @colorId, @ownerUserId, @speciesId, @currentHitPoints, false) RETURNING petID",
                new
                {
                    petName = pet.PetName,
                    gender = pet.Gender,
                    colorId = pet.ColorId,
                    ownerUserId = ownerUserId,
                    speciesId = pet.SpeciesId,
                    currentHitPoints = pet.CurrentHitPoints
                })).First();
            return output;
        }

        public async Task<IEnumerable<Pet>> RetrievePetsByIds(params int[] petIds)
        {
            dbConnection.TryOpen();
            return await dbConnection.QueryAsync<Pet>("SELECT * FROM pets WHERE petID = ANY(@petIds)",
                new { petIds = petIds.Distinct().AsList() });
        }

        public async Task<IEnumerable<PetDetails>> RetrieveFullPetsByIds(params int[] petIds)
        {
            dbConnection.TryOpen();
            return await dbConnection.QueryAsync<Pet,PetColorConfig, PetSpeciesConfig, PetDetails>(@"
                SELECT * FROM pets p
                INNER JOIN petColorConfigs pcc ON p.PetID = ANY(@petIds) AND p.colorID = pcc.petColorConfigID 
                INNER JOIN petSpeciesConfigs psc ON p.speciesID = psc.petSpeciesConfigID",
                param: new { petIds = petIds.Distinct().AsList() },
                splitOn: "petColorConfigId,petSpeciesConfigID",
                map: (p, pcc, psc) => new PetDetails(p, psc, pcc));
        }

        public async Task<IEnumerable<PetDetails>> RetrieveFullPetsByNames(params string[] names)
        {
            dbConnection.TryOpen();
            return await dbConnection.QueryAsync<Pet, PetSpeciesConfig, PetColorConfig, PetDetails>(@"
                SELECT * FROM pets p
                INNER JOIN petColorConfigs pcc ON p.petName = ANY(@names) AND p.colorID = pcc.petColorConfigID 
                INNER JOIN petSpeciesConfigs psc ON p.speciesID = psc.petSpeciesConfigID",
                param: new { names = names.Distinct().AsList() },
                splitOn: "petColorConfigId,petSpeciesConfigID",
                map: (p, psc, pcc) => new PetDetails(p, psc, pcc));
        }


        public async Task<IEnumerable<Pet>> RetrievePetsByNames(params string[] names)
        {
            dbConnection.TryOpen();
            return await dbConnection.QueryAsync<Pet>(@"
                SELECT * FROM pets
                WHERE petName = ANY(@names)",
                param: new { names = names.Distinct().AsList() });
        }

        public async Task<IEnumerable<PetDetails>> RetrieveFullPetsByOwnerId(int ownerUserId)
        {
            dbConnection.TryOpen();
            return await dbConnection.QueryAsync<Pet, PetSpeciesConfig, PetColorConfig, PetDetails>(@"
                SELECT p.*, psc.*, pcc.* FROM pets p
                INNER JOIN petColorConfigs pcc ON p.ownerID = @ownerUserId AND p.colorID = pcc.petColorConfigID 
                INNER JOIN petSpeciesConfigs psc ON p.speciesID = psc.petSpeciesConfigID",
                param: new { ownerUserId },
                splitOn: "petspeciesconfigid,petcolorconfigid",
                map: (p, psc, pcc) => new PetDetails(p, psc, pcc));
        }

        public async Task<IEnumerable<Pet>> RetrievePetsByOwnerId(int ownerUserId)
        {
            dbConnection.TryOpen();
            return await dbConnection.QueryAsync<Pet>("SELECT * FROM pets WHERE ownerID = @ownerUserId", new { ownerUserId });
        }

        public async Task UpdatePet(string petName, string gender, int petId)
        {
            dbConnection.TryOpen();
            await dbConnection.ExecuteAsync($"UPDATE pets SET petName = @petName, gender = @gender WHERE petID = @petId", new { petId, petName, gender });
        }

        public async Task AbandonPet(int petId)
        {
            dbConnection.TryOpen();
            await dbConnection.ExecuteAsync($"UPDATE pets SET isAbandoned = true, ownerId = NULL WHERE petID = @petId", new { petId });
        }
    }

    public interface IPetRepository : IRepository
    {
        Task<int> CreatePet(Pet pet, int ownerUserId);
        Task<IEnumerable<Pet>> RetrievePetsByIds(params int[] petIds);
        Task<IEnumerable<Pet>> RetrievePetsByOwnerId(int ownerUserId);
        Task<IEnumerable<PetDetails>> RetrieveFullPetsByOwnerId(int ownerUserId);
        Task<IEnumerable<PetDetails>> RetrieveFullPetsByIds(params int[] petIds);
        Task UpdatePet(string petName, string gender, int petId);
        Task AbandonPet(int petId);
        Task<IEnumerable<Pet>> RetrievePetsByNames(params string[] names);
    }
}
