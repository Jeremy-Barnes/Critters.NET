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
            int output = (await dbConnection.QueryAsync<int>(@"INSERT INTO petSpeciesConfigs(name, maxHitPoints, description, imageBasePath) 
                VALUES(@speciesName, @maxHP, @desc, @image) RETURNING petSpeciesConfigID",
                new
                {
                    speciesName = species.Name,
                    maxHP = species.MaxHitPoints,
                    desc = species.Description,
                    image = species.ImageBasePath
                })).First();
            return output;
        }

        public async Task<int> CreatePetColor(PetColorConfig color)
        {
            int output = (await dbConnection.QueryAsync<int>(@"INSERT INTO petColorConfigs(name, imagePatternPath) 
                VALUES(@color, @imagePath) RETURNING petColorConfigID",
                new
                {
                    color = color.Name,
                    imagePath = color.ImagePatternPath
                })).First();
            return output;
        }

        public async Task<int> CreateGame(GameConfig game)
        {
            int output = (await dbConnection.QueryAsync<int>(@"INSERT INTO gameConfig(isactive, name, description, iconPath, cashCap, dailyCashCountCap, scoreToCashFactor, leaderboardMaxSpot, gameUrl) 
                VALUES(@active, @name, @description, @icon, @cashCap, @dailyCashCountLimit, @cashFactor, @lastPlace, @url) RETURNING gameConfigId",
                new
                {
                    active = game.IsActive,
                    name = game.Name,
                    description = game.Description,
                    icon = game.IconPath,
                    cashCap = game.CashCap,
                    dailyCashCountLimit = game.DailyCashCountCap,
                    cashFactor = game.ScoreToCashFactor,
                    lastPlace = game.LeaderboardMaxSpot,
                    url = game.GameURL,

                })).First();
            return output;
        }

        public async Task<IEnumerable<PetSpeciesConfig>> RetrieveSpeciesByIds(params int[] species)
        {
            return await dbConnection.QueryAsync<PetSpeciesConfig>("SELECT * FROM petSpeciesConfigs WHERE petSpeciesConfigID = ANY(@species)",
                new { species = species.Distinct().AsList() });
        }

        public async Task<IEnumerable<PetColorConfig>> RetrieveColorsByIds(params int[] colors)
        {
            var output = await dbConnection.QueryAsync<PetColorConfig>("SELECT * FROM petColorConfigs WHERE petColorConfigID = ANY(@colors)",
                new { colors = colors.Distinct().AsList() });
            return output;
        }

        public async Task<IEnumerable<PetSpeciesConfig>> RetrieveSpecies()
        { 
            var output = await dbConnection.QueryAsync<PetSpeciesConfig>("SELECT * FROM petSpeciesConfigs");
            return output;
        }

        public async Task<IEnumerable<PetColorConfig>> RetrieveColors()
        {
            var output = await dbConnection.QueryAsync<PetColorConfig>("SELECT * FROM petColorConfigs");
            return output;
        }

        public async Task<IEnumerable<GameConfig>> RetrieveGameConfigs(bool isActiveOnly = true)
        {
            var output = await dbConnection.QueryAsync<GameConfig>($"SELECT * FROM gameConfigs{(isActiveOnly ?  "WHERE isActive = true" : "")}");
            return output;
        }

        public async Task<IEnumerable<GameConfig>> RetrieveGamesConfigByIds(bool isActiveOnly = true, params int[] games)
        {
            return await dbConnection.QueryAsync<GameConfig>($"SELECT * FROM gameConfigs " +
                $"WHERE gameConfigID = ANY(@games){(isActiveOnly ? "AND isActive = true" : "")}",
                new { games = games.Distinct().AsList() });
        }
    }

    public interface IConfigRepository : IRepository
    {
        Task<int> CreatePetSpecies(PetSpeciesConfig species);
        Task<int> CreatePetColor(PetColorConfig color);
        Task<int> CreateGame(GameConfig game);
        Task<IEnumerable<PetSpeciesConfig>> RetrieveSpeciesByIds(params int[] species);
        Task<IEnumerable<PetColorConfig>> RetrieveColorsByIds(params int[] colors);
        Task<IEnumerable<PetSpeciesConfig>> RetrieveSpecies();
        Task<IEnumerable<PetColorConfig>> RetrieveColors();
        Task<IEnumerable<GameConfig>> RetrieveGameConfigs(bool isActiveOnly = true);
        Task<IEnumerable<GameConfig>> RetrieveGamesConfigByIds(bool isActiveOnly = true, params int[] games);
    }
}
