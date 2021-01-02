using CritterServer.Contract;
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

        public async Task<int> CreateMessage(Message message, IEnumerable<int> recipientUserIds, int senderUserId)
        {
            int newMessageId = (await dbConnection.QueryAsync<int>("INSERT INTO messages(senderUserID, dateSent, messageText, subject, deleted, parentMessageID, channelID)" +
               "VALUES(@senderUserId, @dateSent, @messageText, @channelNamemessageSubject, @deleted, @parentMessageId, @channelId) RETURNING messageID",
               new
               {
                   senderUserId = senderUserId,
                   read = false,
                   dateSent = DateTime.Now,
                   messageText = message.MessageText,
                   messageSubject = message.Subject,
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

        public async Task<IEnumerable<Message>> RetrieveReplyThread(int userId, int pageDelimiterMessageId)
        {

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
            return output;
        }

        public async Task<IEnumerable<Message>> RetrieveMessagesSinceMessage(int userId, int? channelId, bool unreadOnly, int pageDelimiterMessageId = Int32.MaxValue)
        {
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
            return output;
        }

        public async Task<int> ReadMessages(IEnumerable<int> readMessageIds, int userId)
        {

            int output = await dbConnection.ExecuteAsync(
                $@"UPDATE readReceipts 
                SET read = true
                WHERE messageID = ANY (@readMessageIDs) AND recipientID = @userID",
                new
                {
                    readMessageIDs = readMessageIds.AsList(),
                    userID = userId
                });
            return output;
        }

        public async Task<int> DeleteMessages(IEnumerable<int> deleteMessageIds, int userId)
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

        public async Task<IEnumerable<int>> GetAllChannelMemberIds(int channelId)
        {
            var output = await dbConnection.QueryAsync<int>(
                $@"SELECT memberID from channelUsers
                    WHERE channelID = @channelId",
               new
               {
                   channelId
               });
            return output;
        }

        public async Task<bool> UserIsChannelMember(int channelId, int userId)
        {

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
            int output = (await dbConnection.QueryAsync<int>("INSERT INTO channels(name)" +
               "VALUES(@channelName) RETURNING channelID",
               new
               {
                   channelName
               })).First();
            return output;
        }

        public async Task AddUsersToChannel(int channelId, IEnumerable<int> userIds)
        {
            int rowsUpdated = await dbConnection.ExecuteAsync("INSERT INTO channelUsers(channelID, memberID)" +
               "VALUES(@channelId, @userId)",
               userIds.Distinct().Select(uid =>
               new
               {
                   userId = uid,
                   channelId
               }).ToArray());
        }

        public async Task<IEnumerable<Channel>> FindChannelsWithMembers(IEnumerable<int> userIds, bool exactMatch)
        {
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
            if (channelIDsInt != null && channelIDsInt.Any())
            {
                var channels = await dbConnection.QueryAsync<Channel>(@"
                SELECT * FROM channels where channelID = ANY (@channelIDs)
                ", new { channelIDs = channelIDsInt.AsList() });

                return channels;
            } else
            {
                return new List<Channel>();
            }
        }

        public async Task<IEnumerable<Channel>> GetChannels(params int[] channelIds)
        {
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
        Task<int> CreateMessage(Message message, IEnumerable<int> recipientUserIds, int senderUserId);
        Task<IEnumerable<Message>> RetrieveMessagesSinceMessage(int userId, int? channelId, bool unreadOnly, int pageDelimiterMessageId = Int32.MaxValue);
        Task<IEnumerable<Message>> RetrieveReplyThread(int userId, int pageDelimiterMessageId);
        Task<int> ReadMessages(IEnumerable<int> readMessageIds, int userId);
        Task<int> DeleteMessages(IEnumerable<int> deleteMessageIds, int userId);
        Task<IEnumerable<int>> GetAllChannelMemberIds(int channelId);
        Task<bool> UserIsChannelMember(int channelId, int userId);
        Task<int> CreateChannel(string channelName);
        Task AddUsersToChannel(int channelId, IEnumerable<int> userIds);
        Task<IEnumerable<Channel>> FindChannelsWithMembers(IEnumerable<int> userIds, bool exactMatch);
        Task<IEnumerable<Channel>> GetChannels(params int[] channelId);
        Task<IEnumerable<int>> GetChannelsForUser(int userId);




    }
}
