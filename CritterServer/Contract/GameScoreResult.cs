using CritterServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Contract
{
    public class GameScoreResult
    {
        public int CashWon { get; set; }
        public int RemainingSubmissions { get; set; }
    }
}
