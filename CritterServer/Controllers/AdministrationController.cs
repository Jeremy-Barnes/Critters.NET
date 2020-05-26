using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CritterServer.Domains;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CritterServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdministrationController : ControllerBase
    {
        UserDomain domain;

        public AdministrationController(UserDomain domain)
        {
            this.domain = domain;
        }

        // GET api/administration requires a JWT
        [HttpGet]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "Hello World" };
        }

        // GET api/administration
        [HttpGet("test")]
        public ActionResult<IEnumerable<string>> Gett()
        {
            return new string[] { "Hello World!" };
        }

        // GET api/administration/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "Hello World!" + id;
        }

        // POST api/administration
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/administration/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/administration/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
