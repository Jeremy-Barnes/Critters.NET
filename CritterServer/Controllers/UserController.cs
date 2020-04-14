using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
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
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CreateAccount([FromBody] User user)
        {
            UserAuthResponse response = new UserAuthResponse();
            response.authToken = await domain.CreateAccount(user);
            response.user = user;
            addLoginCookie(this.HttpContext, user.UserName);
            return Ok(response);
        }

        [HttpPost("login")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult Login([FromBody] User user)
        {
            UserAuthResponse response = new UserAuthResponse();
            response.authToken = domain.Login(user);
            response.user = user;
            addLoginCookie(this.HttpContext, user.UserName);
            return Ok(response);
        }

        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetUser()
        {
            User user = domain.RetrieveUserByUserName(HttpContext.User.Identity.Name);
            addLoginCookie(this.HttpContext, userName: user.UserName);
            return Ok(user);
        }

        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [HttpDelete("token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult Logout()
        {
            this.HttpContext.SignOutAsync("Cookie");
            return Ok();
        }

        private async void addLoginCookie(HttpContext context, string userName)
        {
            await this.HttpContext.SignInAsync("Cookie", 
                new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, userName)
                        }
                    )
                )
            );
        }

    }
}
