using CritterServer.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.DataAccess
{
    public class ConfigRepository : IConfigRepository
    {
        IDbConnection dbConnection;

        public ConfigRepository(IDbConnection dbConnection)
        {
            this.dbConnection = dbConnection;
        }

        public async Task<int> CreatePetSpecies(PetSpeciesConfig species)
        {
            dbConnection.TryOpen();
            int output = (await dbConnection.QueryAsync<int>("INSERT INTO petSpeciesConfigs(speciesName, maxHitPoints, description, imageBasePath)" +
                "VALUES(@speciesName, @maxHP, @desc, @image) RETURNING petSpeciesConfigID",
                new
                {
                    speciesName = species.SpeciesName,
                    maxHP = species.MaxHitPoints,
                    desc = species.Description,
                    image = species.ImageBasePath
                })).First();
            return output;
        }

        public async Task<int> CreatePetColor(PetColorConfig color)
        {
            dbConnection.TryOpen();
            int output = (await dbConnection.QueryAsync<int>("INSERT INTO petColorConfigs(colorName, imagePatternPath)" +
                "VALUES(@color, @imagePath) RETURNING petColorConfigID",
                new
                {
                    color = color.ColorName,
                    imagePath = color.ImagePatternPath
                })).First();
            return output;
        }

        public async Task<IEnumerable<PetSpeciesConfig>> RetrieveSpeciesByIds(params int[] species)
        {
            dbConnection.TryOpen();
            return await dbConnection.QueryAsync<PetSpeciesConfig>("SELECT * FROM petSpeciesConfigs WHERE petSpeciesConfigID = ANY(@species)",
                new { species = species.Distinct().AsList() });
        }

        public async Task<IEnumerable<PetColorConfig>> RetrieveColorsByIds(params int[] colors)
        {
            dbConnection.TryOpen();
            return await dbConnection.QueryAsync<PetColorConfig>("SELECT * FROM petColorConfig WHERE petColorConfigID = ANY(@colors)",
                new { colors = colors.Distinct().AsList() });
        }
    }

    public interface IConfigRepository : IRepository
    {
        Task<int> CreatePetSpecies(PetSpeciesConfig species);
        Task<int> CreatePetColor(PetColorConfig color);
        Task<IEnumerable<PetSpeciesConfig>> RetrieveSpeciesByIds(params int[] species);
        Task<IEnumerable<PetColorConfig>> RetrieveColorsByIds(params int[] colors);
    }
}
