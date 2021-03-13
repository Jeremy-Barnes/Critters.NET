import { Component, OnInit } from '@angular/core';
import { UserService } from '../user.service';
import { FormsModule }   from '@angular/forms';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {

    user : any;
    constructor(private userService: UserService) { 
        this.user = this.userService.cookieSignIn();
    }

    signInClicked(){
    }

    ngOnInit(): void {
    }

}
