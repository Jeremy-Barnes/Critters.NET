import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FriendshipDetails, User } from '../dto';
import { UserService } from '../user.service';

@Component({
  selector: 'user',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css'] 
})
export class ProfileComponent implements OnInit {

  user : User = new User();
  
  constructor(private userService : UserService, private route: ActivatedRoute, ) {
      const userName = <string>this.route.snapshot.paramMap.get('userName');
      userService.getUser(userName)
      .subscribe(u => 
          this.user = u
      );
      
   }

   addFriend(friendUserName: string) {
     this.userService.addFriend(friendUserName)
     .subscribe();
   }

  ngOnInit(): void {
  }


}
