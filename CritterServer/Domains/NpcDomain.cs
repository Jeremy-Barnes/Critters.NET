using CritterServer.DataAccess;
using CritterServer.Domains.Components;
using CritterServer.Models;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace CritterServer.Domains
{
    public class NpcDomain
    {
        INpcRepository npcRepo;
        NpcScriptProvider npcScriptProvider;

        public NpcDomain(INpcRepository npcRepo, NpcScriptProvider npcScriptProvider)
        {
            this.npcRepo = npcRepo;
            this.npcScriptProvider = npcScriptProvider;
        }

        public object Test()
        {
            return null;
        }

        public int RandomTest()
        {
            return new Random().Next();
        }

    }

}
