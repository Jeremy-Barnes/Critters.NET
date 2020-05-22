using CritterServer.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CritterServer.DataAccess
{
    public class MessageRepository : IMessageRepository
    {
        IDbConnection dbConnection;

        public MessageRepository(IDbConnection dbConnection)
        {
            this.dbConnection = dbConnection;
        }

        public async Task<int> CreateMessage(Message message, List<int> recipientUserIds, int senderUserId)
        {
            dbConnection.TryOpen();
            int newMessageId = (await dbConnection.QueryAsync<int>("INSERT INTO messages(senderUserID, dateSent, messageText, messageSubject, deleted, parentMessageID, channelID)" +
               "VALUES(@senderUserId, @dateSent, @messageText, @messageSubject, @deleted, @parentMessageId, @channelId) RETURNING messageID",
               new
               {
                   senderUserId = senderUserId,
                   read = false,
                   dateSent = DateTime.Now,
                   messageText = message.MessageText,
                   messageSubject = message.MessageSubject,
                   deleted = false,
                   parentMessageId = message.ParentMessageId,
                   channelId = message.ChannelId
               })).First();

            await dbConnection.ExecuteAsync("INSERT INTO readReceipts(messageID, recipientID) VALUES(@messageId, @recipientId)",
               recipientUserIds.Select(uid => 
               new 
               { 
                   messageId = newMessageId, 
                   recipientId = uid
               }
               ).ToArray());

            return newMessageId;
        }

        public async Task<List<Message>> RetrieveReplyThread(int userId, int pageDelimiterMessageId)
        {
            dbConnection.TryOpen();
            var output = await dbConnection.QueryAsync<Message>(
                $@"WITH RECURSIVE allMessages AS (
                    SELECT * FROM messages rootMessage  
                        WHERE rootMessage.messageID = @rootId
                        AND exists(select 1 from channelUsers cu where cu.channelID = rootMessage.channelid and memberID = @userId)
                    UNION ALL (SELECT children.* FROM messages children 
                    JOIN allMessages 
                        ON children.messageID = allMessages.parentMessageID
                        AND children.messageID < @pageDelimiterMessageId)
		        )
                SELECT *
                FROM allMessages
                WHERE deleted = false
                ORDER BY dateSent desc LIMIT 100",
               new
               {
                   userId,
                   rootId = pageDelimiterMessageId,
                   pageDelimiterMessageId = pageDelimiterMessageId
               });
            return output.ToList();
        }

        public async Task<List<Message>> RetrieveMessagesByDate(int userId, int? channelId, bool unreadOnly, int pageDelimiterMessageId = Int32.MaxValue)
        {
            dbConnection.TryOpen();

            string joinTables = string.Join(" AND ", new string[] { unreadOnly ? "readReceipts rr" : "", channelId.HasValue || !unreadOnly ? "channelUsers cu" : "" }
            .Where(s => s.Length > 0));

            string joinOnCols = string.Join(" AND ", new string[] { 
                unreadOnly ? "rr.recipientID = @recipientId AND rr.read = false AND m.messageID = rr.messageID" : "", 
                channelId.HasValue ? "cu.channelID = @channelId"  : "",
                channelId.HasValue || !unreadOnly ? "cu.memberID = @recipientId and m.channelID = cu.channelID" : "" 
                  }
            .Where(s => s.Length > 0));

            var output = await dbConnection.QueryAsync<Message>(
                $@"SELECT m.* from messages m
                    INNER JOIN {joinTables} ON
                    m.messageID < @pageDelimiterMessageId AND
                    {joinOnCols}
                    AND deleted = false
                    ORDER BY m.dateSent DESC LIMIT 100",
               new
               {
                   recipientId = userId,
                   pageDelimiterMessageId = pageDelimiterMessageId,
                   channelId = channelId
               });
            return output.ToList();
        }

        public async Task<int> UpdateMessageStatus(IEnumerable<int> deleteMessageIds, IEnumerable<int> readMessageIds, int userId)
        {
            dbConnection.TryOpen();
            if (readMessageIds?.Any() ?? false)
            {
                int output = await dbConnection.ExecuteAsync(
                    $@"UPDATE readReceipts 
                    SET read = true
                    WHERE messageID = ANY (@readMessageIDs) AND recipientID = @userID",
                   new
                   {
                       readMessageIDs = readMessageIds.ToList(),
                       userID = userId
                   });
                return output;
            }

            if (deleteMessageIds?.Any() ?? false)
            {
                int output = await dbConnection.ExecuteAsync(
                    $@"UPDATE messages 
                    SET deleted = true
                    WHERE messageID = ANY (@deleteMessageIDs)",
                   new
                   {
                       deleteMessageIDs = deleteMessageIds,
                       userID = userId
                   });
                return output;
            }
            throw new InvalidOperationException("You cannot update an empty list of messages. User " + userId);
        }

        public async Task<List<int>> GetAllChannelMemberIds(int channelId)
        {
            dbConnection.TryOpen();
            var output = await dbConnection.QueryAsync<int>(
                $@"SELECT memberID from channelUsers
                    WHERE channelID = @channelId",
               new
               {
                   channelId
               });
            return output.ToList();
        }

        public async Task<bool> UserIsChannelMember(int channelId, int userId)
        {
            dbConnection.TryOpen();
            var output = await dbConnection.QueryAsync<bool>(
                $@"select exists (select 1 from channelusers where channelID = @channelId and memberID = @userId)",
               new
               {
                   channelId,
                   userId
               });
            return output.FirstOrDefault();
        }

        public async Task<int> CreateChannel(string channelName)
        {
            dbConnection.TryOpen();
            int output = (await dbConnection.QueryAsync<int>("INSERT INTO channels(channelName)" +
               "VALUES(@channelName) RETURNING channelID",
               new
               {
                   channelName
               })).First();
            return output;
        }

        public async Task AddUsersToChannel(int channelId, List<int> userIds)
        {
            dbConnection.TryOpen();
            int rowsUpdated = await dbConnection.ExecuteAsync("INSERT INTO channelUsers(channelID, memberID)" +
               "VALUES(@channelId, @userId)",
               userIds.Distinct().Select(uid =>
               new
               {
                   userId = uid,
                   channelId
               }).ToArray());
            if(rowsUpdated != userIds.Count)
            {
                throw new Exception();
            }
        }

        public async Task<List<Channel>> FindChannelWithMembers(List<int> userIds, bool exactMatch)
        {
            dbConnection.TryOpen();
            await dbConnection.ExecuteAsync(@"
                CREATE TEMP TABLE findChannelMembers(userID int);");
            await dbConnection.ExecuteAsync(@"INSERT INTO findChannelMembers(userID) VALUES
                (@userId)", 
                userIds.Select(uid =>
               new
               {
                   userId = uid,
               }
               ).ToArray());

            var channelIDs = await dbConnection.QueryAsync<int?>($@"
                SELECT cu.channelID FROM channelUsers cu LEFT JOIN findChannelMembers fcu ON cu.memberID = fcu.userID GROUP BY cu.channelID 
                HAVING COUNT(fcu.userID) = (SELECT COUNT(*) FROM findChannelMembers)
                {(exactMatch ? "AND COUNT(cu.memberID) = (SELECT COUNT(*) FROM findChannelMembers);" : "")}
                ");
            var channelIDsInt = channelIDs.Where(c => c.HasValue).Select(c => c.Value);

            var channels = await dbConnection.QueryAsync<Channel>(@"
                SELECT * FROM channels where channelID = ANY (@channelIDs)
                ", new { channelIDs = channelIDsInt.ToList()});

            return channels.ToList();
        }

        public async Task<IEnumerable<Channel>> GetChannel(params int[] channelIds)
        {
            dbConnection.TryOpen();
            var output = await dbConnection.QueryAsync<Channel>(@"
                SELECT * FROM channels
                WHERE channelID = ANY(@channelIds)",
               new
               {
                   channelIds
               });
            return output;
        }

        public async Task<IEnumerable<int>> GetChannelsForUser(int userId)
        {
            dbConnection.TryOpen();
            var output = await dbConnection.QueryAsync<int>("SELECT channelID FROM channelUsers WHERE memberID = @userId",
               new
               {
                   userId = userId
               });
            return output;
        }

    }

    public interface IMessageRepository : IRepository
    {
        Task<int> CreateMessage(Message message, List<int> recipientUserIds, int senderUserId);
        //Task<List<Message>> RetrieveChannelConversation(int channelId, int userId, int? pageDelimiterMessageId);
        Task<List<Message>> RetrieveMessagesByDate(int userId, int? channelId, bool unreadOnly, int pageDelimiterMessageId = Int32.MaxValue);
        Task<List<Message>> RetrieveReplyThread(int userId, int pageDelimiterMessageId);
        Task<int> UpdateMessageStatus(IEnumerable<int> deleteMessageIds, IEnumerable<int> readMessageIds, int userId);

        Task<List<int>> GetAllChannelMemberIds(int channelId);
        Task<bool> UserIsChannelMember(int channelId, int userId);
        Task<int> CreateChannel(string channelName);
        Task AddUsersToChannel(int channelId, List<int> userIds);
        Task<List<Channel>> FindChannelWithMembers(List<int> userIds, bool exactMatch);
        Task<IEnumerable<Channel>> GetChannel(params int[] channelId);
        Task<IEnumerable<int>> GetChannelsForUser(int userId);




    }
}
