import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { ChatService } from '../chat.service';
import { ChannelDetails, FriendshipDetails, MessageDetails, User } from '../dto';
import { ActiveChannel } from '../message-popup/message-popup.component';
import { UserService } from '../user.service';

@Component({
  selector: 'social-pane',
  templateUrl: './social.component.html',
  styleUrls: ['./social.component.css']
})
export class SocialComponent implements OnInit {

    friends: Observable<FriendshipDetails[]>;
    user: Observable<User>;
    messages: Observable<MessageDetails[]>;
    channels: Observable<ChannelDetails[]>;
    activeChannels: ActiveChannel[] = [];
    constructor(private userService: UserService, private chatService: ChatService) {

        this.friends = this.userService.friendsListSubject.asObservable();
        this.user = this.userService.activeUserSubject.asObservable();
        this.messages = this.chatService.UnreadMessages.asObservable();
        this.channels = this.chatService.Channels.asObservable();
     }

    ngOnInit(): void {

    }

    fetchMessages(){
        this.chatService.getMessagesPage(false);
    }

    extractFriendName(friend: FriendshipDetails, activeUser: User){
        return friend.RequestedUserName == activeUser.UserName ? friend.RequesterUserName : friend.RequestedUserName;
    }

    getMessagePreview(channel: ChannelDetails): MessagePreview {
        if(channel.Messages.length == 0) {
            return null;
        } else {
            var message = channel.Messages[channel.Messages.length -1];
            var preview = message.Message.MessageText.substr(0, Math.min(40, message.Message.MessageText.length));

            return {
                BodyPreview : preview,
                DateSent : message.Message.DateSent,
                Sender : channel.Users.find(x => x.UserName == message.SenderUsername)
            };
        }
    }

    openChannel(channel: ChannelDetails): void {
        var idx = this.activeChannels.findIndex(ch => ch.channel.Channel.ChannelId == channel.Channel.ChannelId);
        if(idx > -1) {
            //open er up
            this.activeChannels[idx].visible = !this.activeChannels[idx].visible
        } else {
            if(this.activeChannels.length > 5) {
                this.activeChannels.shift;
            }
            this.activeChannels.push(new ActiveChannel(channel));
        }
    }

}
export class MessagePreview {
    public BodyPreview: string;
    public DateSent: Date;
    public Sender: User;
}
