using CritterServer.DataAccess;
using CritterServer.Domains;
using CritterServer.Models;
using CritterServer.Pipeline.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CritterServer.Controllers
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        NotificationDomain domain;

        public NotificationController(NotificationDomain domain)
        {
            this.domain = domain;
        }

        [HttpGet("new")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> RetrieveUnreadMessages()
        {
            return Ok();
        }

        [HttpGet()]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> RetrieveMessages() => await RetrieveMessagesPage(1);

        [HttpGet("page/{pageNumber}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> RetrieveMessagesPage(int pageNumber)
        {
            return Ok();
        }

        [HttpPost()]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> SendMessage([FromBody] Message message, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            message.MessageId = await domain.SendMessage(message, activeUser);
            return Ok(new { MessageId = message.MessageId, DateSent = message.DateSent });//lightweight JSON object
        }

        [HttpPatch("{messageId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> ReadMessage(List<int> messageIds, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            await domain.ReadMessage(messageIds, activeUser);
            return Ok();
        }

        [HttpDelete("{messageId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> DeleteMessage(List<int> messageIds, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {

            await domain.ReadMessage(messageIds, activeUser);
            return Ok();
        }

    }


}
