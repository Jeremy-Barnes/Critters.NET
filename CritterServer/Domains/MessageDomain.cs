using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Transactions;
using CritterServer.Contract;
using CritterServer.DataAccess;
using CritterServer.Domains.Components;
using CritterServer.Models;
using Dapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
namespace CritterServer.Domains
{
    public class MessageDomain
    {
        IMessageRepository MessageRepo;
        UserDomain UserDomain;
        IHubContext<NotificationHub, IUserClient> SignalRHubContext; 
        ITransactionScopeFactory TransactionScopeFactory;

        public MessageDomain(IMessageRepository messageRepo, UserDomain userDomain, 
            IHubContext<NotificationHub, IUserClient> hubContext, ITransactionScopeFactory transactionScopeFactory)
        {
            MessageRepo = messageRepo;
            UserDomain = userDomain;
            SignalRHubContext = hubContext;
            TransactionScopeFactory = transactionScopeFactory;
        }

        public async Task<List<ChannelDetails>> GetMessages(bool unreadOnly, int? lastMessageRetrieved, User activeUser)
        {
            List<Message> messages;
            messages = (await MessageRepo.RetrieveMessagesSinceMessage(activeUser.UserId, null, unreadOnly, lastMessageRetrieved ?? Int32.MaxValue)).AsList();

            return await messagesToChannelInfo(messages);
        }

        public async Task<ChannelDetails> RetrieveThread(int lastMessageRetrieved, User activeUser)
        {
            List<Message> messages;
            messages = (await MessageRepo.RetrieveReplyThread(activeUser.UserId, lastMessageRetrieved)).AsList();
            if(messages.Count == 0)
            {
                return null;
            }
            return (await messagesToChannelInfo(messages)).First();
        }

        public async Task<int> SendMessage(Message message, User activeUser)
        {
            IEnumerable<int> recipientIds = new List<int>();
            message.SenderUserId = activeUser.UserId;

            using (var trans = TransactionScopeFactory.Create())
            {
                if (!await MessageRepo.UserIsChannelMember(message.ChannelId, activeUser.UserId))
                {
                    throw new CritterException($"Could not send that message, recipient does not exist!",
                        $"Invalid channel provided - channel: {message.ChannelId}, sender: {message.SenderUserId}", 
                        System.Net.HttpStatusCode.BadRequest);
                }
                recipientIds = await MessageRepo.GetAllChannelMemberIds(message.ChannelId);
                
                message.MessageId = await MessageRepo.CreateMessage(message, recipientIds.Where(id => id != activeUser.UserId), activeUser.UserId);

                trans.Complete();
            }

            SignalRHubContext?.Clients?.GroupExcept(NotificationHub.GetChannelGroupIdentifier(message.ChannelId), activeUser.UserName)
                ?.ReceiveNotification(new NewMessageAlert(new MessageDetails() { Message = message, SenderUsername = activeUser.UserName }));
            
            return message.MessageId;
        }

        public async Task<IEnumerable<Channel>> FindChannelWithUsers(List<string> userNames, User activeUser)
        {
            List<int> userIds = new List<int>();
            userIds.Add(activeUser.UserId);
            if (userNames != null)
            {
                var recipients = (await UserDomain.RetrieveUsersByUserName(userNames)).ToDictionary(u => u.UserName);

                foreach (var userName in userNames)
                {
                    if (!recipients.ContainsKey(userName) || !recipients[userName].IsActive)
                    {
                        throw new CritterException($"Could not find a group for {userName}!", $"Invalid message recipient provided - {userName}", System.Net.HttpStatusCode.BadRequest);
                    }
                    userIds.Add(recipients[userName].UserId);
                }
            }

            return await MessageRepo.FindChannelsWithMembers(userIds, false);
        }

        public async Task<int> CreateChannel(User activeUser, string groupTitle, IEnumerable<string> addUserNames)
        {
            List<int> recipientIds = new List<int>();
            int channelId;
            using (var trans = TransactionScopeFactory.Create())
            {
                if (addUserNames != null && addUserNames.Count() > 0)
                {
                    var recipients = (await UserDomain.RetrieveUsersByUserName(addUserNames)).ToDictionary(u => u.UserName);
                    foreach (var userName in addUserNames)
                    {
                        if (!recipients.ContainsKey(userName) || !recipients[userName].IsActive)
                        {
                            throw new CritterException($"Could not add {userName} to a group!", $"Invalid message recipient provided - {userName}", System.Net.HttpStatusCode.BadRequest);
                        }
                        recipientIds.Add(recipients[userName].UserId);
                    }
                }
                if (string.IsNullOrEmpty(groupTitle)) groupTitle = string.Join(", ", addUserNames);
                channelId = await MessageRepo.CreateChannel(groupTitle);
                recipientIds.Add(activeUser.UserId);
                await MessageRepo.AddUsersToChannel(channelId, recipientIds);

                trans.Complete();
            }
            return channelId;
        }

