using CritterServer.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Game
{
    public class Player
    {
        public User User { get; set; }
        public string SignalRConnectionId { get; set; }
        public Player(User user, string signalRConnectionId)
        {
            this.User = user;
            this.SignalRConnectionId = signalRConnectionId;
        }

        public Player(User user)
        {
            this.User = user;
        }

    }

    public class PlayerCache
    {
        private ConcurrentDictionary<int, Player> PlayersById { get; set; }
        private ConcurrentDictionary<string, Player> PlayersByUsername { get; set; }

        public IEnumerable<Player> Values { get => PlayersById.Values; }
        public IEnumerable<string> PlayerNames { get => PlayersByUsername.Keys; }

        public PlayerCache()
        {
            PlayersById = new ConcurrentDictionary<int, Player>();
            PlayersByUsername = new ConcurrentDictionary<string, Player>();
        }

        public bool Contains(string userName)
        {
            return PlayersByUsername.ContainsKey(userName);
        }

        public bool Contains(int userId)
        {
            return PlayersById.ContainsKey(userId);

        }

        public Player GetPlayer(string username)
        {
            if (PlayersByUsername.TryGetValue(username, out Player player))
            {
                return player;
            }
            else
            {
                if (!PlayersByUsername.ContainsKey(username))
                {
                    return null;
                }
            }
            return PlayersByUsername[username]; //no more TryGet stuff, just GET IT
        }

        public Player GetPlayer(int userId)
        {
            if (PlayersById.TryGetValue(userId, out Player player))
            {
                return player;
            }
            else
            {
                if (!PlayersById.ContainsKey(userId))
                {
                    return null;
                }
            }
            return PlayersById[userId]; //no more TryGet stuff, just GET IT
        }

        public void AddPlayer(Player player)
        {
            if (!PlayersById.TryAdd(player.User.UserId, player))
            {
                PlayersById[player.User.UserId] = player;
            }
            if (!PlayersByUsername.TryAdd(player.User.UserName, player))
            {
                PlayersByUsername[player.User.UserName] = player;
            }
        }
    }
}
