using CritterServer.Models;
using CritterServer.DataAccess;
using CritterServer.Domains;
using CritterServer.Domains.Components;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using Xunit;
using CritterServer.Contract;
using System.Linq;

namespace Tests.IntegrationTests
{
    /// <summary>
    /// Creaated once, reused for all tests in UserTests
    /// Used to hold expensive resources that can be reused (like a DB connection!)
    /// </summary>
    public class MessageTestsContext : TestUtilities
    {
        private static string jwtSecretKey = "T25lIEV4Y2VwdGlvbmFsbHkgTG9uZyBTZWNyZXQgS2V5IFBsZWFzZSEgRm9yIFJlYWwhIEV2ZW4gTG9uZ2VyIFRoYW4gWW91J2QgUmVhc29uYWJseSBBbnRpY2lwYXRl";

        public UserDomain userAccountDomain => new UserDomain(userRepo, jwtProvider);
        public IUserRepository userRepo => new UserRepository(GetNewDbConnection());
        public MessageDomain messageDomain => new MessageDomain(messageRepo, userAccountDomain, null);
        public IMessageRepository messageRepo => new MessageRepository(GetNewDbConnection());

        public User AUser1;
        public User AUser2;

        public User BUser1;
        public User BUser2;

        public JwtProvider jwtProvider = new JwtProvider(
            jwtSecretKey,
            new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSecretKey)),
                ValidIssuer = "critters!",
                ValidateAudience = false,
                ValidateActor = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = true
            });

        public List<User> extantUsers = new List<User>();

        public MessageTestsContext()
        {
            DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
           
            AUser1 = RandomUserNotPersisted();
            AUser2 = RandomUserNotPersisted();
            BUser1 = RandomUserNotPersisted();
            BUser2 = RandomUserNotPersisted();
            userAccountDomain.CreateAccount(AUser1).Wait();
            userAccountDomain.CreateAccount(AUser2).Wait();

            userAccountDomain.CreateAccount(BUser1).Wait();
            userAccountDomain.CreateAccount(BUser2).Wait();
        }


        public List<int> SendMessage(User sender, User receiver, int times, int channel, int? parentId = null)
        {
            List<int> messageIds = new List<int>();
            for (int i = 0; i < times; i++) {
                messageIds.Add(messageDomain.SendMessage(new Message
                {
                    MessageSubject = $"This message created at {DateTime.UtcNow}",
                    MessageText = $"My dearest {receiver.FirstName}, I hope this component test passes and finds you well. Happy {DateTime.UtcNow.DayOfWeek}! -{sender.FirstName}",
                    ChannelId = channel,
                    ParentMessageId = parentId
                },
                    sender).Result);
            }
            return messageIds;
        }
    }

    public class MessageTests: IClassFixture<MessageTestsContext>
    {
        MessageTestsContext context;

        public MessageTests(MessageTestsContext context)
        {
            this.context = context;
        }

        [Fact]
        public void CreateChannelAddsCreator()
        {
            var generatedChannelId = context.messageDomain.CreateChannel(context.AUser1, $"{context.GetRandomString(30)}Component", null).Result;
            Assert.IsType<int>(generatedChannelId);
            var channels = context.messageDomain.FindChannelWithUsers(null, context.AUser1).Result;
            Assert.Contains(channels, c => c.ChannelId == generatedChannelId);
        }

        [Fact]
        public void SendMessageGetsIntDbId()
        {
            var generatedChannelId = context.messageDomain.CreateChannel(context.AUser1, $"{context.GetRandomString(30)}Component", new List<string> { context.AUser2.UserName }).Result;
            var messageIdGenerated = context.messageDomain.SendMessage(new Message { 
                MessageSubject = $"This message created at {DateTime.UtcNow}", 
                MessageText = $"My dearest {context.AUser2.FirstName}, I hope this component test passes and finds you well. Happy {DateTime.UtcNow.DayOfWeek}! -{context.AUser1.FirstName}",
                ChannelId = generatedChannelId
            },
                context.AUser1).Result;
            Assert.IsType<int>(messageIdGenerated);
        }


        [Fact]
        public void SendMessageToNonMemberChannelFails()
        {
            var generatedChannelId = context.messageDomain.CreateChannel(context.AUser2, $"{context.GetRandomString(30)}Component", null).Result;

            Assert.Throws<AggregateException>(() => context.messageDomain.SendMessage(new Message
            {
                MessageSubject = $"This message created at {DateTime.UtcNow}",
                MessageText = $"My dearest {context.AUser2.FirstName}, I hope this component test passes and finds you well. Happy {DateTime.UtcNow.DayOfWeek}! -{context.AUser1.FirstName}",
            },
                context.AUser1).Wait());
        }

        [Fact]
        public void SendMessageToNonExtantChannelFails()
        {
            Assert.Throws<AggregateException>(() => context.messageDomain.SendMessage(new Message
            {
                MessageSubject = $"This message created at {DateTime.UtcNow}",
                MessageText = $"{context.GetRandomString(6)} My dearest {context.AUser2.FirstName}, I hope this component test passes and finds you well. Happy {DateTime.UtcNow.DayOfWeek}! -{context.AUser1.FirstName}",
                ChannelId = -1
            },
                context.AUser1).Wait());
        }

        [Fact]
        public void RetrieveMessagesWorks()
        {
            var receiver = context.RandomUserNotPersisted();
            var sender = context.RandomUserNotPersisted();
            context.userAccountDomain.CreateAccount(receiver).Wait();
            context.userAccountDomain.CreateAccount(sender).Wait();

            var channelId = context.messageDomain.CreateChannel(sender, $"{context.GetRandomString(6)} Component Test Friends", new List<string>() { receiver.UserName }).Result;
            var messageIDs = context.SendMessage(sender, receiver, 30, channelId);

            var conversationChannels = context.messageDomain.GetMessages(false, null, receiver).Result;

            Assert.Single(conversationChannels);
            Assert.Equal(messageIDs.Count, conversationChannels.SelectMany(cc => cc.Messages).Count());
        }


        [Fact]
        public void RetrieveThreadRetrievesAll()
        {
            var receiver = context.RandomUserNotPersisted();
            var sender = context.RandomUserNotPersisted();
            context.userAccountDomain.CreateAccount(receiver).Wait();
            context.userAccountDomain.CreateAccount(sender).Wait();

            var channelId = context.messageDomain.CreateChannel(sender, $"{context.GetRandomString(6)} Component Test Friends", new List<string>() { receiver.UserName }).Result;
            var messageIDs = context.SendMessage(sender, receiver, 5, channelId);
            messageIDs = messageIDs.Concat(context.SendMessage(receiver, sender, 5, channelId)).ToList();
            messageIDs = messageIDs.Concat(context.SendMessage(sender, receiver, 5, channelId)).ToList();

            List<int> replIds = new List<int>();
            int? parent2 = null;
            for (int i = 0; i < 10; i++) {
                if (parent2.HasValue) replIds.Add(parent2.Value);
                var parent1 = context.SendMessage(receiver, sender, 1, channelId, parent2).First();
                replIds.Add(parent1);

                parent2 = context.SendMessage(sender, receiver, 1, channelId, parent1).First();
            }
            replIds.Add(parent2.Value);

            var replyThread = context.messageDomain.RetrieveThread(parent2.Value, receiver).Result;

            Assert.Equal(channelId, replyThread.Channel.ChannelId);
            Assert.All(replyThread.Messages, (m) => replIds.Contains(m.Message.MessageId));

            Assert.Equal(replIds.Count, replyThread.Messages.Count());
        }

        [Fact]
        public void GetChannelsGetsAllChannelsForUser()
        {
            var channelOwner = context.RandomUserNotPersisted();
            context.userAccountDomain.CreateAccount(channelOwner).Wait();

            var channelIds = new List<int>(); 
            for(int i = 0; i < 10; i++)
            channelIds.Add(context.messageDomain.CreateChannel(channelOwner, $"{context.GetRandomString(6)} Component Test Channel", new List<string>() { channelOwner.UserName }).Result);

            var channels = context.messageDomain.GetChannels(channelIds, channelOwner);
            Assert.Equal(channels.Result.Select(c => c.Channel.ChannelId).ToList(), channelIds);
        }


        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public void MessagesStatusChangesMessagesRetrieved(bool delete, bool read)
        {
            var receiver = context.RandomUserNotPersisted();
            var sender = context.RandomUserNotPersisted();
            context.userAccountDomain.CreateAccount(receiver).Wait();
            context.userAccountDomain.CreateAccount(sender).Wait();

            var channelId = context.messageDomain.CreateChannel(sender, $"{context.GetRandomString(6)} Component Test Friends", new List<string>() { receiver.UserName }).Result;
            var originalMessageIds = context.SendMessage(sender, receiver, 30, channelId);

            var messagesAcrossAllChannels = context.messageDomain.GetMessages(false, null, receiver).Result;
            Assert.Single(messagesAcrossAllChannels); //this test is single channel only
            Assert.Equal(originalMessageIds.Count, messagesAcrossAllChannels.SelectMany(cc => cc.Messages).Count());

            int reducedBy = 0;
            List<int> disallowedMessageIds = new List<int>();
            if (read)
            {
                reducedBy += 1;
                context.messageDomain.ReadMessages(new List<int> { originalMessageIds[12] }, receiver).Wait();
                disallowedMessageIds.Add(originalMessageIds[12]);

            }
            if (delete)
            {
                reducedBy += 1; 
                context.messageDomain.DeleteMessages(new List<int> { originalMessageIds[6] }, receiver).Wait();
                disallowedMessageIds.Add(originalMessageIds[6]);
            }

            messagesAcrossAllChannels = context.messageDomain.GetMessages(true, null, receiver).Result;
            Assert.Equal(originalMessageIds.Count - reducedBy, messagesAcrossAllChannels.SelectMany(cc => cc.Messages).Count());

            var mergedConversationsMessageIds = messagesAcrossAllChannels.SelectMany(cc => cc.Messages).Select(m => m.Message.MessageId);

            foreach (int removedMessageId in disallowedMessageIds)
            {
                Assert.DoesNotContain(removedMessageId, mergedConversationsMessageIds);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RetrievePagingGetsAllMessages(bool unreadOnly)
        {
            var receiver = context.RandomUserNotPersisted();
            var sender = context.RandomUserNotPersisted();
            context.userAccountDomain.CreateAccount(receiver).Wait();
            context.userAccountDomain.CreateAccount(sender).Wait();
            var channelId = context.messageDomain.CreateChannel(sender, "Component Test Friends", new List<string>() { receiver.UserName }).Result;
            var messageIDs = context.SendMessage(sender, receiver, 300, channelId);


            var messagesAcrossAllChannels = context.messageDomain.GetMessages(unreadOnly, null, receiver).Result;
            var mergedConversations = messagesAcrossAllChannels.SelectMany(cc => cc.Messages);
            Assert.Equal(100, mergedConversations.Count()); //we only page out 100 messages at once

            messagesAcrossAllChannels = context.messageDomain.GetMessages(unreadOnly, mergedConversations.Last().Message.MessageId, receiver).Result;
            var mergedConversations2 = messagesAcrossAllChannels.SelectMany(cc => cc.Messages);
            Assert.Equal(100, mergedConversations2.Count()); //we only page out 100 messages at once

            Assert.False(mergedConversations2.Any(m => mergedConversations.Any(m2 => m2.Message.MessageId == m.Message.MessageId)));

            messagesAcrossAllChannels = context.messageDomain.GetMessages(unreadOnly, mergedConversations2.Last().Message.MessageId, receiver).Result;
            var mergedConversations3 = messagesAcrossAllChannels.SelectMany(cc => cc.Messages);
            Assert.Equal(100, mergedConversations3.Count()); //we only page out 100 messages at once

            messagesAcrossAllChannels = context.messageDomain.GetMessages(unreadOnly, mergedConversations3.Last().Message.MessageId, receiver).Result;
            var messageBatchLast = messagesAcrossAllChannels.SelectMany(cc => cc.Messages);
            Assert.Empty(messageBatchLast); //300 sent, 300 retrieved, none left
        }
    }
}
