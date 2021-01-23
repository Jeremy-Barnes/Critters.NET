using System.Threading.Tasks;
using CritterServer.Domains;
using CritterServer.Models;
using CritterServer.Pipeline.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CritterServer.Controllers
{
    [Route("api/search")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        SearchDomain SearchDomain;

        public SearchController(SearchDomain domain)
        {
            SearchDomain = domain;
        }

        [HttpGet("{searchTerm}")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> Search(string searchTerm, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
             var search = await SearchDomain.Search(searchTerm);
            return Ok(search);
        }
    }
}
