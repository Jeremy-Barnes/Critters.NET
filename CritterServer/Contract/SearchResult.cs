using CritterServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Contract
{
    public class SearchResult
    {
        public SearchResult()
        {
        }

        public IEnumerable<Pet> Pets { get; set; }
        public IEnumerable<User> Users { get; set; }
    }
}
