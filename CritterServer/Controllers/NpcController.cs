using CritterServer.Domains;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Controllers
{
    [Route("api/npc")]
    [ApiController]
    public class NpcController : ControllerBase
    {
        NpcDomain domain;

        public NpcController(NpcDomain domain)
        {
            this.domain = domain;
        }

        [HttpPost("test")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult Test()
        {
            return Ok(domain.Test());
        }
    }
}
