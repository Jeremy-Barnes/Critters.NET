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
        AdminDomain AdminDomain;
        EventDomain EventDomain;
        public AdministrationController(AdminDomain adminDomain, EventDomain eventDomain)
        {
            this.AdminDomain = adminDomain;
            this.EventDomain = eventDomain;
        }

        [HttpPost("loginDev")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> LoginDev([FromBody] User dev)
        {
            var authToken = await AdminDomain.LoginDev(dev);
            return Ok(new { AuthToken = authToken });
        }

        [HttpPost("createDev")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer", Roles = RoleTypes.Dev)]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CreateAccount([FromBody] User user, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            await AdminDomain.CreateDev(user, activeUser);
            return Ok();
        }

        [HttpPost("createSpecies")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer", Roles = RoleTypes.Dev)]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CreateSpecies([FromBody] PetSpeciesConfig species, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            return Ok(await AdminDomain.CreatePetSpecies(species, activeUser));
        }

        [HttpPost("createColor")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer", Roles = RoleTypes.Dev)]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CreateColor([FromBody] PetColorConfig color, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeDev)
        {
            return Ok(await AdminDomain.CreatePetColor(color, activeDev));
        }

        [HttpPost("generateRandomEvent/{{userId:int?}}")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer", Roles = RoleTypes.Dev)]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CreateColor(int? userId, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeDev)
        {
            return Ok(await EventDomain.CreateRandomEvent(userId, activeDev));
        }
        //todo delete species and color
    }
}
