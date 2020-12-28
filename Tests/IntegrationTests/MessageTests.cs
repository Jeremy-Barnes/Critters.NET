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
    /// Creaated once, reused for all tests in MessageTests
    /// Used to hold durable resources that can be reused (like user info)
    /// </summary>
    public class MessageTestsContext : TestUtilities
    {
       

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

        public MessageTestsContext()
        {
            AUser1 = RandomUserNotPersisted();
            AUser2 = RandomUserNotPersisted();
            BUser1 = RandomUserNotPersisted();
            BUser2 = RandomUserNotPersisted();
            using (var scope = GetScope())
            {
                scope.UserAccountDomain.CreateAccount(AUser1).Wait();
                scope.UserAccountDomain.CreateAccount(AUser2).Wait();
                scope.UserAccountDomain.CreateAccount(BUser1).Wait();
                scope.UserAccountDomain.CreateAccount(BUser2).Wait();
            }
        }

        public MessageTestScope GetScope()
        {
            return new MessageTestScope(GetNewDbConnection(), jwtProvider);
        }


        public List<int> SendMessage(User sender, User receiver, int times, int channel, int? parentId = null)
        {
            List<int> messageIds = new List<int>();

            using (var scope = GetScope())
            {
                for (int i = 0; i < times; i++)
                {
                    messageIds.Add(scope.MessageDomain.SendMessage(new Message
                    {
                        MessageSubject = $"This message created at {DateTime.UtcNow}",
                        MessageText = $"My dearest {receiver.FirstName}, I hope this component test passes and finds you well. Happy {DateTime.UtcNow.DayOfWeek}! -{sender.FirstName}",
                        ChannelId = channel,
                        ParentMessageId = parentId
                    },
                        sender).Result);
                }
            }
            return messageIds;
        }
    }

    public class MessageTestScope : IDisposable
    {

        IDbConnection ScopedDbConn;
        JwtProvider JWTProvider;

        public MessageTestScope(IDbConnection dbc, JwtProvider jwtProvider)
        {
            ScopedDbConn = dbc;
            ScopedDbConn.Open();
            this.JWTProvider = jwtProvider;
            UserRepo = new UserRepository(ScopedDbConn);
            var transactionScopeFactory = new TransactionScopeFactory(ScopedDbConn);
            UserAccountDomain = new UserDomain(UserRepo, JWTProvider, transactionScopeFactory);
            MessageRepo = new MessageRepository(ScopedDbConn);

            MessageDomain = new MessageDomain(MessageRepo, UserAccountDomain, null, transactionScopeFactory);
        }

        public UserDomain UserAccountDomain;
        public IUserRepository UserRepo;
        public MessageDomain MessageDomain;
        public IMessageRepository MessageRepo;

        public void Dispose()
        {
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
            using (var scope = context.GetScope())
            {
                var generatedChannelId = scope.MessageDomain.CreateChannel(context.AUser1, $"{context.GetRandomString(30)}Component", null).Result;
                Assert.IsType<int>(generatedChannelId);

                var channels = scope.MessageDomain.FindChannelWithUsers(null, context.AUser1).Result;
                Assert.Contains(channels, c => c.ChannelId == generatedChannelId);
            }
        }

        [Fact]
        public void SendMessageGetsIntDbId()
        {
            using (var scope = context.GetScope())
            {
                var generatedChannelId = scope.MessageDomain.CreateChannel(context.AUser1, $"{context.GetRandomString(30)}Component", new List<string> { context.AUser2.UserName }).Result;
                var messageIdGenerated = scope.MessageDomain.SendMessage(new Message
                {
                    MessageSubject = $"This message created at {DateTime.UtcNow}",
                    MessageText = $"My dearest {context.AUser2.FirstName}, I hope this component test passes and finds you well. Happy {DateTime.UtcNow.DayOfWeek}! -{context.AUser1.FirstName}",
                    ChannelId = generatedChannelId
                },
                    context.AUser1).Result;
                Assert.IsType<int>(messageIdGenerated);
            }
        }


        [Fact]
        public void SendMessageToNonMemberChannelFails()
        {
            using (var scope = context.GetScope())
            {
                var generatedChannelId = scope.MessageDomain.CreateChannel(context.AUser2, $"{context.GetRandomString(30)}Component", null).Result;

                Assert.Throws<AggregateException>(() => scope.MessageDomain.SendMessage(new Message
                {
                    MessageSubject = $"This message created at {DateTime.UtcNow}",
                    MessageText = $"My dearest {context.AUser2.FirstName}, I hope this component test passes and finds you well. Happy {DateTime.UtcNow.DayOfWeek}! -{context.AUser1.FirstName}",
                },
                    context.AUser1).Wait());
            }
        }

        [Fact]
        public void SendMessageToNonExtantChannelFails()
        {
            using (var scope = context.GetScope())
            {
                Assert.Throws<AggregateException>(() => scope.MessageDomain.SendMessage(new Message
                {
                    MessageSubject = $"This message created at {DateTime.UtcNow}",
                    MessageText = $"{context.GetRandomString(6)} My dearest {context.AUser2.FirstName}, I hope this component test passes and finds you well. Happy {DateTime.UtcNow.DayOfWeek}! -{context.AUser1.FirstName}",
                    ChannelId = -1
                },
                context.AUser1).Wait());
            }
        }

        [Fact]
        public void RetrieveMessagesWorks()
        {
            var receiver = context.RandomUserNotPersisted();
            var sender = context.RandomUserNotPersisted();
            int channelId;
            using (var scope = context.GetScope())
            {
                scope.UserAccountDomain.CreateAccount(receiver).Wait();
                scope.UserAccountDomain.CreateAccount(sender).Wait();

                channelId = scope.MessageDomain.CreateChannel(sender, $"{context.GetRandomString(6)} Component Test Friends", new List<string>() { receiver.UserName }).Result;
            }
            var messageIDs = context.SendMessage(sender, receiver, 30, channelId);

            using (var scope = context.GetScope())
            {
                var conversationChannels = scope.MessageDomain.GetMessages(false, null, receiver).Result;

                Assert.Single(conversationChannels);
                Assert.Equal(messageIDs.Count, conversationChannels.SelectMany(cc => cc.Messages).Count());
            }
            
        }


        [Fact]
        public void RetrieveThreadRetrievesAll()
        {
            var receiver = context.RandomUserNotPersisted();
            var sender = context.RandomUserNotPersisted();
            int channelId;
            using (var scope = context.GetScope())
            {
                scope.UserAccountDomain.CreateAccount(receiver).Wait();
                scope.UserAccountDomain.CreateAccount(sender).Wait();

                channelId = scope.MessageDomain.CreateChannel(sender, $"{context.GetRandomString(6)} Component Test Friends", new List<string>() { receiver.UserName }).Result;
            }
            var messageIDs = context.SendMessage(sender, receiver, 5, channelId);
            messageIDs = messageIDs.Concat(context.SendMessage(receiver, sender, 5, channelId)).ToList();
            messageIDs = messageIDs.Concat(context.SendMessage(sender, receiver, 5, channelId)).ToList();

            List<int> replIds = new List<int>();
            int? parent2 = null;
            for (int i = 0; i < 10; i++)
            {
                if (parent2.HasValue) replIds.Add(parent2.Value);
                var parent1 = context.SendMessage(receiver, sender, 1, channelId, parent2).First();
                replIds.Add(parent1);

                parent2 = context.SendMessage(sender, receiver, 1, channelId, parent1).First();
            }
            replIds.Add(parent2.Value);

            using (var scope = context.GetScope())
            {
                var replyThread = scope.MessageDomain.RetrieveThread(parent2.Value, receiver).Result;

                Assert.Equal(channelId, replyThread.Channel.ChannelId);
                Assert.All(replyThread.Messages, (m) => replIds.Contains(m.Message.MessageId));

                Assert.Equal(replIds.Count, replyThread.Messages.Count());
            }
        }

        [Fact]
        public void GetChannelsGetsAllChannelsForUser()
        {
            var channelOwner = context.RandomUserNotPersisted();
            using (var scope = context.GetScope())
            {
                scope.UserAccountDomain.CreateAccount(channelOwner).Wait();

                var channelIds = new List<int>();
                for (int i = 0; i < 10; i++)
                    channelIds.Add(scope.MessageDomain.CreateChannel(channelOwner, $"{context.GetRandomString(6)} Component Test Channel", new List<string>() { channelOwner.UserName }).Result);

                var channels = scope.MessageDomain.GetChannels(channelIds, channelOwner);
                Assert.Equal(channels.Result.Select(c => c.Channel.ChannelId).ToList(), channelIds);
            }
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
            int channelId;
            using (var scope = context.GetScope())
            {
                scope.UserAccountDomain.CreateAccount(receiver).Wait();

                scope.UserAccountDomain.CreateAccount(sender).Wait();

                channelId = scope.MessageDomain.CreateChannel(sender, $"{context.GetRandomString(6)} Component Test Friends", new List<string>() { receiver.UserName }).Result;
            }


            var originalMessageIds = context.SendMessage(sender, receiver, 30, channelId);
            int reducedBy;
            List<int> disallowedMessageIds;
            List<ChannelDetails> messagesAcrossAllChannels;
            using (var scope = context.GetScope())
            {
                messagesAcrossAllChannels = scope.MessageDomain.GetMessages(false, null, receiver).Result;
                Assert.Single(messagesAcrossAllChannels); //this test is single channel only
                Assert.Equal(originalMessageIds.Count, messagesAcrossAllChannels.SelectMany(cc => cc.Messages).Count());

                reducedBy = 0;
                disallowedMessageIds = new List<int>();
                if (read)
                {
                    reducedBy += 1;
                    scope.MessageDomain.ReadMessages(new List<int> { originalMessageIds[12] }, receiver).Wait();
                    disallowedMessageIds.Add(originalMessageIds[12]);

                }
                if (delete)
                {
                    reducedBy += 1;
                    scope.MessageDomain.DeleteMessages(new List<int> { originalMessageIds[6] }, receiver).Wait();
                    disallowedMessageIds.Add(originalMessageIds[6]);
                }
            }

            using (var scope = context.GetScope())
            {
                messagesAcrossAllChannels = scope.MessageDomain.GetMessages(true, null, receiver).Result;
                Assert.Equal(originalMessageIds.Count - reducedBy, messagesAcrossAllChannels.SelectMany(cc => cc.Messages).Count());

                var mergedConversationsMessageIds = messagesAcrossAllChannels.SelectMany(cc => cc.Messages).Select(m => m.Message.MessageId);

                foreach (int removedMessageId in disallowedMessageIds)
                {
                    Assert.DoesNotContain(removedMessageId, mergedConversationsMessageIds);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RetrievePagingGetsAllMessages(bool unreadOnly)
        {
            var receiver = context.RandomUserNotPersisted();
            var sender = context.RandomUserNotPersisted();
            int channelId;
            using (var scope = context.GetScope())
            {
                scope.UserAccountDomain.CreateAccount(receiver).Wait();
                scope.UserAccountDomain.CreateAccount(sender).Wait();
                channelId = scope.MessageDomain.CreateChannel(sender, "Component Test Friends", new List<string>() { receiver.UserName }).Result;
            }
            var messageIDs = context.SendMessage(sender, receiver, 300, channelId);

            IEnumerable<MessageDetails> mergedConversationsCurrentPage;

            using (var scope = context.GetScope())
            {
                var messagesAcrossAllChannels = scope.MessageDomain.GetMessages(unreadOnly, null, receiver).Result;
                mergedConversationsCurrentPage = messagesAcrossAllChannels.SelectMany(cc => cc.Messages);
                Assert.Equal(100, mergedConversationsCurrentPage.Count()); //we only page out 100 messages at once
            }
            using (var scope = context.GetScope())
            {
                var messagesAcrossAllChannels = scope.MessageDomain.GetMessages(unreadOnly, mergedConversationsCurrentPage.Last().Message.MessageId, receiver).Result;
                var mergedConversationsPreviousPage = mergedConversationsCurrentPage;
                mergedConversationsCurrentPage = messagesAcrossAllChannels.SelectMany(cc => cc.Messages);
                Assert.Equal(100, mergedConversationsCurrentPage.Count()); //we only page out 100 messages at once
                Assert.False(mergedConversationsCurrentPage.Any(m => mergedConversationsPreviousPage.Any(m2 => m2.Message.MessageId == m.Message.MessageId)));
            }
            using (var scope = context.GetScope())
            {
                var messagesAcrossAllChannels = scope.MessageDomain.GetMessages(unreadOnly, mergedConversationsCurrentPage.Last().Message.MessageId, receiver).Result;
                mergedConversationsCurrentPage = messagesAcrossAllChannels.SelectMany(cc => cc.Messages);
                Assert.Equal(100, mergedConversationsCurrentPage.Count()); //we only page out 100 messages at once
            }
            using (var scope = context.GetScope())
            {
                var messagesAcrossAllChannels = scope.MessageDomain.GetMessages(unreadOnly, mergedConversationsCurrentPage.Last().Message.MessageId, receiver).Result;
                var messageBatchLast = messagesAcrossAllChannels.SelectMany(cc => cc.Messages);
                Assert.Empty(messageBatchLast); //300 sent, 300 retrieved, none left
            }
        }
    }
}
