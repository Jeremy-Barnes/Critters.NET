using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CritterServer.Domains;
using CritterServer.Domains.Components;
using CritterServer.Models;
using CritterServer.Pipeline.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CritterServer.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdministrationController : ControllerBase
    {
        AdminDomain domain;

        public AdministrationController(AdminDomain domain)
        {
            this.domain = domain;
        }

        [HttpPost("loginDev")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> LoginDev([FromBody] User dev)
        {
            var authToken = await domain.LoginDev(dev);
            return Ok(new { AuthToken = authToken });
        }

        [HttpPost("createDev")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer", Roles = RoleTypes.Dev)]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CreateAccount([FromBody] User user, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            await domain.CreateDev(user, activeUser);
            return Ok();
        }

        [HttpPost("createSpecies")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer", Roles = RoleTypes.Dev)]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CreateSpecies([FromBody] PetSpeciesConfig species, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            return Ok(await domain.CreatePetSpecies(species, activeUser));
        }

        [HttpPost("createColor")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer", Roles = RoleTypes.Dev)]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CreateColor([FromBody] PetColorConfig color, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            return Ok(await domain.CreatePetColor(color, activeUser));
        }
        //todo delete species and color
    }
}
