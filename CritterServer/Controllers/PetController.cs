using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using CritterServer.Models;
using CritterServer.Domains;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using CritterServer.Contract;
using CritterServer.Pipeline;
using CritterServer.Pipeline.Middleware;

namespace CritterServer.Controllers
{
    [Route("api/pet")]
    [ApiController]
    public class PetController : ControllerBase
    {
        PetDomain domain;

        public PetController(PetDomain domain)
        {
            this.domain = domain;
        }

        [HttpPost("create")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CreatePet([FromBody] Pet pet, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            PetDetails response;
            var dbPet = await domain.CreatePet(pet, activeUser);
            response = (await domain.RetrieveFullPetInformation(dbPet.PetId)).FirstOrDefault();
            return Ok(response);
        }

        [HttpGet("all")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetPets([ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            var response = await domain.RetrieveFullPetInformationByOwner(activeUser.UserId);
            return Ok(new { Pets = response });
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetPets([FromQuery(Name = "id")] int[] ids)
        {
            var response = await domain.RetrieveFullPetInformation(ids);
            if (response == null || response.Count() == 0) return NotFound();
            return Ok(new { Pets = response });
        }

        [HttpGet("colors")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetColors([ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            var response = await domain.RetrieveAvailableColors(activeUser);
            return Ok(new { Colors = response });
        }

        [HttpGet("species")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetSpecies([ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            var response = await domain.RetrieveAvailableSpecies(activeUser);
            return Ok(new { Species = response });
        }
    }

  
}
