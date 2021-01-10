using CritterServer.Contract;
using CritterServer.Domains;
using CritterServer.Game;
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
        MultiplayerGameService GameManager;
        GameDomain GameDomain;
        public GameController(MultiplayerGameService gameManager, GameDomain gameDomain)
        {
            GameManager = gameManager;
            GameDomain = gameDomain;

        }

#region multiplayer
        [HttpPut("create/{gameId}/{gameType}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> CreateGame(string gameId, GameType gameType, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            return Ok(GameManager.StartGame(gameId, gameType, activeUser));
        }

        [HttpGet("list/{gameType:int?}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> FindGames(GameType? gameType)
        {
            return Ok(GameManager.GetGames(gameType));
        }

        [HttpPatch("command/{gameId}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> SendGameCommand(string gameId, [FromBody]string command, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            GameManager.Dispatch(command, gameId, activeUser);
            return Ok();
        }

        [HttpPut("join/{gameId}/{joinGameData}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> JoinGame(string gameId, string joinGameData, [ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser)
        {
            if (await GameManager.RequestJoinGame(gameId, activeUser, joinGameData)) return Ok();
            else return NotFound();
        }
        #endregion


        [HttpGet]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetGames([FromQuery(Name = "id")] int[] ids)
        {
            var response = await GameDomain.RetrieveGameConfigs(ids);
            if (response == null || response.Count() == 0) return NotFound();
            return Ok(new { Games = response });
        }

        [HttpGet("scoreAuth/{gameId}")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetGameKey([ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser, int gameId)
        {
            string key = await GameDomain.CreateKey(activeUser, gameId);
            return Ok(new { Key = key });
        }

        [HttpPost("gameScore/{gameId}/{score}/{scoreToken}")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> SubmitGameScore([ModelBinder(typeof(LoggedInUserModelBinder))] User activeUser, int gameId, int score, string scoreToken)
        {
            GameScoreResult result = await GameDomain.SubmitScore(activeUser, gameId, score, scoreToken);
            return Ok(result);
        }

        [HttpGet("leaderboard/{gameId}")]
        [Authorize(AuthenticationSchemes = "Cookie,Bearer")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetLeaderboard(int gameId)
        {
            var result = await GameDomain.RetrieveLeaderboard(gameId);
            return Ok(result);
        }
    }


}
