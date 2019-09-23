using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using CritterServer.Models;
using CritterServer.Domains;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CritterServer.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        UserAuthenticationDomain domain;

        public UserController(UserAuthenticationDomain domain)
        {
            this.domain = domain;
        }


        [HttpPut("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult CreateAccount([FromBody] User user)
        {
            domain.CreateAccount(user);
            return Ok(user);
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult Login([FromBody] User user)
        {
            Serilog.Log.Information("Logging in {user}", user.UserName);
            Serilog.Log.Warning("");
            domain.Login(user);
            return Ok(user);
        }

        [Authorize]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetUser()
        {
            Serilog.Log.Information("Wow a JWT came in!!?!??!");
            return Ok();
        }

    }
}
