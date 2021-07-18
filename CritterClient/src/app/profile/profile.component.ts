import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { Friendship, FriendshipDetails, User } from '../dto';
import { UserService } from '../user.service';

@Component({
  selector: 'user',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css'] 
})
export class ProfileComponent implements OnInit {

  user : User = new User();  
  friendship : FriendshipDetails;
  
  constructor(private userService : UserService, private route: ActivatedRoute ) {
      const userName = <string>this.route.snapshot.paramMap.get('userName');
      userService.getUser(userName)
      .subscribe(u => {
          this.user = u;
          this.findCurrentFriendship(this.user.UserName);
        }
      );

  }

  addFriend(friendUserName: string): void {
     this.userService.addFriend(friendUserName)
     .subscribe();
  }

  removeFriend(friendUserName: string): void {
     this.userService.removeFriend(friendUserName)
     .subscribe();
  }

  findCurrentFriendship(friendUserName: string): void {
    let friends: FriendshipDetails[] = this.userService.friendSubject.getValue();
    this.friendship = friends.find(fsd => fsd.RequestedUserName == friendUserName || fsd.RequesterUserName == friendUserName);
  }

  ngOnInit(): void {
    
  }


}
