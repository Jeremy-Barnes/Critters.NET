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
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CreateAccount([FromBody] User user)
        {
            var authToken = await domain.CreateAccount(user);
            user = await domain.RetrieveUserByUserName(user.UserName);
            await addLoginCookie(this.HttpContext, user.UserName, user.EmailAddress);
            user.ShowPrivateData = true;
            return Ok(new { AuthToken = authToken, User = user});
        }

        [HttpPost("login")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> Login([FromBody] User user)
        {
            var authToken = await domain.Login(user);
            if (!string.IsNullOrEmpty(user.EmailAddress))
                user = await domain.RetrieveUserByEmail(user.EmailAddress);
            else
                user = await domain.RetrieveUserByUserName(user.UserName);
 
            await addLoginCookie(this.HttpContext, user.UserName, user.EmailAddress);
            user.ShowPrivateData = true;
            return Ok(new { AuthToken = authToken, User = user });
        }

        [HttpGet("{username}")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [HttpGet]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetUser(string username)
        {
            User user = await domain.RetrieveUserByUserName(username ?? HttpContext.User.Identity.Name);
            if (username == null)
            {
                await addLoginCookie(this.HttpContext, user.UserName, user.EmailAddress);
                user.ShowPrivateData = true;
            }
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

        [HttpGet("friend")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetFriends([ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            IEnumerable<FriendshipDetails> fs = await domain.RetrieveFriends(activeUser);
            return Ok(fs);
        }

        [HttpPut("friend/{friendUserName}")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> AddFriend(string friendUserName, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            FriendshipDetails f = await domain.UpdateFriendship(friendUserName, activeUser, false);
            return Ok(f);
        }

        [HttpDelete("friend/{friendUserName}")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> RemoveFriend(string friendUserName, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            await domain.UpdateFriendship(friendUserName, activeUser, true);
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
