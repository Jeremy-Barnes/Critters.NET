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
            string jwt = domain.CreateAccount(user);
            addLoginCookie(this.HttpContext, user.UserName);
            return Ok(user);
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult Login([FromBody] User user)
        {
            string jwt = domain.Login(user);
            addLoginCookie(this.HttpContext, user.UserName);
            return Ok(user);
        }

        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [HttpGet]
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
            this.HttpContext.SignOutAsync();
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
