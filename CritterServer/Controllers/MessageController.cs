using CritterServer.Contract;
using CritterServer.Domains;
using CritterServer.Models;
using CritterServer.Pipeline.Middleware;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Controllers
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        MessageDomain domain;

        public MessageController(MessageDomain domain)
        {
            this.domain = domain;
        }

        [HttpGet("new/{lastMessageId:int?}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> RetrieveUnreadMessages(int? lastMessageId, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            var channelsAndMessages = await domain.GetMessages(true, lastMessageId, activeUser);
            return Ok(new { ChannelDetails = channelsAndMessages });
        }

        [HttpGet()]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> RetrieveMessages([ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser) => await RetrieveMessagesPage(null, activeUser);

        [HttpGet("page/{lastMessageId}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> RetrieveMessagesPage(int? lastMessageId, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            var channelsAndMessages = await domain.GetMessages(false, lastMessageId, activeUser);
            return Ok(new { ChannelDetails = channelsAndMessages });
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> SendMessage([FromBody] Message message, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            message.MessageId = await domain.SendMessage(message, activeUser);
            return Ok(new { MessageId = message.MessageId, DateSent = message.DateSent });//lightweight JSON object
        }

        [HttpPatch]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> ReadMessage(List<int> messageIds, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            await domain.ReadMessage(messageIds, activeUser);
            return Ok();
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> DeleteMessage([FromBody]List<int> messageIds, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {

            await domain.ReadMessage(messageIds, activeUser);
            return Ok();
        }

        [HttpGet("thread/{lastMessageId}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> RetrieveThread(int lastMessageId, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            var messageThread = await domain.RetrieveThread(lastMessageId, activeUser);
            return Ok(messageThread);
        }

        [HttpPost("channel")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CreateChannel([FromBody]ChannelDetails channel, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            var newChannelId = await domain.CreateChannel(activeUser, channel.Channel?.ChannelName, channel.UserNames);
            return Ok(new { ChannelId = newChannelId });
        }


        [HttpGet("channel")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> RetrieveChannel([ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser) => await RetrieveChannel("", activeUser);

        [HttpGet("channel/{channelIdsCSV}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> RetrieveChannel(string channelIdsCSV, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            if (channelIdsCSV.Length < 100)
            {
                try
                {
                    var channelIds = channelIdsCSV.Length > 0 ? channelIdsCSV.Split(',').Select(csv => Int32.Parse(csv)).AsList() : null;
                    List<ChannelDetails> channelDetails = await domain.GetChannels(channelIds, activeUser);
                    return Ok(channelDetails);
                } catch(Exception ex)
                {
                    throw new CritterException("Sorry, that wasn't a valid list of channel IDs", $"{channelIdsCSV} provided to RetrieveChannel", System.Net.HttpStatusCode.BadRequest, ex);
                }
            }
            return BadRequest();
        }

    }


}
