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


namespace CritterServer.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class AuthenticationController: ControllerBase
    {
        UserAuthenticationDomain domain;

        public AuthenticationController(UserAuthenticationDomain domain)
        {
            this.domain = domain;
        }


        [HttpPut("createUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult CreateUserAccount([FromBody] User user)
        {
            domain.CreateUserAccount(user);
            return Ok();
        }
    }
}
