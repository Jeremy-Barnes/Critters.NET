using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using CritterServer.Models;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace CritterServer.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class AuthenticationController: ControllerBase
    {
        [HttpPut("createUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult CreateUserAccount([FromBody] User user)
        {

            return Ok();
        }
    }
}
