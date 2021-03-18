import { Component, OnInit } from '@angular/core';
import { UserService } from '../user.service';
import { FormsModule, NgForm }   from '@angular/forms';
import { AuthResponse } from '../dto';

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

    signInClicked(loginForm: NgForm){
        var auth = this.userService.signIn(loginForm.value.emailAddress, loginForm.value.password);
        this.user = auth.subscribe();
        alert(this.user);
    }

    ngOnInit(): void {
    }

}
