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
    public class AdminDomain
    {
        IConfigRepository ConfigRepo;
        IUserRepository UserRepo;
        IJwtProvider JwtProvider;
        ITransactionScopeFactory TransactionScopeFactory;

        public AdminDomain(IConfigRepository cfgRepo, IUserRepository userRepo, IJwtProvider jwtProvider, ITransactionScopeFactory transactionScopeFactory)
        {
            ConfigRepo = cfgRepo;
            UserRepo = userRepo;
            JwtProvider = jwtProvider;
            TransactionScopeFactory = transactionScopeFactory;
        }

        public async Task<PetSpeciesConfig> CreatePetSpecies(PetSpeciesConfig species, User activeDev)
        {
            using (var trans = TransactionScopeFactory.Create())
            {
                species.PetSpeciesConfigId = await ConfigRepo.CreatePetSpecies(species);
                trans.Complete();
            }
            species = (await ConfigRepo.RetrieveSpeciesByIds(species.PetSpeciesConfigId)).FirstOrDefault();
            return species;
        }

        public async Task<PetColorConfig> CreatePetColor(PetColorConfig color, User activeDev)
        {
            using (var trans = TransactionScopeFactory.Create())
            {
                color.PetColorConfigId = await ConfigRepo.CreatePetColor(color);
                trans.Complete();
            }
            color = (await ConfigRepo.RetrieveColorsByIds(color.PetColorConfigId)).FirstOrDefault();
            return color;
        }

        public async Task<PetColorConfig> RetrievePetColorConfig(int cfgId)
        {
            return (await ConfigRepo.RetrieveColorsByIds(cfgId)).First();
        }

        public async Task<PetSpeciesConfig> RetrievePetSpeciesConfig(int cfgId)
        {
            return (await ConfigRepo.RetrieveSpeciesByIds(cfgId)).First();
        }

        public async Task<string> CreateDev(User dev, User creatingUser) //todo log out activities by devs
        {
            using (var trans = TransactionScopeFactory.Create())
            {
                dev.Cash = 1000000;
                dev.IsActive = true;
                dev.Salt = BCrypt.Net.BCrypt.GenerateSalt();
                dev.Password = BCrypt.Net.BCrypt.HashPassword(dev.Password, dev.Salt);

                dev.UserId = UserRepo.CreateDeveloper(dev);

                trans.Complete();
            }
            dev = (await UserRepo.RetrieveUsersByIds(dev.UserId)).First();
            return JwtProvider.GenerateToken(dev.UserName, dev.EmailAddress, RoleTypes.Dev);
        }

        public async Task<string> LoginDev(User dev)
        {
            User dbUser = null;
            if (!string.IsNullOrEmpty(dev.UserName))
            {
                dbUser = await UserRepo.RetrieveDevByUserName(dev.UserName);
            }
            else if (!string.IsNullOrEmpty(dev.EmailAddress))
            {
                dbUser = await UserRepo.RetrieveDevByEmail(dev.EmailAddress);
            }

            if (dbUser != null && !string.IsNullOrEmpty(dev.Password))
            {
                string hashPassword = BCrypt.Net.BCrypt.HashPassword(dev.Password, dbUser.Salt);
                if (dbUser.Password == hashPassword) //success
                {
                    dev = dbUser;
                    return JwtProvider.GenerateToken(dev.UserName, dev.EmailAddress, RoleTypes.Dev);
                }
            }
            throw new CritterException($"The provided credentials were invalid for {dev.UserName ?? dev.EmailAddress}", null, System.Net.HttpStatusCode.Unauthorized, LogLevel.Critical);
        }
    }
}
