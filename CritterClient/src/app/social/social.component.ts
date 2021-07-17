import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { FriendshipDetails } from '../dto';
import { UserService } from '../user.service';

@Component({
  selector: 'social-pane',
  templateUrl: './social.component.html',
  styleUrls: ['./social.component.css']
})
export class SocialComponent implements OnInit {

    friends: Observable<FriendshipDetails[]>;
  constructor(private userService: UserService) { }

  ngOnInit(): void {
    this.friends = this.userService.friendSubject.asObservable();
  }

}
