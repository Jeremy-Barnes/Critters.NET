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
    public class NotificationDomain
    {
        IMessageRepository messageRepo;
        UserDomain userDomain;
        IHubContext<NotificationHub, IUserClient> hubContext;
        public NotificationDomain(IMessageRepository messageRepo, UserDomain userDomain, IHubContext<NotificationHub, IUserClient> hubContext)
        {
            this.messageRepo = messageRepo;
            this.userDomain = userDomain;
            this.hubContext = hubContext;
        }

        public async Task<List<ChannelDetails>> GetMessages(bool unreadOnly, int? lastMessageRetrieved, User activeUser)
        {
            List<Message> messages;
            messages = (await messageRepo.RetrieveMessagesByDate(activeUser.UserId, null, unreadOnly, lastMessageRetrieved ?? Int32.MaxValue)).AsList();

            return await messagesToChannelInfo(messages);
        }

        public async Task<ChannelDetails> RetrieveThread(int lastMessageRetrieved, User activeUser)
        {
            List<Message> messages;
            messages = (await messageRepo.RetrieveReplyThread(activeUser.UserId, lastMessageRetrieved)).AsList();
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

            using (var trans = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                if(!await messageRepo.UserIsChannelMember(message.ChannelId, activeUser.UserId))
                {
                    throw new CritterException($"Could not send that message, recipient does not exist!",
                        $"Invalid channel provided - channel: {message.ChannelId}, sender: {message.SenderUserId}", 
                        System.Net.HttpStatusCode.BadRequest);
                }
                recipientIds = await messageRepo.GetAllChannelMemberIds(message.ChannelId);
            
                message.MessageId = await messageRepo.CreateMessage(message, recipientIds, activeUser.UserId);

                trans.Complete();
            }
            List<User> recipientUsers = userDomain.RetrieveUsers(recipientIds);

            foreach (User user in recipientUsers) //TODO just send to the group
            {
                if (user.IsActive)
                {
                    var clients = hubContext?.Clients.User(user.UserName);
                    clients?.ReceiveNotification(new MessageAlert(new MessageDetails() { Message = message, SenderUsername = activeUser.UserName }));
                }
            }

            return message.MessageId;
        }

        public async Task<IEnumerable<Channel>> FindChannelWithUsers(List<string> userNames, User activeUser)
        {
            List<int> userIds = new List<int>();
            userIds.Add(activeUser.UserId);
            if(userNames != null)
            foreach(var userName in userNames)
            {
                var recipient = userDomain.RetrieveUserByUserName(userName);
                if (recipient == null || !recipient.IsActive)
                {
                    throw new CritterException($"Could not find a group for {userName}!", $"Invalid message recipient provided - {userName}", System.Net.HttpStatusCode.BadRequest);
                }
                userIds.Add(recipient.UserId);
            }

            return await messageRepo.FindChannelWithMembers(userIds, false);
        }

        public async Task<int> CreateChannel(User activeUser, string groupTitle, IEnumerable<string> addUserNames)
        {
            List<int> recipientIds = new List<int>();
            int channelId;
            using (var trans = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                if (addUserNames != null && addUserNames.Count() > 0)
                {
                    foreach (var userName in addUserNames)
                    {
                        var recipient = userDomain.RetrieveUserByUserName(userName);
                        if (recipient == null || !recipient.IsActive)
                        {
                            throw new CritterException($"Could not add {userName} to a group!", $"Invalid message recipient provided - {userName}", System.Net.HttpStatusCode.BadRequest);
                        }
                        recipientIds.Add(recipient.UserId);
                    }
                }
                if (string.IsNullOrEmpty(groupTitle)) groupTitle = string.Join(", ", addUserNames);
                channelId = await messageRepo.CreateChannel(groupTitle);
                recipientIds.Add(activeUser.UserId);
                await messageRepo.AddUsersToChannel(channelId, recipientIds.Distinct());

                trans.Complete();
            }
            return channelId;
        }

        public async Task DeleteMessage(IEnumerable<int> messages, User activeUser)
        {
            if (messages == null || messages.Count() == 0)
            {
                throw new CritterException($"Could not delete messages!",
                    $"Invalid message IDs provided - user ID {activeUser.UserId}",
                    System.Net.HttpStatusCode.BadRequest, LogLevel.Error);
            }

            using (var trans = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                var outp = await messageRepo.UpdateMessageStatus(messages, null, activeUser.UserId);
                trans.Complete();
            }
        }

        public async Task ReadMessage(IEnumerable<int> messages, User activeUser)
        {
            if (messages == null || messages.Count() == 0)
            {
                throw new CritterException($"Could not mark messages as read!",
                    $"Invalid message IDs provided - user ID {activeUser.UserId}",
                    System.Net.HttpStatusCode.BadRequest, LogLevel.Error);
            }

            using (var trans = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                var outp = await messageRepo.UpdateMessageStatus(null, messages, activeUser.UserId);
                trans.Complete();
            }
        }

        public async Task<List<ChannelDetails>> GetChannels(IEnumerable<int> channelIds, User activeUser)
        {
            var allUserChannelIds = await messageRepo.GetChannelsForUser(activeUser.UserId);

            if (channelIds == null || !channelIds.Any()) 
                channelIds = allUserChannelIds.ToList();
            else 
                channelIds = channelIds.Intersect(allUserChannelIds).ToList();
            var channels = await messageRepo.GetChannel(channelIds.ToArray());
            Dictionary<int, IEnumerable<int>> channelIdToUserIds = new Dictionary<int, IEnumerable<int>>();
            foreach (var channel in channels) 
            {
                channelIdToUserIds.Add(channel.ChannelId, await messageRepo.GetAllChannelMemberIds(channel.ChannelId));
            }
            var users = userDomain.RetrieveUsers(channelIdToUserIds.SelectMany(val => val.Value).ToList());//.SelectMany<int>((int v) => v).ToList();
            var channelsDetails = channels.Select(c => {
                var channelUsers = users.Where(u => channelIdToUserIds[c.ChannelId].Contains(u.UserId)).ToList();
                return new ChannelDetails()
                {
                    Channel = c,
                    Users = channelUsers,
                    UserNames = channelUsers.Select(u => u.UserName).ToList()
                }; }).ToList();
            channelsDetails.ForEach(cd => cd.UserNames = cd.Users.Select(u => u.UserName).ToList());
            return channelsDetails;
        }

        private IEnumerable<MessageDetails> messageToMessageDetails(IEnumerable<Message> messages, Dictionary<int, User> userIdToUserMap)
        {
            if(userIdToUserMap == null || !userIdToUserMap.Any())
                userIdToUserMap = userDomain.RetrieveUsers(messages.Select(m => m.SenderUserId)).ToDictionary(u => u.UserId);

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
            
            Dictionary<int, Channel> channelIdMap = (await messageRepo.GetChannel(messagesGroupedByChannel.Select(mgbc => mgbc.Key).ToArray())).ToDictionary(c => c.ChannelId);
            Dictionary<int, User> userIdMap = userDomain.RetrieveUsers(messages.Select(m => m.SenderUserId)).ToDictionary(u => u.UserId);//, u => u.UserName);

            List<ChannelDetails> channelMessages = new List<ChannelDetails>();
            foreach (var channelsMessages in messagesGroupedByChannel)
            {
                channelMessages.Add(new ChannelDetails()
                {
                    Messages = messageToMessageDetails(channelsMessages, userIdMap),
                    Channel = channelIdMap[channelsMessages.Key],
                    UserNames = channelsMessages.Select(m => userIdMap[m.SenderUserId].UserName).Distinct(),
                    Users = channelsMessages.Select(m => userIdMap[m.SenderUserId])
                });
            }
            return channelMessages;
        }


    }//class
}//namespace
