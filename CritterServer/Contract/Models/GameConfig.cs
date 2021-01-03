using CritterServer.Pipeline;
using CritterServer.Utilities.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace CritterServer.Models
{
    public class GameConfig
    {
        public int GameConfigId { get; set; }
        public bool IsActive { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconPath { get; set; }

        [InternalOnly]
        public int? CashCap { get; set; }
        [InternalOnly]
        public int? DailyCashCountCap { get; set; }
        [InternalOnly]
        public float? ScoreToCashFactor { get; set; }
        public int? LeaderboardMaxSpot { get; set; }
        public string GameURL { get; set; }
    }
}
