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
        [UserValidate("user", UserValidate.ValidationType.All)]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CreatePet([FromBody] User user)
        {
            UserAuthResponse response = new UserAuthResponse();
            response.authToken = await domain.CreatePet(user);
            response.user = user;
            return Ok(response);
        }

        [HttpPost("login")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> Login([FromBody] User user)
        {
            UserAuthResponse response = new UserAuthResponse();
            response.authToken = await domain.Login(user);
            response.user = user;
            return Ok(response);
        }

        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [Consumes("application/json")]
        [Produces("application/json")]

        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetUser()
        {
            User user = await domain.RetrieveUserByUserName(HttpContext.User.Identity.Name);
            return Ok(user);
        }
    }

  
}
