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
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        UserDomain domain;

        public UserController(UserDomain domain)
        {
            this.domain = domain;
        }

        [HttpPost("create")]
        [UserValidate("user", UserValidate.ValidationType.All)]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CreateAccount([FromBody] User user)
        {
            UserAuthResponse response = new UserAuthResponse();
            response.authToken = await domain.CreateAccount(user);
            response.user = await domain.RetrieveUserByUserName(user.UserName);
            await addLoginCookie(this.HttpContext, response.user.UserName, response.user.EmailAddress);
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
            if (!string.IsNullOrEmpty(user.EmailAddress))
                user = await domain.RetrieveUserByEmail(user.EmailAddress);
            else
                user = await domain.RetrieveUserByUserName(user.UserName);
            response.user = user;
            await addLoginCookie(this.HttpContext, user.UserName, user.EmailAddress);
            return Ok(response);
        }

        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [Consumes("application/json")]
        [Produces("application/json")]

        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetUser()
        {
            User user = await domain.RetrieveUserByUserName(HttpContext.User.Identity.Name);
            await addLoginCookie(this.HttpContext, user.UserName, user.EmailAddress);
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

        private async Task addLoginCookie(HttpContext context, string userName, string email)
        {
            var claims = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, userName),
                            new Claim(ClaimTypes.Email, email)
                        }, "Cookie"
                    )
                );
            context.User = claims;
            await this.HttpContext.SignInAsync("Cookie", claims);
        }
    }

  
}
