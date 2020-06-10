using CritterServer.Contract;
using CritterServer.Domains;
using CritterServer.Models;
using Dapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Game
{
    public class Battle : Game
    {
        private DateTime StartTime;
        public override GameType GameType => GameType.Battle;

        public Battle(User host, IServiceProvider services, Action<string> gameOverCallback, string gameName = null) : base(host, services, gameOverCallback, gameName)
        {
            this.TicksPerSecond = 0.5f;
            StartTime = DateTime.UtcNow;
        }

        public override void Tick(TimeSpan deltaT)
        {

        }

        public override Task AcceptUserInput(string userCommand, User user)
        {
            throw new NotImplementedException();
        }
    }
}
