using CritterServer.Domains.Components;
using CritterServer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Tests.IntegrationTests;
using Serilog;
using Xunit;

namespace Tests.UnitTests
{
    public class NpcScriptTests
    {
        public NpcScriptProvider scriptEngine = new NpcScriptProvider();

        public Npc CountLongardeaux = new Npc {
            npcID = 12,
            name = "Count Longardeaux",
            description = "A very interesting man, and definitely not a vampire.",
            imagePath = "https://i.imgur.com/MoQgQ7H.jpg",
            methodScripts = @"
            {
                'getInteractionOptions' : '
                    var dict = new Dictionary<string, int> { { ""Say Hello"", 1 }, { ""Try To Set A World Record"", 3 } };
        
                    if(new Random().Next(0,2) == 1) {
                        dict.Add(""Odd"", 9);
                    } else {
                        dict.Add(""Even"", 10);
                    }

                    if(actionCode == 1)
                        dict.Add(""Hi!"", 2);

                    dict.Add(""Random Test"", npcDomain.RandomTest());

                    return dict;
                ',
                'submitInteraction' : '
                    return ""Submitted!"";
                '
            }"
        };


        [Fact]
        public void ScriptProviderCreatesExecutableCode()
        {
            scriptEngine.AddNpc(CountLongardeaux);
            var interactionOptions = scriptEngine.GetInteractions(
                CountLongardeaux, UserTestsContext.RandomUser(), new CritterServer.Domains.NpcDomain(null, scriptEngine));

            Assert.NotEmpty(interactionOptions);
            Assert.True(interactionOptions["Say Hello"] == 1);
            Assert.True(interactionOptions["Random Test"] >= 0);
        }

        [Fact]
        public void ScriptProviderInvalidatesBadScripts()
        {
            //valid C# but missing the getInteraction implementation
            var badScript1 = @" {
                'submitInteraction' : '
                        return ""Submitted!"";
                '
            }";

            //valid C# but invalid JSON (missing comma between getInteractionOptions and submitInteraction)
            var badScript2 = @" {
                'getInteractionOptions' : '
                        return new Dictionary<string, int> { { ""Say Hello"", 1 }, { ""Try To Set A World Record"", 3 } };
                '

                'submitInteraction' : '
                        return ""Submitted!"";
                '
            }";

            //invalid C# (but is valid JSON)
            var badScript3 = @" {
                'getInteractionOptions' : '
                        return new Ditionary<string, int> { { ""Say Hello"", 1 }, { ""Try To Set A World Record"", 3 } };
                ',

                'submitInteraction' : '
                        return ""Submitted!"";
                '
            }";

            Assert.Throws<Exception>(()=>scriptEngine.ValidateNpcScript(badScript1));
            Assert.ThrowsAny<Exception>(() => scriptEngine.ValidateNpcScript(badScript2));
            Assert.Throws<InvalidProgramException>(() => scriptEngine.ValidateNpcScript(badScript3));

        }

        [Fact]
        public void ScriptProviderValidatesGoodScripts()
        {
            scriptEngine.ValidateNpcScript(CountLongardeaux.methodScripts);
            var interactionOptions = scriptEngine.GetInteractions(
                CountLongardeaux, UserTestsContext.RandomUser(), new CritterServer.Domains.NpcDomain(null, scriptEngine));

            Assert.NotEmpty(interactionOptions);
            Assert.True(interactionOptions["Say Hello"] == 1);
            Assert.True(interactionOptions["Random Test"] >= 0);
        }
    }
}
