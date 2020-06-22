using CritterServer.Contract;
using CritterServer.DataAccess;
using CritterServer.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CritterServer.Domains
{
    public class EventDomain
    {
        IConfigRepository ConfigRepo;
        UserDomain UserDomain;
        IHubContext<NotificationHub, IUserClient> HubContext;
        Random random = new Random();

        public EventDomain(UserDomain userDomain, IHubContext<NotificationHub, IUserClient> hubContext)
        {
            this.UserDomain = userDomain;
            this.HubContext = hubContext;
        }

        public async Task CreateRandomEvent(int? userId, User activeDev)
        {
            User user;
            if (userId.HasValue)
                user = await this.UserDomain.RetrieveUser(userId.Value);
            else
                user = activeDev;//just a dumb testing mechanism til we have a cache of online Users

           
            var d3 = random.Next(0, 3);
            if (d3 == 0)
            {
                await RandomMoneyEvent(user);
            } 
            else if (d3 == 1)
            {
                await RandomHelloEvent(user);
            }
            else if (d3 == 2)
            {
                //
            }

        }

        private async Task RandomMoneyEvent(User user)
        {
            int amount = 0;
            while (amount == 0)
            {
                amount = random.Next(-500, 500);//todo economics
            }

            string message;
            if(amount > 0)
            {
                message = $"An Angel comes by and gives you ${amount}!";
            } 
            else
            {
                amount = (-1) * Math.Min(Math.Abs(amount), user.Cash);
                message = $"A little goblin comes by and takes ${amount} from you!";
            }
            user = await UserDomain.ChangeUserCash(amount, user);
            await SendEvent(message, user);
        }

        private async Task RandomHelloEvent(User user)
        {
            await SendEvent("A friendly pech says hallo!", user);
        }

        private async Task SendEvent(string message, User recipient)
        {
            Notification alert = new Notification(message);
            await HubContext.Clients.User(recipient.UserName).ReceiveNotification(alert);            
    }
}
}
