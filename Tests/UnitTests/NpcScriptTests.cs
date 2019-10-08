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
        public NpcScriptProvider scriptEngine = new NpcScriptProvider();
        [Fact]
        public void test1()
        {
            scriptEngine.AddNpc(CountLongardeaux);
            var interactionOptions = scriptEngine.getInteractions(
                CountLongardeaux, UserTestsContext.RandomUser(), new CritterServer.Domains.NpcDomain(null, scriptEngine));

            Assert.NotEmpty(interactionOptions);
            Assert.True(interactionOptions["Say Hello"] == 1);
            Assert.True(interactionOptions["Random Test"] >= 0);

        }

    }
}
