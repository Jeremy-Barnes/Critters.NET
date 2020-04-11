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
        UserAuthenticationDomain domain;

        public AdministrationController(UserAuthenticationDomain domain)
        {
            this.domain = domain;
        }

        // GET api/values
        [HttpGet]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "Hello World" };
        }

        // GET api/values
        [HttpGet("test")]
        public ActionResult<IEnumerable<string>> Gett()
        {
            return new string[] { "Hello World!" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "Hello World!" + id;
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
