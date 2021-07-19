import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { ChatService } from '../chat.service';
import { ChannelDetails, FriendshipDetails, MessageDetails, User } from '../dto';
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

}
