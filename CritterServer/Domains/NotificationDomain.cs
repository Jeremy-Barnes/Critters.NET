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

        public async Task<List<ChannelConversation>> GetMessages(bool unreadOnly, int? lastMessageRetrieved, User activeUser)
        {
            List<Message> messages;
            messages = await messageRepo.RetrieveMessagesByDate(activeUser.UserId, null, unreadOnly, lastMessageRetrieved ?? Int32.MaxValue);


            Dictionary<int, Message> messageMap = new Dictionary<int, Message>();
            messages.ToDictionary(m => m.MessageId);
            //IEnumerable<int> parentIds = messages.Where(m => m.ParentMessageId.HasValue).Select(m => m.ParentMessageId.Value).Distinct();
            //IEnumerable<Message> leafNodes = messages.Where(m => !parentIds.Contains(m.MessageId));
            //IEnumerable<Conversation> conversations = leafNodes.Select(leafNode => constructConversation(leafNode, messageMap));
            List<ChannelConversation> channelMessages = new List<ChannelConversation>();
            var channels = messages.GroupBy(m => m.ChannelId);
            foreach(var channel in channels)
            {
                channelMessages.Add(new ChannelConversation() {
                    Messages = channel.ToList(),
                });
            }
            //now we have a list of unique conversations, but if I reply to the same message twice, a day apart, that should be counted as one conversation.
            //Conversations are trees flattened into lists

            ////if they share a subject, same conversation;
            //IEnumerable<IGrouping<int, Conversation>> mergedConversations = conversations.GroupBy(c => messageMap[c.messageIds[0]].ChannelId.Value);

            //List<Conversation> inbox = new List<Conversation>();
            //foreach(var mergeGroup in mergedConversations)
            //{
            //    Conversation thread = mergeGroup.First();
            //    if(mergeGroup.Count() > 1)
            //    {
            //        thread.messageIds = mergeGroup.SelectMany(c => c.messageIds).OrderBy(id => id).ToList();
            //        thread.userNames = mergeGroup.SelectMany(c => c.userNames).ToList();
            //    }
            //    inbox.Add(thread);

            //}

            return channelMessages;
        }

        //private ChannelConversation constructConversation(Message message, Dictionary<int, Message> messageMap, ChannelConversation conversation = null)
        //{
        //    if(conversation == null)
        //    {
        //        conversation = new ChannelConversation();
        //    }
        //    conversation.messageIds.Add(message.MessageId);
        //    if(message.ParentMessageId != null)
        //    {
        //        constructConversation(messageMap[message.ParentMessageId.Value], messageMap, conversation);
        //    }
        //    return conversation;
        //}

        public async Task<int> SendMessage(Message message, User activeUser)
        {
            List<int> recipientIds = new List<int>();
            message.SenderUserName = activeUser.UserName;

            using (var trans = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                if(!await messageRepo.UserIsChannelMember(message.ChannelId, activeUser.UserId))
                {
                    throw new CritterException($"Could not send that message, recipient does not exist!",
                        $"Invalid channel provided - channel: {message.ChannelId}, sender: {message.SenderUserName}, sketchy userID: {activeUser.UserId}", 
                        System.Net.HttpStatusCode.BadRequest);
                }
                recipientIds = await messageRepo.GetAllChannelMemberIds(message.ChannelId);
            
                message.MessageId = await messageRepo.CreateMessage(message, recipientIds, activeUser.UserId);

                trans.Complete();
            }
            List<User> recipientUsers = userDomain.RetrieveUsers(recipientIds);

            foreach(User user in recipientUsers)
            {
                if (user.IsActive)
                {
                    var clients = hubContext?.Clients.User(user.UserName);
                    clients?.ReceiveNotification(new MessageAlert(message));
                }
            }

            return message.MessageId;
        }

        public async Task<List<Channel>> FindChannelWithUsers(List<string> userNames, User activeUser)
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

            return await messageRepo.FindMembershipChannel(userIds, false);
        }

        public async Task<int> CreateChannel(User activeUser, string groupTitle, List<string> addUserNames)
        {
            List<int> recipientIds = new List<int>();
            int channelId;
            using (var trans = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                if (addUserNames != null && addUserNames.Count > 0)
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
                await messageRepo.AddUsersToChannel(channelId, recipientIds);
            }
            return channelId;
        }

        public async Task DeleteMessage(List<int> messages, User activeUser)
        {
            if (messages == null || messages.Count == 0)
            {
                throw new CritterException($"Could not delete messages!",
                    $"Invalid message IDs provided - user ID {activeUser.UserId}",
                    System.Net.HttpStatusCode.BadRequest, LogLevel.Error);
            }

            using (var trans = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
            {
                var outp = await messageRepo.UpdateMessageStatus(messages/*.Select(m => m.MessageId)*/, null, activeUser.UserId);
                trans.Complete();
            }
        }

        public async Task ReadMessage(List<int> messages, User activeUser)
        {
            if (messages == null || messages.Count == 0)
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


    }//class
}//namespace
