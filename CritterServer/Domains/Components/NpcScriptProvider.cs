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
            loadedNpcs.TryAdd(npc.npcID, parseJsonIntoNpc(npc));
        }

        public Dictionary<string, int> GetInteractions(Npc npc, User user, NpcDomain npcDomain)
        {
            NpcScript scriptEng = loadedNpcs.GetOrAdd(npc.npcID, id => parseJsonIntoNpc(npc));
            return (Dictionary<string, int>)scriptEng.getInteractionOptions(new NpcInputParameters { user = user, npcDomain = npcDomain }).Result;
        }

        public string SubmitInteraction(Npc npc, User user, int action, NpcDomain npcDomain)
        {
            NpcScript scriptEng = loadedNpcs.GetOrAdd(npc.npcID, id => parseJsonIntoNpc(npc));
            return (string)scriptEng.submitInteraction(new NpcInputParameters { user = user, npcDomain = npcDomain }).Result;
        }

        /// <summary>
        /// Throws an exception if the script is invalid
        /// </summary>
        /// <param name="npcScript">JSON Object containing C# function implementations for all functions defined it NpcScript type</param>
        public void ValidateNpcScript(string npcScript)
        {
            parseJsonIntoCSharp(npcScript, out NpcScript discard); //value doesn't matter, just need this to throw exceptions or not
        }

        private NpcScript parseJsonIntoNpc(Npc npc)
        {
            NpcScript npcScript;
            try
            {
                parseJsonIntoCSharp(npc.methodScripts, out npcScript);
                return npcScript;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"NPCScript failed to load for NPC {npc.npcID} - {npc.name}. " +
                    $"This NPC is badly configured and will not work correctly til this is resolved.");
                return null;
            }
         }

        private void parseJsonIntoCSharp(string npcScript, out NpcScript npcScriptObject)
        {
            JObject methods = JObject.Parse(npcScript);
            npcScriptObject = new NpcScript();

            foreach (var property in typeof(NpcScript).GetProperties())
            {
                try
                {
                    string methodScript = methods[property.Name].ToString();
                    var script = CSharpScript.Create(methodScript,
                        ScriptOptions.Default
                            .WithImports(getImports())
                            .WithReferences(typeof(Serilog.Log).Assembly),
                        typeof(NpcInputParameters));
                    var compiled = script.Compile();
                    if (compiled.Where(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Count() > 0)
                    {
                        string messages = $"{property.Name}'s script was invalid or not provided \n" + 
                            string.Join("\n", compiled
                            .Where(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                            .Select(diagnostic => diagnostic.ToString()));
                        throw new InvalidProgramException(messages);
                    }
                    property.SetValue(npcScriptObject, script.CreateDelegate());
                }
                catch(Exception ex)
                {
                    if (ex is InvalidProgramException) throw;
                    else throw new Exception($"{property.Name}'s script was invalid", ex);
                }
            }
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

    /// <summary>
    /// Serves as the 'interface' for Npc Scripts 
    /// All NPCs must implement these methods in their C# Scripts
    /// When an NPC script is loaded it will be compiled into an object this type
    /// </summary>
    public class NpcScript
    {
        public ScriptRunner<object> getInteractionOptions { get; set; }
        public ScriptRunner<object> submitInteraction { get; set; }
    }

    /// <summary>
    /// The standard way to pass data into an NPCs functions
    /// </summary>
    public class NpcInputParameters
    {
        public NpcDomain npcDomain;
        public User user;
        public int actionCode;
    }

}
