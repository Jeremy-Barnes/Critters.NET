using CritterServer.Contract;
using CritterServer.Domains;
using CritterServer.Models;
using CritterServer.Pipeline.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CritterServer.Controllers
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
    [ApiController]
    public class GameController : ControllerBase
    {
        MessageDomain domain;
        GameManagerService gms;
        public GameController(/*MessageDomain domain*/IHostedService gms)
        {
            //this.domain = domain;
            this.gms = gms as GameManagerService;
        }

        [HttpGet("join/{gameId}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> JoinGame(int gameId, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            //var channelsAndMessages = await domain.GetMessages(true, lastMessageId, activeUser);
            //return Ok(new { ChannelDetails = channelsAndMessages });
            return Ok(gms.StartGame(activeUser));
        }

        [HttpPut("command/{gameId}/{command}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GameCommand(string gameId, string command, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            //var channelsAndMessages = await domain.GetMessages(true, lastMessageId, activeUser);
            //return Ok(new { ChannelDetails = channelsAndMessages });
            gms.Dispatch(command, gameId, activeUser);
            return Ok();
        }

        //[HttpPost]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //public async Task<ActionResult> SendMessage([FromBody] Message message, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        //{
        //    message.MessageId = await domain.SendMessage(message, activeUser);
        //    return Ok(new { MessageId = message.MessageId, DateSent = message.DateSent });//lightweight JSON object
        //}

        //[HttpGet("thread/{lastMessageId}")]
        //[Produces("application/json")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //public async Task<ActionResult> RetrieveThread(int lastMessageId, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        //{
        //    var messageThread = await domain.RetrieveThread(lastMessageId, activeUser);
        //    return Ok(new { ChannelDetails = messageThread });
        //}

        //[HttpPost("channel")]
        //[Produces("application/json")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //public async Task<ActionResult> CreateChannel([FromBody]ChannelDetails channel, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        //{
        //    var newChannelId = await domain.CreateChannel(activeUser, channel.Channel?.ChannelName, channel.UserNames);
        //    return Ok(new { ChannelId = newChannelId });
        //}

    }


}