        public async Task DeleteMessages(IEnumerable<int> messages, User activeUser)
        {
            if (messages == null || messages.Count() == 0)
            {
                throw new CritterException($"Could not delete messages!",
                    $"Invalid message IDs provided - user ID {activeUser.UserId}",
                    System.Net.HttpStatusCode.BadRequest, LogLevel.Error);
            }

            using (var trans = TransactionScopeFactory.Create())
            {
                var outp = await MessageRepo.DeleteMessages(messages, activeUser.UserId);
                trans.Complete();
            }
        }

        public async Task ReadMessages(IEnumerable<int> messages, User activeUser)
        {
            if (messages == null || messages.Count() == 0)
            {
                throw new CritterException($"Could not mark messages as read!",
                    $"Invalid message IDs provided - user ID {activeUser.UserId}",
                    System.Net.HttpStatusCode.BadRequest, LogLevel.Error);
            }

            using (var trans = TransactionScopeFactory.Create())
            {
                var outp = await MessageRepo.ReadMessages(messages, activeUser.UserId);
                trans.Complete();
            }
        }

        public async Task<IEnumerable<ChannelDetails>> GetChannels(IEnumerable<int> channelIds, User activeUser)
        {
            var allUserChannelIds = await MessageRepo.GetChannelsForUser(activeUser.UserId);

            if (channelIds == null || !channelIds.Any()) 
                channelIds = allUserChannelIds;
            else 
                channelIds = channelIds.Intersect(allUserChannelIds);
            var channels = await MessageRepo.GetChannels(channelIds.ToArray());
            Dictionary<int, IEnumerable<int>> channelIdToUserIds = new Dictionary<int, IEnumerable<int>>();
            foreach (var channel in channels) 
            {
                channelIdToUserIds.Add(channel.ChannelId, await MessageRepo.GetAllChannelMemberIds(channel.ChannelId));
            }
            var users = await UserDomain.RetrieveUsers(channelIdToUserIds.SelectMany(val => val.Value));
            var channelsDetails = channels.Select(c => {
                var channelUsers = users.Where(u => channelIdToUserIds[c.ChannelId].Contains(u.UserId));
                return new ChannelDetails()
                {
                    Channel = c,
                    Users = channelUsers,
                    UserNames = channelUsers.Select(u => u.UserName)
                }; });
            return channelsDetails;
        }

        private async Task<IEnumerable<MessageDetails>> messageToMessageDetails(IEnumerable<Message> messages, Dictionary<int, User> userIdToUserMap)
        {
            if(userIdToUserMap == null || !userIdToUserMap.Any())
                userIdToUserMap = (await UserDomain.RetrieveUsers(messages.Select(m => m.SenderUserId))).ToDictionary(u => u.UserId);

            var messageDetails = messages.Select(m => new MessageDetails()
            {
                Message = m,
                SenderUsername = userIdToUserMap[m.SenderUserId].UserName
            });
            return messageDetails;
        }

        private async Task<List<ChannelDetails>> messagesToChannelInfo(IEnumerable<Message> messages)
        {
            IEnumerable<IGrouping<int, Message>> messagesGroupedByChannel = messages.GroupBy(m => m.ChannelId);
            
            Dictionary<int, Channel> channelIdMap = (await MessageRepo.GetChannels(messagesGroupedByChannel.Select(mgbc => mgbc.Key).ToArray())).ToDictionary(c => c.ChannelId);
            Dictionary<int, User> userIdMap = (await UserDomain.RetrieveUsers(messages.Select(m => m.SenderUserId))).ToDictionary(u => u.UserId);

            List<ChannelDetails> channelMessages = new List<ChannelDetails>();
            foreach (var channelsMessages in messagesGroupedByChannel)
            {
                channelMessages.Add(new ChannelDetails()
                {
                    Messages = await messageToMessageDetails(channelsMessages, userIdMap),
                    Channel = channelIdMap[channelsMessages.Key],
                    UserNames = channelsMessages.Select(m => userIdMap[m.SenderUserId].UserName).Distinct(),
                    Users = channelsMessages.Select(m => userIdMap[m.SenderUserId])
                });
            }
            return channelMessages;
        }


    }//class
}//namespace
