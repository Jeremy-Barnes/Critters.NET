import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { FriendshipDetails, User } from '../dto';
import { UserService } from '../user.service';

@Component({
  selector: 'social-pane',
  templateUrl: './social.component.html',
  styleUrls: ['./social.component.css']
})
export class SocialComponent implements OnInit {

    friends: Observable<FriendshipDetails[]>;
    user: Observable<User>;
    constructor(private userService: UserService) { }

    ngOnInit(): void {
        this.friends = this.userService.friendSubject.asObservable();
        this.user = this.userService.userSubject.asObservable();
    }

    extractFriendName(friend: FriendshipDetails, activeUser: User){
        return friend.RequestedUserName == activeUser.UserName ? friend.RequesterUserName : friend.RequestedUserName;
    }

}
