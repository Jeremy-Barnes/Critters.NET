using CritterServer.Models;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Domains.Components
{
    public class NpcScriptProvider
    {
        private ConcurrentDictionary<int, NpcScript> loadedNpcs = new ConcurrentDictionary<int, NpcScript>();

        public void AddNpc(Npc npc)
        {
            if (loadedNpcs.ContainsKey(npc.npcID))
                return;
            loadedNpcs.TryAdd(npc.npcID, parseJsonCSharp(npc));
        }

        public Dictionary<string, int> getInteractions(Npc npc, User user, NpcDomain npcDomain)
        {
            NpcScript scriptEng = loadedNpcs.GetOrAdd(npc.npcID, id => parseJsonCSharp(npc));
            return (Dictionary<string, int>)scriptEng.getInteractionOptions(new NpcInputParameters { user = user, npcDomain = npcDomain }).Result;
        }

        public string submitInteraction(Npc npc, User user, int action, NpcDomain npcDomain)
        {
            NpcScript scriptEng = loadedNpcs.GetOrAdd(npc.npcID, id => parseJsonCSharp(npc));
            return (string)scriptEng.submitInteraction(new NpcInputParameters { user = user, npcDomain = npcDomain }).Result;
        }

        private NpcScript parseJsonCSharp(Npc npc)
        {
            JObject methods = JObject.Parse(npc.methodScripts);

            NpcScript npcScript = new NpcScript();
            foreach (var property in typeof(NpcScript).GetProperties())
            {
                try
                {
                    string methodScript = methods[property.Name].ToString();
                    property.SetValue(
                        npcScript, CSharpScript.Create(methodScript,
                        ScriptOptions.Default
                            .WithImports(getImports())
                            .WithReferences(typeof(Serilog.Log).Assembly),
                        typeof(NpcInputParameters)).CreateDelegate());
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{property.Name} failed to load for NPC {npc.npcID} - {npc.name}. " +
                        $"This NPC is badly configured and will not work correctly til this is resolved.");
                    throw;
                }
            }
            return npcScript;
        }

        private List<string> getImports()
        {
            return new List<string>
            {
                "System",
                "System.Collections.Generic",
                "Serilog"
            };
        }

    }

    public class NpcScript
    {
        public ScriptRunner<object> getInteractionOptions { get; set; }
        public ScriptRunner<object> submitInteraction { get; set; }
    }

    public class NpcInputParameters
    {
        public NpcDomain npcDomain;
        public User user;
        public int actionCode;
    }

}
