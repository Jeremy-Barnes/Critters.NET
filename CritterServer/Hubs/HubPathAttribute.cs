using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Hubs
{
    public class HubPathAttribute : Attribute
    {
        public string HubPath { get; internal set; }

        public HubPathAttribute(string path)
        {
            HubPath = CorrectPath(path);
        }

        private string CorrectPath(string path)
        {
            if (path.StartsWith('/')) return path;
            return $"/{path}";
        }
    }
}
